namespace Tests
{
    using System;
    using System.IO;
    using System.Reflection;

    using Mono.Cecil;

    using NUnit.Framework;

    using ObjectDisposedFodyAddin;

    [TestFixture]
    public abstract class WeaverTests
    {
        private static Assembly newAssembly;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WeaverTests" /> class.
        /// </summary>
        protected WeaverTests()
        {
            if (newAssembly == null)
            {
                var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));

                var directoryName = Path.GetDirectoryName(projectPath);
                if (directoryName == null)
                {
                    throw new IOException("Cannot determines the project directory.");
                }

#if DEBUG
                var assemblyPath = Path.Combine(directoryName, @"bin\Debug\AssemblyToProcess.dll");
#else
            var assemblyPath = Path.Combine(directoryName, @"bin\Release\AssemblyToProcess.dll");
#endif

                var newAssemblyPath = assemblyPath.Replace(".dll", ".modified.dll");

                if (File.Exists(newAssemblyPath))
                {
                    File.Delete(newAssemblyPath);
                }

                var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath);
                var weavingTask = new ModuleWeaver
                                      {
                                          ModuleDefinition = moduleDefinition
                                      };
                weavingTask.Execute();
                moduleDefinition.Write(newAssemblyPath);

                // Verify that the assembly is correctly before running tests.
                Verifier.Verify(assemblyPath, newAssemblyPath);

                newAssembly = Assembly.LoadFile(newAssemblyPath);
            }
        }

        protected dynamic Instance { get; private set; }

        protected abstract dynamic GetInstance();

        [SetUp]
        public virtual void SetUp()
        {
            this.Instance = this.GetInstance();
        }

        protected dynamic CreateInstance(string className)
        {
            return Activator.CreateInstance(newAssembly.GetType(className, true));
        }

        /// <summary>
        ///     Contains unit tests for the <see cref="AsyncDisposable" /> class.
        /// </summary>
        public abstract class WithAsyncDisposableClass : WeaverTests
        {
            protected override dynamic GetInstance()
            {
                return this.CreateInstance("AssemblyToProcess.AsyncDisposable");
            }

            [Test]
            public void HasIsDisposedField()
            {
                var isDisposedField = this.Instance.GetType().GetField("isDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(isDisposedField);
            }

            /// <summary>
            ///     Unit tests when the AsyncDispose method has not been called.
            /// </summary>
            public sealed class WithNoCallToDispose : WithAsyncDisposableClass
            {
                [Test]
                public void CallSayMeHelloWorld()
                {
                    var result = this.Instance.SayMeHelloWorld();
                    Assert.AreEqual("Hello World!", result);
                }
            }

            /// <summary>
            ///     Unit tests when the AsyncDispose method has been called.
            /// </summary>
            public sealed class WithCallToDispose : WithAsyncDisposableClass
            {
                [SetUp]
                public override void SetUp()
                {
                    base.SetUp();

                    this.Instance.DisposeAsync().Wait();
                }

                [Test]
                [ExpectedException(typeof(ObjectDisposedException))]
                public void CallDoSomethingAsync()
                {
                    this.Instance.DoSomethingAsync().Wait();
                }
            }
        }

        /// <summary>
        ///     Contains unit tests for the <see cref="Disposable" /> class.
        /// </summary>
        public abstract class WithDisposableClass : WeaverTests
        {
            protected override dynamic GetInstance()
            {
                return this.CreateInstance("AssemblyToProcess.Disposable");
            }

            [Test]
            public void HasIsDisposedField()
            {
                var isDisposedField = this.Instance.GetType().GetField("isDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(isDisposedField);
            }

            /// <summary>
            ///     Unit tests when the Dispose method has not been called.
            /// </summary>
            public sealed class WithNoCallToDispose : WithDisposableClass
            {
                [Test]
                public void CallSayMeHelloWorld()
                {
                    var result = this.Instance.SayMeHelloWorld();
                    Assert.AreEqual("Hello World!", result);
                }

                [Test]
                public void AccessToAPublicText()
                {
                    var result = this.Instance.APublicText;
                    Assert.AreEqual("Hello World!", result);
                }
            }

            /// <summary>
            ///     Unit tests when the Dispose method has been called.
            /// </summary>
            public sealed class WithCallToDispose : WithDisposableClass
            {
                [SetUp]
                public override void SetUp()
                {
                    base.SetUp();

                    this.Instance.Dispose();
                }

                [Test]
                [ExpectedException(typeof(ObjectDisposedException))]
                public void CallDoSomething()
                {
                    this.Instance.DoSomething();
                }

                [Test]
                [ExpectedException(typeof(ObjectDisposedException))]
                public void AccessToAPublicText()
                {
                    var result = this.Instance.APublicText;
                }
            }
        }
    }
}