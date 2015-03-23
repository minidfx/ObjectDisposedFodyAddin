namespace AssemblyToProcess
{
    using System.Threading.Tasks;

    public class AsyncDisposableWithAwait : IAsyncDisposable
    {
        public async Task DisposeAsync()
        {
            await Task.FromResult(0);
        }

        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }
    }
}