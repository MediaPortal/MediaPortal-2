using System;
using System.Collections.Generic;

namespace MediaPortal.Common.Services.Dokan
{
  /// <summary>
  /// Handle for a virtual root directory.
  /// </summary>
  public class VirtualRootDirectory : VirtualBaseDirectory
  {
    protected IDictionary<string, VirtualFileSystemResource> _children =
        new Dictionary<string, VirtualFileSystemResource>(StringComparer.InvariantCultureIgnoreCase);

    public VirtualRootDirectory(string name) : base(name, null) { }

    public override void Dispose()
    {
      foreach (VirtualFileSystemResource resource in _children.Values)
        resource.Dispose();
      base.Dispose();
    }

    public override IDictionary<string, VirtualFileSystemResource> ChildResources
    {
      get { return _children; }
    }

    public void AddResource(string name, VirtualFileSystemResource resource)
    {
      _children.Add(name, resource);
    }

    public void RemoveResource(string name)
    {
      _children.Remove(name);
    }

    public override string ToString()
    {
      return string.Format("Virtual root directory '{0}'", _name);
    }
  }
}