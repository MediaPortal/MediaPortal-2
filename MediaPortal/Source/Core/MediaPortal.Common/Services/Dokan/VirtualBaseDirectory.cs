using System.Collections.Generic;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.Dokan
{
  public abstract class VirtualBaseDirectory : VirtualFileSystemResource
  {
    protected VirtualBaseDirectory(string name, IResourceAccessor resourceAccessor) : base(name, resourceAccessor) { }

    public abstract IDictionary<string, VirtualFileSystemResource> ChildResources { get; }
  }
}