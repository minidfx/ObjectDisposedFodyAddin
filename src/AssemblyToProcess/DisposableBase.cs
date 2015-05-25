namespace AssemblyToProcess
{
    using System;

    public abstract class DisposableBase : IDisposable
    {
        public virtual void Dispose()
        {
        }

        public void DoSomething()
        {
            Console.WriteLine("Yuhuu!");
        }
    }
}