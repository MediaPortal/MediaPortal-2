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
using System.Runtime.InteropServices;
using DirectShowLib;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;

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

    protected string _playerIdString = "{C77220B0-5489-4F59-8594-DD46E88F7C62}";
    protected IBaseFilter _fileSource = null;
    protected bool _useTsReader;

    public LiveRadioPlayer(bool useTsReader)
    {
      PlayerTitle = "LiveRadioPlayer"; // for logging
      _useTsReader = useTsReader;
    }

    public override Guid PlayerId
    {
      get { return new Guid(_playerIdString); }
    }

    public override string Name
    {
      get { return "Live Radio"; }
    }

    /// <summary>
    /// Adds the file source filter to the graph.
    /// </summary>
    protected override void AddFileSource()
    {
      if (!_useTsReader)
      {
        base.AddFileSource();
        return;
      }

      // Render the file
      _fileSource = (IBaseFilter) new TsReader();

      ITsReader tsReader = (ITsReader) _fileSource;
      tsReader.SetRelaxedMode(1);
      tsReader.SetTsReaderCallback(this);
      tsReader.SetRequestAudioChangeCallback(this);

      _graphBuilder.AddFilter(_fileSource, TSREADER_FILTER_NAME);

      IFileSourceFilter f = (IFileSourceFilter) _fileSource;
      f.Load(_resourceAccessor.LocalFileSystemPath, null);
    }

    protected override void OnBeforeGraphRunning()
    {
      if (!_useTsReader)
      {
        base.OnBeforeGraphRunning();
        return;
      }
      
      FilterGraphTools.RenderOutputPins(_graphBuilder, _fileSource);
    }

    protected override void FreeCodecs ()
    {
      // Free all filters from graph
      if (_graphBuilder != null)
        FilterGraphTools.RemoveAllFilters(_graphBuilder, true);

      FilterGraphTools.TryDispose(ref _rot);
      FilterGraphTools.TryRelease(ref _graphBuilder);

      // Free file source
      FilterGraphTools.TryRelease(ref _fileSource); 
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
      IAMStreamSelect streamSelect = _fileSource as IAMStreamSelect;
      if (streamSelect != null)
        streamSelect.Enable(0, 0);
      return 0;
    }
  }
}
