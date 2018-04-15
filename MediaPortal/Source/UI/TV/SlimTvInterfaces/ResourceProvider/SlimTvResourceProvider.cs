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

namespace MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider
{
  /// <summary>
  /// Resource provider implementation for accessing TvServer via WebServiceInterface.
  /// </summary>
  public class SlimTvResourceProvider : IBaseResourceProvider, IDisposable
  {
    #region Consts

    /// <summary>
    /// GUID string for the SlimTV resource provider based on network url (rtsp).
    /// </summary>
    protected const string SLIMTV_RESOURCE_PROVIDER_ID_STR = "04AFFA6C-EA42-4bd3-AA6F-C16DCEF1D693";

    /// <summary>
    /// GUID string for the SlimTV resource provider based on local files.
    /// </summary>
    protected const string SLIMTV_FS_RESOURCE_PROVIDER_ID_STR = "2E6A22C2-386E-43C7-9FA3-BB36547B5583";

    /// <summary>
    /// SlimTV resource provider GUID based on network url (rtsp).
    /// </summary>
    public static Guid SLIMTV_RESOURCE_PROVIDER_ID = new Guid(SLIMTV_RESOURCE_PROVIDER_ID_STR);

    /// <summary>
    /// SlimTV resource provider GUID based on local files.
    /// </summary>
    public static Guid SLIMTV_FS_RESOURCE_PROVIDER_ID = new Guid(SLIMTV_FS_RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[SlimTvResourceProvider.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[SlimTvResourceProvider.Description]";

    #endregion

    #region Protected fields

    #endregion

    #region Ctor

    public SlimTvResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(SLIMTV_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, true, true);
    }

    #endregion

    #region Static methods

    /// <summary>
    /// Creates a matching resource accessor, depending on type of the given <see cref="path"/>.
    /// </summary>
    /// <param name="path">RTSP url or local path</param>
    /// <returns>Either a <see cref="INetworkResourceAccessor"/> or <see cref="ILocalFsResourceAccessor"/></returns>
    public static IResourceAccessor GetResourceAccessor(string path)
    {
      // Parse slotindex from path and cut the prefix off.
      int slotIndex;
      if (!Int32.TryParse(path.Substring(0, 1), out slotIndex))
        return null;
      path = path.Substring(2, path.Length - 2);
      return GetResourceAccessor(slotIndex, path);
    }

    /// <summary>
    /// Creates a matching resource accessor, depending on type of the given <see cref="path"/>.
    /// </summary>
    /// <param name="slotIndex">Slot index</param>
    /// <param name="path">RTSP url or local path</param>
    /// <returns>Either a <see cref="INetworkResourceAccessor"/> or <see cref="ILocalFsResourceAccessor"/></returns>
    public static IResourceAccessor GetResourceAccessor(int slotIndex, string path)
    {
      // Test for RTSP url
      if (path.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
        return new SlimTvResourceAccessor(slotIndex, path);
      // otherwise it's a local file.
      return new SlimTvFsResourceAccessor(slotIndex, path);
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

    public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
    {
      result = GetResourceAccessor(path);
      return result != null;
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
