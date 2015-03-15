namespace AssemblyToProcess
{
    using System;

    public class Diposable : IDisposable
    {
        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        ///     Returns something.
        /// </summary>
        /// <returns>
        ///     Hello World!
        /// </returns>
        public string SayMeSomething()
        {
            return "Hello World!";
        }
    }
}