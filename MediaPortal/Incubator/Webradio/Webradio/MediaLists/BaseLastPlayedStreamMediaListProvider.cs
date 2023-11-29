#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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

using MediaPortal.Common.Commands;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.ContentLists;
using System.Collections.Generic;
using System.Threading.Tasks;
using Webradio.Helper;
using Webradio.Models;
using Webradio.Player;

namespace Webradio.MediaLists
{
  public abstract class BaseLastPlayedStreamMediaListProvider : WebradioMediaListProviderBase
  {
    public override async Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason, ICollection<object> updatedObjects)
    {
      if (!updateReason.HasFlag(UpdateReason.Forced) && !updateReason.HasFlag(UpdateReason.PlaybackComplete) && !updateReason.HasFlag(UpdateReason.UserChanged))
        return true;

      ICollection<RadioStation> lastStreams = new List<RadioStation>();

      var stats = await GetSiteStats();

      lock (Radiostations.LOCK)
      {
        var streams = Radiostations.InstanceDictionary;
        if (Radiostations.InstanceDictionary.Count == 0)
        {
          return false;
        }

        foreach (var streamKey in GetStreamKeys(stats))
        {
          if (streams.TryGetValue(streamKey, out var stream))
          {
            lastStreams.Add(stream);
          }
        }
      }

      _allItems.Clear();
      foreach (RadioStation stream in lastStreams)
      {
        var item = WebradioHomeModel.CreateStreamListItem(stream);
        item.Command = new AsyncMethodDelegateCommand(async () => WebRadioPlayerHelper.PlayStream(stream));
        _allItems.Add(item);
      }

      _allItems.FireChange();

      return true;
    }

    protected abstract List<string> GetStreamKeys(UsageStatistics statistics);
  }
}
