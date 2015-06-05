namespace ObjectDisposedFodyAddin
{
    using System;

    using Mono.Cecil;

    public static class ImporterService
    {
        private static readonly object initializeLock = new object();

        private static Lazy<ModuleDefinition> MainModuleDefinition { get; set; }

        /// <summary>
        ///     Initializes the service.
        /// </summary>
        /// <param name="mainModuleDefinition">
        ///     The main module that will contains reference.
        /// </param>
        public static void Init(ModuleDefinition mainModuleDefinition)
        {
            if (MainModuleDefinition == null || !MainModuleDefinition.IsValueCreated)
            {
                lock (initializeLock)
                {
                    if (MainModuleDefinition == null || !MainModuleDefinition.IsValueCreated)
                    {
                        MainModuleDefinition = new Lazy<ModuleDefinition>(() => mainModuleDefinition);
                    }
                }
            }
        }

        /// <summary>
        ///     Imports any references into the main module.
        /// </summary>
        /// <param name="methodDefinition">
        ///     The <see cref="MethodReference" /> that will be imported.
        /// </param>
        /// <returns>
        ///     The <see cref="MethodReference" /> imported.
        /// </returns>
        public static MethodReference Import(MethodReference methodDefinition)
        {
            if (MainModuleDefinition == null)
            {
                throw new WeavingException("You must initialized the ImporterService.", WeavingErrorCodes.None);
            }

            return MainModuleDefinition.Value.Import(methodDefinition);
        }

        /// <summary>
        ///     Imports any references into the main module.
        /// </summary>
        /// <param name="typeReference">
        ///     The <see cref="TypeReference" /> that will be imported.
        /// </param>
        /// <returns>
        ///     The <see cref="TypeReference" /> imported.
        /// </returns>
        public static TypeReference Import(TypeReference typeReference)
        {
            if (MainModuleDefinition == null)
            {
                throw new WeavingException("You must initialized the ImporterService.", WeavingErrorCodes.None);
            }

            return MainModuleDefinition.Value.Import(typeReference);
        }

        public static TypeReference ImportFromExternalAssembly(TypeDefinition localAssemblyType,
                                                               TypeReference externalTypeReference)
        {
            return localAssemblyType.Module.Import(externalTypeReference);
        }
    }
}