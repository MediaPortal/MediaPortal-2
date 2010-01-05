#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.FileEventNotification;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Media.MediaProviders.Tve3MediaProvider
{
  /// <summary>
  /// Media provider implementation for accessing TvServer via WebServiceInterface.
  /// </summary>
  public class Tve3MediaProvider : IBaseMediaProvider
  {
    /// <summary>
    /// GUID string for the Tve3 media provider.
    /// </summary>
    protected const string TVE3_MEDIA_PROVIDER_ID_STR = "{DE191DC6-9E95-41b2-8459-36099E2C2774}";

    /// <summary>
    /// Tve3 media provider GUID.
    /// </summary>
    public static Guid TVE3_MEDIA_PROVIDER_ID = new Guid(TVE3_MEDIA_PROVIDER_ID_STR);


    #region Protected fields

    #endregion

    #region Ctor

    public Tve3MediaProvider()
    {
      _metadata = new MediaProviderMetadata(TVE3_MEDIA_PROVIDER_ID, "[Tve3MediaProvider.Name]", false);
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
      return new Tve3ResourceAccessor(this, path);
    }

    #endregion

    #region IMediaProvider Member

    public MediaProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    #endregion
  }
}
