#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

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

      IFileSystemResourceAccessor resourceAccessor = _resource.ResourceAccessor;
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