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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DirectShowLib;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Subtitles;
using MediaPortal.UI.Players.Video.Tools;
using System.Timers;

namespace MediaPortal.UI.Players.Video
{
  public class TsVideoPlayer : VideoPlayer, ITsReaderCallback, ITsReaderCallbackAudioChange
  {
    #region imports

    [ComImport, Guid("b9559486-E1BB-45D3-A2A2-9A7AFE49B23F")]
    protected class TsReader { }

    #endregion

    #region constants and structs

    private const string TSREADER_FILTER_NAME = "TsReader";
  
    #endregion

    #region variables

    protected IBaseFilter _fileSource = null;
    protected bool _bMediaTypeChanged = false;
    protected bool _bRequestAudioChange = false;
    SubtitleRenderer _renderer;
    IBaseFilter _subtitleFilter;

    #endregion

    #region constructor
    /// <summary>
    /// Constructs a TsReader player object.
    /// </summary>
    public TsVideoPlayer()
      : base()
    {
      PlayerTitle = "TsVideoPlayer"; // for logging
      _requiredCapabilities = CodecHandler.CodecCapabilities.VideoH264 | CodecHandler.CodecCapabilities.VideoMPEG2 | CodecHandler.CodecCapabilities.AudioMPEG;
    }
    #endregion

    #region graph building
    /// <summary>
    /// Frees the audio/video codecs.
    /// </summary>
    protected override void FreeCodecs()
    {
      base.FreeCodecs();
      _renderer.Clear();
      _renderer.ReleaseResources();
      _renderer = null;
      FilterGraphTools.TryRelease(ref _subtitleFilter);
      FilterGraphTools.TryRelease(ref _fileSource);
    }

    /// <summary>
    /// Adds the file source filter to the graph.
    /// </summary>
    protected override void AddFileSource()
    {
      // Render the file
      _fileSource = (IBaseFilter)new TsReader();

      ITsReader tsReader = (ITsReader) _fileSource;
      tsReader.SetRelaxedMode(1);
      tsReader.SetTsReaderCallback(this);
      tsReader.SetRequestAudioChangeCallback(this);

      _graphBuilder.AddFilter(_fileSource, TSREADER_FILTER_NAME);

      _renderer = new SubtitleRenderer();
      _subtitleFilter = _renderer.AddSubtitleFilter(_graphBuilder);
      if (_subtitleFilter != null)
      {
        _renderer.RenderSubtitles = true;
      }
      _renderer.SetPlayer(this);

      IFileSourceFilter f = (IFileSourceFilter)_fileSource;
      f.Load(_resourceAccessor.LocalFileSystemPath, null);
    }

    protected override void OnBeforeGraphRunning()
    {
      FilterGraphTools.RenderOutputPins(_graphBuilder, _fileSource);
    }

    /// <summary>
    /// no extension based changes
    /// </summary>
    protected override void SetCapabilitiesByExtension()
    { }


    #endregion

    #region ITSReaderCallback members
    /// <summary>
    /// Callback when MediaType has changed.
    /// </summary>
    /// <param name="mediaType">new MediaType</param>
    /// <returns>0</returns>
    public int OnMediaTypeChanged(int mediaType)
    {
      Timer timer = new Timer(1) { AutoReset = false };
      timer.Elapsed += AsynchRebuild;
      timer.Enabled = true;
      return 0;
    }

    void AsynchRebuild(object sender, ElapsedEventArgs e)
    {
      DoGraphRebuild();
    }

    /// <summary>
    /// Callback when VideoFormat changed.
    /// </summary>
    /// <param name="streamType"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="aspectRatioX"></param>
    /// <param name="aspectRatioY"></param>
    /// <param name="bitrate"></param>
    /// <param name="isInterlaced"></param>
    /// <returns></returns>
    public int OnVideoFormatChanged(int streamType, int width, int height, int aspectRatioX, int aspectRatioY,
                                    int bitrate, int isInterlaced)
    {
      //_videoFormat.IsValid = true;
      //_videoFormat.streamType = (VideoStreamType)streamType;
      //_videoFormat.width = width;
      //_videoFormat.height = height;
      //_videoFormat.arX = aspectRatioX;
      //_videoFormat.arY = aspectRatioY;
      //_videoFormat.bitrate = bitrate;
      //_videoFormat.isInterlaced = (isInterlaced == 1);
      //Log.Info("TsReaderPlayer: OnVideoFormatChanged - {0}", _videoFormat.ToString());
      return 0;
    }
    #endregion

    #region ITSReaderAudioCallback members

    /// <summary>
    /// Callback when Audio change is requested.
    /// </summary>
    /// <returns></returns>
    public int OnRequestAudioChange()
    {
      _bRequestAudioChange = true;
      
      //FIXME: TsReader request an explicit choice for Audio Stream! Otherwise there is a 5 seconds delay, because
      // the demuxer is waiting...
      if (_streamInfoAudio == null)
        EnumerateStreams();

      if (_streamInfoAudio.Count > 0)
        _streamInfoAudio.EnableStream(_streamInfoAudio[0].Name);
      return 0;
    }

    #endregion

    #region graph rebuilding

    //check if the pin connections can be kept, or if a graph rebuilding is necessary!
    private bool GraphNeedsRebuild(IBaseFilter baseFilter)
    {
      IEnumPins pinEnum;
      int hr = baseFilter.EnumPins(out pinEnum);
      if (hr != 0 || pinEnum == null) return true;
      IPin[] pins = new IPin[1];
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      for (; ; )
      {
        hr = pinEnum.Next(1, pins, ptrFetched);
        if (hr != 0 || Marshal.ReadInt32(ptrFetched) == 0) 
          break;
        IPin other;
        hr = pins[0].ConnectedTo(out other);
        try
        {
          if (hr == 0 && other != null)
          {
            try
            {
              PinInfo pinInfo;
              pins[0].QueryPinInfo(out pinInfo);
              FilterInfo filterInfo = FilterGraphTools.QueryFilterInfoAndFree(pinInfo.filter);
              try
              {
                if (!QueryConnect(pins[0], other))
                {
                  ServiceRegistration.Get<ILogger>().Info("Graph needs a rebuild. Filter: {0}, Pin: {1}",
                                                          filterInfo.achName, pinInfo.name);
                  return true;
                }
              }
              finally
              {
                FilterGraphTools.FreePinInfo(pinInfo);
              }
            }
            finally
            {
              Marshal.ReleaseComObject(other);
            }
          }
        }
        finally
        {
          Marshal.ReleaseComObject(pins[0]);
        }
      }
      Marshal.ReleaseComObject(pinEnum);
      Marshal.FreeCoTaskMem(ptrFetched);
      //this is only debug output at the moment. always do a rebuild for now.
      ServiceRegistration.Get<ILogger>().Info("Graph would _not_ need a rebuild");
      ServiceRegistration.Get<ILogger>().Info("TSReaderPlayer: GraphNeedsRebuild() original return value is false.");
      return false; // Eabin ; this one breaks channel change, when going from one channel with mpeg audio to another with ac3 and vice versa.     
    }

    void DoGraphRebuild()
    {
      IMediaControl mediaCtrl = _graphBuilder as IMediaControl;
      ServiceRegistration.Get<ILogger>().Info("TSReaderPlayer:DoGraphRebuild()");
      if (mediaCtrl != null)
      {
        lock (SyncObj)
        {
          int hr = mediaCtrl.Stop();
          if (hr != 0)
          {
            ServiceRegistration.Get<ILogger>().Error("Error stopping graph: ({0:x})", hr);
          }
          for (; ; )
          {
            FilterState state;
            hr = mediaCtrl.GetState(200, out state);
            if (hr != 0)
            {
              ServiceRegistration.Get<ILogger>().Info("GetState failed: {0:x}", hr);
            }
            else if (state == FilterState.Stopped)
            {
              break;
            }
            ServiceRegistration.Get<ILogger>().Info("TSReaderPlayer:OnMediaTypeChanged(): Graph not yet stopped, waiting some more.");
            mediaCtrl.Stop();
            System.Threading.Thread.Sleep(100);

          }
          ServiceRegistration.Get<ILogger>().Info("Graph stopped.");
          bool needRebuild = GraphNeedsRebuild(_fileSource);
          
          if (needRebuild)
          {
            ServiceRegistration.Get<ILogger>().Info("Doing full graph rebuild.");
            DisconnectAllPins(_graphBuilder, _fileSource);
            FilterGraphTools.RenderOutputPins(_graphBuilder, _fileSource);
          }
          else
          {
            ServiceRegistration.Get<ILogger>().Info("Reconnecting all pins of base filter.");
            ReConnectAll(_graphBuilder, _fileSource);
          }
          mediaCtrl.Run();
        }
        ServiceRegistration.Get<ILogger>().Info("Reconfigure graph done");
      }
      return;
    }

    //FIXME: Move all pin connection methods to FilterGraphTools
    bool QueryConnect(IPin pin, IPin other)
    {
      IEnumMediaTypes enumTypes;
      int hr = pin.EnumMediaTypes(out enumTypes);
      if (hr != 0 || enumTypes == null)
        return false;

      int count = 0;
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      try
      {
        for (; ; )
        {
          AMMediaType[] types = new AMMediaType[1];
          hr = enumTypes.Next(1, types, ptrFetched);
          if (hr != 0 || Marshal.ReadInt32(ptrFetched) == 0)
            break;

          count++;
          if (other.QueryAccept(types[0]) == 0)
            return true;

          FilterGraphTools.FreeAMMediaType(types[0]);
        }

        PinInfo info;
        PinInfo infoOther;
        pin.QueryPinInfo(out info);
        other.QueryPinInfo(out infoOther);
        ServiceRegistration.Get<ILogger>().Info("Pins {0} and {1} do not accept each other. Tested {2} media types",
                                                info.name, infoOther.name, count);
        FilterGraphTools.FreePinInfo(info);
        FilterGraphTools.FreePinInfo(infoOther);
        return false;
      }
      finally
      {
        Marshal.FreeCoTaskMem(ptrFetched);
      }
    }

    bool ReConnectAll(IGraphBuilder graphBuilder, IBaseFilter filter)
    {
      bool bAllConnected = true;
      IEnumPins pinEnum;
      FilterInfo info = FilterGraphTools.QueryFilterInfoAndFree(filter);
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      //filter.QueryFilterInfo(out info);
      int hr = filter.EnumPins(out pinEnum);
      if ((hr == 0) && (pinEnum != null))
      {
        ServiceRegistration.Get<ILogger>().Info("got pins");
        IPin[] pins = new IPin[1];
        int iFetched;
        int iPinNo = 0;
        do
        {
          // Get the next pin
          iPinNo++;
          hr = pinEnum.Next(1, pins, ptrFetched);

          // in case of error stop the pin enumeration
          if (hr != 0)
            break;

          iFetched = Marshal.ReadInt32(ptrFetched);
          if (iFetched == 1 && pins[0] != null)
          {
            PinInfo pinInfo;
            hr = pins[0].QueryPinInfo(out pinInfo);
            if (hr == 0)
            {
              ServiceRegistration.Get<ILogger>().Info("  got pin#{0}:{1}", iPinNo - 1, pinInfo.name);
              FilterGraphTools.FreePinInfo(pinInfo);
            }
            else
            {
              ServiceRegistration.Get<ILogger>().Info("  got pin:?");
            }
            PinDirection pinDir;
            pins[0].QueryDirection(out pinDir);
            if (pinDir == PinDirection.Output)
            {
              IPin other;
              hr = pins[0].ConnectedTo(out other);
              if (hr == 0 && other != null)
              {
                ServiceRegistration.Get<ILogger>().Info("Reconnecting {0}:{1}", info.achName, pinInfo.name);
                hr = graphBuilder.Reconnect(pins[0]);
                if (hr != 0)
                {
                  ServiceRegistration.Get<ILogger>().Warn("Reconnect failed: {0}:{1}, code: 0x{2:x}", info.achName,
                                                          pinInfo.name, hr);
                  bAllConnected = false;
                }
                PinInfo otherPinInfo;
                other.QueryPinInfo(out otherPinInfo);
                ReConnectAll(graphBuilder, otherPinInfo.filter);
                FilterGraphTools.FreePinInfo(otherPinInfo);
                FilterGraphTools.TryRelease(ref other);
              }
            }
            FilterGraphTools.TryRelease(ref pins[0]);
          }
          else
          {
            ServiceRegistration.Get<ILogger>().Info("no pins?");
            break;
          }
        } 
        while (iFetched == 1);
        FilterGraphTools.TryRelease(ref pinEnum);
        Marshal.FreeCoTaskMem(ptrFetched);
      }
      return bAllConnected;
    }

    bool DisconnectAllPins(IGraphBuilder graphBuilder, IBaseFilter filter)
    {
      IEnumPins pinEnum;
      int hr = filter.EnumPins(out pinEnum);
      if (hr != 0 || pinEnum == null) return false;
      FilterInfo info = FilterGraphTools.QueryFilterInfoAndFree(filter);
      ServiceRegistration.Get<ILogger>().Info("Disconnecting all pins from filter {0}", info.achName);
      bool allDisconnected = true;
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      for (; ; )
      {
        IPin[] pins = new IPin[1];

        hr = pinEnum.Next(1, pins, ptrFetched);
        if (hr != 0 || Marshal.ReadInt32(ptrFetched) == 0) break;
        PinInfo pinInfo;
        pins[0].QueryPinInfo(out pinInfo);
        if (pinInfo.dir == PinDirection.Output)
        {
          if (!DisconnectPin(graphBuilder, pins[0]))
            allDisconnected = false;
        }
        FilterGraphTools.FreePinInfo(pinInfo);
        Marshal.ReleaseComObject(pins[0]);
      }
      Marshal.ReleaseComObject(pinEnum);
      Marshal.FreeCoTaskMem(ptrFetched);
      return allDisconnected;
    }

    bool DisconnectPin(IGraphBuilder graphBuilder, IPin pin)
    {
      IPin other;
      int hr = pin.ConnectedTo(out other);
      bool allDisconnected = true;
      if (hr == 0 && other != null)
      {
        PinInfo info;
        pin.QueryPinInfo(out info);
        ServiceRegistration.Get<ILogger>().Info("Disconnecting pin {0}", info.name);
        FilterGraphTools.FreePinInfo(info);

        other.QueryPinInfo(out info);
        if (!DisconnectAllPins(graphBuilder, info.filter))
          allDisconnected = false;

        FilterGraphTools.FreePinInfo(info);

        hr = pin.Disconnect();
        if (hr != 0)
        {
          allDisconnected = false;
          ServiceRegistration.Get<ILogger>().Error("Error disconnecting: {0:x}", hr);
        }
        hr = other.Disconnect();
        if (hr != 0)
        {
          allDisconnected = false;
          ServiceRegistration.Get<ILogger>().Error("Error disconnecting other: {0:x}", hr);
        }
        Marshal.ReleaseComObject(other);
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Info("  Not connected");
      }
      return allDisconnected;
    }

    #endregion

    #region subtitles

    public string[] Subtitles
    {
      get
      {
        ISubtitleStream subtitleStream = _fileSource as ISubtitleStream;
        int count = 0;
        List<String> subs = new List<String>();
        if (subtitleStream != null)
        {
          subtitleStream.GetSubtitleStreamCount(ref count);
          for (int i = 0; i < count; ++i)
          {
            SubtitleLanguage language = new SubtitleLanguage();
            int type = 0;
            subtitleStream.GetSubtitleStreamLanguage(i, ref language);
            subtitleStream.GetSubtitleStreamType(i, ref type);
            subs.Add(type == 0
                       ? String.Format("{0} (DVB)", language.lang)
                       : String.Format("{0} (Teletext)", language.lang));
          }
        }
        return subs.ToArray();
      }
    }

    public void SetSubtitle(string subtitle)
    {
      for (int i = 0; i < Subtitles.Length; ++i)
      {
        if (subtitle == Subtitles[i])
        {
          ISubtitleStream subtitleStream = _fileSource as ISubtitleStream;
          if (subtitleStream != null)
            subtitleStream.SetSubtitleStream(i);
          return;
        }
      }
    }

    public string CurrentSubtitle
    {
      get
      {
        ISubtitleStream subtitleStream = _fileSource as ISubtitleStream;
        int i = 0;
        if (subtitleStream != null)
          subtitleStream.GetCurrentSubtitleStream(ref i);
        if (i >= 0 && i < Subtitles.Length)
          return Subtitles[i];
        return "";
      }
    }

    #endregion

    public override TimeSpan CurrentTime
    {
      get { return base.CurrentTime; }
      set
      {
        base.CurrentTime = value;
        if (_renderer != null)
          _renderer.OnSeek(CurrentTime.TotalSeconds);
      }
    }

    public override void ReleaseGUIResources()
    {
      if (_renderer != null)
        _renderer.ReleaseResources();
      base.ReleaseGUIResources();
    }

    public override void ReallocGUIResources()
    {
      if (_renderer != null)
        _renderer.ReallocResources();
      base.ReallocGUIResources();
    }
  }
}
