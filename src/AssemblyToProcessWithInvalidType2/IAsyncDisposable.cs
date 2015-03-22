namespace AssemblyToProcessWithInvalidType2
{
    using System.Threading.Tasks;

    public interface IAsyncDisposable
    {
        Task DisposeAsync();
    }
}