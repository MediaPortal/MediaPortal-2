using System;
using System.Collections.Generic;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.Dokan
{
  /// <summary>
  /// Handle for a virtual resource.
  /// </summary>
  /// <remarks>
  /// Multithreading safety is ensured by locking on the mounting service's synchronization object.
  /// </remarks>
  public abstract class VirtualFileSystemResource
  {
    protected string _name;

    protected IResourceAccessor _resourceAccessor;
    protected ICollection<FileHandle> _fileHandles = new HashSet<FileHandle>();
    protected DateTime _creationTime;

    protected VirtualFileSystemResource(string name, IResourceAccessor resourceAccessor)
    {
      _name = name;
      _resourceAccessor = resourceAccessor;
      _creationTime = DateTime.Now;
    }

    public virtual void Dispose()
    {
      foreach (FileHandle handle in _fileHandles)
        handle.Cleanup();
      if (_resourceAccessor != null)
        try
        {
          _resourceAccessor.Dispose();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Dokan virtual filesystem resource: Error disposing resource accessor '{0}'", e, _resourceAccessor);
        }
      _resourceAccessor = null;
    }

    public IResourceAccessor ResourceAccessor
    {
      get { return _resourceAccessor; }
    }

    public DateTime CreationTime
    {
      get { return _creationTime; }
    }

    public void AddFileHandle(FileHandle handle)
    {
      _fileHandles.Add(handle);
    }

    public void RemoveFileHandle(FileHandle handle)
    {
      _fileHandles.Remove(handle);
    }
  }
}