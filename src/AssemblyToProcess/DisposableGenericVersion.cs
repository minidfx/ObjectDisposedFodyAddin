namespace AssemblyToProcess
{
  using System;

  public sealed class DisposableGenericVersion : DisposableGenericBase<Version>
  {
    public string SayMeHelloWorld()
    {
      return "Hello World!";
    }
  }
}