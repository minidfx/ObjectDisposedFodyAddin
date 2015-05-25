namespace AssemblyToProcess
{
    using System.Threading.Tasks;

    public class AsyncDisposableWithDelay : AsyncDisposableBase
    {
        public override async Task DisposeAsync()
        {
            await base.DisposeAsync();
            await Task.Delay(100);
        }

        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }

        public string SayMeHello()
        {
            return "Hello";
        }
    }
}