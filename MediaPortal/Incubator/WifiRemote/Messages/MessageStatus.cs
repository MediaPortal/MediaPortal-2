﻿#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  /// <summary>
  /// Contains status information about the MediaPortal instance.
  /// </summary>
  internal class MessageStatus : IMessage
  {
    /// <summary>
    /// The localized name of "fullscreen".
    /// Used to detect if the player is in fullscreen
    /// mode without any dialog on top.
    /// </summary>
    private String localizedFullscreen;

    private String localizedFullscreenVideo;
    private String localizedFullscreenTV;
    private String localizedFullscreenMusic;

    public string Type
    {
      get { return "status"; }
    }

    private bool isPlaying;

    /// <summary>
    /// <code>true</code> if MediaPortal is playing a file
    /// </summary>
    public bool IsPlaying
    {
      get
      {
        if (ServiceRegistration.Get<IPlayerManager>().NumActiveSlots > 0)
          isPlaying = Helper.IsNowPlaying();
        return isPlaying;
      }
    }

    private bool isPaused;

    /// <summary>
    /// <code>true</code> if MediaPortal is playing a file but it's paused
    /// </summary>
    public bool IsPaused
    {
      get
      {
        if (ServiceRegistration.Get<IPlayerManager>().NumActiveSlots > 0)
          isPaused = ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext.PlaybackState == PlaybackState.Paused;
        return isPaused;
      }
    }

    /// <summary>
    /// <code>true</code> if g_Play is in fullscreen and on top
    /// </summary>
    public bool IsPlayerOnTop
    {
      get
      {
        return ServiceRegistration.Get<IPlayerContextManager>().IsFullscreenContentWorkflowStateActive;
        /*return (currentModule == localizedFullscreen ||
                currentModule == localizedFullscreenMusic ||
                currentModule == localizedFullscreenTV ||
                currentModule == localizedFullscreenVideo);*/
      }
    }

    private string title;

    /// <summary>
    /// Media title
    /// </summary>
    public string Title
    {
      get
      {
        try
        {
          if (ServiceRegistration.Get<IPlayerManager>().NumActiveSlots > 0)
            title = ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext.CurrentPlayer.MediaItemTitle;
          return title;
        }
        catch (Exception)
        {
          title = "";
          return "";
        }
      }
    }

    private string currentModule;

    /// <summary>
    /// Currently active module
    /// </summary>
    public string CurrentModule
    {
      get
      {
        try
        {
          currentModule = LocalizationHelper.Translate(ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext.WorkflowState.DisplayLabel);
          //currentModule = ServiceRegistration.Get<IScreenManager>().ActiveScreenName;

          return currentModule;
        }
        catch (Exception)
        {
          currentModule = "";
          return "";
        }
      }
    }

    /// <summary>
    /// Contructor.
    /// </summary>
    public MessageStatus()
    {

    }

    /// <summary>
    /// Checks if the status message has changed since 
    /// the last call.
    /// </summary>
    /// <returns>true if the status has changed, false otherwise</returns>
    public bool IsChanged()
    {
      return (isPlaying != IsPlaying
              || isPaused != IsPaused
              || title != Title
              || currentModule != CurrentModule);
    }
  }
}
