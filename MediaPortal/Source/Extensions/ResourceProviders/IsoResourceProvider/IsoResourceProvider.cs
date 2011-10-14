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
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Utilities;

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

    internal static string ToDosPath(string providerPath)
    {
      if (providerPath == "/")
        return string.Empty;
      providerPath = StringUtils.RemovePrefixIfPresent(providerPath, "/");
      return providerPath.Replace('/', Path.DirectorySeparatorChar);
    }

    internal static string ToProviderPath(string dosPath)
    {
      string path = dosPath.Replace(Path.DirectorySeparatorChar, '/');
      return StringUtils.CheckPrefix(path, "/");
    }

    #region IResourceProvider implementation

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    #region IChainedResourceProvider implementation

    public bool TryChainUp(IResourceAccessor potentialBaseResourceAccessor, string path, out IResourceAccessor resultResourceAccessor)
    {
      resultResourceAccessor = null;
      string resourcePathName = potentialBaseResourceAccessor.ResourcePathName;
      if (string.IsNullOrEmpty(resourcePathName) || !potentialBaseResourceAccessor.IsFile ||
          !".iso".Equals(DosPathHelper.GetExtension(resourcePathName), StringComparison.OrdinalIgnoreCase))
        return false;

      resultResourceAccessor = new IsoResourceAccessor(this, potentialBaseResourceAccessor, path);
      return true;
    }

    public bool IsResource(IResourceAccessor baseResourceAccessor, string path)
    {
      string resourceName = baseResourceAccessor.ResourceName;
      if (string.IsNullOrEmpty(resourceName) || baseResourceAccessor.IsFile)
        return false;

      IResourceAccessor ra = baseResourceAccessor.Clone();
      try
      {
        using (ILocalFsResourceAccessor localFsResourceAccessor = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(ra))
        using (IsoReader isoReader = new IsoReader())
        {
          isoReader.Open(localFsResourceAccessor.LocalFileSystemPath);

          string isoPath = ToDosPath(path);
          string dirPath = Path.GetDirectoryName(isoPath);
          string isoResource = "\\" + isoPath;

          string[] dirList = isoReader.GetFileSystemEntries(dirPath, SearchOption.TopDirectoryOnly);
          return dirList.Any(entry => entry.Equals(isoResource, StringComparison.OrdinalIgnoreCase));
        }
      }
      catch
      {
        ra.Dispose();
        throw;
      }
    }

    #endregion
  }
}
