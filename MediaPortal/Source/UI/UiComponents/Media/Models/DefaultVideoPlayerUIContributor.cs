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
using MediaPortal.UiComponents.SkinBase.Models;

namespace MediaPortal.UiComponents.Media.Models
{
  public class DefaultVideoPlayerUIContributor : BaseTimerControlledModel, IPlayerUIContributor
  {
    protected static string[] EMPTY_STRING_ARRAY = new string[] { };

    protected MediaWorkflowStateType _mediaWorkflowStateType;

    #region Constructor & maintainance

    public DefaultVideoPlayerUIContributor() : base(300)
    {
      _subtitlesAvailableProperty = new WProperty(typeof(bool), false);
      _chaptersAvailableProperty = new WProperty(typeof(bool), false);
      _titlesAvailableProperty = new WProperty(typeof(bool), false);
      StartTimer();
    }

    #endregion

    #region Variables

    protected ISubtitlePlayer _subtitlePlayer;
    protected AbstractProperty _subtitlesAvailableProperty;
    protected string[] _subtitles = EMPTY_STRING_ARRAY;
    protected ItemsList _subtitleMenuItems;

    protected IChapterPlayer _chapterPlayer;
    protected AbstractProperty _chaptersAvailableProperty;
    protected string[] _chapters = EMPTY_STRING_ARRAY;
    protected ItemsList _chapterMenuItems;

    protected ITitlePlayer _titlePlayer;
    protected AbstractProperty _titlesAvailableProperty;
    protected string[] _titles = EMPTY_STRING_ARRAY;
    protected ItemsList _titleMenuItems;

    protected bool _updating;

    #endregion

    public AbstractProperty SubtitlesAvailableProperty
    {
      get { return _subtitlesAvailableProperty; }
    }

    public bool SubtitlesAvailable
    {
      get { return (bool) _subtitlesAvailableProperty.GetValue(); }
      set { _subtitlesAvailableProperty.SetValue(value); }
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

    public AbstractProperty TitlesAvailableProperty
    {
      get { return _titlesAvailableProperty; }
    }

    public bool TitlesAvailable
    {
      get { return (bool)_titlesAvailableProperty.GetValue(); }
      set { _titlesAvailableProperty.SetValue(value); }
    }

    /// <summary>
    /// Provides a list of items to be shown in the subtitle selection menu.
    /// </summary>
    public ItemsList SubtitleMenuItems
    {
      get
      {
        _subtitleMenuItems.Clear();
        ISubtitlePlayer subtitlePlayer = _subtitlePlayer;
        if (subtitlePlayer != null && _subtitles.Length > 0)
        {
          string currentSubtitle = subtitlePlayer.CurrentSubtitle;
           
          foreach (string subtitle in _subtitles)
          {
            // Use local variable, otherwise delegate argument is not fixed
            string localSubtitle = subtitle;

            ListItem item = new ListItem(Consts.KEY_NAME, localSubtitle)
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
        string currentChapter = _chapterPlayer.CurrentChapter;
        _chapterMenuItems.Clear();
        if (ChaptersAvailable)
        {
          foreach (string chapter in _chapterPlayer.Chapters)
          {
            // use local variable, otherwise delegate argument is not fixed
            string localChapter = chapter;

            ListItem item = new ListItem(Consts.KEY_NAME, localChapter)
            {
              Command = new MethodDelegateCommand(() => _chapterPlayer.SetChapter(localChapter)),
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
    /// Provides a list of items to be shown in the title selection menu.
    /// </summary>
    public ItemsList TitleMenuItems
    {
      get
      {
        _titleMenuItems.Clear();
        ITitlePlayer titlePlayer = _titlePlayer;
        if (titlePlayer != null && _titles.Length > 0)
        {
          string currentTitle = titlePlayer.CurrentTitle;

          foreach (string title in _titles)
          {
            // Use local variable, otherwise delegate argument is not fixed
            string localTitle = title;

            ListItem item = new ListItem(Consts.KEY_NAME, localTitle)
            {
              Command = new MethodDelegateCommand(() => titlePlayer.SetTitle(localTitle)),
              // Check if it is the selected subtitle, then mark it
              Selected = localTitle == currentTitle
            };

            _titleMenuItems.Add(item);
          }
        }
        return _titleMenuItems;
      }
    }

    public bool BackgroundDisabled
    {
      get { return _mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent; }
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
          return Consts.SCREEN_CURRENTLY_PLAYING_VIDEO;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return Consts.SCREEN_FULLSCREEN_VIDEO;
        return null;
      }
    }

    public void Initialize(MediaWorkflowStateType stateType, IPlayer player)
    {
      _mediaWorkflowStateType = stateType;
      _subtitlePlayer = player as ISubtitlePlayer;
      _chapterPlayer = player as IChapterPlayer;
      _titlePlayer = player as ITitlePlayer;
      _subtitleMenuItems = new ItemsList();
      _chapterMenuItems = new ItemsList();
      _titleMenuItems = new ItemsList();
    }

    // Update GUI properties
    protected override void Update()
    {
      if (!_updating)
      {
        try
        {
          if (_subtitlePlayer != null)
          {
            _subtitles = _subtitlePlayer.Subtitles;
            SubtitlesAvailable = _subtitles.Length > 0;
          }
          else
            _subtitles = EMPTY_STRING_ARRAY;

          if (_chapterPlayer != null)
          {
            _chapters = _chapterPlayer.Chapters;
            ChaptersAvailable = _chapterPlayer.ChaptersAvailable;
          }
          else
            _chapters = EMPTY_STRING_ARRAY;

          if (_titlePlayer != null)
          {
            _titles = _titlePlayer.Titles;
            TitlesAvailable = _titles.Length > 0;
          }
          else
            _titles = EMPTY_STRING_ARRAY;
        }
        finally
        {
          _updating = false;
        }
      }
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
    /// Opens the title selection dialog.
    /// </summary>
    public void OpenChooseTitleDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseTitle");
    }

    /// <summary>
    /// Execute selected menu item for subtitle and chapter selection.
    /// </summary>
    /// <param name="item">One of the items of <see cref="SubtitleMenuItems"/>.</param>
    public void Select(ListItem item)
    {
      if (item == null)
        return;
      ICommand command = item.Command;
      if (command != null)
        command.Execute();
    }

    public void ShowZoomModeDialog()
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = pcm.GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
      PlayerConfigurationDialogModel.OpenChooseGeometryDialog(pc);
    }

    // <summary>
    /// Skips to previous chapter.
    /// </summary>
    public void PrevChapter()
    {
      _chapterPlayer.PrevChapter();
    }

    /// <summary>
    /// Skips to next chapter.
    /// </summary>
    public void NextChapter()
    {
      _chapterPlayer.NextChapter();
    }

  }
}