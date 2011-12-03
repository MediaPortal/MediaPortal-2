using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.Dokan
{
  /// <summary>
  /// Handle for a virtual file.
  /// </summary>
  public class VirtualFile : VirtualFileSystemResource
  {
    public VirtualFile(string name, IResourceAccessor resourceAccessor) :
        base(name, resourceAccessor) { }

    public override string ToString()
    {
      return string.Format("Virtual file '{0}'", _name);
    }
  }
}