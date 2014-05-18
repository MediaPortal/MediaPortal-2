#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

    protected IBaseFilter _sourceFilter = null;
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
      _sourceFilter = FilterLoader.LoadFilterFromDll("TsReader.ax", typeof(TsReader).GUID, true);

      IFileSourceFilter fileSourceFilter = (IFileSourceFilter)_sourceFilter;
      ITsReader tsReader = (ITsReader) _sourceFilter;
      tsReader.SetRelaxedMode(1);
      tsReader.SetTsReaderCallback(this);
      tsReader.SetRequestAudioChangeCallback(this);

      _graphBuilder.AddFilter(_sourceFilter, TSREADER_FILTER_NAME);

      if (_resourceLocator.NativeResourcePath.IsNetworkResource)
      {
        // _resourceAccessor points to an rtsp:// stream or network file
        var sourcePathOrUrl = SourcePathOrUrl;

        if (sourcePathOrUrl == null)
          throw new IllegalCallException("The LiveRadioPlayer can only play network resources of type INetworkResourceAccessor");

        ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for stream '{1}'", PlayerTitle, sourcePathOrUrl);

        int hr = fileSourceFilter.Load(SourcePathOrUrl, null);
        new HRESULT(hr).Throw();
      }
      else
      {
        //_resourceAccessor points to a local .ts file
        var localFileSystemResourceAccessor = _resourceAccessor as ILocalFsResourceAccessor;

        if (localFileSystemResourceAccessor == null)
          throw new IllegalCallException("The LiveRadioPlayer can only play file resources of type ILocalFsResourceAccessor");

        ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for stream '{1}'", PlayerTitle, localFileSystemResourceAccessor.LocalFileSystemPath);

        fileSourceFilter.Load(localFileSystemResourceAccessor.LocalFileSystemPath, null);
      }
    }

    protected override void OnBeforeGraphRunning()
    {
      if (!_useTsReader)
      {
        base.OnBeforeGraphRunning();
        return;
      }
      
      FilterGraphTools.RenderOutputPins(_graphBuilder, _sourceFilter);
    }

    protected override void FreeCodecs ()
    {
      // Free file source
      FilterGraphTools.TryRelease(ref _sourceFilter);

      base.FreeCodecs();

      // Free all filters from graph
      if (_graphBuilder != null)
        FilterGraphTools.RemoveAllFilters(_graphBuilder, true);

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

    public int OnRequestAudioChange ()
    {
      IAMStreamSelect streamSelect = _sourceFilter as IAMStreamSelect;
      if (streamSelect != null)
        streamSelect.Enable(0, 0);
      return 0;
    }
  }
}
