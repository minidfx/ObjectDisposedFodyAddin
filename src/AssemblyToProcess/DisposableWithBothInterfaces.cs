namespace AssemblyToProcess
{
    using System;
    using System.Threading.Tasks;

    public class DisposableWithBothInterfaces : IDisposable,
                                                IAsyncDisposable
    {
        public Task DisposeAsync()
        {
            return Task.FromResult(0);
        }

        public void Dispose()
        {
        }
    }
}