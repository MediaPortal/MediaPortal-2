#region Copyright (C) 2007-2015 Team MediaPortal

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
    private bool _isPlaying;
    private bool _isPaused;
    private string _title;
    private string _currentModule;

    public string Type
    {
      get { return "status"; }
    }

    /// <summary>
    /// <code>true</code> if MediaPortal is playing a file
    /// </summary>
    public bool IsPlaying
    {
      get
      {
        var playerManager = ServiceRegistration.Get<IPlayerManager>(false);
        if (playerManager?.NumActiveSlots > 0)
          return Helper.IsNowPlaying();

        return false;
      }
    }

    /// <summary>
    /// <code>true</code> if MediaPortal is playing a file but it's paused
    /// </summary>
    public bool IsPaused
    {
      get
      {
        var playerManager = ServiceRegistration.Get<IPlayerManager>(false);
        if (playerManager?.NumActiveSlots > 0)
          return ServiceRegistration.Get<IPlayerContextManager>(false)?.PrimaryPlayerContext?.PlaybackState == PlaybackState.Paused;

        return false;
      }
    }

    /// <summary>
    /// <code>true</code> if g_Play is in fullscreen and on top
    /// </summary>
    public bool IsPlayerOnTop
    {
      get
      {
        return ServiceRegistration.Get<IPlayerContextManager>(false)?.IsFullscreenContentWorkflowStateActive ?? false;
        /*return (currentModule == localizedFullscreen ||
                currentModule == localizedFullscreenMusic ||
                currentModule == localizedFullscreenTV ||
                currentModule == localizedFullscreenVideo);*/
      }
    }

    /// <summary>
    /// Media title
    /// </summary>
    public string Title
    {
      get
      {
        try
        {
          var playerManager = ServiceRegistration.Get<IPlayerManager>(false);
          if (playerManager?.NumActiveSlots > 0)
            return ServiceRegistration.Get<IPlayerContextManager>(false).PrimaryPlayerContext?.CurrentPlayer?.MediaItemTitle;

          return "";
        }
        catch (Exception)
        {
          return "";
        }
      }
    }

    /// <summary>
    /// Currently active module
    /// </summary>
    public string CurrentModule
    {
      get
      {
        try
        {
          var label = ServiceRegistration.Get<IWorkflowManager>(false)?.CurrentNavigationContext?.WorkflowState?.DisplayLabel;
          if (label != null)
            return LocalizationHelper.Translate(label);

          return "";
        }
        catch (Exception)
        {
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
    /// Checks if the status message has changed since the last call.
    /// </summary>
    /// <returns>true if the status has changed, false otherwise</returns>
    public bool IsChanged()
    {
      bool changed = (_isPlaying != IsPlaying
              || _isPaused != IsPaused
              || _title != Title
              || _currentModule != CurrentModule);
      if (changed)
      {
        _isPlaying = IsPlaying;
        _isPaused = IsPaused;
        _title = Title;
        _currentModule = CurrentModule;
      }
      return changed;
    }
  }
}
