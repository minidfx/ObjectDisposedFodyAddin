namespace AssemblyToProcess
{
    using AssemblyToProcessExternalDependencies;

    public class AChildClass : ABaseClass
    {
        public string SayMeHelloWorld()
        {
            return "I cannot tell you HelloWorld !";
        }

        public override void Dispose()
        {
        }
    }
}