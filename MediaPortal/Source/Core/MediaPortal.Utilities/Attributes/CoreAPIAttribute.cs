using System;

namespace MediaPortal.Attributes
{
  [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
  public sealed class CoreAPIAttribute : Attribute
  {
    readonly int currentAPI;

    public CoreAPIAttribute(int currentAPI)
    {
      this.currentAPI = currentAPI;
      this.MinCompatibleAPI = currentAPI; // in case this is not set by the assembly, assume the same as current API
    }

    /// <summary>
    /// Returns the current API level of this core component.
    /// </summary>
    public int CurrentAPI 
    { 
      get { return currentAPI; } 
    }

    /// <summary>
    /// Specifies the minimum API level of this core component that is compatible with the current API level of this core component's version.
    /// </summary>
    public int MinCompatibleAPI { get; set; }
  }
}
