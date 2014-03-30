using System;
using System.Collections.Generic;

namespace MediaPortal.Common.PluginManager.Validation
{
  public class ValidationResult
  {
    public HashSet<Guid> MissingDependencies { get; internal set; } 
    public HashSet<Guid> ConflictsWith { get; internal set; } 
    public HashSet<Guid> IncompatibleWith { get; internal set; }

    public bool IsComplete
    {
      get { return MissingDependencies.Count == 0; }
    }
    public bool CanEnable
    {
      get { return IsComplete && MissingDependencies.Count == 0 && IncompatibleWith.Count == 0; }
    }
  }
}
