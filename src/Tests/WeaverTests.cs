namespace Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using Mono.Cecil;

    using NUnit.Framework;

    using ObjectDisposedFodyAddin;

    [TestFixture]
    public class WeaverTests
    {
        private string assemblyPath;

        private string newAssemblyPath;

        [TestFixtureSetUp]
        public void SetUp()
        {
            var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));

            var directoryName = Path.GetDirectoryName(projectPath);
            if (directoryName == null)
            {
                throw new IOException("Cannot determines the project directory.");
            }

            this.assemblyPath = Path.Combine(directoryName, @"bin\Debug\AssemblyToProcess.dll");
#if (!DEBUG)
        assemblyPath = assemblyPath.Replace("Debug", "Release");
#endif

            this.newAssemblyPath = this.assemblyPath.Replace(".dll", ".modified.dll");
            if (File.Exists(this.newAssemblyPath))
            {
                File.Delete(this.newAssemblyPath);
            }

            File.Copy(this.assemblyPath, this.newAssemblyPath);

            var moduleDefinition = ModuleDefinition.ReadModule(this.newAssemblyPath);
            var weavingTask = new ModuleWeaver
                                  {
                                      ModuleDefinition = moduleDefinition
                                  };
            weavingTask.Execute();
            moduleDefinition.Write(this.newAssemblyPath);

            Assembly.LoadFrom(this.newAssemblyPath);

            Process.Start(@"C:\Users\MiniDfx\AppData\Local\JetBrains\Installations\dotPeek01\dotPeek64.exe", @"C:\Users\MiniDfx\Git\ObjectDisposedFodyAddin\src\AssemblyToProcess\bin\Debug\AssemblyToProcess.modified.dll");
        }

#if(DEBUG)
        [Test]
        public void PeVerify()
        {
            Verifier.Verify(this.assemblyPath, this.newAssemblyPath);
        }
#endif
    }
}