namespace AssemblyToProcess
{
    using System;

    public class Disposable : IDisposable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        public Disposable()
        {
            this.APublicText = "Hello World!";
        }

        public string APublicText { get; set; }

        protected string AProtectedText { get; set; }

        protected string APrivateText { get; set; }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        ///     Returns a <see cref="String" /> with 'Hello World!' as result.
        /// </summary>
        /// <returns>
        ///     Hello World!
        /// </returns>
        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }

        /// <summary>
        ///     Writes 'Hello World!' to the console.
        /// </summary>
        public void DoSomething()
        {
            Console.WriteLine("Hello World!");
        }

        /// <summary>
        ///     Does nothing and returns nothing.
        /// </summary>
        public void DoNothing()
        {
        }
    }
}