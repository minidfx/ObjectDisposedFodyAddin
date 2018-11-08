namespace ObjectDisposedFodyAddin.ReferenceAssembly
{
    using JetBrains.Annotations;
    using System;

    /// <summary>
    ///     Indicates whether the process for injecting the guard must be skipped.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [PublicAPI]
    public sealed class SkipDisposeGuardAttribute : Attribute { }
}