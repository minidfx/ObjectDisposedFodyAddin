namespace AssemblyToProcess
{
    using System.Threading.Tasks;

    public interface IAsyncDisposable
    {
        Task DisposeAsync();
    }
}