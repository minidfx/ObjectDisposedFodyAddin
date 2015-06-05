namespace ObjectDisposedFodyAddin.Extensions
{
    using Mono.Cecil;
    using Mono.Cecil.Rocks;

    /// <summary>
    ///     Contains extension methods for the any <see cref="MethodReference" />.
    /// </summary>
    public static class MethodReferenceExtensions
    {
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