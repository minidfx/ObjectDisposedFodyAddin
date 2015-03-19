namespace ObjectDisposedFodyAddin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class ModuleWeaver
    {
        private MethodReference objectDisposedExceptionReference;

        private Lazy<IEnumerable<TypeDefinition>> types;

        private TypeSystem typeSystem;

        /// <summary>
        ///     Initialize an instance of <see cref="ModuleWeaver" /> class.
        /// </summary>
        public ModuleWeaver()
        {
            this.LogDebug = Console.WriteLine;
            this.LogInfo = Console.WriteLine;
            this.LogWarning = Console.WriteLine;
        }

        /// <summary>
        ///     Logger for notifying MSBuild about a debug message.
        /// </summary>
        public Action<string> LogDebug { get; set; }

        /// <summary>
        ///     Logger for notifying MSBuild about an important message.
        /// </summary>
        public Action<string> LogInfo { get; set; }

        /// <summary>
        ///     Logger for notifying MSBuild about a warning message.
        /// </summary>
        public Action<string> LogWarning { get; set; }

        /// <summary>
        ///     An instance of <see cref="IAssemblyResolver" /> for resolving assembly references.
        /// </summary>
        public IAssemblyResolver AssemblyResolver { get; set; }

        /// <summary>
        ///     Contain the full path of the target assembly.
        /// </summary>
        public string AssemblyFilePath { get; set; }

        /// <summary>
        ///     An instance of Mono.Cecil.ModuleDefinition for processing.
        /// </summary>
        public ModuleDefinition ModuleDefinition { get; set; }

        /// <summary>
        ///     Will be executed at the end of the compile time.
        /// </summary>
        public void Execute()
        {
            this.LogDebug("Entry into ObjectDisposedFodyAddin execute method.");

            var msCoreLibDefinition = this.ModuleDefinition.AssemblyResolver.Resolve("mscorlib");
            var msCoreTypes = msCoreLibDefinition.MainModule.Types;

            this.typeSystem = this.ModuleDefinition.TypeSystem;
            this.types = new Lazy<IEnumerable<TypeDefinition>>(() => this.ModuleDefinition.GetTypes().Where(x => x.IsClass &&
                                                                                                                 !x.IsAbstract &&
                                                                                                                 !x.IsInterface &&
                                                                                                                 x.Interfaces.Any(i => i.Name == "IDisposable" || i.Name == "IAsyncDisposable") &&
                                                                                                                 !IsGeneratedCode(x) &&
                                                                                                                 !MustSkipDisposeCheck(x.CustomAttributes)));
            var objectDisposedExceptionConstructor = msCoreTypes.First(x => x.Name == "ObjectDisposedException")
                                                                .Methods.First(x => x.Name == ".ctor" &&
                                                                                    x.Parameters.Any(p => p.ParameterType.Name == "String"));
            this.objectDisposedExceptionReference = this.ModuleDefinition.Import(objectDisposedExceptionConstructor);

            this.AddIsDisposedPrivateMember();
            this.AddSetToDisposedIntructionsIntoDisposeMethods();
            this.AddGuardInstructionsIntoDisposeMethods();

            this.LogDebug("ObjectDisposedFodyAddin executed successfully.");
        }

        private void AddSetToDisposedIntructionsIntoDisposeMethods()
        {
            foreach (var type in this.types.Value)
            {
                var methods = type.Methods
                                  .Where(x => !x.IsStatic &&
                                              x.Name.Equals("Dispose") ||
                                              x.Name.Equals("DisposeAsync"));

                var disposeField = type.Fields.Single(x => x.Name == "isDisposed");

                foreach (var method in methods)
                {
                    var ilProcessor = method.Body.GetILProcessor();
                    var firstInstruction = method.Body.Instructions.FirstOrDefault();

                    var newInstructions = GetSetIsDisposedInstructions(ilProcessor, disposeField);
                    foreach (var instruction in newInstructions)
                    {
                        ilProcessor.InsertBefore(firstInstruction, instruction);
                    }
                }
            }
        }

        /// <summary>
        ///     Will be called when a request to cancel the build occurs.
        /// </summary>
        public void Cancel()
        {
            this.LogDebug("ObjectDisposedFodyAddin is canceled while the execution.");
        }

        /// <summary>
        ///     Will be called after all weaving has occurred and the module has been saved.
        /// </summary>
        public void AfterWeaving()
        {
            this.LogDebug("ObjectDisposedFodyAddin execute some post operations.");
        }

        private void AddIsDisposedPrivateMember()
        {
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var type in this.types.Value)
            {
                if (!type.Fields.Any(x => x.Name.Equals("isDisposed")))
                {
                    type.Fields.Add(new FieldDefinition("isDisposed", FieldAttributes.Private, this.typeSystem.Boolean));
                }
            }
        }

        private static bool MustSkipDisposeCheck(IEnumerable<CustomAttribute> customAttributes)
        {
            return customAttributes.Any(x => x.AttributeType.FullName == "ObjectDisposedFodyAddin.SkipDisposeCheck");
        }

        private static bool IsGeneratedCode(ICustomAttributeProvider typeDefinition)
        {
            return typeDefinition.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute" || a.AttributeType.Name == "GeneratedCodeAttribute");
        }

        private void AddGuardInstructionsIntoDisposeMethods()
        {
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var type in this.types.Value)
            {
                var methods = type.Methods
                                  .Where(x => !x.IsStatic &&
                                              !x.Name.Equals("Dispose") &&
                                              !x.Name.Equals("DisposeAsync") &&
                                              !x.Name.Equals(".ctor"));

                var disposeField = type.Fields.Single(x => x.Name == "isDisposed");

                foreach (var method in methods)
                {
                    var ilProcessor = method.Body.GetILProcessor();
                    var firstInstruction = method.Body.Instructions.FirstOrDefault();

                    var newInstructions = this.GetGuardInstructions(ilProcessor, method, disposeField);
                    foreach (var instruction in newInstructions)
                    {
                        ilProcessor.InsertBefore(firstInstruction, instruction);
                    }
                }
            }
        }

        private static IEnumerable<Instruction> GetSetIsDisposedInstructions(ILProcessor ilProcessor,
                                                                             FieldReference disposeFieldDefinition)
        {
            yield return ilProcessor.Create(OpCodes.Ldarg_0);
            yield return ilProcessor.Create(OpCodes.Ldc_I4_1);
            yield return ilProcessor.Create(OpCodes.Stfld, disposeFieldDefinition);
        }

        private IEnumerable<Instruction> GetGuardInstructions(ILProcessor ilProcessor,
                                                              MemberReference memberReference,
                                                              FieldReference disposeFieldDefinition)
        {
            var normalWay = ilProcessor.Body.Instructions.FirstOrDefault() ?? ilProcessor.Create(OpCodes.Ret);

            yield return ilProcessor.Create(OpCodes.Ldarg_0);
            yield return ilProcessor.Create(OpCodes.Ldfld, disposeFieldDefinition);
            yield return ilProcessor.Create(OpCodes.Brfalse_S, normalWay);

            yield return ilProcessor.Create(OpCodes.Ldstr, memberReference.Name);
            yield return ilProcessor.Create(OpCodes.Newobj, this.objectDisposedExceptionReference);
            yield return ilProcessor.Create(OpCodes.Throw);
        }
    }
}