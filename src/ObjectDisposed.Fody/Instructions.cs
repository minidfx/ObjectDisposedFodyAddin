using System.Threading.Tasks;

namespace ObjectDisposed.Fody
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
        ///     Returns instructions for injecting the guard into a method.
        /// </summary>
        /// <param name="ilProcessor">
        ///     Service for managing instructions of a method.
        /// </param>
        /// <param name="typeDefinition">
        ///     The reference of the member, which contains the method.
        /// </param>
        /// <param name="isDisposedPropertyReference">
        ///     The isDisposed field.
        /// </param>
        /// <param name="objectDisposedExceptionReference">
        ///     The constructor reference of the class <see cref="ObjectDisposedException" />.
        /// </param>
        /// <returns>
        ///     The <see cref="Instruction" />s yielded.
        /// </returns>
        public static IEnumerable<Instruction> GetGuardInstructions(ILProcessor ilProcessor,
                                                                    TypeDefinition typeDefinition,
                                                                    MethodReference isDisposedPropertyReference,
                                                                    MethodReference objectDisposedExceptionReference)
        {
            var normalWay = ilProcessor.Body.Instructions.FirstOrDefault() ?? ilProcessor.Create(OpCodes.Ret);

            // Push the instance on the stack
            yield return ilProcessor.Create(OpCodes.Ldarg_0);

            // Call the parent method
            if (typeDefinition.IsAbstract)
            {
                yield return ilProcessor.Create(OpCodes.Callvirt, isDisposedPropertyReference);    
            }
            else
            {
                yield return ilProcessor.Create(OpCodes.Call, isDisposedPropertyReference);
            }

            // Branch to the normal way whether the value on the stack is equals to 0
            yield return ilProcessor.Create(OpCodes.Brfalse_S, normalWay);

            // Push the class name on the stack
            yield return ilProcessor.Create(OpCodes.Ldstr, typeDefinition.Name);

            // Call the constructor with the previous string pushed
            yield return ilProcessor.Create(OpCodes.Newobj, objectDisposedExceptionReference);

            // Thrown the exception pushed
            yield return ilProcessor.Create(OpCodes.Throw);
        }

        /// <summary>
        ///     Yields instructions for a getter property.
        /// </summary>
        /// <param name="ilProcessor">
        ///     Service for managing instructions of a method.
        /// </param>
        /// <param name="basePropertyGetterReference">
        ///     <see cref="MethodReference" /> of the getter property.
        /// </param>
        /// <param name="backingFieldReference">
        ///     The backing field containing the value whether the object is disposed or not.
        /// </param>
        /// <returns>
        ///     The <see cref="Instruction" />s yielded for a getter property.
        /// </returns>
        public static IEnumerable<Instruction> GetIsDisposedInstructionsGetter(ILProcessor ilProcessor,
                                                                               MethodReference basePropertyGetterReference,
                                                                               FieldReference backingFieldReference)
        {
            if (backingFieldReference != null)
            {
                yield return ilProcessor.Create(OpCodes.Ldarg_0); // Load the field isDisposed
                yield return ilProcessor.Create(OpCodes.Ldfld, backingFieldReference); // Push the field value on the stack
            }

            if (basePropertyGetterReference != null)
            {
                Instruction push0OnStack = null;

                if (backingFieldReference != null)
                {
                    push0OnStack = ilProcessor.Create(OpCodes.Ldc_I4_0);

                    // Branch to the instruction whether the value on the stack is equals to 0
                    yield return ilProcessor.Create(OpCodes.Brfalse_S, push0OnStack);
                }

                // Call the base property
                yield return ilProcessor.Create(OpCodes.Ldarg_0);
                yield return ilProcessor.Create(OpCodes.Call, basePropertyGetterReference);

                // Exit the method whether the base property has been called.
                yield return ilProcessor.Create(OpCodes.Ret);

                if (push0OnStack != null)
                {
                    // Push the field value on the stack
                    yield return push0OnStack;
                }
            }

            if (backingFieldReference != null)
            {
                yield return ilProcessor.Create(OpCodes.Ret);
            }
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
        public static IEnumerable<Instruction> GetSetToDisposedMethodPartialInstructions(ILProcessor ilProcessor,
                                                                                   FieldReference fieldReference)
        {
            // Push the instance on the stack
            yield return ilProcessor.Create(OpCodes.Ldarg_0);

            // Push 1 on the stack
            yield return ilProcessor.Create(OpCodes.Ldc_I4_1);

            // Set the field with the previous value pushed on the stack
            yield return ilProcessor.Create(OpCodes.Stfld, fieldReference);
        }

        /// <summary>
        ///     Yields <see cref="Instruction" />s to set the backing field to <see langword="True" />.
        /// </summary>
        /// <param name="ilProcessor">
        ///     Service for managing instructions of a method.
        /// </param>
        /// <param name="fieldReference">
        ///     The <see cref="FieldReference" /> of the backing field.
        /// </param>
        /// <returns>
        ///     The <see cref="Instruction" />s yielded.
        /// </returns>
        public static IEnumerable<Instruction> GetSetToDisposeFullInstructions(ILProcessor ilProcessor,
                                                                               FieldReference fieldReference)
        {
            foreach (var instruction in GetSetToDisposedMethodPartialInstructions(ilProcessor, fieldReference))
            {
                yield return instruction;
            }

            yield return ilProcessor.Create(OpCodes.Ret);
        }

        /// <summary>
        ///     Yields <see cref="Instruction" />s for calling the
        ///     <see cref="Task.ContinueWith(System.Action{System.Threading.Tasks.Task})" /> and returns the <see cref="Task" /> of
        ///     the <see cref="Task.ContinueWith(System.Action{System.Threading.Tasks.Task})" /> method.
        /// </summary>
        /// <param name="ilProcessor">
        ///     Service for managing instructions of a method.
        /// </param>
        /// <param name="taskContinueWithMethodReference">
        ///     The <see cref="MethodReference" /> of the
        ///     <see cref="Task.ContinueWith(System.Action{System.Threading.Tasks.Task})" /> method.
        /// </param>
        /// <param name="taskTypeReference">
        ///     The <see cref="TypeReference" /> of a <see cref="Task" />.
        /// </param>
        /// <returns>
        ///     The <see cref="Instruction" />s yielded.
        /// </returns>
        public static IEnumerable<Instruction> GetPartialContinueWithInstructions(ILProcessor ilProcessor,
                                                                                  MethodReference taskContinueWithMethodReference,
                                                                                  TypeReference taskTypeReference)
        {
            var variable = new VariableDefinition(taskTypeReference);
            ilProcessor.Body.Variables.Add(variable);
            ilProcessor.Body.InitLocals = true;

            yield return ilProcessor.Create(OpCodes.Stloc, variable);
            yield return ilProcessor.Create(OpCodes.Ldarg_0);
            yield return ilProcessor.Create(OpCodes.Ldloc, variable);
            yield return ilProcessor.Create(OpCodes.Call, taskContinueWithMethodReference);
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