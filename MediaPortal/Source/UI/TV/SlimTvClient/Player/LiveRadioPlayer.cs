#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Runtime.InteropServices;
using DirectShow;
using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Players.ResumeState;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;

namespace MediaPortal.Plugins.SlimTv.Client.Player
{
  class LiveRadioPlayer : BaseDXPlayer, IAudioPlayer, IResumablePlayer, ITsReaderCallback, ITsReaderCallbackAudioChange, 
    IUIContributorPlayer, ILivePlayer
  {
    #region Imports

    [ComImport, Guid(TSREADER_CLSID)]
    protected class TsReader { }

    #endregion

    #region Constants and structs

    public const string TSREADER_CLSID = "b9559486-E1BB-45D3-A2A2-9A7AFE49B23F";
    private const string TSREADER_FILTER_NAME = "TsReader";

    #endregion

    protected FilterFileWrapper _sourceFilter = null;
    protected ITsReader _tsReader;
    protected bool _useTsReader;

    public LiveRadioPlayer(bool useTsReader)
    {
      PlayerTitle = "LiveRadioPlayer"; // for logging
      _useTsReader = useTsReader;
    }

    public override string Name
    {
      get { return "Live Radio"; }
    }

    /// <summary>
    /// Whether this player is playing live radio, or a recording.
    /// Used by LiveRadioUIContributer to find out what screen to put up.
    /// </summary>
    public bool IsLiveRadio {  get { return _mediaItem is LiveTvMediaItem; } }

    #region IUIContributorPlayer Member

    public Type UIContributorType => typeof(LiveRadioUIContributor);

    public EventHandler OnBeginZap;
    public EventHandler OnEndZap;

    #endregion

    /// <summary>
    /// Adds a source filter to the graph and sets the input.
    /// </summary>
    protected override void AddSourceFilter()
    {
      if (!_useTsReader)
      {
        base.AddSourceFilter();
        return;
      }

      // Render the file
      // Notes Morpheus_xx, 2017-04-19:
      // In contrast to TV we need to use a relative path here, as the method is located inside the SlimTV assembly.
      // For TV part, the base class inside VideoPlayers is used and thus the correct path to TsReader.ax
      // The problem with different paths appears only inside RELEASE builds, but not DEBUG. Why this happens I don't know.
      var platform = IntPtr.Size > 4 ? "x64" : "x86";
      _sourceFilter = FilterLoader.LoadFilterFromDll($"..\\VideoPlayers\\{platform}\\TsReader.ax", typeof(TsReader).GUID, true);
      var baseFilter = _sourceFilter.GetFilter();

      IFileSourceFilter fileSourceFilter = (IFileSourceFilter)baseFilter;
      _tsReader = (ITsReader)baseFilter;
      _tsReader.SetRelaxedMode(1);
      _tsReader.SetTsReaderCallback(this);
      _tsReader.SetRequestAudioChangeCallback(this);

      _graphBuilder.AddFilter(baseFilter, TSREADER_FILTER_NAME);

      if (_resourceLocator.NativeResourcePath.IsNetworkResource)
      {
        // _resourceAccessor points to an rtsp:// stream or network file
        var sourcePathOrUrl = SourcePathOrUrl;

        if (sourcePathOrUrl == null)
          throw new IllegalCallException("The LiveRadioPlayer can only play network resources of type INetworkResourceAccessor");

        ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for stream '{1}'", PlayerTitle, sourcePathOrUrl);

        IDisposable accessEnsurer = null;
        if (IsLocalFilesystemResource)
          accessEnsurer = ((ILocalFsResourceAccessor)_resourceAccessor).EnsureLocalFileSystemAccess();
        using (accessEnsurer)
        {
          int hr = fileSourceFilter.Load(SourcePathOrUrl, null);
          new HRESULT(hr).Throw();
        }
      }
      else
      {
        //_resourceAccessor points to a local .ts file
        var localFileSystemResourceAccessor = _resourceAccessor as ILocalFsResourceAccessor;

        if (localFileSystemResourceAccessor == null)
          throw new IllegalCallException("The LiveRadioPlayer can only play file resources of type ILocalFsResourceAccessor");

        using (localFileSystemResourceAccessor.EnsureLocalFileSystemAccess())
        {
          ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for stream '{1}'", PlayerTitle, localFileSystemResourceAccessor.LocalFileSystemPath);
          fileSourceFilter.Load(localFileSystemResourceAccessor.LocalFileSystemPath, null);
        }
      }
    }

    protected override void OnBeforeGraphRunning()
    {
      if (!_useTsReader)
      {
        base.OnBeforeGraphRunning();
        return;
      }

      FilterGraphTools.RenderOutputPins(_graphBuilder, _sourceFilter.GetFilter());
    }

    protected override void FreeCodecs()
    {
      base.FreeCodecs();

      // Free all filters from graph
      if (_graphBuilder != null)
        FilterGraphTools.RemoveAllFilters(_graphBuilder, true);

      // Free file source
      FilterGraphTools.TryDispose(ref _sourceFilter);

      FilterGraphTools.TryDispose(ref _rot);
      FilterGraphTools.TryRelease(ref _graphBuilder);
    }

    public int OnMediaTypeChanged(ChangedMediaType mediaType)
    {
      return 0;
    }

    public int OnVideoFormatChanged(int streamType, int width, int height, int aspectRatioX, int aspectRatioY, int bitrate, int isInterlaced)
    {
      return 0;
    }

    public int OnRequestAudioChange()
    {
      IAMStreamSelect streamSelect = _sourceFilter as IAMStreamSelect;
      if (streamSelect != null)
        streamSelect.Enable(0, 0);
      return 0;
    }

    public int OnBitRateChanged(int bitrate)
    {
      return 0;
    }

    #region Implementation of IResumablePlayer

    /// <summary>
    /// Gets a <see cref="IResumeState"/> from the player.
    /// </summary>
    /// <param name="state">Outputs resume state.</param>
    /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
    public virtual bool GetResumeState(out IResumeState state)
    {
      TimeSpan currentTime = CurrentTime;
      // Workaround for TsReader handling on playback end: it reports a negative position, so we treat it to "stream end"
      if (currentTime.TotalSeconds < 0)
        currentTime = Duration;
      TimeSpan duration = Duration;
      // If we already played back more then 99%, we don't want to ask user to resume playback.
      if (currentTime.TotalSeconds / duration.TotalSeconds > 0.99)
        state = null;
      else
        state = new PositionResumeState
        {
          ResumePosition = CurrentTime,
          ActiveResourceLocatorIndex = _mediaItem?.ActiveResourceLocatorIndex ?? 0,
          ActiveEditionIndex = _mediaItem?.ActiveEditionIndex ?? 0
        };
      return true;
    }

    /// <summary>
    /// Sets a <see cref="IResumeState"/> to the player. The player is responsible to make the required initializations.
    /// </summary>
    /// <param name="state">Resume state.</param>
    /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
    public virtual bool SetResumeState(IResumeState state)
    {
      PositionResumeState pos = state as PositionResumeState;
      if (pos == null)
        return false;

      if (_mediaItem != null)
      {
        // Check for multi-resource media items, first set the matching part, then the position
        if (pos.ActiveResourceLocatorIndex != _mediaItem.ActiveResourceLocatorIndex && pos.ActiveResourceLocatorIndex <= _mediaItem.MaximumResourceLocatorIndex ||
            pos.ActiveEditionIndex != _mediaItem.ActiveEditionIndex && pos.ActiveEditionIndex <= _mediaItem.MaximumEditionIndex)
        {
          _mediaItem.ActiveResourceLocatorIndex = pos.ActiveResourceLocatorIndex;
          _mediaItem.ActiveEditionIndex = pos.ActiveEditionIndex;
          if (!NextItem(_mediaItem, StartTime.AtOnce))
            return false;
        }
      }
      CurrentTime = pos.ResumePosition;
      return true;
    }

    public LiveTvMediaItem CurrentItem
    {
      get
      {
        IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
        for (int index = 0; index < playerContextManager.NumActivePlayerContexts; index++)
        {
          IPlayerContext playerContext = playerContextManager.GetPlayerContext(index);
          if (playerContext == null || playerContext.CurrentPlayer != this)
            continue;

          LiveTvMediaItem liveTvMediaItem = playerContext.CurrentMediaItem as LiveTvMediaItem;
          if (liveTvMediaItem != null)
            return liveTvMediaItem;
        }
        return null;
      }
    }

    public void NotifyBeginZap(object sender)
    {
      OnBeginZap?.Invoke(sender, EventArgs.Empty);
    }

    public void NotifyEndZap(object sender)
    {
      OnEndZap?.Invoke(sender, EventArgs.Empty);
    }

    public void BeginZap()
    {
      ServiceRegistration.Get<ILogger>().Debug("{0}: Begin zapping", PlayerTitle);
      // Tell the TsReader that we are zapping, before we actually tune the new channel.
      if (_useTsReader)
        _tsReader.OnZapping(0x80);
    }

    public void EndZap()
    {
      Resume();
      ServiceRegistration.Get<ILogger>().Debug("{0}: End zapping", PlayerTitle);
    }


    #endregion
  }
}
