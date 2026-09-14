using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;

// Invoke the packaged LEAN launcher with this application's dependency graph.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var entry = Assembly.Load("QuantConnect.Lean.Launcher").EntryPoint
    ?? throw new InvalidOperationException("LEAN package has no launcher entry point.");
try { entry.Invoke(null, new object[] { args }); }
catch (TargetInvocationException e) when (e.InnerException != null)
{ ExceptionDispatchInfo.Capture(e.InnerException).Throw(); }
