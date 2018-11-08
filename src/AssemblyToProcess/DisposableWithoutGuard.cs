using ObjectDisposedFodyAddin.ReferenceAssembly;

namespace AssemblyToProcess
{
    using System;

    [SkipDisposeGuard]
    public class DisposableWithoutGuard : IDisposable
    {
        public void Dispose()
        {
        }

        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }
    }
}