namespace ObjectDisposedFodyAddin.Extensions
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    /// <summary>
    ///     Contains extension methods for the any <see cref="TypeDefinition" />.
    /// </summary>
    public static class TypeDefinitionExtensions
    {
        /// <summary>
        ///     Determines whether the type <paramref name="typeDefinition" /> contains the a field with the name isDisposed.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="TypeDefinition" /> that will be extended.
        /// </param>
        /// <returns>
        ///     <see langword="True" /> whether the type contains the field otherwise <see langword="False" />.
        /// </returns>
        public static bool ContainsIsDisposeField(this TypeDefinition typeDefinition)
        {
            return typeDefinition.Fields.Any(x => x.Name == "isDisposed");
        }

        /// <summary>
        ///     Creates and adds a field into the <paramref name="typeDefinition" />.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="TypeDefinition" /> that will be extended.
        /// </param>
        /// <param name="name">
        ///     The name of the field.
        /// </param>
        /// <param name="fieldAttributes">
        ///     <see cref="FieldAttributes" /> that describe the new field.
        /// </param>
        /// <param name="returnTypeReference">
        ///     The <see cref="TypeReference" /> of the field.
        /// </param>
        /// <param name="customAttributes">
        ///     <see cref="CustomAttribute" /> that will be apply on the field.
        /// </param>
        /// <returns>
        ///     The <see cref="FieldReference" /> of the field.
        /// </returns>
        public static FieldReference CreateField(this TypeDefinition typeDefinition,
                                                 string name,
                                                 FieldAttributes fieldAttributes,
                                                 TypeReference returnTypeReference,
                                                 IEnumerable<CustomAttribute> customAttributes)
        {
            var fieldDefinition = new FieldDefinition(name, fieldAttributes, returnTypeReference);

            foreach (var customAttribute in customAttributes)
            {
                fieldDefinition.CustomAttributes.Add(customAttribute);
            }

            typeDefinition.Fields.Add(fieldDefinition);
            return fieldDefinition;
        }

        /// <summary>
        ///     Creates and adds the method into the <paramref name="typeDefinition" />.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The type that will be extended.
        /// </param>
        /// <param name="name">
        ///     The name of the method.
        /// </param>
        /// <param name="methodAttributes">
        ///     Attributes that describe the method.
        /// </param>
        /// <param name="returnTypeReference">
        ///     The return type of the method.
        /// </param>
        /// <param name="instructions">
        ///     The instructions that will be injected into the method.
        /// </param>
        /// <returns>
        ///     The <see cref="MethodDefinition" /> representing the method.
        /// </returns>
        public static MethodDefinition CreateMethod(this TypeDefinition typeDefinition,
                                                    string name,
                                                    MethodAttributes methodAttributes,
                                                    TypeReference returnTypeReference,
                                                    IEnumerable<CustomAttribute> attribtues,
                                                    Func<ILProcessor, IEnumerable<Instruction>> instructions)
        {
            var newMethod = new MethodDefinition(name, methodAttributes, returnTypeReference);
            var ilProcessor = newMethod.Body.GetILProcessor();

            newMethod.Body.GetILProcessor().AppendRange(instructions(ilProcessor));

            foreach (var customAttribute in attribtues)
            {
                newMethod.CustomAttributes.Add(customAttribute);
            }

            typeDefinition.Methods.Add(newMethod);

            return newMethod;
        }

        /// <summary>
        ///     Determines whether the <paramref name="typeDefinition" /> implements the interface IAsyncDisposable.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <returns>
        ///     <c>True</c> whether the type implements the interface IAsyncDisposable otherwise <c>False</c>.
        /// </returns>
        public static bool HasIAsyncDisposableInterface(this TypeDefinition typeDefinition)
        {
            if (typeDefinition.Interfaces.Any(i => i.Name == "IAsyncDisposable"))
            {
                return true;
            }

            if (typeDefinition.BaseType != null &&
                typeDefinition.BaseType != typeDefinition.Module.TypeSystem.Object)
            {
                var baseType = typeDefinition.Module.Import(typeDefinition.BaseType);

                // ReSharper disable once TailRecursiveCall
                return HasIAsyncDisposableInterface(baseType.Resolve());
            }

            return false;
        }

        /// <summary>
        ///     Determines whether the dispose guard doesn't have to be injected into the method.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <returns>
        ///     <c>True</c> whether the method contains the attribute <see cref="SkipDisposeGuardAttribute" /> otherwise
        ///     <c>False</c>.
        /// </returns>
        public static bool SkipDisposeGuard(this TypeDefinition typeDefinition)
        {
            return typeDefinition.CustomAttributes.Any(x => x.AttributeType.FullName == "ObjectDisposedFodyAddin.SkipDisposeGuardAttribute");
        }

        /// <summary>
        ///     Determimes whether the type contains at least a dispose method.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <returns>
        ///     <c>True</c> whether the type contains at least a dispose method otherwise <c>False</c>.
        /// </returns>
        public static bool HasAnyDisposeMethod(this TypeDefinition typeDefinition)
        {
            return typeDefinition.HasMethods && typeDefinition.Methods.Any(x => x.Name == "Dispose" || x.Name == "DisposeAsync");
        }

        public static bool HasDisposeMethod(this TypeDefinition typeDefinition)
        {
            return typeDefinition.Methods.Any(x => x.Name == "Dispose");
        }

        public static bool HasDisposeAsyncMethod(this TypeDefinition typeDefinition)
        {
            return typeDefinition.Methods.Any(x => x.Name == "DisposeAsync");
        }

        /// <summary>
        ///     Determines whether the <paramref name="typeDefinition" /> implements the interface <see cref="IDisposable" /> or
        ///     IAsyncDisposable.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <returns>
        ///     <c>True</c> whether the type implements the interface <see cref="IDisposable" /> or IAsyncDisposable otherwise
        ///     <c>False</c>.
        /// </returns>
        public static bool HasBothInterfaces(this TypeDefinition typeDefinition)
        {
            return HasIDisposableInterface(typeDefinition) && HasIAsyncDisposableInterface(typeDefinition);
        }

        /// <summary>
        ///     Determines whether the <paramref name="typeDefinition" /> implements the interface <see cref="IDisposable" />.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <returns>
        ///     <c>True</c> whether the type implements the interface <see cref="IDisposable" /> otherwise <c>False</c>.
        /// </returns>
        public static bool HasIDisposableInterface(this TypeDefinition typeDefinition)
        {
            if (typeDefinition.Interfaces.Any(i => i.FullName == "System.IDisposable"))
            {
                return true;
            }

            if (typeDefinition.BaseType != null &&
                typeDefinition.BaseType != typeDefinition.Module.TypeSystem.Object)
            {
                var baseType = typeDefinition.Module.Import(typeDefinition.BaseType);

                // ReSharper disable once TailRecursiveCall
                return HasIDisposableInterface(baseType.Resolve());
            }

            return false;
        }

        /// <summary>
        ///     Generic method for fetching base type of the <paramref name="typeDefinition" />.
        /// </summary>
        /// <typeparam name="T">
        ///     The output type of the method.
        /// </typeparam>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <param name="predicate">
        ///     Predicate determining the result expected.
        /// </param>
        /// <param name="returnResultFunction">
        ///     <see cref="Func{T,TResult}" /> for formatting the result.
        /// </param>
        /// <returns>
        ///     The result formatted in according to the <paramref name="returnResultFunction" />.
        /// </returns>
        private static T FetchBases<T>(this TypeDefinition typeDefinition,
                                       Predicate<TypeDefinition> predicate,
                                       Func<TypeDefinition, T> returnResultFunction)
        {
            var walkerTypeDefinition = typeDefinition;

            while (walkerTypeDefinition.BaseType != null)
            {
                walkerTypeDefinition = walkerTypeDefinition.BaseType.Resolve();

                if (predicate(walkerTypeDefinition))
                {
                    return returnResultFunction(walkerTypeDefinition);
                }
            }

            return default(T);
        }

        /// <summary>
        ///     Fetchs and returns the IsDisposed property.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <returns>
        ///     The property found otherwise null.
        /// </returns>
        public static MethodReference GetIsDisposedBasePropertyGetter(this TypeDefinition typeDefinition)
        {
            Func<PropertyDefinition, bool> predicate = p => p.Name == "IsDisposed";
            var baseProperty = typeDefinition.FetchBases(t => t.Properties.Any(predicate),
                                                         t => t.Properties.SingleOrDefault(predicate));

            return baseProperty != null
                       ? ImporterService.Import(baseProperty.GetMethod)
                       : null;
        }

        /// <summary>
        ///     Fetchs and returns the Dispose method.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <returns>
        ///     The <see cref="MethodReference" /> representing the method otherwise null.
        /// </returns>
        public static MethodReference GetSyncBaseDisposable(this TypeDefinition typeDefinition)
        {
            Func<MethodDefinition, bool> predicate = x => (x.Name == "Dispose") &&
                                                          !x.Attributes.HasFlag(MethodAttributes.Private) &&
                                                          !x.Attributes.HasFlag(MethodAttributes.Final);
            var baseMethod = typeDefinition.FetchBases(t => t.Methods.Any(predicate),
                                                       t => t.Methods.SingleOrDefault(predicate));

            return baseMethod != null
                       ? ImporterService.Import(baseMethod)
                       : null;
        }

        /// <summary>
        ///     Creats and adds a property to the <paramref name="typeDefinition" />.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <param name="name">
        ///     The property name.
        /// </param>
        /// <param name="basePropertyGetterReference">
        ///     The base getter of the property.
        /// </param>
        /// <param name="backingFieldReference">
        ///     The backing field containing the state of the object.
        /// </param>
        /// <param name="propertyTypeReference">
        ///     The type of the property.
        /// </param>
        /// <param name="voidTypeReference">
        ///     The system void type reference.
        /// </param>
        /// <param name="customAttributes">
        ///     The <see cref="CustomAttribute" /> that will be apply on the property.
        /// </param>
        /// <returns>
        ///     The <see cref="PropertyDefinition" /> of the new property created.
        /// </returns>
        public static PropertyDefinition CreateProperty(this TypeDefinition typeDefinition,
                                                        string name,
                                                        MethodReference basePropertyGetterReference,
                                                        FieldReference backingFieldReference,
                                                        TypeReference propertyTypeReference,
                                                        TypeReference voidTypeReference,
                                                        IEnumerable<CustomAttribute> customAttributes)
        {
            return typeDefinition.CreateOverrideProperty(name, null, backingFieldReference, propertyTypeReference, voidTypeReference, customAttributes);
        }

        /// <summary>
        ///     Creates and adds an override on a property.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <param name="name">
        ///     The name of the property.
        /// </param>
        /// <param name="basePropertyReferenceGetter">
        ///     The base <see cref="MethodReference" /> of the parent type.
        /// </param>
        /// <param name="backingFieldReference">
        ///     The backing field of the property.
        /// </param>
        /// <param name="propertyTypeReference">
        ///     The type of the property.
        /// </param>
        /// <param name="voidTypeReference">
        ///     The system void type reference.
        /// </param>
        /// <param name="customAttributes">
        ///     The <see cref="CustomAttribute" /> that will be apply on the property.
        /// </param>
        /// <returns>
        ///     The <see cref="PropertyDefinition" /> of the new property created.
        /// </returns>
        public static PropertyDefinition CreateOverrideProperty(this TypeDefinition typeDefinition,
                                                                string name,
                                                                MethodReference basePropertyReferenceGetter,
                                                                FieldReference backingFieldReference,
                                                                TypeReference propertyTypeReference,
                                                                TypeReference voidTypeReference,
                                                                IEnumerable<CustomAttribute> customAttributes)
        {
            var getterName = String.Format("get_{0}", name);

            var attributes = MethodAttributes.Family | MethodAttributes.HideBySig |
                             MethodAttributes.SpecialName | MethodAttributes.Virtual;

            attributes |= basePropertyReferenceGetter == null
                              ? MethodAttributes.NewSlot
                              : MethodAttributes.ReuseSlot;

            var getter = CreatePropertyGetter(getterName, attributes, basePropertyReferenceGetter, propertyTypeReference, backingFieldReference);

            var newProperty = new PropertyDefinition(name, PropertyAttributes.Unused, propertyTypeReference)
                                  {
                                      GetMethod = getter
                                  };

            foreach (var customAttribute in customAttributes)
            {
                newProperty.CustomAttributes.Add(customAttribute);
            }

            typeDefinition.Methods.Add(getter);
            typeDefinition.Properties.Add(newProperty);

            return newProperty;
        }

        private static MethodDefinition CreatePropertyGetter(string name,
                                                             MethodAttributes attributes,
                                                             MethodReference propertyReferenceGetter,
                                                             TypeReference propertyType,
                                                             FieldReference backingFieldReference)
        {
            var getter = new MethodDefinition(name, attributes, propertyType);

            var ilProcessor = getter.Body.GetILProcessor();

            var instructions = Instructions.GetIsDisposedInstructionsGetter(ilProcessor, propertyReferenceGetter, backingFieldReference);
            ilProcessor.AppendRange(instructions);

            return getter;
        }

        /// <summary>
        ///     Adds the instructions into any public methods of the <paramref name="typeDefinition" />.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <param name="objectDisposedExceptionConstructor">
        ///     The constructor reference for throwing the exception <see cref="ObjectDisposedException" />.
        /// </param>
        /// <param name="basePropertyReferenceGetter">
        ///     The base <see cref="MethodReference" /> of the parent type.
        /// </param>
        public static void AddGuardInstructions(this TypeDefinition typeDefinition,
                                                MethodReference objectDisposedExceptionConstructor,
                                                MethodReference basePropertyReferenceGetter)
        {
            var methods = typeDefinition.Methods
                                        .Where(x => !x.IsStatic &&
                                                    !x.IsGeneratedCode() &&
                                                    x.IsPublic &&
                                                    !x.Name.Equals("Dispose") &&
                                                    !x.Name.Equals("DisposeAsync") &&
                                                    !x.Name.Equals(".ctor"));

            MethodReference propertyIsDisposedGetter = null;

            if (typeDefinition.Properties.Any(x => x.Name == "IsDisposed"))
            {
                propertyIsDisposedGetter = typeDefinition.Properties.Single(x => x.Name == "IsDisposed").GetMethod;
            }

            if (propertyIsDisposedGetter == null)
            {
                propertyIsDisposedGetter = basePropertyReferenceGetter;
            }

            if (propertyIsDisposedGetter == null)
            {
                throw new WeavingException("Cannot find the property IsDisposed for determining whether the object is already disposed.", WeavingErrorCodes.PropertyNotFound);
            }

            foreach (var method in methods.Where(x => x.Body != null))
            {
                var ilProcessor = method.Body.GetILProcessor();
                var firstInstruction = method.Body.Instructions.First();

                var newInstructions = Instructions.GetGuardInstructions(ilProcessor,
                                                                        typeDefinition,
                                                                        propertyIsDisposedGetter,
                                                                        objectDisposedExceptionConstructor);
                ilProcessor.InsertBeforeRange(firstInstruction, newInstructions);

                method.Body.OptimizeMacros();
            }
        }

        /// <summary>
        ///     Determines whether a type has been generated by a tools.
        /// </summary>
        /// <param name="typeDefinition">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <returns>
        ///     <c>True</c> whether the type contains the attribute <see cref="CompilerGeneratedAttribute" /> or
        ///     <see cref="GeneratedCodeAttribute" /> otherwise <c>False</c>.
        /// </returns>
        public static bool IsGeneratedCode(this TypeDefinition typeDefinition)
        {
            return typeDefinition.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute" || a.AttributeType.Name == "GeneratedCodeAttribute");
        }
    }
}