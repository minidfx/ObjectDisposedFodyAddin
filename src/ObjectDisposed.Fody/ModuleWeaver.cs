namespace ObjectDisposedFodyAddin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Mono.Cecil;
    using Mono.Cecil.Rocks;
    using Mono.Collections.Generic;

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

            var msCoreAssemblyDefinition = this.ModuleDefinition.AssemblyResolver.Resolve("mscorlib");
            var systemAssemblyDefinition = this.ModuleDefinition.AssemblyResolver.Resolve("System");
            var msCoreTypeDefinitions = msCoreAssemblyDefinition.MainModule.Types;
            var systemTypeDefinitions = systemAssemblyDefinition.MainModule.Types;

            var typeSystem = this.ModuleDefinition.TypeSystem;
            var syncTypes = this.ModuleDefinition.Types.Where(x => x.IsClass &&
                                                                   !x.IsInterface &&
                                                                   x.HasIDisposableInterface() &&
                                                                   !x.IsGeneratedCode() &&
                                                                   !x.SkipDisposeGuard());

            var asyncTypes = this.ModuleDefinition.Types.Where(x => x.IsClass &&
                                                                    !x.IsInterface &&
                                                                    x.HasIAsyncDisposableInterface() &&
                                                                    !x.IsGeneratedCode() &&
                                                                    !x.SkipDisposeGuard());

            var objectDisposedConstructorMethodReference = this.GetObjectDisposedExceptionConstructor(msCoreTypeDefinitions);
            var generatedCodeCustomAttribute = this.GetGeneratedCodeAttribute(systemTypeDefinitions, typeSystem);
            var taskContinueWithMethodReference = this.GetTaskContinueWithMethodReference(msCoreTypeDefinitions);
            var taskTypeReference = this.GetTaskTypeReference(msCoreTypeDefinitions);
            var actionConstructorMethodReference = this.GetActionConstructor(msCoreTypeDefinitions, taskTypeReference);

            Func<TypeDefinition, Exception> bothInterfacesException = t => new WeavingException(string.Format("The type {0} cannot have both interface {1} and {2}", t.FullName, "IDisposable", "IAsyncDisposable"), WeavingErrorCodes.ContainsBothInterface);

            // Summary : Apply modifications for each types.
            // 1. Create the property (getter/setter)
            // 1.1 The getter property will call the base property if it exists to determines whether the type is completely disposed.
            // 1.3 Set the backing field to <see langword="True" /> when a dispose method is called.
            // 2. Add guard instructions into every public methods.
            // 3. Add instructions to set <see langword="True" /> to the isDisposed field when the dispose method is called.

            // Fetch disposable types
            foreach (var type in syncTypes)
            {
                if (type.HasIAsyncDisposableInterface())
                {
                    throw bothInterfacesException(type);
                }

                // Create the isDisposed field
                var backingDisposeField = type.CreateField("isDisposed", FieldAttributes.Private, typeSystem.Boolean, new[] { generatedCodeCustomAttribute });

                // Create the protected virtual property IsDisposed
                CreateIsDisposedProperty(type, backingDisposeField, typeSystem, new[] { generatedCodeCustomAttribute });

                // Add guard instructions into any public members of the type
                type.AddGuardInstructions(objectDisposedConstructorMethodReference);

                // Set the field isDisposed to True
                AddInstructionsToDipose(type, backingDisposeField);
            }

            // Fetch async disposable types
            foreach (var asyncType in asyncTypes)
            {
                if (asyncType.HasIDisposableInterface())
                {
                    throw bothInterfacesException(asyncType);
                }

                // Create the isDisposed field
                var backingDisposeField = asyncType.CreateField("isDisposed", FieldAttributes.Private, typeSystem.Boolean, new[] { generatedCodeCustomAttribute });

                // Create the protected virtual property IsDisposed
                CreateIsDisposedProperty(asyncType, backingDisposeField, typeSystem, new[] { generatedCodeCustomAttribute });

                var disposeAsyncMethod = asyncType.Methods
                                                  .SingleOrDefault(x => !x.IsStatic &&
                                                                        x.Name.Equals("DisposeAsync") &&
                                                                        !x.Parameters.Any());

                if (disposeAsyncMethod != null)
                {
                    // Add guard instructions into any public members of the type
                    asyncType.AddGuardInstructions(objectDisposedConstructorMethodReference);

                    // INFO: [MiniDfx 23.05.15 15:23] This is not a lambda expression, It's just a simple method but for a better understanding what I do, I called it lambda.
                    var lambdaTaskContinueWithMethodReference = asyncType.CreateMethod("ContinueWithSetToDisposed",
                                                                                       MethodAttributes.Private | MethodAttributes.HideBySig,
                                                                                       typeSystem.Void,
                                                                                       i => Instructions.GetSetIsDisposedFullInstructions(i, backingDisposeField));

                    lambdaTaskContinueWithMethodReference.CustomAttributes.Add(generatedCodeCustomAttribute);
                    lambdaTaskContinueWithMethodReference.Parameters.Add(new ParameterDefinition(taskTypeReference));

                    // Create the method to set to true the local field.
                    var setToDisposedMethod = asyncType.CreateMethod("SetToDisposedAsync", MethodAttributes.Private | MethodAttributes.HideBySig, taskTypeReference, i => Instructions.GetSetIsDisposedAsyncMethodInstructions(i, taskContinueWithMethodReference, actionConstructorMethodReference, lambdaTaskContinueWithMethodReference));

                    setToDisposedMethod.CustomAttributes.Add(generatedCodeCustomAttribute);
                    setToDisposedMethod.Parameters.Add(new ParameterDefinition(taskTypeReference));

                    disposeAsyncMethod.AddSetIsDisposedAsync(setToDisposedMethod);
                }
            }

            this.LogDebug("Execute method executed successfully.");
        }

        private TypeReference GetTaskTypeReference(IEnumerable<TypeDefinition> systemTypeDefinitions)
        {
            var taskTypeDefinition = systemTypeDefinitions.Single(x => x.FullName == "System.Threading.Tasks.Task");

            return this.ModuleDefinition.Import(taskTypeDefinition);
        }

        private MethodReference GetActionConstructor(IEnumerable<TypeDefinition> msCoreTypeDefinitions,
                                                     TypeReference taskTypeReference)
        {
            var actionConstructorMethodDefinition = msCoreTypeDefinitions.Single(x => x.FullName == "System.Action`1");

            var actionTypeReference = this.ModuleDefinition.Import(actionConstructorMethodDefinition);
            var actionGenericInstance = actionTypeReference.MakeGenericInstanceType(taskTypeReference);

            var actionConstructorDefinition = actionGenericInstance.Resolve().GetConstructors().Single();
            var actionConstructorReference = this.ModuleDefinition.Import(actionConstructorDefinition);

            return actionConstructorReference.MakeHostInstanceGeneric(taskTypeReference);
        }

        private static void AddInstructionsToDipose(TypeDefinition type,
                                                    FieldReference backingDisposeField)
        {
            var disposeMethod = type.Methods
                                    .SingleOrDefault(x => !x.IsStatic &&
                                                          x.Name.Equals("Dispose") &&
                                                          !x.Parameters.Any());

            if (disposeMethod != null)
            {
                disposeMethod.AddSetIsDisposedSync(backingDisposeField);
            }
        }

        private MethodReference GetObjectDisposedExceptionConstructor(IEnumerable<TypeDefinition> msCoreTypeDefinitions)
        {
            var objectDisposedExceptionConstructor = msCoreTypeDefinitions.Single(x => x.FullName == "System.ObjectDisposedException")
                                                                          .Methods.Single(x => x.IsConstructor &&
                                                                                               x.Parameters.Count() == 1 &&
                                                                                               x.Parameters.All(p => p.ParameterType.FullName == "System.String"));
            return this.ModuleDefinition.Import(objectDisposedExceptionConstructor);
        }

        private static void CreateIsDisposedProperty(TypeDefinition type,
                                                     FieldReference backingDisposeField,
                                                     TypeSystem typeSystem,
                                                     IEnumerable<CustomAttribute> customAttributes)
        {
            var baseIsDisposedProperty = type.GetIsDisposedBaseProperty();
            type.CreateOverrideProperty("IsDisposed", baseIsDisposedProperty, backingDisposeField, typeSystem.Boolean, typeSystem.Void, customAttributes);
        }

        private CustomAttribute GetGeneratedCodeAttribute(Collection<TypeDefinition> msCoreTypeDefinitions,
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
            this.LogDebug("ObjectDisposedFodyAddin is canceled while the execution.");
        }
    }
}