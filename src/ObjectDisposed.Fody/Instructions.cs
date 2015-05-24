namespace ObjectDisposedFodyAddin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    ///     Contains instructions for injection purposes into any dispose methods.
    /// </summary>
    public sealed class Instructions
    {
        /// <summary>
        ///     Returns instructions for injecting the guard into a method.
        /// </summary>
        /// <param name="ilProcessor">
        ///     Service for managing instructions of a method.
        /// </param>
        /// <param name="memberReference">
        ///     The reference of the member, which contains the method.
        /// </param>
        /// <param name="isDisposedPropertyDefinition">
        ///     The isDisposed field.
        /// </param>
        /// <param name="objectDisposedExceptionReference">
        ///     The constructor reference of the class <see cref="ObjectDisposedException" />.
        /// </param>
        /// <returns>
        ///     The <see cref="Instruction" />s yielded.
        /// </returns>
        public static IEnumerable<Instruction> GetGuardInstructions(ILProcessor ilProcessor,
                                                                    MemberReference memberReference,
                                                                    PropertyDefinition isDisposedPropertyDefinition,
                                                                    MethodReference objectDisposedExceptionReference)
        {
            var normalWay = ilProcessor.Body.Instructions.FirstOrDefault() ?? ilProcessor.Create(OpCodes.Ret);

            // Push the instance on the stack
            yield return ilProcessor.Create(OpCodes.Ldarg_0);

            // Call the parent method
            yield return ilProcessor.Create(OpCodes.Call, isDisposedPropertyDefinition.GetMethod);

            // Branch to the normal way whether the value on the stack is equals to 0
            yield return ilProcessor.Create(OpCodes.Brfalse_S, normalWay);

            // Push the class name on the stack
            yield return ilProcessor.Create(OpCodes.Ldstr, memberReference.Name);

            // Call the constructor with the previous string pushed
            yield return ilProcessor.Create(OpCodes.Newobj, objectDisposedExceptionReference);

            // Thrown the exception pushed
            yield return ilProcessor.Create(OpCodes.Throw);
        }

        public static IEnumerable<Instruction> GetIsDisposedInstructionsGetter(ILProcessor ilProcessor,
                                                                               PropertyDefinition basePropertyDefinition,
                                                                               FieldReference disposeFieldReference)
        {
            yield return ilProcessor.Create(OpCodes.Ldarg_0); // Load the field isDisposed
            yield return ilProcessor.Create(OpCodes.Ldfld, disposeFieldReference); // Push the field value on the stack

            if (basePropertyDefinition != null)
            {
                var push0OnStack = ilProcessor.Create(OpCodes.Ldc_I4_0);

                // Branch to the instruction whether the value on the stack is equals to 0
                yield return ilProcessor.Create(OpCodes.Brfalse_S, push0OnStack);

                // Call the base property
                yield return ilProcessor.Create(OpCodes.Ldarg_0);
                yield return ilProcessor.Create(OpCodes.Call, basePropertyDefinition.GetMethod);

                // Exit the method whether the base property has been called.
                yield return ilProcessor.Create(OpCodes.Ret);

                // Push the field value on the stack
                yield return push0OnStack;
            }

            yield return ilProcessor.Create(OpCodes.Ret);
        }

        /// <summary>
        ///     Yields partial <see cref="Instruction" />s to set to disposed the backing field.
        /// </summary>
        /// <param name="ilProcessor">
        ///     Service for managing instructions of a method.
        /// </param>
        /// <param name="fieldReference">
        ///     The backing field containing the state of the object.
        /// </param>
        /// <returns>
        ///     The <see cref="Instruction" />s yielded.
        /// </returns>
        public static IEnumerable<Instruction> GetDisposeMethodPartialInstructions(ILProcessor ilProcessor,
                                                                                   FieldReference fieldReference)
        {
            // Push the instance on the stack
            yield return ilProcessor.Create(OpCodes.Ldarg_0);

            // Push 1 on the stack
            yield return ilProcessor.Create(OpCodes.Ldc_I4_1);

            // Set the field with the previous value pushed on the stack
            yield return ilProcessor.Create(OpCodes.Stfld, fieldReference);
        }

        public static IEnumerable<Instruction> GetDisposeMethodFullInstructions(ILProcessor ilProcessor,
                                                                                FieldReference fieldReference)
        {
            foreach (var instruction in GetDisposeMethodPartialInstructions(ilProcessor, fieldReference))
            {
                yield return instruction;
            }

            yield return ilProcessor.Create(OpCodes.Ret);
        }

        /// <summary>
        ///     Yields <see cref="Instruction" /> to call the <see cref="MethodReference" /> <paramref name="methodReference" />.
        /// </summary>
        /// <param name="ilProcessor">
        ///     Service for managing instructions of a method.
        /// </param>
        /// <param name="methodReference">
        ///     The method that will be called.
        /// </param>
        /// <param name="latestVariableInstruction">
        ///     The <see cref="Instruction" /> representing the latest variable of the method.
        /// </param>
        /// <returns>
        ///     The <see cref="Instruction" />s yielded.
        /// </returns>
        public static IEnumerable<Instruction> GetCallMethodInstruction(ILProcessor ilProcessor,
                                                                        MethodReference methodReference,
                                                                        Instruction latestVariableInstruction)
        {
            yield return ilProcessor.Create(OpCodes.Ldarg_0);
            yield return ilProcessor.Create(latestVariableInstruction.OpCode);
            yield return ilProcessor.Create(OpCodes.Call, methodReference);
        }

        /// <summary>
        ///     Yields instructions for calling the <see cref="Task.ContinueWith(System.Action{System.Threading.Tasks.Task})" /> of
        ///     a <see cref="Task" /> and set
        ///     to True the local dispose field.
        /// </summary>
        /// <param name="ilProcessor">
        ///     Service for managing instructions of a method.
        /// </param>
        /// <param name="taskContinueWithMethodReference">
        ///     The <see cref="MethodReference" /> of the
        ///     <see cref="Task.ContinueWith(System.Action{System.Threading.Tasks.Task})" /> method.
        /// </param>
        /// <param name="actionConstructionReference">
        ///     The <see cref="MethodReference" /> the <see cref="Action" /> constructor.
        /// </param>
        /// <param name="lambdaActionReference">
        ///     The <see cref="MethodReference" /> of the local method to set to <see langword="True" /> the dispose field.
        /// </param>
        /// <returns>
        ///     The <see cref="Instruction" />s yielded.
        /// </returns>
        public static IEnumerable<Instruction> GetDisposeAsyncMethodInstructions(ILProcessor ilProcessor,
                                                                                 MethodReference taskContinueWithMethodReference,
                                                                                 MethodReference actionConstructionReference,
                                                                                 MethodReference lambdaActionReference)
        {
            yield return ilProcessor.Create(OpCodes.Ldarg_1);
            yield return ilProcessor.Create(OpCodes.Ldarg_0);
            yield return ilProcessor.Create(OpCodes.Ldftn, lambdaActionReference);
            yield return ilProcessor.Create(OpCodes.Newobj, actionConstructionReference);
            yield return ilProcessor.Create(OpCodes.Callvirt, taskContinueWithMethodReference);
            yield return ilProcessor.Create(OpCodes.Ret);
        }
    }
}