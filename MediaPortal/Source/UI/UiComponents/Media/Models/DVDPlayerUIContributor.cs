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

using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Common.General;
using MediaPortal.Common.Commands;

namespace MediaPortal.UiComponents.Media.Models
{
  public class DVDVideoPlayerUIContributor : BaseVideoPlayerUIContributor
  {
    #region Protected fields

    protected IDVDPlayer _dvdPlayer;
    protected AbstractProperty _dvdPlayerHandlesInputProperty;

    #endregion

    #region Constructor & maintainance

    public DVDVideoPlayerUIContributor()
    {
      _dvdPlayerHandlesInputProperty = new WProperty(typeof(bool), false);
    }

    #endregion

    #region Properties

    public AbstractProperty DvdPlayerHandlesInputProperty
    {
      get { return _dvdPlayerHandlesInputProperty; }
    }

    public bool DvdPlayerHandlesInput
    {
      get { return (bool) _dvdPlayerHandlesInputProperty.GetValue(); }
      set { _dvdPlayerHandlesInputProperty.SetValue(value); }
    }

    /// <summary>
    /// Provides a list of items to be shown in the subtitle selection menu.
    /// </summary>
    public override ItemsList SubtitleMenuItems
    {
      get
      {
        _subtitleMenuItems.Clear();
        if (_subtitlePlayer != null && _subtitles.Length > 0)
        {
          string currentSubtitle = _subtitlePlayer.CurrentSubtitle;
          ListItem item = new ListItem(Consts.KEY_NAME, Consts.RES_SUBTITLE_OFF)
          {
            Command = new MethodDelegateCommand(_subtitlePlayer.DisableSubtitle),
            // Check if it is the selected subtitle, then mark it
            Selected = currentSubtitle != null
          };
          _subtitleMenuItems.Add(item); // Subtitles off
          foreach (string subtitle in _subtitles)
          {
            // Use local variable, otherwise delegate argument is not fixed
            string localSubtitle = subtitle;

            item = new ListItem(Consts.KEY_NAME, localSubtitle)
                {
                  Command = new MethodDelegateCommand(() => _subtitlePlayer.SetSubtitle(localSubtitle)),
                  // Check if it is the selected subtitle, then mark it
                  Selected = localSubtitle == currentSubtitle
                };

            _subtitleMenuItems.Add(item);
          }
        }
        return _subtitleMenuItems;
      }
    }

    #endregion

    #region Public Members

    public override void Initialize(MediaWorkflowStateType stateType, IPlayer player)
    {
      base.Initialize(stateType, player);
      _dvdPlayer = player as IDVDPlayer;
    }

    // Update GUI properties
    protected override void Update()
    {
      if (_updating)
      {
        ServiceRegistration.Get<ILogger>().Warn("DVDPlayerUIContributor: last update cycle still not finished.");
        return;
      }

      try
      {
        base.Update();
        _updating = true;
        DvdPlayerHandlesInput = _dvdPlayer.IsHandlingUserInput;
      }
      finally
      {
        _updating = false;
      }
    }
    
    /// <summary>
    /// Shows the DVD menu.
    /// </summary>
    public void ShowDvdMenu()
    {
      _dvdPlayer.ShowDvdMenu();
    }

    /// <summary>
    /// Must be called from the screen when the DVD player is handling the user input and a key is pressed.
    /// </summary>
    /// <param name="key">Key that was pressed.</param>
    public void OnKeyPress(Key key)
    {
      _dvdPlayer.OnKeyPress(key);
    }

    /// <summary>
    /// Must be called from the screen when the DVD player is handling the user input and the mouse is moved.
    /// </summary>
    /// <param name="x">X coordinate relative to the video size.</param>
    /// <param name="y">Y coordinate relative to the video size.</param>
    public void OnMouseMove(float x, float y)
    {
      _dvdPlayer.OnMouseMove(x, y);
    }

    /// <summary>
    /// Must be called from the screen when the DVD player is handling the user input and the mouse is left-clicked.
    /// </summary>
    /// <param name="buttons">Mouse buttons that have been pressed.</param>
    /// <param name="x">X coordinate relative to the video size.</param>
    /// <param name="y">Y coordinate relative to the video size.</param>
    public void OnMouseClick(MouseButtons buttons, float x, float y)
    {
      if (buttons == MouseButtons.Left)
        _dvdPlayer.OnMouseClick(x, y);
    }

    #endregion

    #region IPlayerUIContributor implementation

    public override string Screen
    {
      get
      {
        // Using special screens for DVD player
        if (_mediaWorkflowStateType == MediaWorkflowStateType.CurrentlyPlaying)
          return Consts.SCREEN_CURRENTLY_PLAYING_DVD;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return Consts.SCREEN_FULLSCREEN_DVD;
        return null;
      }
    }
    
    #endregion
  }
}