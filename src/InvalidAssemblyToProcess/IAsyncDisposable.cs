namespace InvalidAssemblyToProcess
{
    using System.Threading.Tasks;

    public interface IAsyncDisposable
    {
        Task DisposeAsync();
    }
}