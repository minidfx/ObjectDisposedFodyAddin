# ObjectDisposedFodyAddin #

[![Build Status](https://img.shields.io/travis/minidfx/ObjectDisposedFodyAddin/master)](https://travis-ci.org/minidfx/ObjectDisposedFodyAddin) [![NuGet](https://img.shields.io/nuget/v/ObjectDisposed.Fody)](http://www.nuget.org/packages/ObjectDisposed.Fody/) [![Downloads](https://img.shields.io/nuget/dt/ObjectDisposed.Fody)](http://www.nuget.org/packages/ObjectDisposed.Fody/)

Just a simple Addin for Fody to check whether an object, which implement the interface IDisposable or IAsyncDisposable is already disposed when you try to access to any public properties or methods.

## What the Addin does ##

It search all classes that implement the Interface **System.IDisposable** or an interface ends with **IAsyncDisposable** because currently this interface exists only in the **Microsoft.VisualStudio.Threading** namespace and you have to create it in your project where you want, just ends with **IAsyncDisposable**.

**IMPORTANT**: You cannot implement both interfaces **System.IDisposable** and **IAsyncDisposable**, the Addin will throw a **WeavingException** when it will proceed.

It creates a backing field **isDisposed** whether the *Dispose* or the *DisposeAsync* method exists in the class.

It creates an **IsDisposed** property that test the base property (if the class inherit another) and the backing field **isDisposed**.

For Disposable classes, It injects the statement just before a return instruction.

    this.isDisposed = true

For AsyncDisposable classes, It injects these statements for calling the ContinueWith method of the returned task.

    task.ContinueWith(t => { this.isDisposed = true; });

For any public methods and properties, It injects these statements at the beginning

    if(this.IsDisposed)
      throw new ObjectDisposedException("class name");

## Simple example ##

If you have this class

    public class Disposable : IDisposable
    {
        public string FooProperty { get; set; }

        public void Dispose()
        {
          // Some statements ...
        }

        public void DoSomething()
        {
            Console.WriteLine("Hello World!");
        }
    }

You will have

    public class Disposable : IDisposable
    {
        [GenerateCode(...)]
        private bool isDisposed;

        /* Not generaged by the Addin */
        [CompilerGenerated]
        private bool backingFieldFooProperty;

        [GenerateCode(...)]
        public bool IsDisposed
        {  
          get
          {
            return this.isDisposed;
          }
        }

        public string FooProperty
        {
          get
          {
            if(this.IsDisposed)
              throw new ObjectDisposedException("Disposable");

              return this.backingFieldFooProperty;
          }

          set
          {
            this.backingFieldFooProperty = value;
          }
        }

        public void Dispose()
        {
          // Some statements ...
          this.isDisposed = true;
        }

        public void DoSomething()
        {
            if(this.IsDisposed)
              throw new ObjectDisposedException("Disposable");

            Console.WriteLine("Hello World!");
        }
    }

## Example with inheritance ##

With these classes

    public class FooBase : IDisposable
    {
        public string FooProperty { get; set; }

        public virtual void Dispose()
        {
          // Some statements ...
        }

        // Some methods ...
    }

    public class Disposable : FooBase
    {
        public string FooProperty { get; set; }

        public override void Dispose()
        {
          // Some statements ...
        }

        public void DoSomething()
        {
            Console.WriteLine("Hello World!");
        }
    }

You will have

    public class FooBase : IDisposable
    {
        [GenerateCode(...)]
        private bool isDisposed;

        /* Not generaged by the Addin */
        [CompilerGenerated]
        private bool backingFieldFooProperty;

        [GenerateCode(...)]
        public bool IsDisposed
        {  
          get
          {
            return this.isDisposed;
          }
        }

        public string FooProperty
        {
          get
          {
            if(this.IsDisposed)
              throw new ObjectDisposedException("Disposable");

              return this.backingFieldFooProperty;
          }

          set
          {
            this.backingFieldFooProperty = value;
          }
        }

        public virtual void Dispose()
        {
          // Some statements ...
          this.isDisposed = true;
        }

        // Some methods ...
    }

    public class Disposable : FooBase
    {
        [GenerateCode(...)]
        private bool isDisposed;

        [GenerateCode(...)]
        public bool IsDisposed
        {  
          get
          {
            return this.isDisposed && base.IsDisposed;
          }
        }

        public override void Dispose()
        {
            base.Dispose();

            // Some statements ...
            this.isDisposed = true;
        }

        public void DoSomething()
        {
            if(this.IsDisposed)
              throw new ObjectDisposedException("Disposable");

            Console.WriteLine("Hello World!");
        }
    }

## Example with DisposeAsync without await keyword ##

With a simple return task

    public class AsyncDisposable : IAsyncDisposable
    {
       public Task DisposeAsync()
       {
           return Task.FromResult(0);
       }

       public string SayMeHelloWorld()
       {
           return "Hello World!";
       }
    }

You will have

    public class AsyncDisposable : IAsyncDisposable
    {
        [GenerateCode(...)]
        private bool isDisposed;

        [GenerateCode(...)]
        public bool IsDisposed
        {  
          get
          {
            return this.isDisposed;
          }
        }

       public Task DisposeAsync()
       {
           return Task.FromResult(0)
                      .ContinueWith(t => { this.isDisposed = true; });
       }

       public string SayMeHelloWorld()
       {
           if(this.IsDisposed)
             throw new ObjectDisposedException("Disposable");

           return "Hello World!";
       }
    }

## Example with DisposeAsync with await keyword ##

With the await keyword

    public class AsyncDisposable : IAsyncDisposable
    {
        [GenerateCode(...)]
        private bool isDisposed;

        [GenerateCode(...)]
        public bool IsDisposed
        {  
          get
          {
            return this.isDisposed;
          }
        }

        public async Task DisposeAsync()
        {
           await this.FooMethod();
        }

        /**
        * This is just a sample what the compiler generated with
        * the await keyword
        *
        * public Task DisposeAsync()
        * {
        *    AsyncDisposableWithAwait.* stateMachine;
        *    stateMachine.*_this = this;
        *    stateMachine.*_builder = AsyncTaskMethodBuilder.Create();
        *    stateMachine.*_state = -1;
        *    stateMachine.*_builder.Start<AsyncDisposableWithAwait.*_0>(ref stateMachine);
        *    return stateMachine.*_builder.Task;
        * }
        */

        private Task FooMethod()
        {
            return Task.FromResult(0);
        }

        public string SayMeHelloWorld()
        {
           if(this.IsDisposed)
             throw new ObjectDisposedException("Disposable");

           return "Hello World!";
        }
    }

You will have

    public class AsyncDisposable : IAsyncDisposable
    {
        [GenerateCode(...)]
        private bool isDisposed;

        [GenerateCode(...)]
        public bool IsDisposed
        {  
          get
          {
            return this.isDisposed;
          }
        }

        public async Task DisposeAsync()
        {
           await this.FooMethod();
           await this.FooMethod2();
           await this.FooMethodN();
        }

        /**
        * This is just a sample what the compiler generated with
        * the await keyword and the ObjectDisposedFodyAddin.
        *
        * public Task DisposeAsync()
        * {
        *    AsyncDisposableWithAwait.* stateMachine;
        *    stateMachine.*_this = this;
        *    stateMachine.*_builder = AsyncTaskMethodBuilder.Create();
        *    stateMachine.*_state = -1;
        *    stateMachine.*_builder.Start<AsyncDisposableWithAwait.*_0>(ref stateMachine);
        *    return stateMachine.*_builder.Task.ContinueWith(t => { this.isDisposed = true; });
        * }
        */

        private Task FooMethod()
        {
            return Task.FromResult(0);
        }

        public string SayMeHelloWorld()
        {
           if(this.IsDisposed)
             throw new ObjectDisposedException("Disposable");

           return "Hello World!";
        }
    }
