using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
    protected object _syncObj = new object();
    protected IDictionary<Thread, Stream> _threadStreams = new Dictionary<Thread, Stream>();

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
        foreach (Stream stream in _threadStreams.Values)
          stream.Dispose();
        _threadStreams.Clear();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan FileHandle: Error cleaning up resource '{0}'", e, _resource.ResourceAccessor);
      }
    }

    /// <summary>
    /// Gets a stream for the resource which is described by this file handle if the stream is already open or opens a new stream for it.
    /// </summary>
    /// <returns>
    /// This method returns the same stream for the same thread; it returns a new stream for a new thread.
    /// </returns>
    public Stream GetOrOpenStream()
    {
      Thread currentThread = Thread.CurrentThread;
      Stream stream;
      lock (_syncObj)
        if (_threadStreams.TryGetValue(currentThread, out stream))
          return stream;

      IResourceAccessor resourceAccessor = _resource.ResourceAccessor;
      try
      {
        if (resourceAccessor != null)
        {
          resourceAccessor.PrepareStreamAccess();
          lock (_syncObj)
            return _threadStreams[currentThread] = resourceAccessor.OpenRead();
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Dokan FileHandle: Error creating stream for resource '{0}'", e, resourceAccessor);
      }
      return null;
    }
  }
}