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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TvMosaic.API;

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

    //ToDo: These should be retrieved from settings, but the settings are currently unavailable on the server
    const string SERVER_IP = "127.0.0.1";
    const int SERVER_PORT = 9270;

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

      var response = await new HttpDataProvider(SERVER_IP, SERVER_PORT).GetObject(request);
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
  }
}
