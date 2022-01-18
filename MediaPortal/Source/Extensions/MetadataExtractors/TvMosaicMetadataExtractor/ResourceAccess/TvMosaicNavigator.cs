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

using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.ServerSettings;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TvMosaic.API;
using TvMosaic.Shared;

namespace TvMosaicMetadataExtractor.ResourceAccess
{
  /// <summary>
  /// Default implementation of <see cref="ITvMosaicNavigator"/> for navigating the TvMosaic API.
  /// </summary>
  public class TvMosaicNavigator : ITvMosaicNavigator
  {
    /// <summary>
    /// Id of the container containing recorded TV items sorted alphabetically
    /// </summary>
    protected internal const string RECORDED_TV_OBJECT_ID = "8F94B459-EFC0-4D91-9B29-EC3D72E92677:E44367A7-6293-4492-8C07-0E551195B99F";

    protected HttpDataProvider _httpDataProvider;

    public ICollection<string> GetRootContainerIds()
    {
      return new List<string>
      {
        // Currently only the recorded TV container seems logical to include
        RECORDED_TV_OBJECT_ID,
      };
    }

    public Items GetChildItems(string containerId)
    {
      return GetObjectResponseAsync(containerId, true).Result?.Items;
    }

    public RecordedTV GetItem(string itemId)
    {
      return GetObjectResponseAsync(itemId, false).Result?.Items?.FirstOrDefault();
    }

    public bool ObjectExists(string objectId)
    {
      return GetObjectResponseAsync(objectId, false).Result != null;
    }

    /// <summary>
    /// Asynchronously retrieves the object with the specified id, or it's child items.
    /// </summary>
    /// <param name="objectId">The id of the object to retrieve.</param>
    /// <param name="childrenRequest">Whether to request the object's child items.</param>
    /// <returns></returns>
    public async Task<ObjectResponse> GetObjectResponseAsync(string objectId, bool childrenRequest)
    {
      ObjectRequester request = new ObjectRequester
      {
        ObjectID = objectId,
        ChildrenRequest = childrenRequest
      };

      // We could be called concurrently from multiple threads so use a local reference
      // to the data provider to avoid another thread changing it whilst we are using it.
      HttpDataProvider httpDataProvider = _httpDataProvider;
      // There's a potential race condition when checking whether to create a new instance if the class
      // reference was null, but it will just cause another instance to be constructed unnecessarily and
      // won't effect usage so just allow it and avoid a lock.
      if (httpDataProvider == null)
        _httpDataProvider = httpDataProvider = GetHttpDataProvider();

      var response = await httpDataProvider.GetObject(request);
      if (response.Status != StatusCode.STATUS_OK)
        return null;
      return response.Result;
    }

    public string GetObjectFriendlyName(string objectId)
    {
      if (objectId == RECORDED_TV_OBJECT_ID)
        return "TvMosaic Recorded TV";
      //ToDo: we could retrieve the actual name of the object from the API, but for now we avoid an additional network request.
      return objectId;
    }

    HttpDataProvider GetHttpDataProvider()
    {
      TvMosaicProviderSettings settings = GetSettings();
      return new HttpDataProvider(settings.Host, 9270, settings.Username ?? string.Empty, settings.Password ?? string.Empty);
    }

    protected TvMosaicProviderSettings GetSettings()
    {
      // If running on the client we need to load the settings from the server
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>(false);
      if (serverSettings != null)
        return serverSettings.Load<TvMosaicProviderSettings>();
      // We are running on the server so can load the settings locally
      return ServiceRegistration.Get<ISettingsManager>().Load<TvMosaicProviderSettings>();
    }
  }
}
