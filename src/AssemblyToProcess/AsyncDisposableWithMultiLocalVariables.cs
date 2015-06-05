namespace AssemblyToProcess
{
    using System.Threading.Tasks;

    public class AsyncDisposableWithMultiLocalVariables : AsyncDisposableBase
    {
        public override Task DisposeAsync()
        {
            var var1 = "fdsfdsfd";
            var var2 = "3432432";
            var var3 = "fdsgfd4";
            var var4 = Task.FromResult(1);

            if (var1 == var2)
            {
                return var4;
            }

            if (var2 == var3)
            {
                return Task.FromResult(3);
            }

            return Task.FromResult(0);
        }
    }
}