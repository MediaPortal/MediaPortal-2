#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ServerCommunication;
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

    public override async Task<bool> IsAvailableAsync(MediaItem mediaItem)
    {
      var isAvailable = IsRecordingItem(mediaItem) && IsResourceDeletor(mediaItem);
      if (!isAvailable)
        return false;

      await TryGetScheduleAsync(mediaItem);
      return true;
    }

    private async Task TryGetScheduleAsync(MediaItem mediaItem)
    {
      _schedule = null;
      var rl = mediaItem.GetResourceLocator();
      ILocalFsResourceAccessor lfsra;
      if (!rl.TryCreateLocalFsAccessor(out lfsra))
        return;

      var tvHandler = ServiceRegistration.Get<ITvHandler>(false);
      using (lfsra)
      {
        string filePath = lfsra.LocalFileSystemPath;
        var result = await tvHandler.ScheduleControl.IsCurrentlyRecordingAsync(filePath);
        if (result.Success)
          _schedule = result.Result;
      }
    }

    public override async Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem)
    {
      var falseResult = new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(false, ContentDirectoryMessaging.MediaItemChangeType.None);
      if (_schedule != null)
      {
        var tvHandler = ServiceRegistration.Get<ITvHandler>(false);
        if (tvHandler != null)
        {
          ServiceRegistration.Get<ILogger>().Warn("DeleteRecordingFromStorage: Failed to remove current schedule.");
          var result = tvHandler.ScheduleControl.RemoveScheduleAsync(_schedule).Result;
          if (!result)
            return falseResult;
        }
      }

      // When the recording is stopped, it will be imported into library. This can lead to locked files by MetaDataExtractors.
      // So we allow some retries after a small delay here.
      for (int i = 1; i <= 3; i++)
      {
        var res = await base.ProcessAsync(mediaItem);
        if (res.Success)
          return res;

        ServiceRegistration.Get<ILogger>().Info("DeleteRecordingFromStorage: Failed to delete recording (try {0})", i);
        await Task.Delay(i * 1000);
      }
      ServiceRegistration.Get<ILogger>().Warn("DeleteRecordingFromStorage: Failed to delete recording.");
      return falseResult;
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
