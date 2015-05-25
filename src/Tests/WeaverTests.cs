// ReSharper disable InconsistentNaming

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
        public abstract class with_valid_assembly : WeaverTests
        {
            #region Context

            protected abstract dynamic GetInstance();

            #endregion

            public abstract class with_AssemblyToProcess : with_valid_assembly
            {
                public abstract class with_exceptions_expected : with_AssemblyToProcess
                {
                    [Test]
                    [ExpectedException(typeof(ObjectDisposedException))]
                    public void then_ObjectDisposedException_is_throwing_with_SayMeHelloWorld()
                    {
                        this.Instance.SayMeHelloWorld();
                    }

                    public abstract class with_Disposable_class : with_exceptions_expected
                    {
                        #region Context

                        protected override dynamic GetInstance()
                        {
                            return this.CreateInstance("AssemblyToProcess.Disposable");
                        }

                        #endregion

                        public sealed class when_Dispose_is_called : with_Disposable_class
                        {
                            #region Context

                            protected override void MethodCalled()
                            {
                                this.Instance.Dispose();
                            }

                            #endregion

                            [Test]
                            [ExpectedException(typeof(ObjectDisposedException))]
                            public void then_ObjectDisposedException_is_throwing_with_DoSomething()
                            {
                                this.Instance.DoSomething();
                            }

                            [Test]
                            [ExpectedException(typeof(ObjectDisposedException))]
                            public void then_ObjectDisposedException_is_throwing_with_DoNothing()
                            {
                                this.Instance.DoNothing();
                            }
                        }
                    }

                    public abstract class with_DisposableChild_class : with_exceptions_expected
                    {
                        #region Context

                        protected override dynamic GetInstance()
                        {
                            return this.CreateInstance("AssemblyToProcess.DisposableChild");
                        }

                        #endregion

                        public sealed class when_Dispose_is_called : with_DisposableChild_class
                        {
                            #region Context

                            protected override void MethodCalled()
                            {
                                this.Instance.Dispose();
                            }

                            #endregion

                            [Test]
                            [ExpectedException(typeof(ObjectDisposedException))]
                            public void then_ObjectDisposedException_is_throwing_with_DoSomething()
                            {
                                this.Instance.DoSomething();
                            }
                        }
                    }

                    public abstract class with_DiposableChildWithOverride_class : with_exceptions_expected
                    {
                        #region Context

                        protected override dynamic GetInstance()
                        {
                            return this.CreateInstance("AssemblyToProcess.DisposableChild");
                        }

                        #endregion

                        public sealed class when_Dispose_is_called : with_DiposableChildWithOverride_class
                        {
                            #region Context

                            protected override void MethodCalled()
                            {
                                this.Instance.Dispose();
                            }

                            #endregion
                        }
                    }

                    public abstract class with_AsyncDisposable_class : with_exceptions_expected
                    {
                        #region Context

                        protected override dynamic GetInstance()
                        {
                            return this.CreateInstance("AssemblyToProcess.AsyncDisposable");
                        }

                        #endregion

                        public sealed class when_DisposeAsync_is_called : with_AsyncDisposable_class
                        {
                            #region Context

                            protected override void MethodCalled()
                            {
                                this.Instance.DisposeAsync().Wait();
                            }

                            #endregion

                            [Test]
                            [ExpectedException(typeof(ObjectDisposedException))]
                            public void then_ObjectDisposedException_is_throwing_with_DoSomethingAsync()
                            {
                                this.Instance.DoSomethingAsync();
                            }
                        }
                    }

                    public abstract class with_AsyncDisposableChild_class : with_exceptions_expected
                    {
                        #region Context

                        protected override dynamic GetInstance()
                        {
                            return this.CreateInstance("AssemblyToProcess.AsyncDisposableChild");
                        }

                        #endregion

                        public sealed class when_DisposeAsync_is_called : with_AsyncDisposableChild_class
                        {
                            #region Context

                            protected override void MethodCalled()
                            {
                                this.Instance.DisposeAsync().Wait(); // Wait for the task is completely finished.
                            }

                            #endregion

                            [Test]
                            [ExpectedException(typeof(ObjectDisposedException))]
                            public void then_ObjectDisposedException_is_throwing_with_DoSomething()
                            {
                                this.Instance.DoSomething();
                            }
                        }
                    }

                    public abstract class with_AsyncDisposableWithAwait_class : with_exceptions_expected
                    {
                        #region Context

                        protected override dynamic GetInstance()
                        {
                            return this.CreateInstance("AssemblyToProcess.AsyncDisposableWithAwait");
                        }

                        #endregion

                        public sealed class when_DisposeAsync_is_called : with_AsyncDisposableWithAwait_class
                        {
                            #region Context

                            protected override void MethodCalled()
                            {
                                this.Instance.DisposeAsync().Wait(); // Wait for the task is completely finished.
                            }

                            #endregion
                        }
                    }

                    public abstract class with_AsyncDisposableWithDelay_class_wait_for : with_exceptions_expected
                    {
                        #region Context

                        protected override dynamic GetInstance()
                        {
                            return this.CreateInstance("AssemblyToProcess.AsyncDisposableWithDelay");
                        }

                        #endregion

                        public sealed class when_DisposeAsync_is_called : with_AsyncDisposableWithDelay_class_wait_for
                        {
                            #region Context

                            protected override void MethodCalled()
                            {
                                this.Instance.DisposeAsync().Wait();
                            }

                            #endregion
                        }
                    }

                    public abstract class with_AsyncDisposableWithMultiTasks_class : with_exceptions_expected
                    {
                        #region Context

                        protected override dynamic GetInstance()
                        {
                            return this.CreateInstance("AssemblyToProcess.AsyncDisposableWithMultiTasks");
                        }

                        #endregion

                        public sealed class when_DisposeAsync_is_called : with_AsyncDisposableWithMultiTasks_class
                        {
                            #region Context

                            protected override void MethodCalled()
                            {
                                this.Instance.DisposeAsync().Wait();
                            }

                            #endregion

                            [Test]
                            [ExpectedException(typeof(ObjectDisposedException))]
                            public void then_ObjectDisposedException_is_throwing_with_DoSomething()
                            {
                                this.Instance.DoSomething();
                            }
                        }
                    }
                }

                public abstract class with_AsyncDisposableWithDelay_class : with_AssemblyToProcess
                {
                    #region Context

                    protected override dynamic GetInstance()
                    {
                        return this.CreateInstance("AssemblyToProcess.AsyncDisposableWithDelay");
                    }

                    #endregion

                    public sealed class when_DisposeAsync_is_called : with_AsyncDisposableWithDelay_class
                    {
                        #region Context

                        protected override void MethodCalled()
                        {
                            this.Instance.DisposeAsync();
                        }

                        #endregion

                        [Test]
                        public void then_SayMeHello_equals_Hello()
                        {
                            Assert.AreEqual("Hello", this.Instance.SayMeHello());
                        }
                    }
                }

                public abstract class with_DisposableWithoutGuard_class : with_AssemblyToProcess
                {
                    #region Context

                    protected override dynamic GetInstance()
                    {
                        return this.CreateInstance("AssemblyToProcess.DisposableWithoutGuard");
                    }

                    #endregion

                    public sealed class when_Dispose_is_called : with_DisposableWithoutGuard_class
                    {
                        #region Context

                        protected override void MethodCalled()
                        {
                            this.Instance.Dispose();
                        }

                        #endregion

                        [Test]
                        public void then_Result_equals_to_Hello_World()
                        {
                            Assert.AreEqual("Hello World!", this.Instance.SayMeHelloWorld());
                        }
                    }
                }

                #region Context

                [SetUp]
                public virtual void SetUp()
                {
                    this.TryToLoadAssembly(Path.Combine("..", "..", "..", "AssemblyToProcess", "AssemblyToProcess.csproj"));
                    this.Instance = this.GetInstance();
                    this.MethodCalled();
                }

                protected abstract void MethodCalled();

                #endregion
            }
        }

        public abstract class with_invalid_assembly : WeaverTests
        {
            [Test]
            public void then_load_assembly_failed()
            {
                var exception = Assert.Throws<WeavingException>(() => { this.TryToLoadAssembly(Path.Combine("..", "..", "..", this.ProjectName, this.ProjectName + ".csproj")); });
                Assert.AreEqual(this.ExpectedErrorCode, exception.ErrorCode);
            }

            public sealed class with_AssemblyToProcessWithInvalidType : with_invalid_assembly
            {
                #region Context

                protected override WeavingErrorCodes EstablishErrorCode()
                {
                    return WeavingErrorCodes.ContainsIsDisposedField;
                }

                protected override string EstablishProjectName()
                {
                    return "AssemblyToProcessWithInvalidType";
                }

                #endregion
            }

            public sealed class with_AssemblyToProcessWithInvalidType2 : with_invalid_assembly
            {
                #region Context

                protected override WeavingErrorCodes EstablishErrorCode()
                {
                    return WeavingErrorCodes.ContainsBothInterface;
                }

                protected override string EstablishProjectName()
                {
                    return "AssemblyToProcessWithInvalidType2";
                }

                #endregion
            }

            #region Context

            protected WeavingErrorCodes ExpectedErrorCode { get; private set; }

            protected abstract WeavingErrorCodes EstablishErrorCode();

            protected string ProjectName { get; private set; }

            protected abstract string EstablishProjectName();

            [SetUp]
            public void SetUp()
            {
                this.ProjectName = this.EstablishProjectName();
                this.ExpectedErrorCode = this.EstablishErrorCode();
            }

            #endregion
        }

        #region Context

        private static Assembly newAssembly;

        protected dynamic Instance { get; private set; }

        protected void TryToLoadAssembly(string relativeProjectPath)
        {
            if (newAssembly != null)
            {
                return;
            }

            var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, relativeProjectPath));
            var projectName = Path.GetFileNameWithoutExtension(relativeProjectPath);

            var directoryName = Path.GetDirectoryName(projectPath);
            if (directoryName == null)
            {
                throw new IOException("Cannot determines the project directory.");
            }

#if DEBUG
            var assemblyPath = Path.Combine(directoryName, Path.Combine("bin", "Debug", string.Format("{0}.dll", projectName)));
#else
            var assemblyPath = Path.Combine(directoryName, Path.Combine("bin", "Release", string.Format("{0}.dll", projectName)));
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

#if !LINUX
            // Verify that the assembly is correctly before running tests.
            Verifier.Verify(assemblyPath, newAssemblyPath);
#endif

            newAssembly = Assembly.LoadFile(newAssemblyPath);
        }

        protected dynamic CreateInstance(string className,
                                         params object[] args)
        {
            return Activator.CreateInstance(newAssembly.GetType(className, true), args);
        }

        #endregion
    }
}