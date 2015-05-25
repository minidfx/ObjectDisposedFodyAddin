namespace AssemblyToProcess
{
    using System;
    using System.Threading.Tasks;

    public abstract class AsyncDisposableBase : IAsyncDisposable
    {
        public virtual Task DisposeAsync()
        {
            return Task.FromResult(0);
        }

        public virtual void DoSomething()
        {
            Console.WriteLine("Yuhuu!");
        }

        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }
    }
}