﻿#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
          VideoAspect.ASPECT_ID,
          new Guid("C389F655-ED60-4271-91EA-EC589BD815C6") /* RecordingAspect.ASPECT_ID*/
      };

    public const string SCREEN_RECORDINGS_FILTER_BY_CHANNEL = "RecordingsByChannel";
    public const string SCREEN_RECORDINGS_FILTER_BY_NAME = "RecordingsByName";

    public const string RES_FILTER_BY_CHANNEL_MENU_ITEM = "[SlimTvClient.ChannelMenuItemLabel]";
    public const string RES_FILTER_BY_NAME_MENU_ITEM = "[SlimTvClient.NameFilterMenuItemLabel]";

    public const string RES_FILTER_CHANNEL_NAVBAR_DISPLAY_LABEL = "[SlimTvClient.ChannelNavBarItemLabel]";
    public const string RES_FILTER_NAME_NAVBAR_DISPLAY_LABEL = "[SlimTvClient.NameFilterNavBarItemLabel]";

    public const string RES_RECORDINGS_VIEW_NAME = "[SlimTvClient.RecordingsRootViewName]";
  }
}
