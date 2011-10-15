#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Linq;
using System.Threading;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Dokan;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Services.SystemResolver;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;

namespace MediaPortal.Common.Services.ResourceAccess
{
  public class ResourceMountingService : IResourceMountingService
  {
    #region Consts

    /// <summary>
    /// Volume label for the virtual drive if our mount point is a drive letter.
    /// </summary>
    public static string VOLUME_LABEL = "MediaPortal 2 resource access";

    protected const FileAttributes FILE_ATTRIBUTES = FileAttributes.ReadOnly;
    protected const FileAttributes DIRECTORY_ATTRIBUTES = FileAttributes.ReadOnly | FileAttributes.NotContentIndexed | FileAttributes.Directory;

    protected static readonly DateTime MIN_FILE_DATE = DateTime.FromFileTime(0); // If we use lower values, resources are not recognized by the Explorer/Windows API

    #endregion

    protected object _syncObj = new object();
    protected bool _started = false;
    protected char? _driveLetter = null;
    protected Thread _mountThread;
    protected Dokan.Dokan _dokanExecutor = null;
    protected VirtualRootDirectory _root = new VirtualRootDirectory("/");

    protected char? ReadDriveLetterFromSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      ResourceMountingSettings settings = settingsManager.Load<ResourceMountingSettings>();
      return settings.DriveLetter;
    }

    #region IResourceMountingService implementation

    public char? DriveLetter
    {
      get
      {
        lock (_syncObj)
          return _driveLetter;
      }
    }

    public ICollection<string> RootDirectories
    {
      get
      {
        lock (_syncObj)
          return new List<string>(_root.ChildResources.Keys);
      }
    }

    public void Startup()
    {
      char? driveLetter = ReadDriveLetterFromSettings();
      if (!driveLetter.HasValue)
      {
        ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
        driveLetter = systemResolver.SystemType == SystemType.Server ? ResourceMountingSettings.DEFAULT_DRIVE_LETTER_SERVER :
            ResourceMountingSettings.DEFAULT_DRIVE_LETTER_CLIENT;
      }
      _dokanExecutor = new Dokan.Dokan(driveLetter.Value);
    }

    public void Shutdown()
    {
      _dokanExecutor.Dispose();
    }

    public string CreateRootDirectory(string rootDirectoryName)
    {
      lock (_syncObj)
      {
        if (!_driveLetter.HasValue)
          return null;
        _root.AddResource(rootDirectoryName, new VirtualRootDirectory(rootDirectoryName));
        return Path.Combine(_driveLetter + ":\\", rootDirectoryName);
      }
    }

    public void DisposeRootDirectory(string rootDirectoryName)
    {
      lock (_syncObj)
      {
        VirtualRootDirectory rootDirectory = _dokanExecutor.GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return;
        _root.RemoveResource(rootDirectoryName);
        rootDirectory.Dispose();
      }
    }

    public ICollection<IResourceAccessor> GetResources(string rootDirectoryName)
    {
      lock (_syncObj)
      {
        VirtualRootDirectory rootDirectory = _dokanExecutor.GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return null;
        return rootDirectory.ChildResources.Values.Select(resource => resource.ResourceAccessor).ToList();
      }
    }

    public string AddResource(string rootDirectoryName, IResourceAccessor resourceAccessor)
    {
      lock (_syncObj)
      {
        if (!_driveLetter.HasValue)
          return null;
        VirtualRootDirectory rootDirectory = _dokanExecutor.GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return null;
        string resourceName = resourceAccessor.ResourceName;
        IFileSystemResourceAccessor fsra = resourceAccessor as IFileSystemResourceAccessor;
        rootDirectory.AddResource(resourceName, fsra != null && fsra.IsDirectory ?
                                                                                     (VirtualFileSystemResource) new VirtualDirectory(resourceName, fsra) :
                                                                                                                                                              new VirtualFile(resourceName, resourceAccessor));
        return Path.Combine(_driveLetter + ":\\", rootDirectoryName + "\\" + resourceName);
      }
    }

    public void RemoveResource(string rootDirectoryName, IResourceAccessor resourceAccessor)
    {
      lock (_syncObj)
      {
        VirtualRootDirectory rootDirectory = _dokanExecutor.GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return;
        string resourceName = resourceAccessor.ResourceName;
        VirtualFileSystemResource toRemove;
        if (!rootDirectory.ChildResources.TryGetValue(resourceName, out toRemove))
          return;
        rootDirectory.ChildResources.Remove(resourceName);
        toRemove.Dispose();
      }
    }

    #endregion
  }
}