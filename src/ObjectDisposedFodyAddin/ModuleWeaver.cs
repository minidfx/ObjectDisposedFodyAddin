namespace ObjectDisposedFodyAddin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class ModuleWeaver
    {
        private readonly FieldDefinition isDisposedField;

        private readonly MethodReference objectDisposedExceptionReference;

        private readonly MethodReference taskFromResultStaticMethodReference;

        private readonly Lazy<IEnumerable<TypeDefinition>> types;

        private readonly TypeSystem typeSystem;

        /// <summary>
        ///     Initialize an instance of <see cref="ModuleWeaver" /> class.
        /// </summary>
        public ModuleWeaver()
        {
            this.LogDebug = s => { };
            this.LogInfo = s => { };

            var msCoreLibDefinition = this.ModuleDefinition.AssemblyResolver.Resolve("mscorlib");
            var msCoreTypes = msCoreLibDefinition.MainModule.Types;

            this.typeSystem = this.ModuleDefinition.TypeSystem;
            this.types = new Lazy<IEnumerable<TypeDefinition>>(() => this.ModuleDefinition.GetTypes().Where(x => x.IsClass &&
                                                                                                                 !x.IsAbstract &&
                                                                                                                 !x.IsInterface &&
                                                                                                                 !IsGeneratedCode(x) &&
                                                                                                                 !MustSkipDisposeCheck(x.CustomAttributes)));
            this.isDisposedField = new FieldDefinition("isDisposed", FieldAttributes.Private, this.typeSystem.Boolean);
            var objectDisposedExceptionConstructor = msCoreTypes.First(x => x.Name == "ObjectDisposedException")
                                                                .Methods.First(x => x.Name == ".ctor" &&
                                                                                    x.Parameters.Any(p => p.Name == "String"));
            this.objectDisposedExceptionReference = this.ModuleDefinition.Import(objectDisposedExceptionConstructor);
            var taskFromResultStaticMethod = msCoreTypes.First(x => x.Name == "Task")
                                                        .Methods.First(x => x.IsStatic && x.Name == "FromResult");
            this.taskFromResultStaticMethodReference = this.ModuleDefinition.Import(taskFromResultStaticMethod);
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

            this.AddIsDisposedPrivateMember();
            this.AddCheckIfObjectIsDisposed();

            this.LogDebug("ObjectDisposedFodyAddin executed successfully.");
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
                    type.Fields.Add(this.isDisposedField);
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

        private void AddCheckIfObjectIsDisposed()
        {
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var type in this.types.Value)
            {
                var methods = type.Methods
                                  .Where(x => !x.IsStatic &&
                                              (x.Name.EndsWith("Dispose") || x.Name.EndsWith("DisposeAsync")));

                if (!methods.Any())
                {
                    var implementIDisposableInterface = type.Interfaces.Any(x => x.Name.Equals("IDiposable"));
                    var implementIAsyncDisposable = type.Interfaces.Any(x => x.Name.Equals("IAsyncDisposable"));

                    if (implementIAsyncDisposable)
                    {
                        var newDisposeMethod = new MethodDefinition("DisposeAsync", MethodAttributes.HideBySig | MethodAttributes.Public, this.typeSystem.TypedReference);
                        foreach (var instruction in this.GetCheckIsDisposedInstructionsAsync(newDisposeMethod))
                        {
                            newDisposeMethod.Body.Instructions.Add(instruction);
                        }
                    }

                    if (implementIDisposableInterface)
                    {
                        var newDisposeMethod = new MethodDefinition("Dispose", MethodAttributes.HideBySig | MethodAttributes.Public, this.typeSystem.Void);
                        foreach (var instruction in this.GetCheckIsDisposedInstructions(newDisposeMethod))
                        {
                            newDisposeMethod.Body.Instructions.Add(instruction);
                        }
                    }
                }
            }
        }

        private IEnumerable<Instruction> GetCheckIsDisposedInstructionsAsync(MemberReference typeDefinition)
        {
            var returnInstruction = Instruction.Create(OpCodes.Ret);

            var taskInstructions = new[]
                                       {
                                           Instruction.Create(OpCodes.Ldstr, 0),
                                           Instruction.Create(OpCodes.Newobj, this.taskFromResultStaticMethodReference),
                                           returnInstruction
                                       };

            yield return Instruction.Create(OpCodes.Ldarg_0);
            yield return Instruction.Create(OpCodes.Ldfld, this.isDisposedField);
            yield return Instruction.Create(OpCodes.Brfalse, taskInstructions);

            yield return Instruction.Create(OpCodes.Ldstr, typeDefinition.Name);
            yield return Instruction.Create(OpCodes.Newobj, this.objectDisposedExceptionReference);
            yield return Instruction.Create(OpCodes.Throw);

            yield return returnInstruction;
        }

        private IEnumerable<Instruction> GetCheckIsDisposedInstructions(MemberReference typeDefinition)
        {
            var returnInstruction = Instruction.Create(OpCodes.Ret);

            yield return Instruction.Create(OpCodes.Ldarg_0);
            yield return Instruction.Create(OpCodes.Ldfld, this.isDisposedField);
            yield return Instruction.Create(OpCodes.Brfalse, returnInstruction);

            yield return Instruction.Create(OpCodes.Ldstr, typeDefinition.Name);
            yield return Instruction.Create(OpCodes.Newobj, this.objectDisposedExceptionReference);
            yield return Instruction.Create(OpCodes.Throw);

            yield return returnInstruction;
        }
    }
}