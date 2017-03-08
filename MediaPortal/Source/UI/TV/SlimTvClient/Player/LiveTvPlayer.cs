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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX;
using SharpDX.Direct3D9;

namespace MediaPortal.Plugins.SlimTv.Client.Player
{
  public class LiveTvPlayer : TsVideoPlayer, IUIContributorPlayer, IReusablePlayer
  {
    #region Variables

    protected IList<ITimeshiftContext> _timeshiftContexes;
    protected IList<IChannel> _channelHistory = null;
    protected static TimeSpan TIMESPAN_LIVE = TimeSpan.FromMilliseconds(50);
    protected bool _zapping; // Indicates that we are currently changing a channel.

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

    public IList<ITimeshiftContext> TimeshiftContexes
    {
      get
      {
        IList<ITimeshiftContext> timeshiftContexes = _timeshiftContexes;
        return (timeshiftContexes != null ? new List<ITimeshiftContext>(timeshiftContexes) : new List<ITimeshiftContext>()).AsReadOnly();
      }
    }

    private void SeekPreviousChapter()
    {
      IList<IChannel> channelHistory;
      lock (SyncObj)
        channelHistory = _channelHistory;

      if (channelHistory == null || channelHistory.Count < 2)
        return;

      IChannel channel = channelHistory[channelHistory.Count - 2];
      TuneToChannel(channel);
    }

    private static void TuneToChannel(IChannel channel)
    {
      if (channel == null)
        return;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      SlimTvClientModel model = workflowManager.GetModel(SlimTvClientModel.MODEL_ID) as SlimTvClientModel;
      if (model != null)
        model.Tune(channel);
    }

    protected override void EnumerateChapters(bool forceRefresh)
    {
      IList<IChannel> channelHistory;
      lock (SyncObj)
        channelHistory = _channelHistory;

      if (channelHistory != null && !forceRefresh)
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
        var reversedList = new List<ITimeshiftContext>(_timeshiftContexes);
        reversedList.Reverse();
        channelHistory = reversedList.Select(timeshiftContext => timeshiftContext.Channel).ToList();
      }
      lock (SyncObj)
        _channelHistory = channelHistory;
    }

    protected string GetContextTitle(ITimeshiftContext timeshiftContext)
    {
      if (timeshiftContext == null)
        return string.Empty;

      // We only use the channel history
      return string.Format("{0}", timeshiftContext.Channel.Name);
    }

    public void BeginZap()
    {
      ServiceRegistration.Get<ILogger>().Debug("{0}: Begin zapping", PlayerTitle);
      // Set indicator for zapping to blank the video surface with black.
      _zapping = true;
      // Tell the TsReader that we are zapping, before we actually tune the new channel.
      _tsReader.OnZapping(0x80);
    }

    public void EndZap()
    {
      SeekToEnd();
      Resume();

      // Clear any subtitle that might be currently displayed
      if (_subtitleRenderer != null)
        _subtitleRenderer.Reset();
      EnumerateStreams(true);
      EnumerateChapters(true);
      SetPreferredSubtitle();

      // First reset zapping indicator
      _zapping = false;
      // Then invalidate the "black" surface to use new frame.
      OnTextureInvalidated();
      ServiceRegistration.Get<ILogger>().Debug("{0}: End zapping", PlayerTitle);
    }

    public void OnProgramChange()
    {
      EnumerateChapters(true);
    }

    protected override void PostProcessTexture(Texture targetTexture)
    {
      if (_zapping)
      {
        // While zapping fill the current video frame with black. This avoids a frozen last frame from previous channel.
        using (Surface surface = targetTexture.GetSurfaceLevel(0))
          SkinContext.Device.ColorFill(surface, Color.Black);
      }
      else
        base.PostProcessTexture(targetTexture);
    }

    /// <summary>
    /// Checks the current stream position and seeks to end, if it is less than <see cref="TIMESPAN_LIVE"/> behind the live point.
    /// </summary>
    /// <returns><c>true</c> if seeked to end.</returns>
    protected bool SeekToEnd()
    {
      // Call a seek only if the stream is not "live"
      if (Duration - CurrentTime > TIMESPAN_LIVE)
      {
        ServiceRegistration.Get<ILogger>().Debug("{0}: SeekToEnd: Duration: {1}, CurrentTime: {2}", PlayerTitle, Duration, CurrentTime);
        CurrentTime = Duration; // Seek to end
        return true;
      }
      return false;
    }

    #region IChapterPlayer overrides

    public override string[] Chapters
    {
      get
      {
        EnumerateChapters();
        IList<IChannel> channelHistory;
        lock (SyncObj)
          channelHistory = _channelHistory;

        return channelHistory == null || channelHistory.Count == 0 ? EMPTY_STRING_ARRAY : channelHistory.Select(c => c.Name).ToArray();
      }
    }

    public override void SetChapter(string chapter)
    {
      IList<IChannel> channelHistory;
      lock (SyncObj)
        channelHistory = _channelHistory;

      if (channelHistory == null)
        return;

      IChannel channel = channelHistory.FirstOrDefault(c => c.Name == chapter);
      TuneToChannel(channel);
    }

    public override void NextChapter()
    {
      SeekPreviousChapter();
    }

    public override void PrevChapter()
    {
      SeekPreviousChapter();
    }

    public override string CurrentChapter
    {
      get
      {
        return GetContextTitle(TimeshiftContexes.LastOrDefault());
      }
    }

    #endregion

    #region IReusablePlayer members

    public override bool NextItem(MediaItem mediaItem, StartTime startTime)
    {
      string mimeType;
      string title;
      if (!mediaItem.GetPlayData(out mimeType, out title) || mimeType != LiveTvMediaItem.MIME_TYPE_TV)
      {
        ServiceRegistration.Get<ILogger>().Debug("SlimTvHandler: Cannot reuse current player for new mimetype {0}", mimeType);
        return false;
      }
      Stop();
      // Set new resource locator for existing player, this avoids interim close of player slot
      IResourceLocator resourceLocator = mediaItem.GetResourceLocator();
      ServiceRegistration.Get<ILogger>().Debug("SlimTvHandler: Changing file/stream for player to {0}", resourceLocator.NativeResourcePath);
      SetMediaItem(resourceLocator, mimeType);
      return true;
    }

    #endregion
  }
}
