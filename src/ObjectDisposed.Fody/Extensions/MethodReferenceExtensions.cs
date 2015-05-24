namespace ObjectDisposedFodyAddin.Extensions
{
    using Mono.Cecil;
    using Mono.Cecil.Rocks;

    public static class MethodReferenceExtensions
    {
        public static MethodReference MakeHostInstanceGeneric(this MethodReference reference,
                                                              params TypeReference[] args)
        {
            var instance = new MethodReference(
                reference.Name,
                reference.ReturnType,
                reference.DeclaringType.MakeGenericInstanceType(args))
                               {
                                   HasThis = reference.HasThis,
                                   ExplicitThis = reference.ExplicitThis,
                                   CallingConvention = reference.CallingConvention
                               };

            foreach (var parameter in reference.Parameters)
            {
                instance.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }

            foreach (var genericParam in reference.GenericParameters)
            {
                instance.GenericParameters.Add(new GenericParameter(genericParam.Name, instance));
            }

            return instance;
        }
    }
}