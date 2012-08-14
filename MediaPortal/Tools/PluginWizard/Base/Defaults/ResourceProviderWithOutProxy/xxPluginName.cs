#region Copyright (C) 2007-xxCurrentYear Team MediaPortal

/*
    Copyright (C) 2007-xxCurrentYear Team MediaPortal
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
using MediaPortal.Extensions.BassLibraries;

namespace MediaPortal.Extensions.ResourceProviders.xxPluginName
{
  /// <summary>
  /// Resource provider implementation providing resource accessor for xxx.
  /// </summary>
  public class xxPluginName : IBaseResourceProvider
  {
    #region Public constants

    /// <summary>
    /// GUID string for xxPluginName.
    /// </summary>
    protected const string RESOURCE_PROVIDER_ID_STR = "{xxPluginID}";

    /// <summary>
    /// xxPluginName GUID.
    /// </summary>
    public static Guid RESOURCE_PROVIDER_ID = new Guid(RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[xxPluginName.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[xxPluginName.Description]";

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    #endregion

    #region Ctor

    public xxPluginName()
    {
      _metadata = new ResourceProviderMetadata(RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, true);
    }

    #endregion


    #region Public methods
    public static string BuildProviderPath()
    {
      return ;
    }

    public static ResourcePath ToResourcePath()
    {
      return ResourcePath.BuildBaseProviderPath(RESOURCE_PROVIDER_ID, BuildProviderPath());
    }

    #endregion

    #region IBaseResourceProvider implementation

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool IsResource(string path)
    {
      return ;
    }

    public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
    {
      result = new xxShortNameResourceAccessor();
      return true;
    }

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      if (string.IsNullOrEmpty(pathStr))
        return null;

      return ;
    }

    #endregion
  }
}
