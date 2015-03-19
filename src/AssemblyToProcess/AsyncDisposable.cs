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
        ///     Returns a <see cref="string" /> with 'Hello World!' as result.
        /// </summary>
        /// <returns>
        ///     Hello World!
        /// </returns>
        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }

        public Task DoSomethingAsync()
        {
            return Task.FromResult(0);
        }
    }
}