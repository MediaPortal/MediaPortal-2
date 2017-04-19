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
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Plugins.SlimTv.Client.Player
{
  class LiveRadioPlayer : BaseDXPlayer, IAudioPlayer, ITsReaderCallback, ITsReaderCallbackAudioChange
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
      _sourceFilter = FilterLoader.LoadFilterFromDll("..\\VideoPlayers\\TsReader.ax", typeof(TsReader).GUID, true);
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
  }
}
