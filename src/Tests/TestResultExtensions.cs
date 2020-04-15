namespace Tests
{
  using System;

  using Fody;

  public static class TestResultExtensions
  {
    #region Public Methods

    public static void PrintAll(this TestResult testResult)
    {
      foreach (var error in testResult.Errors)
      {
        Console.WriteLine(error.Text);
      }

      foreach (var warning in testResult.Warnings)
      {
        Console.WriteLine(warning.Text);
      }

      foreach (var message in testResult.Messages)
      {
        Console.WriteLine(message.Text);
      }
    }

    #endregion
  }
}