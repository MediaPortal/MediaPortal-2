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

using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Plugins.SlimTv.Client.TvHandler
{
  public class SlimTvConsts
  {
    public const string MEDIA_NAVIGATION_MODE = "Recordings";

    public static Guid WF_MEDIA_NAVIGATION_ROOT_STATE = new Guid("9D5B01A7-035F-46CF-8246-3C158C6CA960");
    public static Guid[] NECESSARY_RECORDING_MIAS = new []
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          new Guid("8DB70262-0DCE-4C80-AD03-FB1CDF7E1913") /* RecordingAspect.ASPECT_ID*/
      };

    public static Guid[] OPTIONAL_RECORDING_MIAS = new[]
    {
      VideoStreamAspect.ASPECT_ID,    // Needed for calculating play percentage
      VideoAspect.Metadata.AspectId,  // Needed for playing TV recording
      AudioAspect.Metadata.AspectId   // Needed for playing Radio recording
    };

    public static Guid WF_TV_MAIN_STATE = new Guid("C7646667-5E63-48c7-A490-A58AC9518CFA");
    public static Guid WF_TV_SINGLE_CHANNEL_GUIDE_STATE = new Guid("A40F05BB-022E-4247-8BEE-16EB3E0B39C5");
    public static Guid WF_TV_MULTI_CHANNEL_GUIDE_STATE = new Guid("7323BEB9-F7B0-48c8-80FF-8B59A4DB5385");
    public static Guid WF_TV_PROGRAM_SEARCH_STATE = new Guid("CB5D4851-27D2-4222-B6A0-703EDC2071B5");

    public static Guid WF_RADIO_MAIN_STATE = new Guid("55F6CC8D-1D98-426F-8733-E6DF2861F706");
    public static Guid WF_RADIO_SINGLE_CHANNEL_GUIDE_STATE = new Guid("7365DA33-4687-43F4-9652-F6652468D4B8");
    public static Guid WF_RADIO_MULTI_CHANNEL_GUIDE_STATE = new Guid("64AEE61A-7E45-450D-AA65-F4C109E3A7B3");
    public static Guid WF_RADIO_PROGRAM_SEARCH_STATE = new Guid("F6B76F5F-1E37-4C4D-BB32-79AFB7A67951");

    public static Guid WF_SCHEDULES_STATE = new Guid("88842E97-2EF9-4658-AD35-8D74E3C689A4");

    public static HashSet<Guid> TV_WF_STATES = new HashSet<Guid>
    {
      WF_TV_MAIN_STATE,
      WF_TV_SINGLE_CHANNEL_GUIDE_STATE,
      WF_TV_MULTI_CHANNEL_GUIDE_STATE,
      WF_TV_PROGRAM_SEARCH_STATE,
    };

    public static HashSet<Guid> RADIO_WF_STATES = new HashSet<Guid>
    {
      WF_RADIO_MAIN_STATE,
      WF_RADIO_SINGLE_CHANNEL_GUIDE_STATE,
      WF_RADIO_MULTI_CHANNEL_GUIDE_STATE,
      WF_RADIO_PROGRAM_SEARCH_STATE,
    };

    public const string KEY_CHANNEL = "Channel";
    public const string KEY_STARTTIME = "StartTime";
    public const string KEY_ENDTIME = "EndTime";

    public const string SCREEN_RECORDINGS_FILTER_BY_CHANNEL = "RecordingsByChannel";
    public const string SCREEN_RECORDINGS_FILTER_BY_MEDIA_TYPE = "RecordingsByMediaType";
    public const string SCREEN_RECORDINGS_SHOW_ITEMS = "RecordingsShowItems";

    public const string RES_FILTER_BY_CHANNEL_MENU_ITEM = "[SlimTvClient.ChannelMenuItemLabel]";
    public const string RES_FILTER_BY_MEDIA_TYPE_MENU_ITEM = "[SlimTvClient.MediaTypeMenuItemLabel]";
    public const string RES_SHOW_ALL_RECORDINGS_ITEMS_MENU_ITEM = "[SlimTvClient.ShowAllRecordingsItemsMenuItem]";

    public const string RES_FILTER_CHANNEL_NAVBAR_DISPLAY_LABEL = "[SlimTvClient.ChannelNavBarItemLabel]";
    public const string RES_FILTER_MEDIA_TYPE_NAVBAR_DISPLAY_LABEL = "[SlimTvClient.MediaTypeNavBarItemLabel]";
    public const string RES_FILTER_RECORDINGS_ITEMS_NAVBAR_DISPLAY_LABEL = "[SlimTvClient.FilterRecordingsItemsNavbarDisplayLabel]";

    public const string RES_RECORDINGS_VIEW_NAME = "[SlimTvClient.RecordingsRootViewName]";
  }
}
