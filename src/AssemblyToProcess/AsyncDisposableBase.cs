namespace AssemblyToProcess
{
    using System.Threading.Tasks;

    public abstract class AsyncDisposableBase : IAsyncDisposable
    {
        public virtual Task DisposeAsync()
        {
            return Task.FromResult(0);
        }
    }
}