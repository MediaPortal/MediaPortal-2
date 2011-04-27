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
using System.IO;
using System.Linq;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using ISOReader;

namespace MediaPortal.Extensions.MediaProviders.IsoMediaProvider
{
  /// <summary>
  /// Media provider implementation for the ZIP files.
  /// </summary>
  public class IsoMediaProvider : IChainedMediaProvider
  {
    #region Public constants

    protected const string ISO_MEDIA_PROVIDER_ID_STR = "{112728B1-F71D-4284-9E5C-3462E8D3C74D}";
    public static Guid ISO_MEDIA_PROVIDER_ID = new Guid(ISO_MEDIA_PROVIDER_ID_STR);

    #endregion

    #region Protected fields

    protected MediaProviderMetadata _metadata;

    #endregion

    #region Ctor

    public IsoMediaProvider()
    {
      _metadata = new MediaProviderMetadata(ISO_MEDIA_PROVIDER_ID, "[IsoMediaProvider.Name]", false);
    }

    #endregion

    #region Implementation of IMediaProvider

    public MediaProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    #region Implementation of IChainedMediaProvider

    public bool CanChainUp(IResourceAccessor potentialBaseResourceAccessor)
    {
      if (string.IsNullOrEmpty(potentialBaseResourceAccessor.ResourceName) || !potentialBaseResourceAccessor.IsFile)
        return false;
      if (Path.GetExtension(potentialBaseResourceAccessor.ResourceName).Equals(".iso",StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
      return false;
    }

    public bool IsResource(IResourceAccessor baseResourceAccessor, string path)
    {
      if (string.IsNullOrEmpty(baseResourceAccessor.ResourceName) || baseResourceAccessor.IsFile)
        return false;

      IsoReader isoReader = new IsoReader();
      isoReader.Open(baseResourceAccessor.ResourcePathName);
      
      string dosPath = "\\" + LocalFsMediaProviderBase.ToDosPath(Path.GetDirectoryName(path));
      string dosResource = "\\" + LocalFsMediaProviderBase.ToDosPath(path);
      
      string[] dirList = isoReader.GetDirectories(dosPath, SearchOption.TopDirectoryOnly);
      return dirList.Any(entry => entry.Equals(dosResource, StringComparison.OrdinalIgnoreCase));
      
    }

    public IResourceAccessor CreateResourceAccessor(IResourceAccessor baseResourceAccessor, string path)
    {
      return new IsoResourceAccessor(this, baseResourceAccessor, path);
    }

    #endregion
  }
}
