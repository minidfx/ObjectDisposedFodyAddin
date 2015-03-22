namespace AssemblyToProcessWithInvalidTyp3
{
    using System;

    public abstract class DisposableBaseWithoutVirtualKeyword : IDisposable
    {
        public void Dispose()
        {
        }
    }
}