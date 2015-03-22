namespace AssemblyToProcessWithInvalidType3
{
    using System;

    public abstract class DisposableBaseWithoutVirtualKeyword : IDisposable
    {
        public void Dispose()
        {
        }
    }
}