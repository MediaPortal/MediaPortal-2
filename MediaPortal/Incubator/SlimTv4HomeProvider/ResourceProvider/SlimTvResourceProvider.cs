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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.SlimTv.Providers
{
  /// <summary>
  /// Resource provider implementation for accessing TvServer via WebServiceInterface.
  /// </summary>
  public class SlimTvResourceProvider : IBaseResourceProvider, IDisposable
  {
    #region Consts

    /// <summary>
    /// GUID string for the Tve3 resource provider.
    /// </summary>
    protected const string SLIMTV_RESOURCE_PROVIDER_ID_STR = "04AFFA6C-EA42-4bd3-AA6F-C16DCEF1D693";

    /// <summary>
    /// Tve3 resource provider GUID.
    /// </summary>
    public static Guid SLIMTV_RESOURCE_PROVIDER_ID = new Guid(SLIMTV_RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[SlimTvResourceProvider.Name]";

    #endregion

    #region Protected fields

    #endregion

    #region Ctor

    public SlimTvResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(SLIMTV_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, true);
    }

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;

    #endregion

    #region Public methods

    #endregion

    #region IBaseResourceProvider implementation

    public bool IsResource(string path)
    {
      return true;
    }

    public IResourceAccessor CreateResourceAccessor(string path)
    {
      return SlimTvResourceAccessor.GetResourceAccessor(path);
    }

    #endregion

    #region IResourceProvider implementation

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    #region IBaseResourceProvider implementation


    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
    }

    #endregion
  }
}
