// ReSharper disable InconsistentNaming
// ReSharper disable SealedMemberInSealedClass
#pragma warning disable 618

using System;
using System.IO;
using System.Linq;
using Fody;

using NUnit.Framework;
using ObjectDisposed.Fody;
using WeavingException = ObjectDisposed.Fody.WeavingException;

namespace Tests
{
    using System.Reflection;

    [TestFixture]
    public abstract class WeaverTests
    {
        public abstract class with_valid_assembly : WeaverTests
        {
            protected abstract dynamic GetInstance(TestResult weaverResult);

            public abstract class with_AssemblyToProcess : with_valid_assembly
            {
                [SetUp]
                public void SetUp()
                {
                    var weaverResult = TryToLoadAssembly("AssemblyToProcess.csproj", "AssemblyToProcessExternalDependencies.dll");
                    this.Instance = this.GetInstance(weaverResult);

                    this.MethodCalled();
                }

                public abstract class with_exceptions_expected : with_AssemblyToProcess
                {
                    public abstract class with_Disposable_class : with_exceptions_expected
                    {
                        protected sealed override dynamic GetInstance(TestResult weaverResult)
                        {
                            return weaverResult.GetInstance("AssemblyToProcess.Disposable");
                        }

                        public sealed class when_Dispose_is_called : with_Disposable_class
                        {
                            protected sealed override void MethodCalled()
                            {
                                this.Instance.Dispose();
                            }

                            [Test]
                            public void then_ObjectDisposedException_is_throwing_with_DoNothing()
                            {
                                Assert.Throws<ObjectDisposedException>(() => this.Instance.DoNothing());
                            }

                            [Test]
                            public void then_ObjectDisposedException_is_throwing_with_DoSomething()
                            {
                                Assert.Throws<ObjectDisposedException>(() => this.Instance.DoSomething());
                            }
                        }
                    }

                    public abstract class with_DisposableChild_class : with_exceptions_expected
                    {
                        protected override dynamic GetInstance(TestResult weaverResult)
                        {
                            return weaverResult.GetInstance("AssemblyToProcess.DisposableChild");
                        }

                        public sealed class when_Dispose_is_called : with_DisposableChild_class
                        {
                            protected override void MethodCalled()
                            {
                                this.Instance.Dispose();
                            }

                            [Test]
                            public void then_ObjectDisposedException_is_throwing_with_DoSomething()
                            {
                                Assert.Throws<ObjectDisposedException>(() => this.Instance.DoSomething());
                            }
                        }
                    }

                    public abstract class with_DisposableChildWithOverride_class : with_exceptions_expected
                    {
                        protected override dynamic GetInstance(TestResult weaverResult)
                        {
                            return weaverResult.GetInstance("AssemblyToProcess.DisposableChild");
                        }

                        public sealed class when_Dispose_is_called : with_DisposableChildWithOverride_class
                        {
                            protected override void MethodCalled()
                            {
                                this.Instance.Dispose();
                            }
                        }
                    }

                    public abstract class with_AsyncDisposable_class : with_exceptions_expected
                    {
                        protected override dynamic GetInstance(TestResult weaverResult)
                        {
                            return weaverResult.GetInstance("AssemblyToProcess.AsyncDisposable");
                        }

                        public sealed class when_DisposeAsync_is_called : with_AsyncDisposable_class
                        {
                            protected override void MethodCalled()
                            {
                                this.Instance.DisposeAsync().Wait();
                            }

                            [Test]
                            public void then_ObjectDisposedException_is_throwing_with_DoSomethingAsync()
                            {
                                Assert.Throws<ObjectDisposedException>(() => this.Instance.DoSomethingAsync());
                            }
                        }
                    }

                    public abstract class with_AsyncDisposableChild_class : with_exceptions_expected
                    {
                        protected override dynamic GetInstance(TestResult weaverResult)
                        {                            
                            return weaverResult.GetInstance("AssemblyToProcess.AsyncDisposableChild");
                        }

                        public sealed class when_DisposeAsync_is_called : with_AsyncDisposableChild_class
                        {
                            protected override void MethodCalled()
                            {
                                this.Instance.DisposeAsync().Wait(); // Wait for the task is completely finished.
                            }

                            [Test]
                            public void then_ObjectDisposedException_is_throwing_with_DoSomething()
                            {
                                Assert.Throws<ObjectDisposedException>(() => this.Instance.DoSomething());
                            }
                        }
                    }

                    public abstract class with_AsyncDisposableWithAwait_class : with_exceptions_expected
                    {
                        protected override dynamic GetInstance(TestResult weaverResult)
                        {
                            return weaverResult.GetInstance("AssemblyToProcess.AsyncDisposableWithAwait");
                        }

                        public sealed class when_DisposeAsync_is_called : with_AsyncDisposableWithAwait_class
                        {
                            protected override void MethodCalled()
                            {
                                this.Instance.DisposeAsync().Wait(); // Wait for the task is completely finished.
                            }
                        }
                    }

                    public abstract class with_AsyncDisposableWithDelay_class_wait_for : with_exceptions_expected
                    {
                        protected override dynamic GetInstance(TestResult weaverResult)
                        {
                            return weaverResult.GetInstance("AssemblyToProcess.AsyncDisposableWithDelay");
                        }

                        public sealed class when_DisposeAsync_is_called : with_AsyncDisposableWithDelay_class_wait_for
                        {
                            protected override void MethodCalled()
                            {
                                this.Instance.DisposeAsync().Wait();
                            }
                        }
                    }

                    public abstract class with_AsyncDisposableWithMultiTasks_class : with_exceptions_expected
                    {
                        protected override dynamic GetInstance(TestResult weaverResult)
                        {
                            return weaverResult.GetInstance("AssemblyToProcess.AsyncDisposableWithMultiTasks");
                        }

                        public sealed class when_DisposeAsync_is_called : with_AsyncDisposableWithMultiTasks_class
                        {
                            protected override void MethodCalled()
                            {
                                this.Instance.DisposeAsync().Wait();
                            }

                            [Test]
                            public void then_ObjectDisposedException_is_throwing_with_DoSomething()
                            {
                                Assert.Throws<ObjectDisposedException>(() => this.Instance.DoSomething());
                            }
                        }
                    }
                }

                public abstract class with_AChildClass_class : with_AssemblyToProcess
                {
                    protected sealed override dynamic GetInstance(TestResult weaverResult)
                    {
                        return weaverResult.GetInstance("AssemblyToProcess.AChildClass");
                    }
                
                    public sealed class when_Dispose_is_called : with_AChildClass_class
                    {
                        protected sealed override void MethodCalled()
                        {
                            this.Instance.Dispose();
                        }
                
                        [Test]
                        public void then_SayMeHelloWorld_equals_HelloWorld()
                        {
                            Assert.Throws<ObjectDisposedException>(() => this.Instance.SayMeHelloWorld());
                        }
                    }
                }
                
                public abstract class with_AsyncDisposableWithDelay_class : with_AssemblyToProcess
                {
                    protected sealed override dynamic GetInstance(TestResult weaverResult)
                    {
                        return weaverResult.GetInstance("AssemblyToProcess.AsyncDisposableWithDelay");
                    }

                    public sealed class when_DisposeAsync_is_called : with_AsyncDisposableWithDelay_class
                    {
                        protected sealed override void MethodCalled()
                        {
                            this.Instance.DisposeAsync();
                        }

                        [Test]
                        public void then_SayMeHello_equals_Hello()
                        {
                            Assert.AreEqual("Hello", this.Instance.SayMeHello());
                        }
                    }
                }

                public abstract class with_DisposableWithoutGuard_class : with_AssemblyToProcess
                {
                    protected override dynamic GetInstance(TestResult weaverResult)
                    {
                        return weaverResult.GetInstance("AssemblyToProcess.DisposableWithoutGuard");
                    }

                    public sealed class when_Dispose_is_called : with_DisposableWithoutGuard_class
                    {
                        protected sealed override void MethodCalled()
                        {
                            this.Instance.Dispose();
                        }

                        [Test]
                        public void then_Result_equals_to_Hello_World()
                        {
                            Assert.AreEqual("Hello World!", this.Instance.SayMeHelloWorld());
                        }
                    }
                }

                protected abstract void MethodCalled();
            }
        }

        public abstract class with_invalid_assembly : WeaverTests
        {
            [SetUp]
            public void SetUp()
            {
                this.ProjectName = this.EstablishProjectName();
                this.ExpectedErrorCode = this.EstablishErrorCode();
            }

            public sealed class with_AssemblyToProcessWithInvalidType : with_invalid_assembly
            {
                protected sealed override WeavingErrorCodes EstablishErrorCode()
                {
                    return WeavingErrorCodes.ContainsIsDisposedField;
                }

                protected sealed override string EstablishProjectName()
                {
                    return "AssemblyToProcessWithInvalidType";
                }
            }

            public sealed class with_AssemblyToProcessWithInvalidType2 : with_invalid_assembly
            {
                protected sealed override WeavingErrorCodes EstablishErrorCode()
                {
                    return WeavingErrorCodes.ContainsBothInterface;
                }

                protected sealed override string EstablishProjectName()
                {
                    return "AssemblyToProcessWithInvalidType2";
                }
            }

            private WeavingErrorCodes ExpectedErrorCode { get; set; }

            protected abstract WeavingErrorCodes EstablishErrorCode();

            private string ProjectName { get; set; }

            protected abstract string EstablishProjectName();

            [Test]
            public void then_load_assembly_failed()
            {
                var exception = Assert.Throws<WeavingException>(() => { TryToLoadAssembly(this.ProjectName + ".csproj"); });
                Assert.AreEqual(this.ExpectedErrorCode, exception.ErrorCode);
            }
        }
        
        private dynamic Instance { get; set; }

        private static TestResult TryToLoadAssembly(string project, params string[] dependencies)
        {
            var srcPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", ".."));
            var projectPath = Directory.GetFiles(srcPath, project, SearchOption.AllDirectories).Single();
            var projectDirectoryPath = Path.GetDirectoryName(projectPath);
            var projectName = Path.GetFileNameWithoutExtension(project);

            #if DEBUG            
            var projectOutput = Path.Combine(projectDirectoryPath, Path.Combine("bin", "Debug", "netstandard2.0"));
            #else
            var projectOutput = Path.Combine(projectDirectoryPath, Path.Combine("bin", "Release", "netstandard2.0"));
            #endif

            var assemblyPath = Path.Combine(projectOutput, $"{projectName}.dll");
            var fodyTemp = Path.Combine(projectOutput, "fodytemp");
            Directory.CreateDirectory(fodyTemp);
            
            var weaver = new ModuleWeaver();
            
            foreach (var dependency in dependencies)
            {
                var tempDependency = Path.Combine(fodyTemp, dependency);
                var originalDependency = Path.Combine(projectOutput, dependency);

                if (File.Exists(tempDependency))
                {
                    continue;
                }
                
                File.Copy(originalDependency, tempDependency, true);

                // INFO [Benjamin 15.04.2020 11:03]: Apply the weave on the dependency.
                weaver.ExecuteTestRun(tempDependency).PrintAll();
            }

            // INFO [Benjamin 15.04.2020 11:17]: To resolve the test dependencies.
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                                                       {
                                                           var assemblyName = args.Name.Split(",").First();
                                                           var localAssemblyPath = Path.Combine(fodyTemp, $"{assemblyName}.dll");
                                                           return File.Exists(localAssemblyPath) ? Assembly.LoadFile(localAssemblyPath) : null;
                                                       };

            var testResult = weaver.ExecuteTestRun(assemblyPath);

            testResult.PrintAll();

            return testResult;
        }
    }
}