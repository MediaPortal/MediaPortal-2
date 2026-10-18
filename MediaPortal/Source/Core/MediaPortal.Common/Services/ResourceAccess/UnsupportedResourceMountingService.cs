#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.ResourceAccess
{
  /// <summary>
  /// <see cref="IResourceMountingService"/> for platforms that have no way of mounting a virtual
  /// file system: it reports that mounting is unavailable and does nothing.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Mounting resources into the local file system is the job that lets another program (a media
  /// player, a game console, a Windows Explorer window) open a MediaPortal resource through an
  /// ordinary path. On Windows that was <c>MediaPortal.Common.Services.Dokan</c>, a binding to the
  /// Dokan driver, and it moved to <c>MediaPortal.Common.Windows</c> because that is a Windows file
  /// system driver and nothing else. Removing it from the portable core leaves a hole: the callers
  /// ask for the service unconditionally, so the core needs an implementation that says "not here".
  /// </para>
  /// <para>
  /// This is not a new state for the code. The Dokan implementation already behaved exactly like
  /// this whenever the driver was not installed - <c>Dokan.Install()</c> returns <c>null</c>, it
  /// logged a warning and every member degraded to "no mount" (<c>RootPath</c> null,
  /// <see cref="IsVirtualResource"/> false, the mutators return null), which is also what the
  /// interface documents. So the behaviour below is a state the callers already had to survive on
  /// Windows, promoted to a deliberate implementation rather than an error path.
  /// </para>
  /// <para>
  /// <see cref="RootDirectories"/> is the one place where this class is better-behaved than the
  /// Dokan one, which dereferenced its executor without a null check and threw in that state.
  /// </para>
  /// <para>
  /// A Linux implementation is possible (FUSE, via <c>fusermount3</c> or a .NET binding) and Android
  /// has no equivalent at all. Neither is a reason to keep Dokan compiling on those platforms.
  /// </para>
  /// </remarks>
  public class UnsupportedResourceMountingService : IResourceMountingService
  {
    private bool _warned;

    public ResourcePath RootPath
    {
      get { return null; }
    }

    public ICollection<string> RootDirectories
    {
      get { return new List<string>(); }
    }

    public void Startup()
    {
      WarnOnce();
    }

    public void Shutdown() {}

    public bool IsVirtualResource(ResourcePath rp)
    {
      return false;
    }

    public string CreateRootDirectory(string rootDirectoryName)
    {
      WarnOnce();
      return null;
    }

    public void DisposeRootDirectory(string rootDirectoryName) {}

    public ICollection<IFileSystemResourceAccessor> GetResources(string rootDirectoryName)
    {
      return null;
    }

    public string AddResource(string rootDirectoryName, IFileSystemResourceAccessor resourceAccessor)
    {
      WarnOnce();
      return null;
    }

    public void RemoveResource(string rootDirectoryName, IFileSystemResourceAccessor resourceAccessor) {}

    /// <summary>
    /// Logs once per process. The core calls through this service while browsing the file system
    /// (<c>FileSystemResourceNavigator</c> asks <see cref="IsVirtualResource"/> for every candidate
    /// path), so an unthrottled warning would fill the log with an entry per path and hide the one
    /// message that explains why mounting is off.
    /// </summary>
    private void WarnOnce()
    {
      if (_warned)
        return;
      _warned = true;
      ServiceRegistration.Get<ILogger>().Warn(
          "ResourceMountingService: This platform cannot mount resources into the local file system " +
          "(that requires a file system driver such as Dokan on Windows). Resources are still " +
          "available through MediaPortal's own resource access and over the network.");
    }
  }
}
