using System;
using System.Reflection;
using System.Resources;

[assembly: CLSCompliant(true)]

[assembly: AssemblyProduct("ObjectDisposed.Fody")]
[assembly: AssemblyCopyright("Copyright © Burgy Benjamin 2015")]
[assembly: AssemblyVersion("0.2.2.*")]
[assembly: NeutralResourcesLanguage("en")]

#if DEBUG
[assembly : AssemblyConfiguration("Debug")]
#else

[assembly: AssemblyConfiguration("Release")]
#endif