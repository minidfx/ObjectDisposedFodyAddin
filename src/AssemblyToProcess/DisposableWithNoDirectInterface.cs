namespace AssemblyToProcess
{
    public class DisposableWithNoDirectInterface : IAnInterface
    {
        public void Dispose()
        {
        }

        /// <summary>
        ///     Returns a <see cref="string" /> with 'Hello World!' as result.
        /// </summary>
        /// <returns>
        ///     Hello World!
        /// </returns>
        public string SayMeHelloWorld()
        {
            return "Hello World!";
        }
    }
}