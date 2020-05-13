using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using ObjectDisposed.Fody.Extensions;

namespace ObjectDisposed.Fody
{
    using FieldAttributes = Mono.Cecil.FieldAttributes;
    using MethodAttributes = Mono.Cecil.MethodAttributes;

    /// <inheritdoc />
    public sealed class ModuleWeaver : BaseModuleWeaver
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
        ///     Will be executed at the end of the compile time.
        /// </summary>
        public override void Execute()
        {
            this.LogInfo("Entry into ObjectDisposedFodyAddin Execute method.");
            this.LogInfo("Searching classes with the IDisposable interface ...");

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
            var generatedCodeCustomAttribute = this.GetGeneratedCodeAttribute();

            // References
            var objectDisposedConstructorMethodReference = this.GetObjectDisposedExceptionConstructor();
            var taskContinueWithMethodReference = this.GetTaskContinueWithMethodReference();
            var taskTypeReference = this.GetTaskTypeReference();
            var actionAsTaskConstructorMethodReference = this.GetActionAsTaskConstructor(taskTypeReference);
            
            this.LogInfo("Done");

            Exception BothInterfacesException(TypeDefinition t) => new WeavingException($"The type {t.FullName} cannot have both interface {typeof(IDisposable)} or IAsyncDisposable.", WeavingErrorCodes.ContainsBothInterface);

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
                this.LogInfo($"Check the class {type.FullName} ...");

                if (type.HasIAsyncDisposableInterface(this))
                {
                    throw BothInterfacesException(type);
                }

                if (type.ContainsIsDisposeField())
                {
                    throw new WeavingException($"The type {type.Name} already contains a field isDisposed.", WeavingErrorCodes.ContainsIsDisposedField);
                }

                var disposeMethod = type.Methods
                                        .SingleOrDefault(x => !x.IsStatic &&
                                                              x.Name.Equals("Dispose") &&
                                                              !x.Parameters.Any());

                var isDisposedBasePropertyGetter = type.GetIsDisposedBasePropertyGetter(this);

                if (disposeMethod != null)
                {
                    // Create the isDisposed field
                    var backingDisposeField = type.CreateField("isDisposed",
                                                               FieldAttributes.Private,
                                                               this.TypeSystem.BooleanReference,
                                                               new[] { generatedCodeCustomAttribute });

                    // Create the protected virtual property IsDisposed
                    type.CreateIsDisposedProperty(backingDisposeField, this.TypeSystem, new[] { generatedCodeCustomAttribute }, isDisposedBasePropertyGetter);

                    // Set the field isDisposed to True
                    disposeMethod.AddSetIsDisposedSync(backingDisposeField);

                    // Add guard instructions into any public members of the type
                    type.AddGuardInstructions(objectDisposedConstructorMethodReference, isDisposedBasePropertyGetter);
                }
                else if (isDisposedBasePropertyGetter != null)
                {
                    // Create the protected virtual property IsDisposed
                    type.CreateIsDisposedProperty(null, this.TypeSystem, new[] { generatedCodeCustomAttribute }, isDisposedBasePropertyGetter);

                    // Add guard instructions into any public members of the type
                    type.AddGuardInstructions(objectDisposedConstructorMethodReference, isDisposedBasePropertyGetter);
                }

                this.LogInfo($"{type.FullName} modified.");
            }

            this.LogInfo("Done.");
            this.LogInfo("Fetching classes with IAsyncDisposable interface ...");

            // Fetch async disposable types
            foreach (var asyncType in asyncTypes)
            {
                this.LogInfo($"Check the class {asyncType.FullName} ...");

                if (asyncType.HasIDisposableInterface(this))
                {
                    throw BothInterfacesException(asyncType);
                }

                if (asyncType.ContainsIsDisposeField())
                {
                    throw new WeavingException($"The type {asyncType.Name} already contains a field isDisposed.", WeavingErrorCodes.ContainsIsDisposedField);
                }

                var disposeAsyncMethod = asyncType.Methods
                                                  .SingleOrDefault(x => !x.IsStatic &&
                                                                        x.Name.Equals("DisposeAsync") &&
                                                                        !x.Parameters.Any());

                var isDisposedBasePropertyGetter = asyncType.GetIsDisposedBasePropertyGetter(this);

                if (disposeAsyncMethod != null)
                {
                    // Create the isDisposed field
                    var backingDisposeField = asyncType.CreateField("isDisposed", FieldAttributes.Private, this.TypeSystem.BooleanReference, new[] { generatedCodeCustomAttribute });

                    // Create the protected virtual property IsDisposed
                    asyncType.CreateIsDisposedProperty(backingDisposeField, this.TypeSystem, new[] { generatedCodeCustomAttribute }, isDisposedBasePropertyGetter);

                    // INFO: [MiniDfx 23.05.15 15:23] This is not a lambda expression, It's just a simple method but for a better understanding what I do, I called it lambda.
                    var lambdaSetToDisposed = asyncType.CreateMethod("SetToDisposed",
                                                                     MethodAttributes.Private | MethodAttributes.HideBySig,
                                                                     this.TypeSystem.VoidReference,
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
                    asyncType.CreateIsDisposedProperty(null, this.TypeSystem, new[] { generatedCodeCustomAttribute }, isDisposedBasePropertyGetter);

                    // Add guard instructions into any public members of the type
                    asyncType.AddGuardInstructions(objectDisposedConstructorMethodReference, isDisposedBasePropertyGetter);
                }

                this.LogInfo($"{asyncType.FullName} modified.");
            }

            this.LogInfo("Done.");
            this.LogInfo("Module modified.");
        }

        private TypeReference GetTaskTypeReference()
        {
            var taskTypeDefinition = this.FindTypeDefinition("System.Threading.Tasks.Task");

            return this.ModuleDefinition.ImportReference(taskTypeDefinition);
        }

        private MethodReference GetActionAsTaskConstructor(TypeReference taskTypeReference)
        {
            var actionConstructorMethodDefinition = this.FindTypeDefinition("System.Action`1");

            var actionTypeReference = this.ModuleDefinition.ImportReference(actionConstructorMethodDefinition);
            var actionGenericInstance = actionTypeReference.MakeGenericInstanceType(taskTypeReference);

            var actionConstructorDefinition = actionGenericInstance.Resolve().GetConstructors().Single();
            var actionConstructorReference = this.ModuleDefinition.ImportReference(actionConstructorDefinition);

            return actionConstructorReference.MakeHostInstanceGeneric(taskTypeReference);
        }

        private MethodReference GetObjectDisposedExceptionConstructor()
        {            
            var objectDisposedExceptionConstructor = this.FindTypeDefinition("System.ObjectDisposedException")
                                                                          .Methods.Single(x => x.IsConstructor &&
                                                                                               x.Parameters.Count() == 1 &&
                                                                                               x.Parameters.All(p => p.ParameterType.FullName == "System.String"));
            return this.ModuleDefinition.ImportReference(objectDisposedExceptionConstructor);
        }

        private CustomAttribute GetGeneratedCodeAttribute()
        {
            var generateCodeAttributeDefinition = this.FindTypeDefinition("System.CodeDom.Compiler.GeneratedCodeAttribute")
                                                      .Methods
                                                      .Single(x => x.IsConstructor && x.Parameters.Count == 2);
            var assemblyFileVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var customAttribute = new CustomAttribute(this.ModuleDefinition.ImportReference(generateCodeAttributeDefinition));
            
            customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(this.TypeSystem.StringReference, "ObjectDisposed.Fody"));
            customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(this.TypeSystem.StringReference, assemblyFileVersion));

            return customAttribute;
        }

        private MethodReference GetTaskContinueWithMethodReference()
        {
            var continueWithMethodDefinition = this.FindTypeDefinition("System.Threading.Tasks.Task")
                                                                    .Methods.Single(x => x.Name.Equals("ContinueWith") &&
                                                                                         x.Parameters.Any(p => p.ParameterType.Name == "Action`1") &&
                                                                                         x.Parameters.Count == 1);
            return this.ModuleDefinition.ImportReference(continueWithMethodDefinition);
        }

        /// <summary>
        ///     Will be called when a request to cancel the build occurs.
        /// </summary>
        public override void Cancel()
        {
            this.LogInfo("ObjectDisposedFodyAddin is canceled while the execution.");
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "mscorlib";
            yield return "System";
        }
    }
}