namespace ObjectDisposedFodyAddin
{
    using System;
    using System.Reflection;

    /// <summary>
    ///     Thrown when an error occured while the injected in the <see cref="Assembly" />.
    /// </summary>
    public class WeavingException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="WeavingException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The error code of the <see cref="WeavingException" />.</param>
        public WeavingException(string message,
                               WeavingErrorCodes errorCode)
            : base(message)
        {
            this.ErrorCode = errorCode;
        }

        /// <summary>
        ///     Returns the error code of the <see cref="WeavingException" />.
        /// </summary>
        public WeavingErrorCodes ErrorCode { get; private set; }
    }
}