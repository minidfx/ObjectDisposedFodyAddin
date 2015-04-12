namespace ObjectDisposedFodyAddin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    /// <summary>
    ///     The module containing the logic for injecting instructions to check whether an object is already disposed.
    /// </summary>
    public sealed class ModuleWeaver
    {
        /// <summary>
        ///     Contains <see cref="MethodReference"/>s already resolved.
        /// </summary>
        private readonly IDictionary<string, MethodReference> cacheMethodReferences = new Dictionary<string, MethodReference>();

        private MethodReference compilerAttributeReference;

        /// <summary>
        ///     The constructor reference with string as argument of the <see cref="ObjectDisposedException" /> class.
        /// </summary>
        private MethodReference objectDisposedExceptionReference;

        /// <summary>
        ///     Contains <see cref="TypeReference"/>s available in the module.
        /// </summary>
        private IEnumerable<TypeReference> typeReferences;

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
            this.cacheMethodReferences.Clear();

            var msCoreLibDefinition = this.ModuleDefinition.AssemblyResolver.Resolve("mscorlib");
            var msCoreTypes = msCoreLibDefinition.MainModule.Types;

            this.typeSystem = this.ModuleDefinition.TypeSystem;
            this.types = new Lazy<IEnumerable<TypeDefinition>>(() => this.ModuleDefinition.Types.Where(x => x.IsClass &&
                                                                                                            !x.IsAbstract &&
                                                                                                            !x.IsInterface &&
                                                                                                            x.HasDisposeInterface() &&
                                                                                                            !x.IsGeneratedCode() &&
                                                                                                            !x.SkipDisposeGuard()));

            this.typeReferences = this.ModuleDefinition.GetTypeReferences();

            this.InitializeWeaving(msCoreTypes);

            this.CreateDisposeMethodIfNotExists();
            this.AddIsDisposedPrivateMember();
            this.AddSetToDisposedIntructionsIntoDisposeMethods();
            this.AddGuardInstructionsIntoMethods();

            this.LogDebug("Execute method executed successfully.");
        }

        /// <summary>
        ///     Creates the disposable method for any types that inherit another type that implements a disposable interface
        ///     for injecting the instructions to set isDisposed to true when it is called.
        /// </summary>
        private void CreateDisposeMethodIfNotExists()
        {
            var typeWithoutDisposeMethod = this.types.Value.Where(x => x.HasIDisposableInterface() && x.Methods.All(m => m.Name != "Dispose")).ToArray();
            var typeWithoutDisposeAsyncMethod = this.types.Value.Where(x => x.HasIAsyncDisposableInterface() && x.Methods.All(m => m.Name != "DisposeAsync")).ToArray();

            foreach (var typeDefinition in typeWithoutDisposeMethod)
            {
                typeDefinition.CreateOverrideMethod("Dispose", this.typeSystem.Void, this.cacheMethodReferences, this.compilerAttributeReference);
            }

            if (typeWithoutDisposeAsyncMethod.Any())
            {
                var taskTypeReference = this.typeReferences.Single(x => x.FullName == "System.Threading.Tasks.Task");

                foreach (var typeDefinition in typeWithoutDisposeAsyncMethod)
                {
                    typeDefinition.CreateOverrideMethod("DisposeAsync", taskTypeReference, this.cacheMethodReferences, this.compilerAttributeReference);
                }
            }
        }

        private void InitializeWeaving(IEnumerable<TypeDefinition> msCoreTypes)
        {
            var msCoretypeDefinitions = msCoreTypes as TypeDefinition[] ?? msCoreTypes.ToArray();
            if (this.objectDisposedExceptionReference == null)
            {
                var objectDisposedExceptionConstructor = msCoretypeDefinitions.Single(x => x.FullName == "System.ObjectDisposedException")
                                                                              .Methods.Single(x => x.Name == ".ctor" &&
                                                                                                   x.Parameters.Count() == 1 &&
                                                                                                   x.Parameters.All(p => p.ParameterType.FullName == "System.String"));
                this.objectDisposedExceptionReference = this.ModuleDefinition.Import(objectDisposedExceptionConstructor);
            }

            if (this.compilerAttributeReference == null)
            {
                var compilerAttributeDefinition = msCoretypeDefinitions.Single(x => x.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
                                                                       .Methods.Single(x => x.Name == ".ctor");
                this.compilerAttributeReference = this.ModuleDefinition.Import(compilerAttributeDefinition);
            }

            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var typeDefinition in this.types.Value)
            {
                if (typeDefinition.Interfaces.Any(x => x.FullName == "System.IDisposable") && typeDefinition.Interfaces.Any(x => x.Name == "IAsyncDisposable"))
                {
                    throw new WeavingException(string.Format("The type {0} cannot have both interface {1} and {2}", typeDefinition.Name, "IDisposable", "IAsyncDisposable"),
                                               WeavingErrorCodes.ContainsBothInterface);
                }
            }

            TypeDefinition typeNotUseable;
            if ((typeNotUseable = this.types.Value.FirstOrDefault(t => t.Fields.Any(f => f.Name == "isDisposed" &&
                                                                                         (f.FieldType.MetadataType != MetadataType.Boolean || f.Attributes != FieldAttributes.Private)))) != null)
            {
                throw new WeavingException(string.Format("The type {0} contains already a member 'isDisposed' not useable.", typeNotUseable.Name),
                                           WeavingErrorCodes.NotUseable);
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
                    var returnInstruction = method.Body.Instructions.Single(x => x.OpCode.Code == Code.Ret);

                    var newInstructions = Instructions.GetSetIsDisposedInstructions(ilProcessor, disposeField);
                    ilProcessor.InsertBeforeRange(returnInstruction, newInstructions);

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
                    var fieldDefinition = new FieldDefinition("isDisposed", FieldAttributes.Private, this.typeSystem.Boolean);
                    fieldDefinition.CustomAttributes.Add(new CustomAttribute(this.compilerAttributeReference));
                    type.Fields.Add(fieldDefinition);
                }
            }

            this.LogDebug("AddIsDisposedPrivateMember method executed successfully.");
        }

        private void AddGuardInstructionsIntoMethods()
        {
            this.LogDebug("Entry into ObjectDisposedFodyAddin AddGuardInstructionsIntoMethods method");

            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var type in this.types.Value)
            {
                var methods = type.Methods
                                  .Where(x => !x.IsStatic &&
                                              !x.IsGeneratedCode() &&
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
                    ilProcessor.InsertBeforeRange(firstInstruction, newInstructions);

                    method.Body.OptimizeMacros();
                }
            }

            this.LogDebug("AddGuardInstructionsIntoMethods method executed successfully.");
        }
    }
}