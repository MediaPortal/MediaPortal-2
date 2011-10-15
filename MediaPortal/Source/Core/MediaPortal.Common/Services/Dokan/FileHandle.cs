using System;
using System.IO;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.Dokan
{
  /// <summary>
  /// File handle of a resource which is associated to a user file handle.
  /// </summary>
  public class FileHandle
  {
    protected VirtualFileSystemResource _resource;
    protected Stream _stream = null;

    public FileHandle(VirtualFileSystemResource resource)
    {
      _resource = resource;
    }

    public VirtualFileSystemResource Resource
    {
      get { return _resource; }
    }

    public void Cleanup()
    {
      try
      {
        if (_stream != null)
          _stream.Dispose();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan FileHandle: Error cleaning up resource '{0}'", e, _resource.ResourceAccessor);
      }
      _stream = null;
    }

    public Stream GetOrOpenStream()
    {
      if (_stream == null)
      {
        IResourceAccessor resourceAccessor = _resource.ResourceAccessor;
        try
        {
          if (resourceAccessor != null)
          {
            resourceAccessor.PrepareStreamAccess();
            _stream = resourceAccessor.OpenRead();
          }
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Dokan FileHandle: Error creating stream for resource '{0}'", e, resourceAccessor);
        }
      }
      return _stream;
    }
  }
}