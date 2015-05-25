namespace AssemblyToProcess
{
    using System.Threading.Tasks;

    public class AsyncDisposableWithMultiTasks : AsyncDisposableBase
    {
        private bool field1 = true;
        private bool field2 = true;

        public override Task DisposeAsync()
        {
            if (this.field1)
            {
                return base.DisposeAsync();
            }

            if (this.field2)
            {
                return Task.FromResult(0);
            }

            return Task.FromResult(1);
        }
    }
}