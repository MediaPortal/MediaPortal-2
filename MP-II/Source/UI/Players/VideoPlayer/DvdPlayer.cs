#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using DirectShowLib;
using DirectShowLib.Dvd;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.UI.General;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Logging;
using MediaPortal.UI.Presentation.Players;
using SlimDX;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace Ui.Players.Video
{
  public class DvdPlayer : VideoPlayer, ISubtitlePlayer, IDVDPlayer
  {
    #region constants

    protected const int WM_DVD_EVENT = 0x00008002; // message from dvd graph
    protected const int WS_CHILD = 0x40000000; // attributes for video window
    protected const int WS_CLIPCHILDREN = 0x02000000;
    protected const int WS_CLIPSIBLINGS = 0x04000000;
    protected const int WM_MOUSEMOVE = 0x0200;
    protected const int WM_LBUTTONUP = 0x0202;

    private const uint VFW_E_DVD_OPERATION_INHIBITED = 0x80040276;
    private const uint VFW_E_DVD_INVALIDDOMAIN = 0x80040277;
    private const int UOP_FLAG_Play_Title_Or_AtTime = 0x00000001;
    private const int UOP_FLAG_Play_Chapter_Or_AtTime = 0x00000020;

    #endregion

    #region enums


    protected enum MenuMode
    {
      No,
      Buttons,
      Still
    }

    #endregion

    #region variables

    /// <summary> graph event interface. </summary>
    protected IMediaEventEx _mediaEvt = null;

    private IDvdGraphBuilder _dvdGraph;
    private IBaseFilter _dvdbasefilter;
    private IDvdControl2 _dvdCtrl;
    private IDvdInfo2 _dvdInfo;
    private IAMLine21Decoder _line21Decoder;

    /// <summary> asynchronous command interface. </summary>
    protected IDvdCmd _cmdOption = null;

    /// <summary> asynchronous command pending. </summary>
    protected bool _pendingCmd;


    protected DvdHMSFTimeCode _currTime; // copy of current playback states, see OnDvdEvent()
    protected int _currTitle = 0;
    protected int _currChapter = 0;
    protected DvdDomain _currDomain;

    /// <summary> current mode of playback (movie/menu/still). </summary>
    protected MenuMode _menuMode;

    protected bool _menuOn = false;
    protected ValidUOPFlag _UOPs;
    protected double _duration;
    protected double _currentTime = 0;

    private readonly string dvdDNavigator = "DVD Navigator";
    protected List<Message> _mouseMsg;

    protected DvdPreferredDisplayMode _videoPref = DvdPreferredDisplayMode.DisplayContentDefault;
    protected AspectRatioMode arMode = AspectRatioMode.Stretched;
    protected DvdVideoAttributes _videoAttr;
    private readonly StringId _subtitleLanguage = new StringId("playback", "3");
    private readonly StringId _audioStreams = new StringId("playback", "4");
    private readonly StringId _titles = new StringId("playback", "28");
    private readonly StringId _chapters = new StringId("playback", "29");

    #endregion

    public DvdPlayer()
    {
      SubscribeToMessages();
    }

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(WindowsMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, QueueMessage message)
    {
      if (message.ChannelName == WindowsMessaging.CHANNEL)
      {
        Message m = (Message) message.MessageData[WindowsMessaging.MESSAGE];

        try
        {
          if (m.Msg == WM_DVD_EVENT)
          {
            if (_mediaEvt != null)
              OnDvdEvent();
            return;
          }

          if (m.Msg == WM_MOUSEMOVE)
            if (_menuMode != MenuMode.No)
              _mouseMsg.Add(m);

          if (m.Msg == WM_LBUTTONUP)
            if (_menuMode != MenuMode.No)
              _mouseMsg.Add(m);
        }
        catch (Exception ex)
        {
          Trace.WriteLine(String.Format("DVDPlayer:WndProc() {0} {1} {2}", ex.Message, ex.Source, ex.StackTrace));
        }
      }
    }

    protected override void CreateGraphBuilder()
    {
      _currTime = new DvdHMSFTimeCode();
      _mouseMsg = new List<Message>();
      _dvdGraph = (IDvdGraphBuilder)new DvdGraphBuilder();
      _dvdGraph.GetFiltergraph(out _graphBuilder);
      _streamCount = 2;
    }

    // FIXME: To be called
    public void OnIdle()
    {
      HandleMouseMessages();
    }

    /// <summary>
    /// adds prefferred audio/video codecs
    /// </summary>
    protected override void AddPreferredCodecs()
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();

      if (!string.IsNullOrEmpty(settings.Mpeg2Codec))
      {
        _videoCodec =
          FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                           settings.Mpeg2Codec);
      }

      if (!string.IsNullOrEmpty(settings.AudioCodec))
      {
        _audioCodec =
          FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                           settings.AudioCodec);
      }

      if (_videoCodec == null)
      {
        _videoCodec =
          FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                          "Microsoft MPEG-2 Video Decoder");
      }
      if (_videoCodec == null)
      {
        _videoCodec =
          FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                           "CyberLink Video/SP Decoder (PDVD7)");
      }
      if (_videoCodec == null)
      {
        _videoCodec =
          FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                           "CyberLink Video/SP Decoder");
      }
      if (_videoCodec == null)
      {
        _videoCodec =
          FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, "NVIDIA Video Decoder");
      }
      if (_videoCodec == null)
      {
        _videoCodec =
          FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, "MPV Decoder Filter");
      }
      if (_audioCodec == null)
      {
        _audioCodec =
          FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory,
                                           "Microsoft MPEG-1/DD Audio Decoder");
      }
      if (_audioCodec == null)
      {
        _audioCodec =
          FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, "MPA Decoder Filter");
      }
    }

    /// <summary>
    /// Adds the file source filter to the graph.
    /// </summary>
    protected override void AddFileSource()
    {
      ServiceScope.Get<ILogger>().Debug("DvdPlayer.Play");
      _pendingCmd = true;
      if (dvdDNavigator == "DVD Navigator")
      {
        _dvdbasefilter = (IBaseFilter)new DVDNavigator();
        _graphBuilder.AddFilter(_dvdbasefilter, dvdDNavigator);
      }
      else
      {
        //_dvdbasefilter = DirectShowUtil.AddFilterToGraph(_graphBuilder, dvdDNavigator);
      }

      if (_dvdbasefilter != null)
      {
        _dvdCtrl = _dvdbasefilter as IDvdControl2;
        int hr;
        if (_dvdCtrl != null)
        {
          _dvdInfo = _dvdbasefilter as IDvdInfo2;
          string path = Path.GetDirectoryName(_resourceAccessor.LocalFileSystemPath);
          if (path[path.Length - 1] != Path.VolumeSeparatorChar)
          {
            if (path.Length != 0)
            {
              hr = _dvdCtrl.SetDVDDirectory(path);
            }
          }
          _dvdCtrl.SetOption(DvdOptionFlag.HMSFTimeCodeEvents, true); // use new HMSF timecode format
          _dvdCtrl.SetOption(DvdOptionFlag.ResetOnStop, false);
        }


        object comobj;
        if (_dvdInfo == null)
        {
          Guid riid = typeof(IDvdInfo2).GUID;
          hr = _dvdGraph.GetDvdInterface(riid, out comobj);
          if (hr < 0)
          {
            Marshal.ThrowExceptionForHR(hr);
          }
          _dvdInfo = (IDvdInfo2)comobj;
        }

        if (_dvdCtrl == null)
        {
          Guid riid = typeof(IDvdControl2).GUID;
          hr = _dvdGraph.GetDvdInterface(riid, out comobj);
          if (hr < 0)
          {
            Marshal.ThrowExceptionForHR(hr);
          }
          _dvdCtrl = (IDvdControl2)comobj;
        }

        //DirectShowUtil.SetARMode(_graphBuilder, arMode);
        //DirectShowUtil.EnableDeInterlace(_graphBuilder);
      }
      _mediaEvt = (_graphBuilder as IMediaEventEx);
      _mediaEvt.SetNotifyWindow(SkinContext.Form.Handle, WM_DVD_EVENT, _instancePtr);
      SetDefaultLanguages();
    }


    // FIXME: Take care: The call order was changed in VideoPlayer: This method now gets called before the
    // IMediaControl.Run() method was called. Have to check, if this still works.
    protected override void OnGraphRunning()
    {
      base.OnGraphRunning();
      int hr;
      // hr = _dvdCtrl.SelectVideoModePreference(_videoPref);
      hr = _dvdInfo.GetCurrentVideoAttributes(out _videoAttr);

      DvdDiscSide side;
      int titles, numOfVolumes, volume;
      hr = _dvdInfo.GetDVDVolumeInfo(out numOfVolumes, out volume, out side, out titles);
      if (hr < 0)
      {
        ServiceScope.Get<ILogger>().Error("DVDPlayer:Unable to get dvdvolumeinfo 0x{0:X}", hr);
        //return false;
      }
      else
      {
        if (titles <= 0)
        {
          ServiceScope.Get<ILogger>().Error("DVDPlayer:DVD does not contain any titles? {0}", titles);
          //return false;
        }
      }
      _pendingCmd = false;


      _dvdCtrl.SetSubpictureState(true, DvdCmdFlags.None, out _cmdOption);
    }

    /// <summary>
    /// Frees the audio/video codecs.
    /// </summary>
    protected override void FreeCodecs()
    {
      int hr;
      if (_cmdOption != null)
      {
        Marshal.ReleaseComObject(_cmdOption);
      }
      _cmdOption = null;
      _pendingCmd = false;

      if (_audioCodec != null)
      {
        _graphBuilder.RemoveFilter(_audioCodec);
        while ((hr = Marshal.ReleaseComObject(_audioCodec)) > 0)
        {
          ;
        }
        _audioCodec = null;
      }
      if (_videoCodec != null)
      {
        _graphBuilder.RemoveFilter(_videoCodec);
        while ((hr = Marshal.ReleaseComObject(_videoCodec)) > 0)
        {
          ;
        }
        _videoCodec = null;
      }
      if (_dvdbasefilter != null)
      {
        _graphBuilder.RemoveFilter(_dvdbasefilter);
        while ((hr = Marshal.ReleaseComObject(_dvdbasefilter)) > 0)
        {
          ;
        }
        _dvdbasefilter = null;
      }

      _line21Decoder = null;


      if (_dvdCtrl != null)
      {
        while ((hr = Marshal.ReleaseComObject(_dvdCtrl)) > 0)
        {
          ;
        }
        _dvdCtrl = null;
      }

      if (_dvdInfo != null)
      {
        while ((hr = Marshal.ReleaseComObject(_dvdInfo)) > 0)
        {
          ;
        }
        _dvdInfo = null;
      }
      if (_dvdGraph != null)
      {
        while ((hr = Marshal.ReleaseComObject(_dvdGraph)) > 0)
        {
          ;
        }
        _dvdGraph = null;
      }
    }

    protected override void OnBeforeGraphRunning()
    {
      base.OnBeforeGraphRunning();
      RenderOutputPins(_dvdbasefilter);
      if (_videoCodec != null)
      {
        RenderOutputPins(_videoCodec);
      }
      // disable Closed Captions!
      IBaseFilter basefilter;
      _graphBuilder.FindFilterByName("Line 21 Decoder", out basefilter);
      if (basefilter == null)
      {
        _graphBuilder.FindFilterByName("Line21 Decoder", out basefilter);
      }
      if (basefilter != null)
      {
        _line21Decoder = (IAMLine21Decoder)basefilter;
        if (_line21Decoder != null)
        {
          AMLine21CCState state = AMLine21CCState.Off;
          int hr = _line21Decoder.SetServiceState(state);
          if (hr == 0)
          {
            Trace.WriteLine(String.Format("DVDPlayer9:Closed Captions disabled"));
          }
          else
          {
            Trace.WriteLine(String.Format("DVDPlayer9:failed 2 disable Closed Captions"));
          }
        }
      }
      _dvdCtrl.SetSubpictureState(true, DvdCmdFlags.None, out _cmdOption);
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
          PinDirection pinDir;
          pins[0].QueryDirection(out pinDir);
          if (pinDir == PinDirection.Output)
          {
            PinInfo pinInfo;
            pins[0].QueryPinInfo(out pinInfo);
            string name = pinInfo.name.ToLower();
            if (name.IndexOf("21") < 0 && name.IndexOf("cc") < 0 && name.IndexOf("closed caption") < 0)
            {
              _graphBuilder.Render(pins[0]);
            }
            Marshal.ReleaseComObject(pinInfo.filter);
          }
          Marshal.ReleaseComObject(pins[0]);
        }
      }
      Marshal.FreeCoTaskMem(pFetched);
      Marshal.ReleaseComObject(enumer);
    }

    /// <summary>
    /// Called to handle DVD event window messages
    /// </summary>
    private void OnDvdEvent()
    {
      IntPtr p1, p2;
      try
      {
        int hr;
        do
        {
          IMediaEventEx eventEx = (IMediaEventEx)_graphBuilder;
          EventCode code;
          hr = eventEx.GetEvent(out code, out p1, out p2, 0);
          if (hr < 0)
            break;

          //Trace.WriteLine(String.Format("DVDPlayer DVD EVT :" + code.ToString()));

          switch (code)
          {
            case EventCode.DvdPlaybackStopped:
              Trace.WriteLine(
                String.Format("DVDPlayer DvdPlaybackStopped event :{0:X} {1:X}", p1.ToInt32(), p2.ToInt32()));
              break;
            case EventCode.DvdError:
              Trace.WriteLine(String.Format("DVDPlayer DvdError event :{0:X} {1:X}", p1.ToInt32(), p2.ToInt32()));
              break;
            case EventCode.VMRReconnectionFailed:
              Trace.WriteLine(
                String.Format("DVDPlayer VMRReconnectionFailed event :{0:X} {1:X}", p1.ToInt32(), p2.ToInt32()));
              break;
            case EventCode.DvdWarning:
              Trace.WriteLine(String.Format("DVDPlayer DVD warning :{0} {1}", p1.ToInt32(), p2.ToInt32()));
              break;
            case EventCode.DvdCurrentHmsfTime:
              {
                byte[] ati = BitConverter.GetBytes(p1.ToInt32());
                if (ati != null)
                {
                  _currTime.bHours = ati[0];
                  _currTime.bMinutes = ati[1];
                  _currTime.bSeconds = ati[2];
                  _currTime.bFrames = ati[3];
                  _currentTime = _currTime.bHours * 3600d;
                  _currentTime += (_currTime.bMinutes * 60d);
                  _currentTime += _currTime.bSeconds;
                  //Trace.WriteLine(String.Format("  time:{0}", _currentTime));
                }

                break;
              }

            case EventCode.DvdSubPicictureStreamChange:
              {
                Trace.WriteLine(
                  String.Format("EVT:DvdSubPicture Changed to:{0} Enabled:{1}", p1.ToInt32(), p2.ToInt32()));
              }
              break;

            case EventCode.DvdChapterStart:
              {
                Trace.WriteLine(String.Format("EVT:DvdChaptStart:{0}", p1.ToInt32()));
                _currChapter = p1.ToInt32();
                // Dhu?! Path to disaster, what about multiple tracks of same lang.
                // The DVD graph should remember language setting, if not it's a bug
                // in the DVD software.
                // SelectSubtitleLanguage(_subtitleLanguage);

                DvdHMSFTimeCode totaltime = new DvdHMSFTimeCode();
                DvdTimeCodeFlags ulTimeCodeFlags;
                _dvdInfo.GetTotalTitleTime(totaltime, out ulTimeCodeFlags);

                _duration = totaltime.bHours * 3600d;
                _duration += (totaltime.bMinutes * 60d);
                _duration += totaltime.bSeconds;
                Trace.WriteLine(String.Format("  _duration:{0}", _currentTime));

                break;
              }

            case EventCode.DvdTitleChange:
              {
                Trace.WriteLine(String.Format("EVT:DvdTitleChange:{0}", p1.ToInt32()));
                _currTitle = p1.ToInt32();
                // Dhu?! Path to disaster, what about multiple tracks of same lang.
                // The DVD graph should remember language setting, if not it's a bug
                // in the DVD software.
                // SelectSubtitleLanguage(_subtitleLanguage);

                DvdHMSFTimeCode totaltime = new DvdHMSFTimeCode();
                DvdTimeCodeFlags ulTimeCodeFlags;
                _dvdInfo.GetTotalTitleTime(totaltime, out ulTimeCodeFlags);

                _duration = totaltime.bHours * 3600d;
                _duration += (totaltime.bMinutes * 60d);
                _duration += totaltime.bSeconds;

                Trace.WriteLine(String.Format("  _duration:{0}", _currentTime));
                break;
              }

            case EventCode.DvdCmdStart:
              {
                //if (_pendingCmd)
                Trace.WriteLine(String.Format("  DvdCmdStart with pending"));
                break;
              }

            case EventCode.DvdCmdEnd:
              {
                OnCmdComplete(p1);
                break;
              }

            case EventCode.DvdStillOn:
              {
                Trace.WriteLine(String.Format("EVT:DvdStillOn:{0}", p1.ToInt32()));
                if (p1 == IntPtr.Zero)
                {
                  _menuMode = MenuMode.Buttons;
                }
                else
                {
                  _menuMode = MenuMode.Still;
                }

                EnableFrameSkipping(false);

                break;
              }

            case EventCode.DvdStillOff:
              {
                Trace.WriteLine(String.Format("EVT:DvdStillOff:{0}", p1.ToInt32()));
                if (_menuMode == MenuMode.Still)
                {
                  _menuMode = MenuMode.No;
                }
                EnableFrameSkipping(true);
                break;
              }

            case EventCode.DvdButtonChange:
              {
                //Repaint();

                // Menu buttons might not be available even if the menu is on
                // (buttons appear after menu animation) ( DvdDomain.VideoManagerMenu or 
                // DvdDomain.VideoTitleSetMenu event is already received at that point )

                if (!_menuOn)
                {
                  int buttonCount, focusedButton;
                  int result = _dvdInfo.GetCurrentButton(out buttonCount, out focusedButton);
                  if (result == 0 && buttonCount > 0 && focusedButton > 0)
                  {
                    // Menu button(s) found, enable menu
                    _menuOn = true;
                    if ((ValidUOPFlag.ShowMenuRoot & _UOPs) != 0)
                      hr = _dvdCtrl.ShowMenu(DvdMenuId.Root, DvdCmdFlags.Block | DvdCmdFlags.Flush, out _cmdOption);
                    if ((ValidUOPFlag.ShowMenuTitle & _UOPs) != 0)
                      hr = _dvdCtrl.ShowMenu(DvdMenuId.Title, DvdCmdFlags.Block | DvdCmdFlags.Flush, out _cmdOption);
                    else if ((ValidUOPFlag.ShowMenuChapter & _UOPs) != 0)
                      hr = _dvdCtrl.ShowMenu(DvdMenuId.Chapter, DvdCmdFlags.Block | DvdCmdFlags.Flush, out _cmdOption);
                  }
                  else
                    _menuOn = false;
                  Trace.WriteLine(String.Format("EVT:DVDPlayer:domain=title (menu:{0}) hr:{1:X}", _menuOn, hr));
                }

                Trace.WriteLine(String.Format("EVT:DvdButtonChange: buttons:#{0}", p1.ToInt32()));
                if (p1.ToInt32() <= 0)
                  _menuMode = MenuMode.No;
                else
                  _menuMode = MenuMode.Buttons;
                break;
              }

            case EventCode.DvdNoFpPgc:
              {
                Trace.WriteLine(String.Format("EVT:DvdNoFpPgc:{0}", p1.ToInt32()));
                if (_dvdCtrl != null)
                  hr = _dvdCtrl.PlayTitle(1, DvdCmdFlags.None, out _cmdOption);
                break;
              }

            case EventCode.DvdAudioStreamChange:
              // audio stream changed
              Trace.WriteLine(String.Format("EVT:DvdAudioStreamChange:{0}", p1.ToInt32()));
              break;

            case EventCode.DvdValidUopsChange:
              _UOPs = (ValidUOPFlag)p1.ToInt32();
              Trace.WriteLine(String.Format("EVT:DvdValidUopsChange:{0}", _UOPs));
              break;

            case EventCode.DvdDomainChange:
              {
                _currDomain = (DvdDomain)p1;
                switch ((DvdDomain)p1)
                {
                  case DvdDomain.FirstPlay:
                    Trace.WriteLine(String.Format("EVT:DVDPlayer:domain=firstplay"));
                    break;
                  case DvdDomain.Stop:
                    Trace.WriteLine(String.Format("EVT:DVDPlayer:domain=stop"));
                    break;
                  case DvdDomain.VideoManagerMenu:
                    Trace.WriteLine(String.Format("EVT:DVDPlayer:domain=videomanagermenu (menu)"));
                    _menuOn = true;
                    break;
                  case DvdDomain.VideoTitleSetMenu:
                    Trace.WriteLine(String.Format("EVT:DVDPlayer:domain=videotitlesetmenu (menu)"));
                    _menuOn = true;
                    break;
                  case DvdDomain.Title:
                    _menuOn = false;
                    break;
                  default:
                    Trace.WriteLine(String.Format("EVT:DvdDomChange:{0}", p1.ToInt32()));
                    break;
                }
                break;
              }
          }

          eventEx.FreeEventParams(code, p1, p2);
        } while (hr == 0);
      }
      catch (Exception ex)
      {
        Trace.WriteLine(String.Format("DVDPlayer:OnDvdEvent() {0} {1} {2}", ex.Message, ex.Source, ex.StackTrace));
      }
      //      Log.Info("DVDEvent done");
    }

    /// <summary>
    /// Called when an asynchronous DVD command is completed
    /// </summary>
    /// <param name="p1">The p1.</param>
    private void OnCmdComplete(IntPtr p1)
    {
      try
      {
        Trace.WriteLine(String.Format("DVD OnCmdComplete.........."));
        if (!_pendingCmd || _dvdInfo == null)
          return;

        IDvdCmd cmd;
        int hr = _dvdInfo.GetCmdFromEvent(p1, out cmd);
        if ((hr != 0) || (cmd == null))
        {
          Trace.WriteLine(String.Format("!!!DVD OnCmdComplete GetCmdFromEvent failed!!!"));
          return;
        }

        if (cmd != _cmdOption)
        {
          Trace.WriteLine(String.Format("DVDPlayer:DVD OnCmdComplete UNKNOWN CMD!!!"));
          Marshal.ReleaseComObject(cmd);
          return;
        }

        Marshal.ReleaseComObject(cmd);
        Marshal.ReleaseComObject(_cmdOption);
        _cmdOption = null;
        _pendingCmd = false;
        Trace.WriteLine(String.Format("DVD OnCmdComplete OK."));
      }
      catch (Exception ex)
      {
        Trace.WriteLine(String.Format("DVDPlayer:OnCmdComplete() {0} {1} {2}", ex.Message, ex.Source, ex.StackTrace));
      }
    }


    /// <summary>
    /// Handles the mouse click/movement messages.
    /// </summary>
    private void HandleMouseMessages()
    {
      try
      {
        foreach (Message m in _mouseMsg)
        {
          if (_menuMode == MenuMode.No)
            break;
          long lParam = m.LParam.ToInt32();
          float x = (int)(lParam & 0xffff);
          float y = (int)(lParam >> 16);

          //scale to skin coordinates
          x *= SkinContext.SkinWidth / SkinContext.Form.ClientSize.Width;
          y *= SkinContext.SkinHeight / SkinContext.Form.ClientSize.Height;


          Vector3 upperLeft = _vertices[0].Position;
          Vector3 upperright = _vertices[1].Position;
          Vector3 bottomleft = _vertices[3].Position;
          if (x >= upperLeft.X && x <= upperright.X)
          {
            if (y >= upperLeft.Y && y <= bottomleft.Y)
            {
              //scale to video window coordinates
              float width = upperright.X - upperLeft.X;
              float height = bottomleft.Y - upperLeft.Y;
              x -= upperLeft.X;
              y -= upperLeft.Y;
              x *= (_videoAttr.sourceResolutionX / width);
              y *= (_videoAttr.sourceResolutionY / height);

              Point pt = new Point((int)x, (int)y);
              if (_currDomain == DvdDomain.FirstPlay || _currDomain == DvdDomain.Title ||
                  _currDomain == DvdDomain.VideoManagerMenu || _currDomain == DvdDomain.VideoTitleSetMenu)
              {
                if (m.Msg == WM_MOUSEMOVE)
                {
                  // Select the button at the current position, if it exists
                  //_dvdCtrl.SelectAtPosition(pt);
                  int buttonIndex;
                  if (0 == _dvdInfo.GetButtonAtPosition(pt, out buttonIndex))
                  {
                    int hr = _dvdCtrl.SelectButton(buttonIndex);
                  }
                  //_dvdCtrl.ActivateButton(buttonIndex);
                }
              }

              if (_currDomain == DvdDomain.VideoManagerMenu || _currDomain == DvdDomain.VideoTitleSetMenu)
              {
                if (m.Msg == WM_LBUTTONUP)
                {
                  // Highlight the button at the current position, if it exists
                  _dvdCtrl.ActivateAtPosition(pt);
                }
              }
            }
          }
        }
      }
      catch (Exception) { }
      _mouseMsg.Clear();
    }

    /// <summary>
    /// Shows the DVD menu.
    /// </summary>
    public void ShowDvdMenu()
    {
      ServiceScope.Get<ILogger>().Debug("DvdPlayer: ShowDvdMenu");
      _dvdCtrl.ShowMenu(DvdMenuId.Root, DvdCmdFlags.None, out _cmdOption);
    }

    /// <summary>
    /// handles DVD navigation
    /// </summary>
    /// <param name="key">The key.</param>
    /// FIXME: Has to be re-integrated
    //public void Navigate(Key key)
    //{
    //  if (key == Key.DvdUp)
    //    _dvdCtrl.SelectRelativeButton(DvdRelativeButton.Upper);
    //  if (key == Key.DvdDown)
    //    _dvdCtrl.SelectRelativeButton(DvdRelativeButton.Lower);
    //  if (key == Key.DvdLeft)
    //    _dvdCtrl.SelectRelativeButton(DvdRelativeButton.Left);
    //  if (key == Key.DvdRight)
    //    _dvdCtrl.SelectRelativeButton(DvdRelativeButton.Right);
    //  if (key == Key.DvdSelect)
    //    _dvdCtrl.ActivateButton();
    //}

    /// <summary>
    /// returns the current play time
    /// </summary>
    /// <value></value>
    public override TimeSpan CurrentTime
    {
      get { return new TimeSpan(0, 0, 0, 0, (int)(_currentTime * 1000.0f)); }
      set
      {
        ServiceScope.Get<ILogger>().Debug("DvdPlayer: seek {0} / {1}", value.TotalSeconds, Duration.TotalSeconds);
        TimeSpan newTime = value;
        if (newTime.TotalSeconds < 0)
        {
          newTime = new TimeSpan();
        }
        if (newTime < Duration)
        {
          int hours = newTime.Hours;
          int minutes = newTime.Minutes;
          int seconds = newTime.Seconds;
          DvdHMSFTimeCode timeCode = new DvdHMSFTimeCode();
          timeCode.bHours = (byte)(hours & 0xff);
          timeCode.bMinutes = (byte)(minutes & 0xff);
          timeCode.bSeconds = (byte)(seconds & 0xff);
          timeCode.bFrames = 0;
          DvdPlaybackLocation2 loc;
          _currTitle = _dvdInfo.GetCurrentLocation(out loc);

          try
          {
            _dvdCtrl.PlayAtTime(timeCode, DvdCmdFlags.Block, out _cmdOption);
          }
          catch { }
        }
      }
    }

    public override TimeSpan Duration
    {
      get { return new TimeSpan(0, 0, 0, 0, (int)(_duration * 1000.0f)); }
    }

    public override string[] AudioStreams
    {
      get
      {
        int streamsAvailable, currentStream;
        int hr = _dvdInfo.GetCurrentAudio(out streamsAvailable, out currentStream);
        string[] streams = new string[streamsAvailable];
        for (int i = 0; i < streamsAvailable; ++i)
        {
          int audioLanguage;
          DvdAudioAttributes attr;
          _dvdInfo.GetAudioLanguage(i, out audioLanguage);
          _dvdInfo.GetAudioAttributes(i, out attr);
          foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
          {
            if (ci.LCID == (audioLanguage & 0x3ff))
            {
              streams[i] =
                String.Format("{0} ({1}/{2} ch/{3} KHz)", ci.EnglishName, attr.AudioFormat,
                              attr.bNumberOfChannels, (attr.dwFrequency / 1000));

              switch (attr.LanguageExtension)
              {
                case DvdAudioLangExt.NotSpecified:
                  break;
                case DvdAudioLangExt.Captions:
                  break;
                case DvdAudioLangExt.VisuallyImpaired:
                  streams[i] += ServiceScope.Get<ILocalization>().ToString("playback", "27"); // " (Visually Impaired)";
                  break;
                case DvdAudioLangExt.DirectorComments1:
                  streams[i] += ServiceScope.Get<ILocalization>().ToString("playback", "24"); // " (Director Comments)";
                  break;
                case DvdAudioLangExt.DirectorComments2:
                  streams[i] += ServiceScope.Get<ILocalization>().ToString("playback", "28"); //" (Director Comments 2)";
                  break;
              }
            }
          }
        }
        return streams;
      }
    }

    public string[] Subtitles
    {
      get
      {
        int streamsAvailable;
        int currentStream;
        bool isDisabled;
        _dvdInfo.GetCurrentSubpicture(out streamsAvailable, out currentStream, out isDisabled);
        string[] streams = new string[streamsAvailable + 1];
        streams[0] = ServiceScope.Get<ILocalization>().ToString("playback", "17"); //off
        for (int i = 0; i < streamsAvailable; ++i)
        {
          DvdSubpictureAttributes attr;
          int iLanguage;
          _dvdInfo.GetSubpictureLanguage(i, out iLanguage);
          _dvdInfo.GetSubpictureAttributes(i, out attr);
          foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
          {
            if (ci.LCID == (iLanguage & 0x3ff))
            {
              streams[i + 1] = ci.EnglishName;
              switch (attr.LanguageExtension)
              {
                case DvdSubPictureLangExt.CaptionNormal:
                  break;
                case DvdSubPictureLangExt.CaptionBig:
                  streams[i + 1] += ServiceScope.Get<ILocalization>().ToString("playback", "18"); //big
                  break;
                case DvdSubPictureLangExt.CaptionChildren:
                  streams[i + 1] += ServiceScope.Get<ILocalization>().ToString("playback", "19"); //Children
                  break;
                case DvdSubPictureLangExt.CCNormal:
                  streams[i + 1] += ServiceScope.Get<ILocalization>().ToString("playback", "20"); //CC
                  break;
                case DvdSubPictureLangExt.CCBig:
                  streams[i + 1] += ServiceScope.Get<ILocalization>().ToString("playback", "21"); //CC big
                  break;
                case DvdSubPictureLangExt.CCChildren:
                  streams[i + 1] += ServiceScope.Get<ILocalization>().ToString("playback", "22"); //CC Children
                  break;
                case DvdSubPictureLangExt.Forced:
                  streams[i + 1] += ServiceScope.Get<ILocalization>().ToString("playback", "23"); //Forced
                  break;
                case DvdSubPictureLangExt.DirectorCommentsNormal:
                  streams[i + 1] += ServiceScope.Get<ILocalization>().ToString("playback", "24"); //Director Comments
                  break;
                case DvdSubPictureLangExt.DirectorCommentsBig:
                  streams[i + 1] += ServiceScope.Get<ILocalization>().ToString("playback", "25"); //Director Comments big
                  break;
                case DvdSubPictureLangExt.DirectorCommentsChildren:
                  streams[i + 1] += " (Directors Comments children)";
                  break;
              }
            }
          }
        }
        return streams;
      }
    }

    public void SetSubtitle(string subtitle)
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      string[] subtitles = Subtitles;
      for (int i = 0; i < subtitles.Length; ++i)
      {
        if (subtitles[i] == subtitle)
        {
          if (i == 0)
          {
            ServiceScope.Get<ILogger>().Debug("DvdPlayer: disable subtitles");
            _dvdCtrl.SetSubpictureState(false, DvdCmdFlags.None, out _cmdOption);
            settings.SubtitleLanguage = "";
            ServiceScope.Get<ISettingsManager>().Save(settings);
          }
          else
          {
            ServiceScope.Get<ILogger>().Debug("DvdPlayer: select subtitle:{0}", subtitle);
            _dvdCtrl.SelectSubpictureStream(i - 1, DvdCmdFlags.None, out _cmdOption);
            _dvdCtrl.SetSubpictureState(true, DvdCmdFlags.None, out _cmdOption);
            int iLanguage;
            _dvdInfo.GetSubpictureLanguage(i - 1, out iLanguage);
            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
            {
              if (ci.LCID == (iLanguage & 0x3ff))
              {
                settings.SubtitleLanguage = ci.EnglishName;
                ServiceScope.Get<ISettingsManager>().Save(settings);
                break;
              }
            }
          }
          return;
        }
      }
    }

    public string CurrentSubtitle
    {
      get
      {
        int streamsAvailable, currentStream;
        bool isDisabled;
        _dvdInfo.GetCurrentSubpicture(out streamsAvailable, out currentStream, out isDisabled);
        string[] subs = Subtitles;
        if (isDisabled)
        {
          return subs[0];
        }
        return subs[currentStream + 1];
      }
    }

    /// <summary>
    /// sets the current audio stream
    /// </summary>
    /// <param name="audioStream">audio stream</param>
    public override void SetAudioStream(string audioStream)
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      string[] audioStreams = AudioStreams;
      for (int i = 0; i < audioStreams.Length; ++i)
      {
        if (audioStreams[i] == audioStream)
        {
          ServiceScope.Get<ILogger>().Debug("DvdPlayer: select audio stream:{0}", audioStream);
          _dvdCtrl.SelectAudioStream(i, DvdCmdFlags.None, out _cmdOption);
          int audioLanguage;
          _dvdInfo.GetAudioLanguage(i, out audioLanguage);
          foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
          {
            if (ci.LCID == (audioLanguage & 0x3ff))
            {
              settings.AudioLanguage = ci.EnglishName;
              ServiceScope.Get<ISettingsManager>().Save(settings);
              break;
            }
          }
          return;
        }
      }
    }

    /// <summary>
    /// Gets the current audio stream.
    /// </summary>
    /// <value>The current audio stream.</value>
    public override string CurrentAudioStream
    {
      get
      {
        int streamsAvailable, currentStream;
        _dvdInfo.GetCurrentAudio(out streamsAvailable, out currentStream);
        string[] subs = AudioStreams;
        return subs[currentStream];
      }
    }

    /// <summary>
    /// Gets the DVD state
    /// </summary>
    /// <param name="resumeData">The resume data.</param>
    /// <returns></returns>
    private bool GetResumeState(out byte[] resumeData)
    {
      try
      {
        resumeData = null;
        IDvdState dvdState;
        int hr = _dvdInfo.GetState(out dvdState);
        if (hr != 0)
        {
          return false;
        }

        IPersistMemory dvdStatePersistMemory = (IPersistMemory)dvdState;
        if (dvdStatePersistMemory == null)
        {
          Marshal.ReleaseComObject(dvdState);
          return false;
        }
        uint resumeSize;
        dvdStatePersistMemory.GetSizeMax(out resumeSize);
        if (resumeSize <= 0)
        {
          Marshal.ReleaseComObject(dvdStatePersistMemory);
          Marshal.ReleaseComObject(dvdState);
          return false;
        }
        IntPtr stateData = Marshal.AllocCoTaskMem((int)resumeSize);

        try
        {
          dvdStatePersistMemory.Save(stateData, true, resumeSize);
          resumeData = new byte[resumeSize];
          Marshal.Copy(stateData, resumeData, 0, (int)resumeSize);
        }
        catch { }

        Marshal.FreeCoTaskMem(stateData);
        Marshal.ReleaseComObject(dvdStatePersistMemory);
        Marshal.ReleaseComObject(dvdState);
      }
      catch (Exception)
      {
        resumeData = null;
      }
      return true;
    }

    /// <summary>
    /// Sets the DVD state
    /// </summary>
    /// <param name="resumeData">The resume data.</param>
    /// <returns></returns>
    private void SetResumeState(byte[] resumeData)
    {
      if ((resumeData != null) && (resumeData.Length > 0))
      {
        IDvdState dvdState;

        int hr = _dvdInfo.GetState(out dvdState);
        if (hr < 0)
          return;
        IPersistMemory dvdStatePersistMemory = (IPersistMemory)dvdState;
        IntPtr stateData = Marshal.AllocHGlobal(resumeData.Length);
        Marshal.Copy(resumeData, 0, stateData, resumeData.Length);

        try
        {
          dvdStatePersistMemory.Load(stateData, (uint)resumeData.Length);
        }
        finally
        {
          Marshal.FreeHGlobal(stateData);
        }

        hr = _dvdCtrl.SetState(dvdState, DvdCmdFlags.Block, out _cmdOption);
        if (hr == 0)
          return;

        Marshal.ReleaseComObject(dvdState);
      }

      return;
    }

    public override void Stop()
    {
      ServiceScope.Get<ILogger>().Debug("DvdPlayer:Stop");

      base.Stop();
    }

    public string[] DvdTitles
    {
      get
      {
        string[] titles;
        DvdDiscSide side;
        int titleCount, numOfVolumes, volume;
        _dvdInfo.GetDVDVolumeInfo(out numOfVolumes, out volume, out side, out titleCount);
        titles = new string[titleCount];
        for (int i = 0; i < titleCount; ++i)
        {
          titles[i] = String.Format("Title {0}", i + 1);
        }
        return titles;
      }
    }

    public void SetDvdTitle(string title)
    {
      string[] titles = DvdTitles;
      for (int i = 0; i < titles.Length; ++i)
      {
        if (title == titles[i])
        {
          int hr = _dvdCtrl.PlayTitle(1 + i, DvdCmdFlags.Flush, out _cmdOption);
          return;
        }
      }
    }

    public string CurrentDvdTitle
    {
      get
      {
        DvdPlaybackLocation2 location;
        _dvdInfo.GetCurrentLocation(out location);
        return String.Format("Title {0}", location.TitleNum);
      }
    }


    public string[] DvdChapters
    {
      get
      {
        string[] chapters;
        int chapterCount;
        DvdPlaybackLocation2 location;
        _dvdInfo.GetCurrentLocation(out location);
        _dvdInfo.GetNumberOfChapters(location.TitleNum, out chapterCount);
        chapters = new string[chapterCount];
        for (int i = 0; i < chapterCount; ++i)
        {
          chapters[i] = String.Format("Chapter {0}", i + 1);
        }
        return chapters;
      }
    }

    public void SetDvdChapter(string title)
    {
      string[] chapters = DvdChapters;
      for (int i = 0; i < chapters.Length; ++i)
      {
        if (title == chapters[i])
        {
          int hr = _dvdCtrl.PlayChapter(i + 1, DvdCmdFlags.Flush, out _cmdOption);
          return;
        }
      }
    }

    public string CurrentDvdChapter
    {
      get
      {
        DvdPlaybackLocation2 location;
        _dvdInfo.GetCurrentLocation(out location);
        return String.Format("Chapter {0}", location.ChapterNum);
      }
    }

    public bool InDvdMenu
    {
      get { return (_menuMode != MenuMode.No); }
    }

    /// <summary>
    /// Gets the name of the resume file
    /// </summary>
    /// 
    protected string GetResumeFilename(Uri fileName)
    {
      IBaseFilter dvdbasefilter;
      IDvdInfo2 dvdInfo;
      int hr;
      long discId;
      int actualSize;

      dvdbasefilter = (IBaseFilter)new DVDNavigator();
      dvdInfo = dvdbasefilter as IDvdInfo2;

      StringBuilder path = new StringBuilder(1024);
      dvdInfo.GetDVDDirectory(path, 1024, out actualSize);
      dvdInfo.GetDiscID(path.ToString(), out discId);

      if (dvdbasefilter != null)
      {
        while ((hr = Marshal.ReleaseComObject(dvdbasefilter)) > 0) ;
        dvdbasefilter = null;
      }
      if (_dvdInfo != null)
      {
        while ((hr = Marshal.ReleaseComObject(dvdInfo)) > 0) ;
        dvdInfo = null;
      }
      return String.Format(@"D_{0:X}.dat", discId.ToString());
    }

    /// <summary>
    /// Set the default languages before playback
    /// </summary>
    private void SetDefaultLanguages()
    {
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      ServiceScope.Get<ILogger>().Info("SetDefaultLanguages");
      int lCID;
      if (settings.AudioLanguage != null && settings.AudioLanguage != "")
      {
        int setError = 0;
        string errorText = "";
        lCID = GetLCID(settings.AudioLanguage);
        if (lCID >= 0)
        {
          setError = 0;
          errorText = "";
          // Flip: Added more detailed message
          setError = _dvdCtrl.SelectDefaultAudioLanguage(lCID, DvdAudioLangExt.NotSpecified);
          switch (setError)
          {
            case 0:
              errorText = "Success.";
              break;
            case 631:
              errorText = "The DVD Navigator filter is not in the Stop domain.";
              break;
            default:
              errorText = "Unknown Error. " + setError;
              break;
          }


          ServiceScope.Get<ILogger>().Info("DVDPlayer:Set default language:{0} {1} {2}", settings.AudioLanguage, lCID, errorText);
        }
      }
      if (settings.SubtitleLanguage != null && settings.SubtitleLanguage != "")
      {
        int setError = 0;
        string errorText = "";
        // For now, the default menu language is the same as the subtitle language
        lCID = GetLCID(settings.SubtitleLanguage);
        if (lCID >= 0)
        {
          setError = 0;
          errorText = "";
          setError = _dvdCtrl.SelectDefaultMenuLanguage(lCID);
          // Flip: Added more detailed message
          switch (setError)
          {
            case 0:
              errorText = "Success.";
              break;
            case 631:
              errorText = "The DVD Navigator filter is not in a valid domain.";
              break;
            default:
              errorText = "Unknown Error. " + setError;
              break;
          }
          ServiceScope.Get<ILogger>().Info("DVDPlayer:Set default menu language:{0} {1} {2}", settings.SubtitleLanguage, lCID, errorText);
        }

        lCID = GetLCID(settings.SubtitleLanguage);
        if (lCID >= 0)
        {
          setError = 0;
          errorText = "";
          setError = _dvdCtrl.SelectDefaultSubpictureLanguage(lCID, DvdSubPictureLangExt.NotSpecified);
          // Flip: Added more detailed message
          switch (setError)
          {
            case 0:
              errorText = "Success.";
              break;
            case 631:
              errorText = "The DVD Navigator filter is not in a valid domain.";
              break;
            default:
              errorText = "Unknown Error. " + setError;
              break;
          }
          ServiceScope.Get<ILogger>().Info("DVDPlayer:Set default subtitle language:{0} {1} {2}", settings.SubtitleLanguage, lCID, errorText);
        }

        // Force subtitles if this option is set in the configuration
        _dvdCtrl.SetSubpictureState(true, DvdCmdFlags.None, out _cmdOption);
      }
      else
      {
        _dvdCtrl.SetSubpictureState(false, DvdCmdFlags.None, out _cmdOption);
      }
    }
    int GetLCID(string language)
    {
      if (language == null) return -1;
      if (language.Length == 0) return -1;
      // Flip: Added to cut off the detailed name info
      string cutName;
      int start = 0;
      // Flip: Changed from CultureTypes.NeutralCultures to CultureTypes.SpecificCultures
      // Flip: CultureTypes.NeutralCultures did not work, provided the wrong CLID
      foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
      {
        // Flip: cut off detailed info, e.g. "English (United States)" -> "English"
        // Flip: to get correct compare
        start = ci.EnglishName.IndexOf(" (");
        if (start > 0)
          cutName = ci.EnglishName.Substring(0, start);
        else
          cutName = ci.EnglishName;

        if (String.Compare(cutName, language, true) == 0)
        {
          return ci.LCID;
        }
      }
      return -1;
    }
  }
}
