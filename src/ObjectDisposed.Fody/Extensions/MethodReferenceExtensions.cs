namespace ObjectDisposedFodyAddin.Extensions
{
    using System.Collections.Generic;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    /// <summary>
    ///     Contains extension methods for the any <see cref="MethodReference" />.
    /// </summary>
    public static class MethodReferenceExtensions
    {
        /// <summary>
        ///     Creates and adds an override of a <paramref name="methodReference" />.
        /// </summary>
        /// <param name="methodReference">
        ///     The <see cref="MethodReference" /> that will be extended.
        /// </param>
        /// <param name="typeDefinition">
        ///     The <see cref="TypeDefinition" /> that will contain the new method.
        /// </param>
        /// <param name="instructions">
        ///     The <see cref="Instruction" />s that will be injected into the new method.
        /// </param>
        /// <remarks>
        ///     How to create an override method : http://stackoverflow.com/a/8103611
        /// </remarks>
        public static void CreateOverride(this MethodReference methodReference,
                                          TypeDefinition typeDefinition,
                                          IEnumerable<Instruction> instructions)
        {
            const MethodAttributes OverrideMethodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig;

            var newMethod = new MethodDefinition(methodReference.Name, OverrideMethodAttributes, methodReference.ReturnType);
            var ilProcessor = newMethod.Body.GetILProcessor();

            ilProcessor.AppendRange(instructions);

            typeDefinition.Methods.Add(newMethod);
        }

        /// <summary>
        ///     Makes the <paramref name="methodReference" /> as generic <see cref="MethodReference" />.
        /// </summary>
        /// <param name="methodReference">
        ///     The <see cref="MethodReference" /> that will be extended.
        /// </param>
        /// <param name="args">
        ///     Generic types that will be apply.
        /// </param>
        /// <returns>
        ///     The generic method.
        /// </returns>
        public static MethodReference MakeHostInstanceGeneric(this MethodReference methodReference,
                                                              params TypeReference[] args)
        {
            var instance = new MethodReference(
                methodReference.Name,
                methodReference.ReturnType,
                methodReference.DeclaringType.MakeGenericInstanceType(args))
                               {
                                   HasThis = methodReference.HasThis,
                                   ExplicitThis = methodReference.ExplicitThis,
                                   CallingConvention = methodReference.CallingConvention
                               };

            foreach (var parameter in methodReference.Parameters)
            {
                instance.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }

            foreach (var genericParam in methodReference.GenericParameters)
            {
                instance.GenericParameters.Add(new GenericParameter(genericParam.Name, instance));
            }

            return instance;
        }
    }
}