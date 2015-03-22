namespace AssemblyToProcess
{
    public class DisposableWithAnotherInterfaceDisposable : IAnInterface
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