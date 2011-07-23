#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Core.General;
using MediaPortal.Core.Commands;

namespace MediaPortal.UiComponents.Media.Models
{
  public class DVDPlayerUIContributor : BaseTimerControlledModel, IPlayerUIContributor
  {
    protected static string[] EMPTY_STRING_ARRAY = new string[] {};

    #region Protected fields

    protected MediaWorkflowStateType _mediaWorkflowStateType;
    protected IDVDPlayer _player;
    protected AbstractProperty _dvdPlayerHandlesInputProperty;
    protected AbstractProperty _chaptersAvailableProperty;
    protected AbstractProperty _subtitlesAvailableProperty;
    protected string[] _subtitles = EMPTY_STRING_ARRAY;
    protected ItemsList _subtitleMenuItems;
    protected ItemsList _chapterMenuItems;

    #endregion

    #region Constructor & maintainance

    public DVDPlayerUIContributor() : base(300)
    {
      _dvdPlayerHandlesInputProperty = new WProperty(typeof(bool), false);
      _chaptersAvailableProperty = new WProperty(typeof(bool), false);
      _subtitlesAvailableProperty = new WProperty(typeof(bool), false);
      StartTimer();
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

    public AbstractProperty ChaptersAvailableProperty
    {
      get { return _chaptersAvailableProperty; }
    }

    public bool ChaptersAvailable
    {
      get { return (bool) _chaptersAvailableProperty.GetValue(); }
      set { _chaptersAvailableProperty.SetValue(value); }
    }

    public AbstractProperty SubtitlesAvailableProperty
    {
      get { return _subtitlesAvailableProperty; }
    }

    public bool SubtitlesAvailable
    {
      get { return (bool) _subtitlesAvailableProperty.GetValue(); }
      set { _subtitlesAvailableProperty.SetValue(value); }
    }

    /// <summary>
    /// Provides a list of items to be shown in the subtitle selection menu.
    /// </summary>
    public ItemsList SubtitleMenuItems
    {
      get
      {
        _subtitleMenuItems.Clear();
        ISubtitlePlayer subtitlePlayer = _player as ISubtitlePlayer;
        if (subtitlePlayer != null && _subtitles.Length > 0)
        {
          string currentSubtitle = subtitlePlayer.CurrentSubtitle;
          ListItem item = new ListItem(Consts.KEY_NAME, Consts.RES_SUBTITLE_OFF)
              {
                Command = new MethodDelegateCommand(subtitlePlayer.DisableSubtitle),
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
                  Command = new MethodDelegateCommand(() => subtitlePlayer.SetSubtitle(localSubtitle)),
                  // Check if it is the selected subtitle, then mark it
                  Selected = localSubtitle == currentSubtitle
                };

            _subtitleMenuItems.Add(item);
          }
        }
        return _subtitleMenuItems;
      }
    }

    /// <summary>
    /// Provides a list of items to be shown in the chapter selection menu.
    /// </summary>
    public ItemsList ChapterMenuItems
    {
      get
      {
        string currentChapter = _player.CurrentChapter;
        _chapterMenuItems.Clear();
        if (ChaptersAvailable)
        {
          foreach (string chapter in _player.Chapters)
          {
            // use local variable, otherwise delegate argument is not fixed
            string localChapter = chapter;

            ListItem item = new ListItem(Consts.KEY_NAME, localChapter)
                {
                  Command = new MethodDelegateCommand(() => _player.SetChapter(localChapter)),
                  // check if it is the selected chapter, then mark it
                  Selected = (localChapter == currentChapter)
                };

            _chapterMenuItems.Add(item);
          }
        }
        return _chapterMenuItems;
      }
    }

    #endregion

    #region Public Members

    public void Initialize(MediaWorkflowStateType stateType, IPlayer player)
    {
      _mediaWorkflowStateType = stateType;
      _player = player as IDVDPlayer;
      _subtitleMenuItems = new ItemsList();
      _chapterMenuItems = new ItemsList();
    }

    // Update GUI properties
    protected override void Update()
    {
      DvdPlayerHandlesInput = _player.IsHandlingUserInput;
      ChaptersAvailable = _player.ChaptersAvailable;
      ISubtitlePlayer subtitlePlayer = _player as ISubtitlePlayer;
      if (subtitlePlayer != null)
      {
        _subtitles = subtitlePlayer.Subtitles;
        SubtitlesAvailable = _subtitles.Length > 0;
      }
      else
        _subtitles = EMPTY_STRING_ARRAY;
    }

    /// <summary>
    /// Opens the subtitle selection dialog.
    /// </summary>
    public void OpenChooseSubtitleDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseSubtitle");
    }

    /// <summary>
    /// Opens the chapter selection dialog.
    /// </summary>
    public void OpenChooseChapterDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseChapter");
    }

    /// <summary>
    /// Shows the DVD menu.
    /// </summary>
    public void ShowDvdMenu()
    {
      _player.ShowDvdMenu();
    }

    /// <summary>
    /// Must be called from the screen when the DVD player is handling the user input and a key is pressed.
    /// </summary>
    /// <param name="key">Key that was pressed.</param>
    public void OnKeyPress(Key key)
    {
      _player.OnKeyPress(key);
    }

    /// <summary>
    /// Must be called from the screen when the DVD player is handling the user input and the mouse is moved.
    /// </summary>
    /// <param name="x">X coordinate relative to the video size.</param>
    /// <param name="y">Y coordinate relative to the video size.</param>
    public void OnMouseMove(float x, float y)
    {
      _player.OnMouseMove(x, y);
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
        _player.OnMouseClick(x, y);
    }

    /// <summary>
    /// Skips to previous chapter.
    /// </summary>
    public void PrevChapter()
    {
      _player.PrevChapter();
    }

    /// <summary>
    /// Skips to next chapter.
    /// </summary>
    public void NextChapter()
    {
      _player.NextChapter();
    }

    /// <summary>
    /// Execute selected menu item for subtitle and chapter selection.
    /// </summary>
    /// <param name="item">One of the items of <see cref="SubtitleMenuItems"/> or
    /// <see cref="ChapterMenuItems"/>.</param>
    public void Select(ListItem item)
    {
      if (item == null)
        return;
      ICommand command = item.Command;
      if (command != null)
        command.Execute();
    }

    #endregion

    #region IPlayerUIContributor implementation

    public MediaWorkflowStateType MediaWorkflowStateType
    {
      get { return _mediaWorkflowStateType; }
    }

    public string Screen
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

    public bool BackgroundDisabled
    {
      get { return _mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent; }
    }

    #endregion
  }
}