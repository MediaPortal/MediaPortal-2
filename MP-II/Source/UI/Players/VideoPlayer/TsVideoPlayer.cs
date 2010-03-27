#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Runtime.InteropServices;
using DirectShowLib;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Localization;
using MediaPortal.UI.SkinEngine.Effects;
using Ui.Players.Video.Subtitles;

[ComVisible(true), ComImport,
Guid("324FAA1F-4DA6-47B8-832B-3993D8FF4151"),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITSReaderCallback
{
  [PreserveSig]
  int OnMediaTypeChanged();
};

namespace Ui.Players.Video
{
  public class TsVideoPlayer : VideoPlayer, ITSReaderCallback
  {
    #region structs and interfaces
    /// <summary>
    /// Structure to pass the subtitle language data from TsReader to this class
    /// </summary>
    /// 
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SUBTITLE_LANGUAGE
    {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
      public string lang;
    }
    /// <summary>
    /// Interface to the TsReader filter wich provides information about the 
    /// subtitle streams and allows us to change the current subtitle stream
    /// </summary>
    /// 
    [Guid("43FED769-C5EE-46aa-912D-7EBDAE4EE93A"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISubtitleStream
    {
      void SetSubtitleStream(Int32 stream);
      void GetSubtitleStreamType(Int32 stream, ref Int32 type);
      void GetSubtitleStreamCount(ref Int32 count);
      void GetCurrentSubtitleStream(ref Int32 stream);
      void GetSubtitleStreamLanguage(Int32 stream, ref SUBTITLE_LANGUAGE szLanguage);
    }
    [Guid("b9559486-E1BB-45D3-A2A2-9A7AFE49B24F"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    protected interface ITSReader
    {
      [PreserveSig]
      int SetTsReaderCallback(ITSReaderCallback callback);
    };
    #endregion

    #region imports

    [ComImport, Guid("b9559486-E1BB-45D3-A2A2-9A7AFE49B23F")]
    protected class TsReader { }

    #endregion

    #region variables

    protected IBaseFilter _fileSource = null;
    protected bool _bMediaTypeChanged = false;
    protected int _currentAudioStream = 0;
    private readonly StringId _audioStreams = new StringId("playback", "4");
    private readonly StringId _subtitleLanguage = new StringId("playback", "3");
    SubtitleRenderer _renderer;
    IBaseFilter _subtitleFilter;
    #endregion

    public TsVideoPlayer()
    {
    }

    #region graph building
    /// <summary>
    /// Frees the audio/video codecs.
    /// </summary>
    protected override void FreeCodecs()
    {
      base.FreeCodecs();
      if (_fileSource != null)
      {
        while (Marshal.ReleaseComObject(_fileSource) > 0) ;
        _fileSource = null;
      }

      if (_subtitleFilter != null)
      {
        while (Marshal.ReleaseComObject(_subtitleFilter) > 0)
          ;
        _subtitleFilter = null;
      }
      _renderer.Clear();
      _renderer.ReleaseResources();
      _renderer = null;
    }

    /// <summary>
    /// Adds the file source filter to the graph.
    /// </summary>
    protected override void AddFileSource()
    {
      // Render the file
      _fileSource = (IBaseFilter)new TsReader();
      ((ITSReader)_fileSource).SetTsReaderCallback(this);
      _graphBuilder.AddFilter((IBaseFilter)_fileSource, "TsReader");

      _renderer = new SubtitleRenderer();
      _subtitleFilter = _renderer.AddSubtitleFilter(_graphBuilder);
      _renderer.RenderSubtitles = true;
      _renderer.SetPlayer(this);

      IFileSourceFilter f = (IFileSourceFilter)_fileSource;
      f.Load(_resourceAccessor.LocalFileSystemPath, null);
    }

    protected override void OnBeforeGraphRunning()
    {
      RenderOutputPins(_fileSource);
    }

    /// <summary>
    /// Renders the output pins of the filter specified
    /// </summary>
    /// <param name="filter">The filter.</param>
    private void RenderOutputPins(IBaseFilter filter)
    {
      IEnumPins enumer;
      filter.EnumPins(out enumer);
      IPin[] pins = new IPin[2];
      IntPtr pFetched = Marshal.AllocCoTaskMem(4);
      while (enumer.Next(1, pins, pFetched) == 0)
      {
        if (Marshal.ReadInt32(pFetched) == 1)
        {
          if (pins[0] != null)
          {
            PinDirection pinDir;
            pins[0].QueryDirection(out pinDir);
            if (pinDir == PinDirection.Output)
            {
              _graphBuilder.Render(pins[0]);
            }
            Marshal.ReleaseComObject(pins[0]);
          }
        }
      }
      Marshal.FreeCoTaskMem(pFetched);
      Marshal.ReleaseComObject(enumer);
    }
    #endregion

    #region graph rebuilding
    public int OnMediaTypeChanged()
    {
      _bMediaTypeChanged = true;
      return 0;
    }

    // FIXME: To be called
    public void OnIdle()
    {
      if (_bMediaTypeChanged)
      {
        DoGraphRebuild();
        _bMediaTypeChanged = false;
      }
    }

    //check if the pin connections can be kept, or if a graph rebuilding is necessary!
    private bool GraphNeedsRebuild()
    {
      IEnumPins pinEnum;
      int hr = _fileSource.EnumPins(out pinEnum);
      if (hr != 0 || pinEnum == null) return true;
      IPin[] pins = new IPin[1];
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      for (; ; )
      {
        hr = pinEnum.Next(1, pins, ptrFetched);
        if (hr != 0 || Marshal.ReadInt32(ptrFetched) == 0) break;
        IPin other;
        hr = pins[0].ConnectedTo(out other);
        try
        {
          if (hr == 0 && other != null)
          {
            try
            {
              if (!QueryConnect(pins[0], other))
              {
                ServiceScope.Get<ILogger>().Info("Graph needs a rebuild");
                return true;
              }
            }
            finally
            {
              Marshal.ReleaseComObject(other);
              Marshal.FreeCoTaskMem(ptrFetched);
            }
          }
        }
        finally
        {
          Marshal.ReleaseComObject(pins[0]);
        }
      }
      Marshal.FreeCoTaskMem(ptrFetched);
      Marshal.ReleaseComObject(pinEnum);
      //this is only debug output at the moment. always do a rebuild for now.
      ServiceScope.Get<ILogger>().Info("Graph would _not_ need a rebuild");
      ServiceScope.Get<ILogger>().Info("BaseTSReaderPlayer: GraphNeedsRebuild() original return value is false.");
      //return true;
      return false; // Eabin ; this one breaks channel change, when going from one channel with mpeg audio to another with ac3 and vice versa.     
    }

    void DoGraphRebuild()
    {
      IMediaControl mediaCtrl = _graphBuilder as IMediaControl;
      ServiceScope.Get<ILogger>().Info("TSReaderPlayer:OnMediaTypeChanged()");
      bool needRebuild = GraphNeedsRebuild();
      if (mediaCtrl != null)
      {
        lock (mediaCtrl)
        {
          int hr = mediaCtrl.Stop();
          if (hr != 0)
          {
            ServiceScope.Get<ILogger>().Error("Error stopping graph: ({0:x})", hr);
          }
          FilterState state;
          for (; ; )
          {
            hr = mediaCtrl.GetState(200, out state);
            if (hr != 0)
            {
              ServiceScope.Get<ILogger>().Info("GetState failed: {0:x}", hr);
            }
            else if (state == FilterState.Stopped)
            {
              break;
            }
            ServiceScope.Get<ILogger>().Info("TSReaderPlayer:OnMediaTypeChanged(): Graph not yet stopped, waiting some more.");
            mediaCtrl.Stop();
            System.Threading.Thread.Sleep(100);

          }
          ServiceScope.Get<ILogger>().Info("Graph stopped.");
          if (needRebuild)
          {
            ServiceScope.Get<ILogger>().Info("Doing full graph rebuild.");
            DisconnectAllPins(_graphBuilder, _fileSource);
            RenderOutputPins(_fileSource);
          }
          else
          {
            ServiceScope.Get<ILogger>().Info("Reconnecting all pins of base filter.");
            ReConnectAll(_graphBuilder, _fileSource);
          }
          mediaCtrl.Run();
        }
        ServiceScope.Get<ILogger>().Info("Reconfigure graph done");
      }
      return;
    }

    bool QueryConnect(IPin pin, IPin other)
    {
      IEnumMediaTypes enumTypes;
      int hr = pin.EnumMediaTypes(out enumTypes);
      if (hr != 0 || enumTypes == null) return false;
      int count = 0;
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      for (; ; )
      {
        AMMediaType[] types = new AMMediaType[1];
        hr = enumTypes.Next(1, types, ptrFetched);
        if (hr != 0 || Marshal.ReadInt32(ptrFetched) == 0) break;
        count++;
        if (other.QueryAccept(types[0]) == 0)
        {
          Marshal.FreeCoTaskMem(ptrFetched);
          return true;
        }
      }
      Marshal.FreeCoTaskMem(ptrFetched);
      PinInfo info;
      PinInfo infoOther;
      pin.QueryPinInfo(out info);
      other.QueryPinInfo(out infoOther);
      ServiceScope.Get<ILogger>().Info("Pins {0} and {1} do not accept each other. Tested {2} media types", info.name, infoOther.name, count);
      return false;
    }

    bool ReConnectAll(IGraphBuilder graphBuilder, IBaseFilter filter)
    {
      bool bAllConnected = true;
      IEnumPins pinEnum;
      FilterInfo info;
      IntPtr ptrFetched = Marshal.AllocCoTaskMem(4);
      filter.QueryFilterInfo(out info);
      int hr = filter.EnumPins(out pinEnum);
      if ((hr == 0) && (pinEnum != null))
      {
        ServiceScope.Get<ILogger>().Info("got pins");
        pinEnum.Reset();
        IPin[] pins = new IPin[1];
        int iFetched;
        int iPinNo = 0;
        do
        {
          // Get the next pin
          //ServiceScope.Get<ILogger>().Info("  get pin:{0}",iPinNo);
          iPinNo++;
          hr = pinEnum.Next(1, pins, ptrFetched);
          iFetched = Marshal.ReadInt32(ptrFetched);
          if (hr == 0)
          {
            if (iFetched == 1 && pins[0] != null)
            {
              PinInfo pinInfo = new PinInfo();
              hr = pins[0].QueryPinInfo(out pinInfo);
              if (hr == 0)
              {
                ServiceScope.Get<ILogger>().Info("  got pin#{0}:{1}", iPinNo - 1, pinInfo.name);
                Marshal.ReleaseComObject(pinInfo.filter);
              }
              else
              {
                ServiceScope.Get<ILogger>().Info("  got pin:?");
              }
              PinDirection pinDir;
              pins[0].QueryDirection(out pinDir);
              if (pinDir == PinDirection.Output)
              {
                IPin other;
                hr = pins[0].ConnectedTo(out other);
                if (hr == 0 && other != null)
                {
                  ServiceScope.Get<ILogger>().Info("Reconnecting {0}:{1}", info.achName, pinInfo.name);
                  hr = graphBuilder.Reconnect(pins[0]);
                  if (hr != 0)
                  {
                    ServiceScope.Get<ILogger>().Warn("Reconnect failed: {0}:{1}, code: 0x{2:x}", info.achName, pinInfo.name, hr);
                  }
                }
              }
              Marshal.ReleaseComObject(pins[0]);
            }
            else
            {
              iFetched = 0;
              ServiceScope.Get<ILogger>().Info("no pins?");
              break;
            }
          }
          else iFetched = 0;
        } while (iFetched == 1);
        Marshal.ReleaseComObject(pinEnum);
        Marshal.ReleaseComObject(ptrFetched);
      }
      return bAllConnected;
    }
    bool DisconnectAllPins(IGraphBuilder graphBuilder, IBaseFilter filter)
    {
      IEnumPins pinEnum;
      int hr = filter.EnumPins(out pinEnum);
      if (hr != 0 || pinEnum == null) return false;
      FilterInfo info;
      filter.QueryFilterInfo(out info);
      ServiceScope.Get<ILogger>().Info("Disconnecting all pins from filter {0}", info.achName);
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
      PinInfo info;
      pin.QueryPinInfo(out info);
      ServiceScope.Get<ILogger>().Info("Disconnecting pin {0}", info.name);
      if (hr == 0 && other != null)
      {
        other.QueryPinInfo(out info);
        if (!DisconnectAllPins(graphBuilder, info.filter))
          allDisconnected = false;
        hr = pin.Disconnect();
        if (hr != 0)
        {
          allDisconnected = false;
          ServiceScope.Get<ILogger>().Error("Error disconnecting: {0:x}", hr);
        }
        hr = other.Disconnect();
        if (hr != 0)
        {
          allDisconnected = false;
          ServiceScope.Get<ILogger>().Error("Error disconnecting other: {0:x}", hr);
        }
        Marshal.ReleaseComObject(other);
      }
      else
      {
        ServiceScope.Get<ILogger>().Info("  Not connected");
      }
      return allDisconnected;
    }
    #endregion

    #region audio streams

    /// <summary>
    /// sets the current audio stream
    /// </summary>
    /// <param name="audioStream">audio stream</param>
    public override void SetAudioStream(string audioStream)
    {
      string[] streams = AudioStreams;
      for (int i = 0; i < streams.Length; ++i)
      {
        if (audioStream == streams[i])
        {
          _currentAudioStream = i;
          IAMStreamSelect pStrm = _fileSource as IAMStreamSelect;
          if (pStrm != null)
          {
            pStrm.Enable(i, AMStreamSelectEnableFlags.Enable);
          }
        }
      }
    }

    // FIXME: Remove this
    //bool IsTimeShifting
    //{
    //  get
    //  {
    //    return (_fileName.LocalPath.ToLower().IndexOf(".tsbuffer") >= 0);
    //  }
    //}
    ///// <summary>

    /// Gets the current audio stream.
    /// </summary>
    /// <value>The current audio stream.</value>
    public override string CurrentAudioStream
    {
      get
      {
        string[] streams = AudioStreams;
        if (_currentAudioStream >= 0 && _currentAudioStream < streams.Length)
        {
          return streams[_currentAudioStream];
        }
        return "";
      }
    }

    /// <summary>
    /// returns list of available audio streams
    /// </summary>
    /// <value></value>
    public override string[] AudioStreams
    {
      get
      {
        int streamCount = 0;
        IAMStreamSelect pStrm = _fileSource as IAMStreamSelect;
        if (pStrm != null)
        {
          pStrm.Count(out streamCount);
        }

        string[] streams = new string[streamCount];
        for (int i = 0; i < streamCount; ++i)
        {
          AMMediaType sType; AMStreamSelectInfoFlags sFlag;
          int sPDWGroup, sPLCid; string sName;
          object pppunk, ppobject;

          // FIXME: Fix the following commented-out code
          //if (IsTimeShifting)
          //{
          //  // The offset +2 is necessary because the first 2 streams are always non-audio and the following are the audio streams
          //  pStrm.Info(i + 2, out sType, out sFlag, out sPLCid, out sPDWGroup, out sName, out pppunk, out ppobject);
          //}
          //else
          {
            pStrm.Info(i, out sType, out sFlag, out sPLCid, out sPDWGroup, out sName, out pppunk, out ppobject);
          }

          streams[i] = sName.Trim();
        }
        return streams;
      }
    }


    #endregion

    #region subtitles

    public string[] Subtitles
    {
      get
      {
        ISubtitleStream subtitleStream = _fileSource as ISubtitleStream;
        int count = 0;
        subtitleStream.GetSubtitleStreamCount(ref count);
        string[] subs = new string[count];
        for (int i = 0; i < count; ++i)
        {
          SUBTITLE_LANGUAGE language = new SUBTITLE_LANGUAGE();
          int type = 0;
          subtitleStream.GetSubtitleStreamLanguage(i, ref language);
          subtitleStream.GetSubtitleStreamType(i, ref type);
          if (type == 0)
            subs[i] = String.Format("{0} (DVB)", language.lang);
          else
            subs[i] = String.Format("{0} (Teletext)", language.lang);
        }
        return subs;
      }
    }

    public void SetSubtitle(string subtitle)
    {
      string[] subs = Subtitles;
      for (int i = 0; i < subs.Length; ++i)
      {
        if (subtitle == subs[i])
        {
          ISubtitleStream subtitleStream = _fileSource as ISubtitleStream;
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
        subtitleStream.GetCurrentSubtitleStream(ref i);
        string[] subs = Subtitles;
        if (i >= 0 && i < subs.Length)
          return subs[i];
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

        OnIdle();//update current position
        if (_renderer != null)
          _renderer.OnSeek(CurrentTime.TotalSeconds);
      }
    }

    public override void EndRender(EffectAsset effect)
    {
      base.EndRender(effect);
      if (_renderer != null)
        _renderer.Render();
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
