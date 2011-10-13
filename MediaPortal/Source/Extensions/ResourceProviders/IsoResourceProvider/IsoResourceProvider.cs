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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using ISOReader;

namespace MediaPortal.Extensions.ResourceProviders.IsoResourceProvider
{
  /// <summary>
  /// Resource provider implementation for ISO files.
  /// </summary>
  public class IsoResourceProvider : IChainedResourceProvider
  {
    #region Consts

    protected const string ISO_RESOURCE_PROVIDER_ID_STR = "{112728B1-F71D-4284-9E5C-3462E8D3C74D}";
    public static Guid ISO_RESOURCE_PROVIDER_ID = new Guid(ISO_RESOURCE_PROVIDER_ID_STR);

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    #endregion

    #region Ctor

    public IsoResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(ISO_RESOURCE_PROVIDER_ID, "[IsoResourceProvider.Name]", false);
    }

    #endregion

    #region IResourceProvider implementation

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    #region IChainedResourceProvider implementation

    public bool CanChainUp(IResourceAccessor potentialBaseResourceAccessor)
    {
      string resourcePathName = potentialBaseResourceAccessor.ResourcePathName;
      if (string.IsNullOrEmpty(resourcePathName) || !potentialBaseResourceAccessor.IsFile ||
          !".iso".Equals(PathHelper.GetExtension(resourcePathName), StringComparison.OrdinalIgnoreCase))
        return false;

      using (ILocalFsResourceAccessor localFsResourceAccessor = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(potentialBaseResourceAccessor.Clone()))
      using (IsoReader isoReader = new IsoReader())
        try
        {
          isoReader.Open(localFsResourceAccessor.LocalFileSystemPath);
          return true;
        }
        catch (Exception) {}
      return false;
    }

    public bool IsResource(IResourceAccessor baseResourceAccessor, string path)
    {
      string resourceName = baseResourceAccessor.ResourceName;
      if (string.IsNullOrEmpty(resourceName) || baseResourceAccessor.IsFile)
        return false;

      using (ILocalFsResourceAccessor localFsResourceAccessor = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(baseResourceAccessor.Clone()))
      using (IsoReader isoReader = new IsoReader())
      {
        isoReader.Open(localFsResourceAccessor.LocalFileSystemPath);

        string dosPath = Path.GetDirectoryName(LocalFsResourceProviderBase.ToDosPath(path));
        string dosResource = "\\" + LocalFsResourceProviderBase.ToDosPath(path);

        string[] dirList = isoReader.GetDirectories(dosPath, SearchOption.TopDirectoryOnly);
        return dirList.Any(entry => entry.Equals(dosResource, StringComparison.OrdinalIgnoreCase));
      }
    }

    public IResourceAccessor CreateResourceAccessor(IResourceAccessor baseResourceAccessor, string path)
    {
      return new IsoResourceAccessor(this, baseResourceAccessor, path);
    }

    #endregion
  }
}
