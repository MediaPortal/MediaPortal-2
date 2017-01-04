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

namespace MediaPortal.Common.Services.ResourceAccess.VirtualResourceProvider
{
  /// <summary>
  /// Resource provider implementation for virtual resources.
  /// </summary>
  public class VirtualResourceProvider : IBaseResourceProvider
  {
    #region Public constants

    /// <summary>
    /// GUID string for the virtual resource provider.
    /// </summary>
    protected const string VIRTUAL_RESOURCE_PROVIDER_ID_STR = "{00000000-0000-0000-0000-000000000000}";

    /// <summary>
    /// Virtual resource provider GUID.
    /// </summary>
    public static Guid VIRTUAL_RESOURCE_PROVIDER_ID = new Guid(VIRTUAL_RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[VirtualResourceProvider.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[VirtualResourceProvider.Description]";

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    #endregion

    #region Ctor

    public VirtualResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(VIRTUAL_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, true, false);
    }

    public static string BuildProviderPath(Guid virtualId)
    {
      return "/" + virtualId;
    }

    public static ResourcePath ToResourcePath(Guid virtualId)
    {
      return ResourcePath.BuildBaseProviderPath(VIRTUAL_RESOURCE_PROVIDER_ID, BuildProviderPath(virtualId));
    }

    #endregion

    #region IBaseResourceProvider implementation

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool IsResource(string path)
    {
      Guid virtualId;
      return Guid.TryParse(path, out virtualId);
    }

    public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
    {
      Guid virtualId;
      if(Guid.TryParse(path, out virtualId))
      {
        result = new VirtualResourceAccessor(this, virtualId);
        return true;
      }
      result = null;
      return false;
    }

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      if (string.IsNullOrEmpty(pathStr))
        return null;
      // The input string is given by the user. We can cope with two formats:
      // 1) A resource provider path in form of a virtual ID (GUID)
      // 2) A resource path in the resource path syntax (i.e. {[Provider-Id]}:///1)
      if (IsResource(pathStr))
        return new ResourcePath(new ProviderPathSegment[]
          {
              new ProviderPathSegment(VIRTUAL_RESOURCE_PROVIDER_ID, pathStr, true),
          });
      try
      {
        return ResourcePath.Deserialize(pathStr);
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    #endregion
  }
}
