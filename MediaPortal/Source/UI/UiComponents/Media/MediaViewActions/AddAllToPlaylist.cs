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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.UiComponents.Media.Extensions;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Views;
using MediaPortal.UiComponents.Media.Models;
using System.Linq;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Media.MediaViewActions
{
  public class AddAllToPlaylist : IMediaViewAction, IDeferredMediaViewAction
  {

    public Task<bool> IsAvailableAsync(View view)
    {
      return Task.FromResult(view != null && !view.IsEmpty);
    }

    public async Task<bool> ProcessAsync(View view)
    {
      MediaNavigationModel model = MediaNavigationModel.GetCurrentInstance();
      List<MediaItem> items = view.AllMediaItems.ToList();
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      var navData = MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false);
      if (navData.CurrentSorting != null)
        items.Sort(navData.CurrentSorting);
      model.AddMediaItemstoPlaylist(() => items);
      return true;
    }
  }
}
