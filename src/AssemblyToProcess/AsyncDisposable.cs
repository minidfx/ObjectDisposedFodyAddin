namespace AssemblyToProcess
{
    using System.Threading.Tasks;

    public class AsyncDisposable : IAsyncDisposable
    {
        public Task DisposeAsync()
        {
            return Task.FromResult(0);
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

        public Task DoSomethingAsync()
        {
            return Task.FromResult(0);
        }
    }
}