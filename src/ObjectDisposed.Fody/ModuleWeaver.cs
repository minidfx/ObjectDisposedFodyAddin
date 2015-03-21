namespace ObjectDisposedFodyAddin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mono.Cecil;
    using Mono.Cecil.Rocks;

    public sealed class ModuleWeaver
    {
        /// <summary>
        ///     The constructor reference with string as argument of the <see cref="ObjectDisposedException" /> class.
        /// </summary>
        private MethodReference objectDisposedExceptionReference;

        /// <summary>
        ///     Filtered <see cref="Type" />s found in the <see cref="System.Reflection.Assembly" />.
        /// </summary>
        private Lazy<IEnumerable<TypeDefinition>> types;

        /// <summary>
        ///     System <see cref="Type" />s loaded from the mscorlib.
        /// </summary>
        private TypeSystem typeSystem;

        /// <summary>
        ///     Initialize an instance of <see cref="ModuleWeaver" /> class.
        /// </summary>
        public ModuleWeaver()
        {
            this.LogDebug = s => { };
            this.LogInfo = s => { };
            this.LogWarning = s => { };
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
            this.LogDebug("Entry into ObjectDisposedFodyAddin Execute method.");

            var msCoreLibDefinition = this.ModuleDefinition.AssemblyResolver.Resolve("mscorlib");
            var msCoreTypes = msCoreLibDefinition.MainModule.Types;

            this.typeSystem = this.ModuleDefinition.TypeSystem;
            this.types = new Lazy<IEnumerable<TypeDefinition>>(() => this.ModuleDefinition.GetTypes().Where(x => x.IsClass &&
                                                                                                                 !x.IsAbstract &&
                                                                                                                 !x.IsInterface &&
                                                                                                                 x.Interfaces.Any(i => i.FullName == "System.IDisposable" || i.Name == "IAsyncDisposable") &&
                                                                                                                 !x.IsGeneratedCode() &&
                                                                                                                 !x.SkipDisposeGuard() &&
                                                                                                                 x.HasDisposeMethod()));
            var objectDisposedExceptionConstructor = msCoreTypes.First(x => x.Name == "ObjectDisposedException")
                                                                .Methods.First(x => x.Name == ".ctor" &&
                                                                                    x.Parameters.Any(p => p.ParameterType.Name == "String"));
            this.objectDisposedExceptionReference = this.ModuleDefinition.Import(objectDisposedExceptionConstructor);

            this.CheckInterfaces();

            this.AddIsDisposedPrivateMember();
            this.AddSetToDisposedIntructionsIntoDisposeMethods();
            this.AddGuardInstructionsIntoDisposeMethods();

            this.LogDebug("Execute method executed successfully.");
        }

        private void CheckInterfaces()
        {
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var typeDefinition in this.types.Value)
            {
                if (typeDefinition.Interfaces.Any(x => x.FullName == "System.IDisposable") && typeDefinition.Interfaces.Any(x => x.Name == "IAsyncDisposable"))
                {
                    throw new WeavinException(string.Format("The type {0} cannot have both interface {1} and {2}", typeDefinition.Name, "IDisposable", "IAsyncDisposable"));
                }
            }
        }

        private void AddSetToDisposedIntructionsIntoDisposeMethods()
        {
            this.LogDebug("Entry into ObjectDisposedFodyAddin AddSetToDisposedIntructionsIntoDisposeMethods method");

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

                    var newInstructions = Instructions.GetSetIsDisposedInstructions(ilProcessor, disposeField);
                    foreach (var instruction in newInstructions)
                    {
                        ilProcessor.InsertBefore(firstInstruction, instruction);
                    }

                    method.Body.OptimizeMacros();
                }
            }

            this.LogDebug("AddSetToDisposedIntructionsIntoDisposeMethods method executed successfully.");
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
            this.LogDebug("Entry into ObjectDisposedFodyAddin AddIsDisposedPrivateMember method");

            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var type in this.types.Value)
            {
                if (!type.Fields.Any(x => x.Name.Equals("isDisposed")))
                {
                    type.Fields.Add(new FieldDefinition("isDisposed", FieldAttributes.Private, this.typeSystem.Boolean));
                }
            }

            this.LogDebug("AddIsDisposedPrivateMember method executed successfully.");
        }

        private void AddGuardInstructionsIntoDisposeMethods()
        {
            this.LogDebug("Entry into ObjectDisposedFodyAddin AddGuardInstructionsIntoDisposeMethods method");

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

                    var newInstructions = Instructions.GetGuardInstructions(ilProcessor,
                                                                            type,
                                                                            disposeField,
                                                                            this.objectDisposedExceptionReference);
                    foreach (var instruction in newInstructions)
                    {
                        ilProcessor.InsertBefore(firstInstruction, instruction);
                    }

                    method.Body.OptimizeMacros();
                }
            }

            this.LogDebug("AddGuardInstructionsIntoDisposeMethods method executed successfully.");
        }
    }
}