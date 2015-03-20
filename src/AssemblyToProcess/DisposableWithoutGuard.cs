namespace AssemblyToProcess
{
    using System;

    using ObjectDisposedFodyAddin;

    [SkipDisposeGuard]
    public class DisposableWithoutGuard : IDisposable
    {
        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }
    }
}