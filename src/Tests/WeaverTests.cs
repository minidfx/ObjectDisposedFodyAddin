
#pragma warning disable 1584,1711,1572,1581,1580

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

        protected dynamic Instance { get; private set; }

        protected void TryToLoadAssembly(string relativeProjectPath)
        {
            if (newAssembly == null)
            {
                var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, relativeProjectPath));
                var projectName = Path.GetFileNameWithoutExtension(relativeProjectPath);

                var directoryName = Path.GetDirectoryName(projectPath);
                if (directoryName == null)
                {
                    throw new IOException("Cannot determines the project directory.");
                }

#if DEBUG
                var assemblyPath = Path.Combine(directoryName, string.Format(@"bin\Debug\{0}.dll", projectName));
#else
                var assemblyPath = Path.Combine(directoryName, string.Format(@"bin\Release\{0}.dll", projectName));
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

        protected dynamic CreateInstance(string className)
        {
            return Activator.CreateInstance(newAssembly.GetType(className, true));
        }

        /// <summary>
        ///     Try to load an assembly, which contains type with the interface IDisposable and IAsyncDisposable.
        /// </summary>
        public sealed class WithBothInterfaces : WeaverTests
        {
            [Test]
            public void LoadAssemblyWithBothInterfaces()
            {
                var exception = Assert.Throws<WeavingException>(() => { this.TryToLoadAssembly(@"..\..\..\AssemblyToProcessWithInvalidType2\AssemblyToProcessWithInvalidType2.csproj"); });
                Assert.AreEqual(WeavingErrorCodes.ContainsBothInterface, exception.ErrorCode);
            }
        }

        /// <summary>
        ///     Try to load an assembly, which contains type with the interface IDisposable and IAsyncDisposable.
        /// </summary>
        public sealed class WithDisposableWithIsDisposedMember : WeaverTests
        {
            [Test]
            public void LoadAssemblyWithBothInterfaces()
            {
                var exception = Assert.Throws<WeavingException>(() => { this.TryToLoadAssembly(@"..\..\..\AssemblyToProcessWithInvalidType\AssemblyToProcessWithInvalidType.csproj"); });
                Assert.AreEqual(WeavingErrorCodes.NotUseable, exception.ErrorCode);
            }
        }

        /// <summary>
        ///     Try to load an assembly, which contains type with the interface IDisposable and IAsyncDisposable.
        /// </summary>
        public sealed class WithDisposableWithoutKeywordVirtualOnBaseClasses : WeaverTests
        {
            [Test]
            public void LoadAssemblyWithBothInterfaces()
            {
                var exception = Assert.Throws<WeavingException>(() => { this.TryToLoadAssembly(@"..\..\..\AssemblyToProcessWithInvalidType3\AssemblyToProcessWithInvalidType3.csproj"); });
                Assert.AreEqual(WeavingErrorCodes.MustHaveVirtualKeyword, exception.ErrorCode);
            }
        }

        /// <summary>
        ///     Load an assembly with valid rules.
        /// </summary>
        public abstract class WithValidAssembly : WeaverTests
        {
            protected abstract dynamic GetInstance();

            [SetUp]
            public virtual void SetUp()
            {
                this.Instance = this.GetInstance();
            }

            [TestFixtureSetUp]
            public void FixtureSetUp()
            {
                this.TryToLoadAssembly(@"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj");
            }

            public abstract class WithDisposableChild : WithValidAssembly
            {
                protected override dynamic GetInstance()
                {
                    return this.CreateInstance("AssemblyToProcess.DisposableChild");
                }

                [Test]
                public void HasIsDisposedField()
                {
                    var isDisposedField = this.Instance.GetType().GetField("isDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.IsNotNull(isDisposedField);
                }

                public sealed class WithCallToDispose : WithDisposableChild
                {
                    [SetUp]
                    public override void SetUp()
                    {
                        base.SetUp();

                        this.Instance.Dispose();
                    }

                    [Test]
                    [ExpectedException(typeof(ObjectDisposedException))]
                    public void CallSayMeHelloWorld()
                    {
                        var result = this.Instance.SayMeHelloWorld();
                        Assert.AreEqual("Hello World!", result);
                    }
                }
            }

            public abstract class WithAsyncDisposableChild : WithValidAssembly
            {
                protected override dynamic GetInstance()
                {
                    return this.CreateInstance("AssemblyToProcess.AsyncDisposableChild");
                }

                [Test]
                public void HasIsDisposedField()
                {
                    var isDisposedField = this.Instance.GetType().GetField("isDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.IsNotNull(isDisposedField);
                }

                public sealed class WithCallToDispose : WithAsyncDisposableChild
                {
                    [SetUp]
                    public override void SetUp()
                    {
                        base.SetUp();

                        this.Instance.DisposeAsync().Wait();
                    }

                    [Test]
                    [ExpectedException(typeof(ObjectDisposedException))]
                    public void CallSayMeHelloWorld()
                    {
                        var result = this.Instance.SayMeHelloWorld();
                        Assert.AreEqual("Hello World!", result);
                    }
                }
            }

            public abstract class WithAnotherInterfaceDisposable : WithValidAssembly
            {
                protected override dynamic GetInstance()
                {
                    return this.CreateInstance("AssemblyToProcess.DisposableWithAnotherInterfaceDisposable");
                }

                [Test]
                public void HasIsDisposedField()
                {
                    var isDisposedField = this.Instance.GetType().GetField("isDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.IsNotNull(isDisposedField);
                }

                public sealed class WithCallToDispose : WithAnotherInterfaceDisposable
                {
                    [SetUp]
                    public override void SetUp()
                    {
                        base.SetUp();

                        this.Instance.Dispose();
                    }

                    [Test]
                    [ExpectedException(typeof(ObjectDisposedException))]
                    public void CallSayMeHelloWorld()
                    {
                        var result = this.Instance.SayMeHelloWorld();
                        Assert.AreEqual("Hello World!", result);
                    }
                }
            }

            /// <summary>
            ///     Contains unit tests for the <see cref="AsyncDisposable" /> class.
            /// </summary>
            public abstract class WithAsyncDisposableClass : WithValidAssembly
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

            public abstract class WithDisposableWithoutGuardClass : WithValidAssembly
            {
                protected override dynamic GetInstance()
                {
                    return this.CreateInstance("AssemblyToProcess.DisposableWithoutGuard");
                }

                [Test]
                public void HasNotIsDisposedField()
                {
                    var isDisposedField = this.Instance.GetType().GetField("isDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.IsNull(isDisposedField);
                }

                public sealed class WithCallToDispose : WithDisposableWithoutGuardClass
                {
                    [SetUp]
                    public override void SetUp()
                    {
                        base.SetUp();

                        this.Instance.Dispose();
                    }

                    [Test]
                    public void CallSayMeHelloWorld()
                    {
                        var result = this.Instance.SayMeHelloWorld();
                        Assert.AreEqual("Hello World!", result);
                    }
                }
            }

            /// <summary>
            ///     Contains unit tests for the <see cref="Disposable" /> class.
            /// </summary>
            public abstract class WithDisposableClass : WithValidAssembly
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
}