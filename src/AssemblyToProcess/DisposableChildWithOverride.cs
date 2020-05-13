namespace AssemblyToProcess
{
    public class DisposableChildWithOverride : DisposableBase
    {
        public override void Dispose()
        {
        }

        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }
    }
}