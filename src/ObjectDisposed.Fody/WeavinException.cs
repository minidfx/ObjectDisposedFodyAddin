namespace ObjectDisposedFodyAddin
{
    using System;
    using System.Reflection;

    /// <summary>
    ///     Thrown when an error occured while the injected in the <see cref="Assembly" />.
    /// </summary>
    public class WeavinException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="WeavinException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public WeavinException(string message)
            : base(message)
        {
        }
    }
}