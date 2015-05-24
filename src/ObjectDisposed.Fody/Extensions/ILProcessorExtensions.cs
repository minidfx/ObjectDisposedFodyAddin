namespace ObjectDisposedFodyAddin.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mono.Cecil.Cil;

    public static class ILProcessorExtensions
    {
        /// <summary>
        ///     Inserts <paramref name="instructions" /> at the start of the method.
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

        public static void RemoveAfter(this ILProcessor ilProcessor,
                                       Instruction removeAfter)
        {
            var hasToRemove = false;
            var instructions = ilProcessor.Body.Instructions.ToArray();

            foreach (var instruction in instructions)
            {
                if (hasToRemove)
                {
                    ilProcessor.Body.Instructions.Remove(instruction);
                }

                if (instruction == removeAfter)
                {
                    hasToRemove = true;
                }
            }
        }

        public static Instruction GetLatestVariableInstruction(this ILProcessor ilProcessor)
        {
            var reversedInstruction = ilProcessor.Body.Instructions.Reverse();

            var latestVariableInstruction = reversedInstruction.First(x => x.OpCode.Name.StartsWith("ldloc"));

            return latestVariableInstruction;
        }
    }
}