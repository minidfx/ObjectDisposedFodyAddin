namespace AssemblyToProcess
{
    using System;

    public abstract class DisposableGenericBase<T> : IDisposable
    {
        public virtual void Dispose()
        {
        }
    }
}