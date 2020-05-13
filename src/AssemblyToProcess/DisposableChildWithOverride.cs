namespace AssemblyToProcess
{
    public class DisposableChildWithOverride : DisposableBase
    {
        public override void Dispose()
        {
            base.Dispose();
        }

        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }
    }
}