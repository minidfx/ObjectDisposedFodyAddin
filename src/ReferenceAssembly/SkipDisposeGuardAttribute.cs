namespace ObjectDisposedFodyAddin
{
    using System;

    /// <summary>
    ///     Indicates whether the process for injecting the guard must skip a type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SkipDisposeGuardAttribute : Attribute
    {
    }
}