namespace AssemblyToProcess
{
    public class DiposableChildWithOverride : DisposableBase
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