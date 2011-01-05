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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.ResourceAccess;

namespace MediaPortal.Plugins.SlimTv.Providers
{
  /// <summary>
  /// Media provider implementation for accessing TvServer via WebServiceInterface.
  /// </summary>
  public class SlimTvMediaProvider : IBaseMediaProvider, IDisposable
  {
    /// <summary>
    /// GUID string for the Tve3 media provider.
    /// </summary>
    protected const string SLIMTV_MEDIA_PROVIDER_ID_STR = "04AFFA6C-EA42-4bd3-AA6F-C16DCEF1D693";

    /// <summary>
    /// Tve3 media provider GUID.
    /// </summary>
    public static Guid SLIMTV_MEDIA_PROVIDER_ID = new Guid(SLIMTV_MEDIA_PROVIDER_ID_STR);


    #region Protected fields

    #endregion

    #region Ctor

    public SlimTvMediaProvider()
    {
      _metadata = new MediaProviderMetadata(SLIMTV_MEDIA_PROVIDER_ID, "[SlimTvMediaProvider.Name]", false);
    }

    #endregion

    #region Protected fields

    protected MediaProviderMetadata _metadata;

    #endregion

    #region Public methods

    #endregion

    #region IBaseMediaProvider Member

    public bool IsResource(string path)
    {
      return true;
    }

    public IResourceAccessor CreateMediaItemAccessor(string path)
    {
      return new SlimTvResourceAccessor(path);
    }

    #endregion

    #region IMediaProvider Member

    public MediaProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion

    #region IBaseMediaProvider Member


    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {
    }

    #endregion
  }
}
