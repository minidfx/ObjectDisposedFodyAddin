﻿namespace AssemblyToProcess
{
    using System;

    using ObjectDisposedFodyAddin;

    [SkipDisposeGuard]
    public class DisposableWithoutGuard : IDisposable
    {
        public void Dispose()
        {
        }

        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }
    }
}