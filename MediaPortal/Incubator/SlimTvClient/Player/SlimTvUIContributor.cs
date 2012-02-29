#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.Plugins.SlimTvClient
{
  public class SlimTvUIContributor : BaseTimerControlledModel, IPlayerUIContributor
  {
    public const string SCREEN_FULLSCREEN_TV = "FullscreenContentTv";
    public const string SCREEN_CURRENTLY_PLAYING_TV = "CurrentlyPlayingTv";

    protected MediaWorkflowStateType _mediaWorkflowStateType;
    protected IChapterPlayer _player;
    protected ItemsList _chapterMenuItems;

    private readonly AbstractProperty _chaptersAvailableProperty;

    #region Construction and destruction

    public SlimTvUIContributor(): base (500)
    {
      _chaptersAvailableProperty = new WProperty(typeof(bool), false);
    }

    #endregion

    public bool BackgroundDisabled
    {
      get { return false; }
    }

    public AbstractProperty ChaptersAvailableProperty
    {
      get { return _chaptersAvailableProperty; }
    }

    public bool ChaptersAvailable
    {
      get { return (bool)_chaptersAvailableProperty.GetValue(); }
      set { _chaptersAvailableProperty.SetValue(value); }
    }

    public MediaWorkflowStateType MediaWorkflowStateType
    {
      get { return _mediaWorkflowStateType; }
    }

    public string Screen
    {
      get
      {
        if (_mediaWorkflowStateType == MediaWorkflowStateType.CurrentlyPlaying)
          return SCREEN_CURRENTLY_PLAYING_TV;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return SCREEN_FULLSCREEN_TV;
        return null;
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

    /// <summary>
    /// Opens the chapter selection dialog.
    /// </summary>
    public void OpenChooseChapterDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseChapter");
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
    /// <param name="item">One of the items of <see cref="ChapterMenuItems"/>.</param>
    public void Select(ListItem item)
    {
      if (item == null)
        return;
      ICommand command = item.Command;
      if (command != null)
        command.Execute();
    }


    public void Initialize(MediaWorkflowStateType stateType, IPlayer player)
    {
      _mediaWorkflowStateType = stateType;
      _player = player as IChapterPlayer;
      _chapterMenuItems = new ItemsList();
    }

    protected override void Update()
    {
      ChaptersAvailable = _player.ChaptersAvailable;
    }
  }
}