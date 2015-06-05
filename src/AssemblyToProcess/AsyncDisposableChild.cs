namespace AssemblyToProcess
{
    public sealed class AsyncDisposableChild : AsyncDisposableBase
    {
        public override string SayMeHelloWorld()
        {
            return "Hello World!";
        }
    }
}