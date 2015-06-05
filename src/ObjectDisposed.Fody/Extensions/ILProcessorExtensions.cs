namespace ObjectDisposedFodyAddin.Extensions
{
    using System;
    using System.Collections.Generic;

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
    }
}