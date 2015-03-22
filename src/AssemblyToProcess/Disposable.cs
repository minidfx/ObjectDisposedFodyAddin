namespace AssemblyToProcess
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable()
        {
            this.APublicText = "Hello World!";
        }

        public string APublicText { get; set; }

        protected string AProtectedText { get; set; }

        protected string APrivateText { get; set; }

        public void Dispose()
        {
        }

        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }

        public void DoSomething()
        {
            Console.WriteLine("Hello World!");
        }

        public void DoNothing()
        {
        }
    }
}