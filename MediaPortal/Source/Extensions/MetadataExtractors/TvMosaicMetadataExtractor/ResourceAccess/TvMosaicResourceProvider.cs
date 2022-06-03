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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities;
using System;

namespace TvMosaicMetadataExtractor.ResourceAccess
{
  /// <summary>
  /// Implementation of <see cref="IBaseResourceProvider"/> that provides access to items located on a TvMosaic server.
  /// </summary>
  public class TvMosaicResourceProvider : IBaseResourceProvider
  {
    #region Consts

    protected const string TVMOSAIC_RESOURCE_PROVIDER_ID_STR = "{8699B370-81A3-4027-A78A-54E68794BCFD}";
    public static Guid TVMOSAIC_RESOURCE_PROVIDER_ID = new Guid(TVMOSAIC_RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[TvMosaicResourceProvider.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[TvMosaicResourceProvider.Description]";

    internal const string ROOT_PROVIDER_PATH = "/";

    #endregion

    protected ResourceProviderMetadata _metadata;

    public TvMosaicResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(TVMOSAIC_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, false, true);
    }

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      if (string.IsNullOrEmpty(pathStr))
        return null;

      // Check if the path is already in the resource path syntax (i.e. {[Base-Provider-Id]}://[Base-Provider-Path])
      // If not, expand it to include the provider id
      if (!pathStr.Contains("://"))
        return ToProviderResourcePath(pathStr);

      // Else, path looks like it is in the resource path syntax so can just be deserialized as is
      try
      {
        return ResourcePath.Deserialize(pathStr);
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    public bool IsResource(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;
      if (path == ROOT_PROVIDER_PATH)
        return true;
      string objectId = ToObjectId(path);
      return TvMosaicResourceAccessor.IsContainerId(objectId) || TvMosaicResourceAccessor.IsItemId(objectId);
    }

    public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
    {
      if (!IsResource(path))
      {
        result = null;
        return false;
      }

      result = new TvMosaicResourceAccessor(this, path);
      return true;
    }

    public static string ToProviderPath(string objectId)
    {
      return StringUtils.CheckPrefix(objectId, ROOT_PROVIDER_PATH);
    }

    /// <summary>
    /// Converts the specified provider path to a TvMosiac object id.
    /// </summary>
    /// <param name="providerPath">The path to convert to an id.</param>
    /// <returns>The TvMosaic object id that this path points to.</returns>
    public static string ToObjectId(string providerPath)
    {
      if (string.IsNullOrEmpty(providerPath) || providerPath == ROOT_PROVIDER_PATH)
        return null;
      providerPath = StringUtils.RemoveSuffixIfPresent(providerPath, "/");
      return StringUtils.RemovePrefixIfPresent(providerPath, ROOT_PROVIDER_PATH);
    }

    public static ResourcePath ToProviderResourcePath(string path)
    {
      return ResourcePath.BuildBaseProviderPath(TVMOSAIC_RESOURCE_PROVIDER_ID, ToProviderPath(path));
    }
  }
}
