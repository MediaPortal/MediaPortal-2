using System;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
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

    /*
        private bool isRecording;
        /// <summary>
        /// <code>true</code> if TV Server is available and recording
        /// </summary>
        public bool IsRecording
        {
            get
            {
                isRecording = WifiRemote.IsAvailableTVPlugin && TvPlugin.TVHome.IsAnyCardRecording;
                return isRecording;
            }
        }
        */

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

    private string selectedItem;

    // TODO: reimplement
    /// <summary>
    /// Currently selected GUI item label
    /// </summary>
    public string SelectedItem
    {
      get
      {
        // The currently selected item may hide in the property 
        // #selecteditem or #highlightedbutton.
        //string selected;

        return selectedItem;
      }
    }

    /*private Guid? windowId = Guid.Empty;

    public Guid? WindowId
    {
      get
      {
        try
        {
          windowId = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext.WorkflowState.StateId;
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error on retrieving current window id: {0}", ex.ToString());
        }
        return windowId;
      }
    }*/

    private int windowId = 0;

    public int WindowId
    {
      get
      {
        
        return 0;
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
//                || isRecording != IsRecording
              || title != Title
              || currentModule != CurrentModule
              || selectedItem != SelectedItem
              || windowId != WindowId);
    }
  }
}