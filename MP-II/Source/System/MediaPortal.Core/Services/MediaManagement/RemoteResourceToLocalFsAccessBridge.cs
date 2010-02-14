#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Core.Services.MediaManagement
{
  /// <summary>
  /// Access bridge logic which maps a complex resource accessor to a local file resource.
  /// </summary>
  public class RemoteResourceToLocalFsAccessBridge : ResourceAccessorBase, ILocalFsResourceAccessor
  {
    #region Protected fields

    protected IResourceAccessor _baseAccessor;

    #endregion

    #region Ctor & maintenance

    /// <summary>
    /// Creates a new instance of this class which is based on the given <paramref name="baseAccessor"/>.
    /// </summary>
    /// <param name="baseAccessor">Resource accessor denoting a file.</param>
    /// <exception cref="ArgumentException">If the given <paramref name="baseAccessor"/> doesn't denote a file
    /// resource (i.e. <c><see cref="IResourceAccessor.IsFile"/> == false</c>.</exception>
    public RemoteResourceToLocalFsAccessBridge(IResourceAccessor baseAccessor)
    {
      if (!baseAccessor.IsFile)
        throw new ArgumentException("The given resource accessor doesn't denote a file resource", "baseAccessor");
      _baseAccessor = baseAccessor;
    }

    #endregion

    #region IResourceAccessor implementation

    public IMediaProvider ParentProvider
    {
      get { return null; }
    }

    public bool IsFile
    {
      get { return true; }
    }

    public string ResourceName
    {
      get { return _baseAccessor.ResourceName; }
    }

    public string ResourcePathName
    {
      get { return _baseAccessor.ResourcePathName; }
    }

    public ResourcePath LocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(LocalFsMediaProviderBase.LOCAL_FS_MEDIA_PROVIDER_ID, LocalFileSystemPath); }
    }

    public DateTime LastChanged
    {
      get { return _baseAccessor.LastChanged; }
    }

    public bool Exists(string path)
    {
      return _baseAccessor.Exists(path);
    }

    public Stream OpenRead()
    {
      // Using the stream on the base accessor doesn't cost so much resources than creating the bridge here
      return _baseAccessor.OpenRead();
    }

    public Stream OpenWrite()
    {
      // Using the stream on the base accessor doesn't cost so much resources than creating the bridge here
      return _baseAccessor.OpenWrite();
    }

    #endregion

    #region IFileSystemResourceAccessor implementation

    public bool IsDirectory
    {
      get { return false; }
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      return new List<IFileSystemResourceAccessor>();
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      return new List<IFileSystemResourceAccessor>();
    }

    #endregion

    #region ILocalFsResourceAccessor implementation

    public string LocalFileSystemPath
    {
      // FIXME: Lazy initialization: Create bridge and add tidy up executor for bridge components
      get { return ""; }
    }

    #endregion
  }
}