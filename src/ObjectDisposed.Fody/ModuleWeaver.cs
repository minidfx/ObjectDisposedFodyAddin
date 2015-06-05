namespace ObjectDisposedFodyAddin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Mono.Cecil;
    using Mono.Cecil.Rocks;

    using ObjectDisposedFodyAddin.Extensions;

    using FieldAttributes = Mono.Cecil.FieldAttributes;
    using MethodAttributes = Mono.Cecil.MethodAttributes;

    /// <summary>
    ///     The module containing the logic for injecting instructions to check whether an object is already disposed.
    /// </summary>
    public sealed class ModuleWeaver
    {
        /// <summary>
        ///     Initialize an instance of <see cref="ModuleWeaver" /> class.
        /// </summary>
        public ModuleWeaver()
        {
            this.LogDebug = s => { Console.WriteLine(@"DEBUG: {0}", s); };
            this.LogInfo = s => { Console.WriteLine(@"INFO: {0}", s); };
            this.LogWarning = s => { Console.WriteLine(@"WARNING: {0}", s); };
            this.LogError = s => { Console.WriteLine(@"ERROR: {0}", s); };
        }

        /// <summary>
        ///     Logger for notifying MSBuild about an error message.
        /// </summary>
        public Action<string> LogError { get; set; }

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
            this.LogInfo("Entry into ObjectDisposedFodyAddin Execute method.");

            var msCoreAssemblyDefinition = this.ModuleDefinition.AssemblyResolver.Resolve("mscorlib");
            var systemAssemblyDefinition = this.ModuleDefinition.AssemblyResolver.Resolve("System");
            var msCoreTypeDefinitions = msCoreAssemblyDefinition.MainModule.Types;
            var systemTypeDefinitions = systemAssemblyDefinition.MainModule.Types;

            this.LogInfo("Searching classes with the IDisposable interface ...");

            var systemType = this.ModuleDefinition.TypeSystem;
            var syncTypes = this.ModuleDefinition.Types.Where(x => !x.SkipDisposeGuard() &&
                                                                   x.IsClass &&
                                                                   !x.IsInterface &&
                                                                   x.HasIDisposableInterface(this) &&
                                                                   !x.IsGeneratedCode())
                                .OrderBy(x => this.ModuleDefinition.Types.Contains(x));

            this.LogInfo("Done.");
            this.LogInfo("Searching classes with the IAsyncDisposable interface ...");

            var asyncTypes = this.ModuleDefinition.Types.Where(x => !x.SkipDisposeGuard() &&
                                                                    x.IsClass &&
                                                                    !x.IsInterface &&
                                                                    x.HasIAsyncDisposableInterface(this) &&
                                                                    !x.IsGeneratedCode())
                                 .OrderBy(x => this.ModuleDefinition.Types.Contains(x));

            this.LogInfo("Done.");
            this.LogInfo("Retrieving references known ...");

            // Custom attribute
            var generatedCodeCustomAttribute = this.GetGeneratedCodeAttribute(systemTypeDefinitions, systemType);

            // References
            var objectDisposedConstructorMethodReference = this.GetObjectDisposedExceptionConstructor(msCoreTypeDefinitions);
            var taskContinueWithMethodReference = this.GetTaskContinueWithMethodReference(msCoreTypeDefinitions);
            var taskTypeReference = this.GetTaskTypeReference(msCoreTypeDefinitions);
            var actionAsTaskConstructorMethodReference = this.GetActionAsTaskConstructor(msCoreTypeDefinitions, taskTypeReference);

            this.LogInfo("Done");

            Func<TypeDefinition, Exception> bothInterfacesException = t => new WeavingException(string.Format("The type {0} cannot have both interface {1} and {2}", t.FullName, "IDisposable", "IAsyncDisposable"), WeavingErrorCodes.ContainsBothInterface);

            // Initialize the importer service.
            References.Init(this.ModuleDefinition);

            // Summary : Apply modifications for each types.
            // 1. Create the property (getter/setter)
            // 1.1 The getter property will call the base property if it exists to determines whether the type is completely disposed.
            // 1.3 Set the backing field to <see langword="True" /> when a dispose method is called.
            // 2. Add guard instructions into every public methods.
            // 3. Add instructions to set <see langword="True" /> to the isDisposed field when the dispose method is called.

            this.LogInfo("Fetching classes with IDisposable interface ...");

            // Fetch disposable types
            foreach (var type in syncTypes)
            {
                this.LogInfo(string.Format("Check the class {0} ...", type.FullName));

                if (type.HasIAsyncDisposableInterface(this))
                {
                    throw bothInterfacesException(type);
                }

                if (type.ContainsIsDisposeField())
                {
                    throw new WeavingException(string.Format("The type {0} already contains a field isDisposed.", type.Name), WeavingErrorCodes.ContainsIsDisposedField);
                }

                var disposeMethod = type.Methods
                                        .SingleOrDefault(x => !x.IsStatic &&
                                                              x.Name.Equals("Dispose") &&
                                                              !x.Parameters.Any());

                var isDisposedBasePropertyGetter = type.GetIsDisposedBasePropertyGetter(this);

                if (disposeMethod != null)
                {
                    // Create the isDisposed field
                    var backingDisposeField = type.CreateField("isDisposed", FieldAttributes.Private, systemType.Boolean, new[] { generatedCodeCustomAttribute });

                    // Create the protected virtual property IsDisposed
                    type.CreateIsDisposedProperty(backingDisposeField, systemType, new[] { generatedCodeCustomAttribute }, isDisposedBasePropertyGetter);

                    // Set the field isDisposed to True
                    disposeMethod.AddSetIsDisposedSync(backingDisposeField);

                    // Add guard instructions into any public members of the type
                    type.AddGuardInstructions(objectDisposedConstructorMethodReference, isDisposedBasePropertyGetter);
                }
                else if (isDisposedBasePropertyGetter != null)
                {
                    // Create the protected virtual property IsDisposed
                    type.CreateIsDisposedProperty(null, systemType, new[] { generatedCodeCustomAttribute }, isDisposedBasePropertyGetter);

                    // Add guard instructions into any public members of the type
                    type.AddGuardInstructions(objectDisposedConstructorMethodReference, isDisposedBasePropertyGetter);
                }

                this.LogInfo(string.Format("{0} modified.", type.FullName));
            }

            this.LogInfo("Done.");
            this.LogInfo("Fetching classes with IAsyncDisposable interface ...");

            // Fetch async disposable types
            foreach (var asyncType in asyncTypes)
            {
                this.LogInfo(string.Format("Check the class {0} ...", asyncType.FullName));

                if (asyncType.HasIDisposableInterface(this))
                {
                    throw bothInterfacesException(asyncType);
                }

                if (asyncType.ContainsIsDisposeField())
                {
                    throw new WeavingException(string.Format("The type {0} already contains a field isDisposed.", asyncType.Name), WeavingErrorCodes.ContainsIsDisposedField);
                }

                var disposeAsyncMethod = asyncType.Methods
                                                  .SingleOrDefault(x => !x.IsStatic &&
                                                                        x.Name.Equals("DisposeAsync") &&
                                                                        !x.Parameters.Any());

                var isDisposedBasePropertyGetter = asyncType.GetIsDisposedBasePropertyGetter(this);

                if (disposeAsyncMethod != null)
                {
                    // Create the isDisposed field
                    var backingDisposeField = asyncType.CreateField("isDisposed", FieldAttributes.Private, systemType.Boolean, new[] { generatedCodeCustomAttribute });

                    // Create the protected virtual property IsDisposed
                    asyncType.CreateIsDisposedProperty(backingDisposeField, systemType, new[] { generatedCodeCustomAttribute }, isDisposedBasePropertyGetter);

                    // INFO: [MiniDfx 23.05.15 15:23] This is not a lambda expression, It's just a simple method but for a better understanding what I do, I called it lambda.
                    var lambdaSetToDisposed = asyncType.CreateMethod("SetToDisposed",
                                                                     MethodAttributes.Private | MethodAttributes.HideBySig,
                                                                     systemType.Void,
                                                                     new[] { generatedCodeCustomAttribute },
                                                                     i => Instructions.GetSetToDisposeFullInstructions(i, backingDisposeField));

                    lambdaSetToDisposed.Parameters.Add(new ParameterDefinition(taskTypeReference));

                    // Create the method to set to true the local field.
                    var functionContinueWith = asyncType.CreateMethod("ContinueWithSetToDisposed",
                                                                      MethodAttributes.Private | MethodAttributes.HideBySig,
                                                                      taskTypeReference,
                                                                      new[] { generatedCodeCustomAttribute },
                                                                      i => Instructions.GetDisposeAsyncMethodInstructions(i, taskContinueWithMethodReference, actionAsTaskConstructorMethodReference, lambdaSetToDisposed));

                    functionContinueWith.Parameters.Add(new ParameterDefinition(taskTypeReference));

                    // Add instructions to dispose the object when the output task is finished
                    disposeAsyncMethod.AddSetIsDisposedAsync(functionContinueWith, taskTypeReference);

                    // Add guard instructions into any public members of the type
                    asyncType.AddGuardInstructions(objectDisposedConstructorMethodReference, isDisposedBasePropertyGetter);
                }
                else
                {
                    // Create the protected virtual property IsDisposed
                    asyncType.CreateIsDisposedProperty(null, systemType, new[] { generatedCodeCustomAttribute }, isDisposedBasePropertyGetter);

                    // Add guard instructions into any public members of the type
                    asyncType.AddGuardInstructions(objectDisposedConstructorMethodReference, isDisposedBasePropertyGetter);
                }

                this.LogInfo(string.Format("{0} modified.", asyncType.FullName));
            }

            this.LogInfo("Done.");
            this.LogInfo("Module modified.");
        }

        private TypeReference GetTaskTypeReference(IEnumerable<TypeDefinition> msCoreTypeDefinitions)
        {
            var taskTypeDefinition = msCoreTypeDefinitions.Single(x => x.FullName == "System.Threading.Tasks.Task");

            return this.ModuleDefinition.Import(taskTypeDefinition);
        }

        private MethodReference GetActionAsTaskConstructor(IEnumerable<TypeDefinition> msCoreTypeDefinitions,
                                                           TypeReference taskTypeReference)
        {
            var actionConstructorMethodDefinition = msCoreTypeDefinitions.Single(x => x.FullName == "System.Action`1");

            var actionTypeReference = this.ModuleDefinition.Import(actionConstructorMethodDefinition);
            var actionGenericInstance = actionTypeReference.MakeGenericInstanceType(taskTypeReference);

            var actionConstructorDefinition = actionGenericInstance.Resolve().GetConstructors().Single();
            var actionConstructorReference = this.ModuleDefinition.Import(actionConstructorDefinition);

            return actionConstructorReference.MakeHostInstanceGeneric(taskTypeReference);
        }

        private MethodReference GetObjectDisposedExceptionConstructor(IEnumerable<TypeDefinition> msCoreTypeDefinitions)
        {
            var objectDisposedExceptionConstructor = msCoreTypeDefinitions.Single(x => x.FullName == "System.ObjectDisposedException")
                                                                          .Methods.Single(x => x.IsConstructor &&
                                                                                               x.Parameters.Count() == 1 &&
                                                                                               x.Parameters.All(p => p.ParameterType.FullName == "System.String"));
            return this.ModuleDefinition.Import(objectDisposedExceptionConstructor);
        }

        private CustomAttribute GetGeneratedCodeAttribute(IEnumerable<TypeDefinition> msCoreTypeDefinitions,
                                                          TypeSystem typeSystem)
        {
            var generateCodeAttributeDefinition = msCoreTypeDefinitions.Single(x => x.FullName == "System.CodeDom.Compiler.GeneratedCodeAttribute")
                                                                       .Methods.Single(x => x.IsConstructor && x.Parameters.Count == 2);

            var assemnlyFileVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var customAttribute = new CustomAttribute(this.ModuleDefinition.Import(generateCodeAttributeDefinition));
            customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(typeSystem.String, "ObjectDisposed.Fody"));
            customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(typeSystem.String, assemnlyFileVersion));

            return customAttribute;
        }

        private MethodReference GetTaskContinueWithMethodReference(IEnumerable<TypeDefinition> systemTypeDefinitions)
        {
            var continueWithMethodDefinition = systemTypeDefinitions.Single(x => x.FullName.Equals("System.Threading.Tasks.Task"))
                                                                    .Methods.Single(x => x.Name.Equals("ContinueWith") &&
                                                                                         x.Parameters.Any(p => p.ParameterType.Name == "Action`1") &&
                                                                                         x.Parameters.Count == 1);
            return this.ModuleDefinition.Import(continueWithMethodDefinition);
        }

        /// <summary>
        ///     Will be called when a request to cancel the build occurs.
        /// </summary>
        public void Cancel()
        {
            this.LogInfo("ObjectDisposedFodyAddin is canceled while the execution.");
        }
    }
}