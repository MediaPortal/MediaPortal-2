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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider
{
  /// <summary>
  /// Resource provider implementation for Url resources.
  /// </summary>
  public class RawUrlResourceProvider : IBaseResourceProvider
  {
    #region Public constants

    /// <summary>
    /// GUID string for the raw url resource provider.
    /// </summary>
    protected const string RAW_URL_RESOURCE_PROVIDER_ID_STR = "{12A38B49-6115-4A8E-B638-087D643669AB}";

    /// <summary>
    /// Raw url resource provider GUID.
    /// </summary>
    public static Guid RAW_URL_RESOURCE_PROVIDER_ID = new Guid(RAW_URL_RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[RawUrlResourceProvider.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[RawUrlResourceProvider.Description]";

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    #endregion

    #region Ctor

    public RawUrlResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(RAW_URL_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, true, true);
    }

    #endregion

    #region IBaseResourceProvider Member

    public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
    {
      result = null;
      if (!IsResource(path))
        return false;

      result = new RawUrlResourceAccessor(path);
      return true;
    }

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      if (IsResource(pathStr))
        return new ResourcePath(new[] { new ProviderPathSegment(_metadata.ResourceProviderId, pathStr, true) });
      return null;
    }

    public bool IsResource(string url)
    {
      Uri uri;
      if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out uri))
        return !uri.IsFile;
      return false;
    }

    #endregion

    #region IResourceProvider Member

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    public static ResourcePath ToProviderResourcePath(string path)
    {
      return ResourcePath.BuildBaseProviderPath(RAW_URL_RESOURCE_PROVIDER_ID, path);
    }
  }
}
