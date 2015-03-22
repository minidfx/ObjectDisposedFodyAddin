namespace AssemblyToProcessWithInvalidType
{
    using System;

    public class DisposableWithIsDisposedMember : IDisposable
    {
        protected object isDisposed;

        public void Dispose()
        {
        }
    }
}