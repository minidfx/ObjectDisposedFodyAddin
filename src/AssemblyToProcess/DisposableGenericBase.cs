namespace AssemblyToProcess
{
    using System;

    public abstract class DisposableGenericBase<T> : IDisposable
    {
        public void Dispose()
        {
        }
        
        public void DoSomething()
        {
            Console.WriteLine("Yuhuu!");
        }
    }
}