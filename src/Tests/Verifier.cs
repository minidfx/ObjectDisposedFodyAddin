namespace Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    public static class Verifier
    {
        public static void Verify(string beforeAssemblyPath,
                                  string afterAssemblyPath)
        {
            var before = Validate(beforeAssemblyPath);
            var after = Validate(afterAssemblyPath);

            var message = string.Format("Failed processing {0}\r\n{1}", Path.GetFileName(afterAssemblyPath), after);

            Assert.AreEqual(TrimLineNumbers(before), TrimLineNumbers(after), message);
        }

        private static string Validate(string assemblyPath)
        {
            var exePath = GetPathToPeVerify();
            if (!File.Exists(exePath))
            {
                return string.Empty;
            }

            var processStartInfo = new ProcessStartInfo(string.Format(@"{0} ""{1}""", exePath, assemblyPath))
                                       {
                                           RedirectStandardOutput = true,
                                           UseShellExecute = false,
                                           CreateNoWindow = true
                                       };
            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new IOException("Cannot start the Peverifier tool.");
            }

            process.WaitForExit(10000);
            return process.StandardOutput.ReadToEnd().Trim().Replace(assemblyPath, string.Empty);
        }

        private static string GetPathToPeVerify()
        {
            var exePaths = new[]
                               {
                                   Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\PEVerify.exe"),
                                   Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\Microsoft SDKs\Windows\v8.0A\Bin\NETFX 4.0 Tools\PEVerify.exe")
                               };

            return exePaths.First(File.Exists);
        }

        private static string TrimLineNumbers(string foo)
        {
            return Regex.Replace(foo, @"0x.*]", "");
        }
    }
}