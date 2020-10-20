#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.SlimTv.Client.MediaItemActions;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Plugins.SlimTv.Client.Models.ScreenData;

namespace MediaPortal.Plugins.SlimTv.Client.Actions
{
  public class DeleteAllRecordings : IWorkflowContributor
  {
#region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateResourceString("[SlimTvClient.DeleteAllRecordings.Text]"); }
    }

    public void Initialize()
    {
    }

    public void Uninitialize()
    {
    }

    public bool IsActionVisible(NavigationContext context)
    {
      NavigationData navigationData = MediaNavigationModel.GetNavigationData(context, false);
      return navigationData != null && navigationData.IsEnabled && navigationData.CurrentScreenData is RecordingsShowItemsScreenData 
          && navigationData.Parent != null && !navigationData.CurrentScreenData.IsItemsEmpty;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public void Execute()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      MediaNavigationModel model = (MediaNavigationModel)workflowManager.GetModel(MediaNavigationModel.MEDIA_MODEL_ID);
      NavigationData navigationData = model.NavigationData;

      workflowManager.NavigatePopAsync(1);
      if (navigationData == null || !navigationData.IsEnabled)
      {
        ServiceRegistration.Get<ILogger>().Error("DeleteAllRecordings: No enabled navigation data present");
        return;
      }
      List<MediaItem> mediaItems = navigationData.CurrentScreenData.GetAllMediaItems().ToList();
      QueryDeleteAll(mediaItems);
    }

    public void QueryDeleteAll(List<MediaItem> mediaItems)
    {
      if (mediaItems.Count == 0)
      {
        ServiceRegistration.Get<ILogger>().Error("DeleteAllRecordings: No items to delete");
        return;
      }
      IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
      string header = LocalizationHelper.Translate(Consts.RES_CONFIRM_HEADER);
      string text = LocalizationHelper.Translate("[SlimTvClient.DeleteAllRecordings.Confirmation]", mediaItems.Count);
      Guid handle = dialogManager.ShowDialog(header, text, DialogType.YesNoDialog, false, DialogButtonType.No);
      DialogCloseWatcher dialogCloseWatcher = null;
      dialogCloseWatcher = new DialogCloseWatcher(this, handle,
        async dialogResult =>
        {
          dialogCloseWatcher?.Dispose();
          if (dialogResult == DialogResult.Yes)
          {
            await DeleteList(mediaItems);
          }
        });
    }

    public async Task DeleteList(List<MediaItem> items)
    {
      DeleteRecordingFromStorage deleter = new DeleteRecordingFromStorage();
      foreach(MediaItem item in items)
      {
        if (await deleter.IsAvailableAsync(item))
          await deleter.ProcessAsync(item);
      }
    }

#endregion
  }
}
