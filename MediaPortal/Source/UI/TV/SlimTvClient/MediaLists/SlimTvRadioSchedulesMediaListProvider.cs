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

using MediaPortal.Common;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.Workflow;
using System;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public class SlimTvRadioSchedulesMediaListProvider : BaseSchedulesMediaListProvider
  {
    public SlimTvRadioSchedulesMediaListProvider()
    {
      _showSchedules = ShowSchedules;
      _mediaType = MediaType.Radio;
    }

    public static void ShowSchedules()
    {
      Guid WF_STATE_ID_SCHEDULE_LIST = new Guid("9C095D73-D65D-42E6-9997-EF328C33F7F8");
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(WF_STATE_ID_SCHEDULE_LIST);
    }
  }
}
