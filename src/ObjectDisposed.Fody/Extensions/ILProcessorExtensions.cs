namespace ObjectDisposedFodyAddin.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mono.Cecil.Cil;

    /// <summary>
    ///     Contains extension methods for the any <see cref="ILProcessor" />.
    /// </summary>
    public static class IlProcessorExtensions
    {
        /// <summary>
        ///     Appends <paramref name="instructions" /> after the last existing <see cref="Instruction" />.
        /// </summary>
        /// <param name="ilProcessor">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <param name="instructions">
        ///     <see cref="Instructions" /> that will be inserted at the start of the method.
        /// </param>
        public static void AppendRange(this ILProcessor ilProcessor,
                                       IEnumerable<Instruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                ilProcessor.Append(instruction);
            }
        }

        /// <summary>
        ///     Inserts <paramref name="instructions" /> after the <paramref name="afterInstruction" />.
        /// </summary>
        /// <param name="ilProcessor">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <param name="afterInstruction">
        ///     The <see cref="Instruction" /> used as reference to insert the new <paramref name="instructions" />.
        /// </param>
        /// <param name="instructions">
        ///     The <see cref="Instruction" />s what will be injected after the <paramref name="afterInstruction" />.
        /// </param>
        public static void InsertAfterRange(this ILProcessor ilProcessor,
                                            Instruction afterInstruction,
                                            IEnumerable<Instruction> instructions)
        {
            foreach (var instruction in instructions.Reverse())
            {
                ilProcessor.InsertAfter(afterInstruction, instruction);
            }
        }

        /// <summary>
        ///     Inserts <paramref name="instructions" /> before the <paramref name="beforeInstruction" />.
        /// </summary>
        /// <param name="ilProcessor">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <param name="beforeInstruction">
        ///     Inserts <paramref name="instructions" /> before this one.
        /// </param>
        /// <param name="instructions">
        ///     <see cref="Instructions" /> that will be inserted.
        /// </param>
        public static void InsertBeforeRange(this ILProcessor ilProcessor,
                                             Instruction beforeInstruction,
                                             IEnumerable<Instruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                ilProcessor.InsertBefore(beforeInstruction, instruction);
            }
        }

        /// <summary>
        ///     Retrieves the latest <see cref="Instruction" /> that represents a variable.
        /// </summary>
        /// <param name="ilProcessor">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <returns>
        ///     The <see cref="Instruction" /> found otherwise null.
        /// </returns>
        public static Instruction GetCallTaskInstruction(this ILProcessor ilProcessor)
        {
            var reversedInstruction = ilProcessor.Body.Instructions.Reverse();
            var callInstruction = reversedInstruction.FirstOrDefault(x => x.OpCode.Code == Code.Call);

            return callInstruction;
        }

        /// <summary>
        ///     Returns the latest instruction representing a variable.
        /// </summary>
        /// <param name="ilProcessor">
        ///     The <see cref="Type" /> that we want to extend.
        /// </param>
        /// <returns>
        ///     The latest instruction representing a variable otherwise null.
        /// </returns>
        public static Instruction GetNextVariableInstruction(this ILProcessor ilProcessor)
        {
            return ilProcessor.Body.Instructions
                              .LastOrDefault(x => x.OpCode.Name.StartsWith("ldlocl"));
        }

        public static Instruction GetReturnedVariableInstruction(this ILProcessor ilProcessor)
        {
            var reversedInstruction = ilProcessor.Body.Instructions.Reverse();
            var returnedVariableInstruction = reversedInstruction.FirstOrDefault(x => x.OpCode.Name.StartsWith("ldloc"));

            return returnedVariableInstruction;
        }

        public static Instruction GetReturnInstruction(this ILProcessor ilProcessor)
        {
            return ilProcessor.Body.Instructions.Reverse().First(x => x.OpCode.Code == Code.Ret);
        }
    }
}