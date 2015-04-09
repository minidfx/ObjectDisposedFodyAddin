namespace ObjectDisposedFodyAddin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    ///     Contains instructions for injection purposes into any dispose methods.
    /// </summary>
    public sealed class Instructions
    {
        /// <summary>
        ///     Returns instruction for injecting the isDisposed field into a type.
        /// </summary>
        /// <param name="ilProcessor">
        ///     Service for managing instructions of a method.
        /// </param>
        /// <param name="disposeFieldDefinition">
        ///     The isDisposed field.
        /// </param>
        /// <returns>
        ///     A collection of <see cref="Instruction" />.
        /// </returns>
        public static IEnumerable<Instruction> GetSetIsDisposedInstructions(ILProcessor ilProcessor,
                                                                            FieldReference disposeFieldDefinition)
        {
            yield return ilProcessor.Create(OpCodes.Ldarg_0);
            yield return ilProcessor.Create(OpCodes.Ldc_I4_1);
            yield return ilProcessor.Create(OpCodes.Stfld, disposeFieldDefinition);
        }

        /// <summary>
        ///     Returns instructions for injecting the guard into a method.
        /// </summary>
        /// <param name="ilProcessor">
        ///     Service for managing instructions of a method.
        /// </param>
        /// <param name="memberReference">
        ///     The reference of the member, which contains the method.
        /// </param>
        /// <param name="disposeFieldDefinition">
        ///     The isDisposed field.
        /// </param>
        /// <param name="objectDisposedExceptionReference">
        ///     The constructor reference of the class <see cref="ObjectDisposedException" />.
        /// </param>
        /// <returns>
        ///     A collection of <see cref="Instruction" />.
        /// </returns>
        public static IEnumerable<Instruction> GetGuardInstructions(ILProcessor ilProcessor,
                                                                    MemberReference memberReference,
                                                                    FieldReference disposeFieldDefinition,
                                                                    MethodReference objectDisposedExceptionReference)
        {
            var normalWay = ilProcessor.Body.Instructions.FirstOrDefault() ?? ilProcessor.Create(OpCodes.Ret);

            yield return ilProcessor.Create(OpCodes.Ldarg_0);
            yield return ilProcessor.Create(OpCodes.Ldfld, disposeFieldDefinition);
            yield return ilProcessor.Create(OpCodes.Brfalse_S, normalWay);

            yield return ilProcessor.Create(OpCodes.Ldstr, memberReference.Name);
            yield return ilProcessor.Create(OpCodes.Newobj, objectDisposedExceptionReference);
            yield return ilProcessor.Create(OpCodes.Throw);
        }

        /// <summary>
        ///     Returns instructions for calling the base method.
        /// </summary>
        /// <param name="ilProcessor">
        ///     Service for managing instructions of a method.
        /// </param>
        /// <param name="baseMethodReference">
        ///     The base <see cref="MethodReference"/> that will be called in the override method.
        /// </param>
        /// <returns>
        ///     A collection of <see cref="Instruction" />.
        /// </returns>
        public static IEnumerable<Instruction> GetDefaultOverrideMethodInstructions(ILProcessor ilProcessor,
                                                                                    MethodReference baseMethodReference)
        {
            yield return ilProcessor.Create(OpCodes.Ldarg_0);
            yield return ilProcessor.Create(OpCodes.Call, baseMethodReference);
            yield return ilProcessor.Create(OpCodes.Ret);
        }
    }
}