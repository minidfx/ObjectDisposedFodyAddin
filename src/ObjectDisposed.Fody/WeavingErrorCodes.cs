namespace ObjectDisposed.Fody
{
    /// <summary>
    ///     Weawing error codes.
    /// </summary>
    public enum WeavingErrorCodes : uint
    {
        /// <summary>
        ///     Unknown error.
        /// </summary>
        None,

        /// <summary>
        ///     Contains already an isDisposed field.
        /// </summary>
        ContainsIsDisposedField,

        /// <summary>
        ///     The type contains the IDisposable and the IAsyncDisposable.
        /// </summary>
        ContainsBothInterface,

        /// <summary>
        ///     The property IsDisposed has been not found.
        /// </summary>
        PropertyNotFound
    }
}