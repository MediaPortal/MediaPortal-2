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

using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.SlimTv.Client.Player
{
  public class LiveTvPlayer : TsVideoPlayer, IUIContributorPlayer
  {
    #region Variables

    protected IList<ITimeshiftContext> _timeshiftContexes;
    protected StreamInfoHandler _chapterInfo = null;
    protected static TimeSpan TIMESPAN_LIVE = TimeSpan.FromSeconds(1.5);

    #endregion

    #region ctor

    /// <summary>
    /// Constructs a LiveTvPlayer player object.
    /// </summary>
    public LiveTvPlayer()
    {
      PlayerTitle = "LiveTvPlayer"; // for logging
    }

    #endregion

    #region IUIContributorPlayer Member

    public Type UIContributorType
    {
      get { return typeof(SlimTvUIContributor); }
    }

    #endregion

    public ITimeshiftContext CurrentTimeshiftContext
    {
      get
      {
        return GetContext(CurrentTime);
      }
    }

    private TimeSpan GetStartDuration(int chapterIndex)
    {
      lock (SyncObj)
      {
        if (_timeshiftContexes == null || chapterIndex >= _timeshiftContexes.Count)
          return TimeSpan.Zero;
        return _timeshiftContexes[chapterIndex].TuneInTime - _timeshiftContexes[0].TuneInTime;
      }
    }

    private ITimeshiftContext GetContext(TimeSpan timeSpan)
    {
      IList<ITimeshiftContext> timeshiftContexes;
      lock (SyncObj)
        timeshiftContexes = _timeshiftContexes;

      if (timeshiftContexes == null)
        return null;

      TimeSpan totalTime = new TimeSpan();
      foreach (ITimeshiftContext timeshiftContext in timeshiftContexes)
      {
        if (timeSpan >= totalTime && 
          (
            (timeSpan <= totalTime + timeshiftContext.TimeshiftDuration) || timeshiftContext.TimeshiftDuration.TotalSeconds == 0 /* currently playing */
          ))
          return timeshiftContext;
        
        totalTime += timeshiftContext.TimeshiftDuration;
      }
      return null;
    }

    private void SeekChapter(bool next)
    {
      IList<ITimeshiftContext> timeshiftContexes;
      lock (SyncObj)
        timeshiftContexes = _timeshiftContexes;

      if (timeshiftContexes == null)
        return;

      TimeSpan timeSpan = CurrentTime;
      TimeSpan totalTime = new TimeSpan();
      int index = 0;
      bool found = false;
      foreach (ITimeshiftContext timeshiftContext in timeshiftContexes)
      {
        if (timeSpan >= totalTime &&
          (
          (timeSpan <= totalTime + timeshiftContext.TimeshiftDuration) ||
          timeshiftContext.TimeshiftDuration.TotalSeconds == 0 /* currently playing */
          ))
        {
          found = true;
          break;
        }
        index++;
        totalTime += timeshiftContext.TimeshiftDuration;
      }

      if (!found) 
        return;

      if (next && index < timeshiftContexes.Count - 1)
        CurrentTime = GetStartDuration(index + 1);

      if (!next && index > 0)
        CurrentTime = GetStartDuration(index - 1);
    }
    
    protected override void EnumerateChapters(bool forceRefresh)
    {
      StreamInfoHandler chapterInfo;
      lock (SyncObj)
        chapterInfo = _chapterInfo;

      if (chapterInfo != null && !forceRefresh)
        return;

      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      for (int index = 0; index < playerContextManager.NumActivePlayerContexts; index++)
      {
        IPlayerContext playerContext = playerContextManager.GetPlayerContext(index);
        if (playerContext == null || playerContext.CurrentPlayer != this)
          continue;

        LiveTvMediaItem liveTvMediaItem = playerContext.CurrentMediaItem as LiveTvMediaItem;
        if (liveTvMediaItem == null)
          continue;

        _timeshiftContexes = liveTvMediaItem.TimeshiftContexes;
        chapterInfo = new StreamInfoHandler();
        int i = 0;
        foreach (ITimeshiftContext timeshiftContext in _timeshiftContexes)
          chapterInfo.AddUnique(new StreamInfo(null, i++, GetContextTitle(timeshiftContext), 0));
      }
      lock (SyncObj)
        _chapterInfo = chapterInfo;
    }

    protected string GetContextTitle(ITimeshiftContext timeshiftContext)
    {
      if (timeshiftContext == null)
        return string.Empty;

      string program = timeshiftContext.Program != null ? timeshiftContext.Program.Title : 
        ServiceRegistration.Get<ILocalization>().ToString("[SlimTvClient.NoProgram]");
      return string.Format("{0}: {1}", timeshiftContext.Channel.Name, program);
    }

    public void ChannelZap()
    {
      // Call a seek only if the stream is not "live"
      if (Duration - CurrentTime > TIMESPAN_LIVE)
        CurrentTime = Duration; // Seek to end
      
      // Clear any subtitle that might be currently displayed
      _subtitleRenderer.Reset();
      EnumerateStreams(true);
      EnumerateChapters(true);
      SetPreferredSubtitle();
    }

    #region IChapterPlayer overrides

    public override string[] Chapters
    {
      get
      {
        EnumerateChapters();
        StreamInfoHandler chapters;
        lock (SyncObj)
          chapters = _chapterInfo;

        return chapters == null || chapters.Count == 0 ? EMPTY_STRING_ARRAY : chapters.GetStreamNames();
      }
    }
    public override void SetChapter(string chapter)
    {
      StreamInfoHandler chapters;
      lock (SyncObj)
        chapters = _chapterInfo;

      if (chapters == null || chapters.Count == 0)
        return;

      StreamInfo chapterInfo = chapters.FindStream(chapter);
      if (chapterInfo != null)
        CurrentTime = GetStartDuration(chapterInfo.StreamIndex);
    }

    public override void NextChapter()
    {
      SeekChapter(true);
    }

    public override void PrevChapter()
    {
      SeekChapter(false);
    }

    public override string CurrentChapter
    {
      get
      {
        return GetContextTitle(GetContext(CurrentTime));
      }
    }

    #endregion
  }
}