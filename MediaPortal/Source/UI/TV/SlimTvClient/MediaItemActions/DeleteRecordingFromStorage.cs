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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UiComponents.Media.MediaItemActions;

namespace MediaPortal.Plugins.SlimTv.Client.MediaItemActions
{
  public class DeleteRecordingFromStorage : DeleteFromStorage
  {
    protected ISchedule _schedule;

    public DeleteRecordingFromStorage()
    {
      _defaultRules.Add(new DeleteRule
      {
        IsEnabled = true,
        DeleteEmptyFolders = true,
        HasAspectGuid = RecordingAspect.ASPECT_ID,
        DeleteOtherExtensions = new List<string> { ".xml", ".jpg" } /* Standard .xml file of recording and optional created thumbnail */
      });
    }

    public override bool IsAvailable(MediaItem mediaItem)
    {
      var isAvailable = IsRecording(mediaItem) && IsResourceDeletor(mediaItem);
      if (!isAvailable)
        return false;
      // TODO: make async as well
      TryGetScheduleAsync(mediaItem).Wait();
      return true;
    }

    private async Task TryGetScheduleAsync(MediaItem mediaItem)
    {
      _schedule = null;
      var rl = mediaItem.GetResourceLocator();
      var serverLocalPath = LocalFsResourceProviderBase.ToDosPath(rl.NativeResourcePath);
      var tvHandler = ServiceRegistration.Get<ITvHandler>(false);
      if (tvHandler == null)
        return;

      var result = await tvHandler.ScheduleControl.IsCurrentlyRecordingAsync(serverLocalPath);
      if (result.Success)
        _schedule = result.Result;
    }

    public override bool Process(MediaItem mediaItem, out ContentDirectoryMessaging.MediaItemChangeType changeType)
    {
      changeType = ContentDirectoryMessaging.MediaItemChangeType.None;
      if (_schedule != null)
      {
        var tvHandler = ServiceRegistration.Get<ITvHandler>(false);
        if (tvHandler != null)
        {
          //var result = await tvHandler.ScheduleControl.RemoveScheduleAsync(_schedule);
          // TODO: Async
          var result = tvHandler.ScheduleControl.RemoveScheduleAsync(_schedule).Result;
          if (!result)
            return false;
        }
      }

      // When the recording is stopped, it will be imported into library. This can lead to locked files by MetaDataExtractors.
      // So we allow a 2nd try after a small delay here.
      for (int i = 2; i > 0; i--)
      {
        if (base.Process(mediaItem, out changeType))
          return true;

        Thread.Sleep(1000);
      }
      return false;
    }

    public override string ConfirmationMessage
    {
      get
      {
        return _schedule != null ?
          "[SlimTvClient.DeleteRecording.Confirmation]" :
          "[Media.DeleteFromStorage.Confirmation]";
      }
    }
  }
}
