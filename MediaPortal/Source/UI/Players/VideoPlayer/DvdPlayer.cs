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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using DirectShowLib;
using DirectShowLib.Dvd;
using MediaPortal.Core;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.General;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.SkinManagement;
using Ui.Players.Video.Interfaces;

namespace Ui.Players.Video
{
  public class DvdPlayer : VideoPlayer, ISubtitlePlayer, IDVDPlayer
  {
    #region Constants

    protected const int WM_DVD_EVENT = 0x00008002; // message from dvd graph
    protected const int WS_CHILD = 0x40000000; // attributes for video window
    protected const int WS_CLIPCHILDREN = 0x02000000;
    protected const int WS_CLIPSIBLINGS = 0x04000000;
    protected const int WM_MOUSEMOVE = 0x0200;
    protected const int WM_LBUTTONUP = 0x0202;

    #endregion

    #region Variables

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

    /// <summary>
    /// List of subtitles. Will be initialized lazily. <c>null</c> if not currently valid.
    /// </summary>
    protected List<String> _subtitleStreams = null;

    protected List<String> _chapters = null;
    
    protected object _guiPropertiesLock = new object();

    protected DvdHMSFTimeCode _currTime; // copy of current playback states, see OnDvdEvent()
    protected int _currTitle = 0;
    protected int _currChapter = 0;
    protected int _buttonCount = 0;
    protected int _focusedButton = 0;
    protected DvdDomain _currDomain;
    
    protected bool _menuOn = false;
    protected ValidUOPFlag _UOPs;
    protected double _duration;
    protected double _currentTime = 0;

    private const string DVD_NAVIGATOR = "DVD Navigator";
    
    protected DvdPreferredDisplayMode _videoPref = DvdPreferredDisplayMode.DisplayContentDefault;
    protected AspectRatioMode arMode = AspectRatioMode.Stretched;
    protected DvdVideoAttributes _videoAttr;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a DVDPlayer player object.
    /// </summary>
    public DvdPlayer()
    {
      PlayerTitle = "DVDPlayer"; // for logging
      _requiredCapabilities = CodecHandler.CodecCapabilities.VideoMPEG2 | CodecHandler.CodecCapabilities.AudioMPEG;
      RegisterKeyBindings();
      SubscribeToMessages();
    }

    #endregion

    #region Input mapping de-/registration

    protected void RegisterKeyBindings()
    {
      InputManager inputManager = InputManager.Instance;
      inputManager.KeyPressed += OnKeyPressed;
    }

    protected void UnregisterKeyBindings()
    {
      InputManager inputManager = InputManager.Instance;
      inputManager.KeyPressed -= OnKeyPressed;
    }

    #endregion

    #region Graphbuilding overrides

    protected override void CreateGraphBuilder()
    {
      _dvdGraph = (IDvdGraphBuilder)new DvdGraphBuilder();
      DsError.ThrowExceptionForHR(_dvdGraph.GetFiltergraph(out _graphBuilder));
      _streamCount = 3; // Allow Video, CC, and Subtitle
    }

    /// <summary>
    /// Adds the file source filter to the graph.
    /// </summary>
    protected override void AddFileSource()
    {
      ServiceRegistration.Get<ILogger>().Debug("DvdPlayer.AddFileSource");
      _pendingCmd = true;
      if (DVD_NAVIGATOR == "DVD Navigator")
      {
        _dvdbasefilter = (IBaseFilter) new DVDNavigator();
        _graphBuilder.AddFilter(_dvdbasefilter, DVD_NAVIGATOR);
      }

      if (_dvdbasefilter == null)
        throw new Exception("Failed to add DVD Navigator!");

      _dvdInfo = _dvdbasefilter as IDvdInfo2;
      _dvdCtrl = _dvdbasefilter as IDvdControl2;

      if (_dvdCtrl == null)
        throw new Exception("Failed to access DVD Control!");

      string path = Path.GetDirectoryName(_resourceAccessor.LocalFileSystemPath);

      // check if path is a drive root (like D:), otherwise append VIDEO_TS 
      // MediaItem always contains the parent folder. Add the required VIDEO_TS subfolder.
      if (!String.IsNullOrEmpty(path) && !path.EndsWith(Path.VolumeSeparatorChar.ToString()))
        path = Path.Combine(path, "VIDEO_TS");

      int hr = _dvdCtrl.SetDVDDirectory(path);
      _dvdCtrl.SetOption(DvdOptionFlag.HMSFTimeCodeEvents, true); // use new HMSF timecode format
      _dvdCtrl.SetOption(DvdOptionFlag.ResetOnStop, false);

      _mediaEvt = (_graphBuilder as IMediaEventEx);
      if (_mediaEvt != null)
        _mediaEvt.SetNotifyWindow(SkinContext.Form.Handle, WM_DVD_EVENT, _instancePtr);

      SetDefaultLanguages();
    }

    protected override void OnBeforeGraphRunning()
    {
      base.OnBeforeGraphRunning();

      // first all automatically rendered pins
      FilterGraphTools.RenderOutputPins(_graphBuilder, _dvdbasefilter);
      
      // MSDN: "During the connection process, the Filter Graph Manager ignores pins on intermediate filters if the pin name begins with a tilde (~)."
      // then connect the skipped "~" output pins
      FilterGraphTools.RenderAllManualConnectPins(_graphBuilder);

      // TODO: get CC from settings
      _line21Decoder = FilterGraphTools.FindFilterByInterface<IAMLine21Decoder>(_graphBuilder);
      if (_line21Decoder != null)
      {
        int hr = _line21Decoder.SetCurrentService(AMLine21CCService.None);
        ServiceRegistration.Get<ILogger>().Debug(hr == 0 ? "DVDPlayer: Closed Captions disabled" :
            "DVDPlayer: failed to disable Closed Captions");
      }
      _currTime = new DvdHMSFTimeCode();
    }

    protected override void OnGraphRunning()
    {
      base.OnGraphRunning();

      int hr = _dvdCtrl.SelectVideoModePreference(_videoPref);
      hr = _dvdInfo.GetCurrentVideoAttributes(out _videoAttr);

      DvdDiscSide side;
      int titles, numOfVolumes, volume;
      hr = _dvdInfo.GetDVDVolumeInfo(out numOfVolumes, out volume, out side, out titles);
      if (hr < 0)
        ServiceRegistration.Get<ILogger>().Error("DVDPlayer: Unable to get dvdvolumeinfo 0x{0:X}", hr);

      if (titles <= 0)
        ServiceRegistration.Get<ILogger>().Error("DVDPlayer: DVD does not contain any titles? {0}", titles);

      _pendingCmd = false;

      _dvdCtrl.SetSubpictureState(true, DvdCmdFlags.None, out _cmdOption);
    }

    /// <summary>
    /// Frees the audio/video codecs.
    /// </summary>
    protected override void FreeCodecs()
    {
      _pendingCmd = false;
      FilterGraphTools.TryRelease(ref _cmdOption);      
      FilterGraphTools.TryRelease(ref _line21Decoder);
      FilterGraphTools.TryRelease(ref _dvdCtrl);
      FilterGraphTools.TryRelease(ref _dvdInfo);
      FilterGraphTools.TryRelease(ref _dvdGraph);
      base.FreeCodecs();
    }

    #endregion

    #region Defaults and resuming information

    /// <summary>
    /// Set the default languages before playback.
    /// </summary>
    private void SetDefaultLanguages()
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      ServiceRegistration.Get<ILogger>().Info("DVDPlayer: SetDefaultLanguages");
      int lcid;
      int setError;
      string errorText;
      if (!string.IsNullOrEmpty(settings.AudioLanguage))
      {
        lcid = GetLCID(settings.AudioLanguage);
        if (lcid >= 0)
        {
          // Flip: Added more detailed message
          setError = _dvdCtrl.SelectDefaultAudioLanguage(lcid, DvdAudioLangExt.NotSpecified);
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
          ServiceRegistration.Get<ILogger>().Info("DVDPlayer: Set default language:{0} {1} {2}", settings.AudioLanguage, lcid, errorText);
        }
      }
      if (!string.IsNullOrEmpty(settings.SubtitleLanguage))
      {
        // For now, the default menu language is the same as the subtitle language
        lcid = GetLCID(settings.SubtitleLanguage);
        if (lcid >= 0)
        {
          setError = _dvdCtrl.SelectDefaultMenuLanguage(lcid);
          errorText = GetErrorText(setError);
          ServiceRegistration.Get<ILogger>().Info("DVDPlayer: Set default menu language:{0} {1} {2}", settings.SubtitleLanguage, lcid, errorText);
        }

        lcid = GetLCID(settings.SubtitleLanguage);
        if (lcid >= 0)
        {
          setError = _dvdCtrl.SelectDefaultSubpictureLanguage(lcid, DvdSubPictureLangExt.NotSpecified);
          errorText = GetErrorText(setError);
          ServiceRegistration.Get<ILogger>().Info("DVDPlayer: Set default subtitle language:{0} {1} {2}", settings.SubtitleLanguage, lcid, errorText);
        }

        // Force subtitles if this option is set in the configuration
        _dvdCtrl.SetSubpictureState(true, DvdCmdFlags.None, out _cmdOption);
      }
      else
      {
        _dvdCtrl.SetSubpictureState(false, DvdCmdFlags.None, out _cmdOption);
      }
    }

    /// <summary>
    /// Gets a DVD navigator related error text for passed error code.
    /// </summary>
    /// <param name="setError">Error code</param>
    /// <returns>Error message</returns>
    private static string GetErrorText(int setError)
    {
      string errorText;
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
      return errorText;
    }

    static int GetLCID(string language)
    {
      if (language == null) return -1;
      if (language.Length == 0) return -1;
      // Flip: Added to cut off the detailed name info
      // Flip: Changed from CultureTypes.NeutralCultures to CultureTypes.SpecificCultures
      // Flip: CultureTypes.NeutralCultures did not work, provided the wrong CLID
      foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
      {
        // Flip: cut off detailed info, e.g. "English (United States)" -> "English"
        // Flip: to get correct compare
        int start = ci.EnglishName.IndexOf(" (");
        string cutName = start > 0 ? ci.EnglishName.Substring(0, start) : ci.EnglishName;

        if (String.Compare(cutName, language, true) == 0)
        {
          return ci.LCID;
        }
      }
      return -1;
    }

    /// <summary>
    /// Gets the DVD resume state.
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
    /// Sets the DVD resume state.
    /// </summary>
    /// <param name="resumeData">The resume data.</param>
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
          dvdStatePersistMemory.Load(stateData, (uint) resumeData.Length);
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

    /// <summary>
    /// Gets the name of the resume file.
    /// </summary>
    protected string GetResumeFilename(Uri fileName)
    {
      long discId = 0;
      int actualSize;

      IBaseFilter dvdbasefilter = (IBaseFilter) new DVDNavigator();
      IDvdInfo2 dvdInfo = dvdbasefilter as IDvdInfo2;

      StringBuilder path = new StringBuilder(1024);
      if (dvdInfo != null)
      {
        dvdInfo.GetDVDDirectory(path, 1024, out actualSize);
        dvdInfo.GetDiscID(path.ToString(), out discId);
      }

      FilterGraphTools.TryRelease(ref dvdbasefilter);
      FilterGraphTools.TryRelease(ref _dvdInfo);

      return String.Format(@"D_{0:X}.dat", discId);
    }

    #endregion

    #region DVD Commands

    /// <summary>
    /// Shows the DVD menu.
    /// </summary>
    public void ShowDvdMenu()
    {
      ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: ShowDvdMenu");
      _dvdCtrl.ShowMenu(DvdMenuId.Root, DvdCmdFlags.None, out _cmdOption);
    }

    /// <summary>
    /// Stops the playback and the input handling.
    /// </summary>
    public override void Stop()
    {
      ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: Stop");
      UnregisterKeyBindings();
      UnsubscribeFromMessages();
      base.Stop();
    }

    #endregion

    #region DVD Event handling

    /// <summary>
    /// Called to handle DVD event window messages.
    /// </summary>
    private void OnDvdEvent()
    {
      if (_mediaEvt == null || _dvdCtrl == null)
        return;

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

          switch (code)
          {
            case EventCode.DvdPlaybackStopped:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer DvdPlaybackStopped event: {0:X} {1:X}", p1.ToInt32(), p2.ToInt32());
              break;
            case EventCode.DvdError:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer DvdError event: {0:X} {1:X}", p1.ToInt32(), p2.ToInt32());
              break;
            case EventCode.VMRReconnectionFailed:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer VMRReconnectionFailed event: {0:X} {1:X}", p1.ToInt32(), p2.ToInt32());
              break;
            case EventCode.DvdWarning:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer DVD warning: {0} {1}", p1.ToInt32(), p2.ToInt32());
              break;
            case EventCode.DvdSubPicictureStreamChange:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdSubPicture Changed to: {0} Enabled: {1}", p1.ToInt32(), p2.ToInt32());
              break;
            case EventCode.DvdCurrentHmsfTime:
              GetCurrentTime(p1);
              break;

            case EventCode.DvdChapterStart:
              {
                ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdChaptStart: {0}", p1.ToInt32());
                _currChapter = p1.ToInt32();
                CalculateDuration();
                ServiceRegistration.Get<ILogger>().Debug("  _duration: {0}", _currentTime);
                break;
              }

            case EventCode.DvdTitleChange:
              {
                ServiceRegistration.Get<ILogger>().Debug("EVT: DvdTitleChange: {0}", p1.ToInt32());
                _subtitleStreams = null; // Invalidate subtitles - could be different in other title
                _chapters = null; // Invalidate chapters - they are different per title
                _currTitle = p1.ToInt32();
                CalculateDuration();
                ServiceRegistration.Get<ILogger>().Debug("  _duration: {0}", _currentTime);
                break;
              }

            case EventCode.DvdCmdStart:
              ServiceRegistration.Get<ILogger>().Debug("  DvdCmdStart with pending");
              break;

            case EventCode.DvdCmdEnd:
              {
                OnCmdComplete(p1);
                break;
              }

            case EventCode.DvdStillOn:
              {
                ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdStillOn: {0}", p1.ToInt32());
                EnableFrameSkipping(false);
                break;
              }

            case EventCode.DvdStillOff:
              {
                ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdStillOff: {0}", p1.ToInt32());
                EnableFrameSkipping(true);
                break;
              }

            case EventCode.DvdButtonChange:
              {
                _buttonCount = p1.ToInt32();
                _focusedButton = p2.ToInt32();
                ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdButtonChange: buttons: {0}, focused: {1}", _buttonCount, _focusedButton);
                break;
              }

            case EventCode.DvdNoFpPgc:
              {
                ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdNoFpPgc: {0}", p1.ToInt32());
                if (_dvdCtrl != null)
                  hr = _dvdCtrl.PlayTitle(1, DvdCmdFlags.None, out _cmdOption);
                break;
              }

            case EventCode.DvdAudioStreamChange:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdAudioStreamChange: {0}", p1.ToInt32());
              break;

            case EventCode.DvdValidUopsChange:
              _UOPs = (ValidUOPFlag)p1.ToInt32();
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdValidUopsChange: {0}", _UOPs);
              break;

            case EventCode.DvdDomainChange:
              {
                _currDomain = (DvdDomain)p1;
                switch ((DvdDomain)p1)
                {
                  case DvdDomain.FirstPlay:
                    ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: domain=firstplay");
                    _menuOn = false;
                    break;
                  case DvdDomain.Stop:
                    ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: domain=stop");
                    Stop();
                    break;
                  case DvdDomain.VideoManagerMenu:
                    ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: domain=videomanagermenu (menu)");
                    _menuOn = true;
                    break;
                  case DvdDomain.VideoTitleSetMenu:
                    ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: domain=videotitlesetmenu (menu)");
                    _menuOn = true;
                    break;
                  case DvdDomain.Title:
                    _menuOn = false;
                    break;
                  default:
                    ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdDomChange: {0}", p1.ToInt32());
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
        ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: OnDvdEvent() {0} {1} {2}", ex.Message, ex.Source, ex.StackTrace);
      }
    }

    /// <summary>
    /// Called when an asynchronous DVD command is completed.
    /// </summary>
    /// <param name="p1">The p1.</param>
    private void OnCmdComplete(IntPtr p1)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("DVD OnCmdComplete..........");
        if (!_pendingCmd || _dvdInfo == null)
          return;

        IDvdCmd cmd;
        int hr = _dvdInfo.GetCmdFromEvent(p1, out cmd);
        if ((hr != 0) || (cmd == null))
        {
          ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DVD OnCmdComplete GetCmdFromEvent failed");
          return;
        }

        if (cmd != _cmdOption)
        {
          ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DVD OnCmdComplete UNKNOWN CMD!!!");
          Marshal.ReleaseComObject(cmd);
          return;
        }

        Marshal.ReleaseComObject(cmd);
        Marshal.ReleaseComObject(_cmdOption);
        _cmdOption = null;
        _pendingCmd = false;
        ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DVD OnCmdComplete OK.");
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: OnCmdComplete() {0} {1} {2}", ex.Message, ex.Source, ex.StackTrace);
      }
    }

    #endregion

    #region Input event handling

    protected override void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WindowsMessaging.CHANNEL)
      {
        Message m = (Message)message.MessageData[WindowsMessaging.MESSAGE];

        try
        {
          if (m.Msg == WM_DVD_EVENT)            
            OnDvdEvent();
          else if (m.Msg == WM_MOUSEMOVE || m.Msg == WM_LBUTTONUP)
            HandleMouseMessage(m);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: WndProc() {0} {1} {2}", ex.Message, ex.Source, ex.StackTrace);
        }
      }
    }

    /// <summary>
    /// Returns the Low-Word of a long.
    /// </summary>
    /// <param name="dWord">long</param>
    /// <returns>Low-Word</returns>
    static int LoWord(long dWord)
    {
      return (int)(dWord & 0xffff);
    }

    /// <summary>
    /// Returns the High-Word of a long.
    /// </summary>
    /// <param name="dWord">long</param>
    /// <returns>High-Word</returns>
    static int HiWord(long dWord)
    {
      return (int)((dWord >> 16) & 0xffff);
    }

    /// <summary>
    /// Handles the mouse click/movement messages.
    /// </summary>
    private void HandleMouseMessage(Message m)
    {
      try
      {
        if (_dvdCtrl == null || !_menuOn || _buttonCount <= 0)
          return;

        long lParam = m.LParam.ToInt32();

        float x = LoWord(lParam); // Mouse position
        float y = HiWord(lParam);

        //scale from ClientSize to video's size
        x *= (float) _videoAttr.sourceResolutionX/SkinContext.Form.ClientSize.Width;
        y *= (float) _videoAttr.sourceResolutionY/SkinContext.Form.ClientSize.Height;

        Point pt = new Point((int) x, (int) y);

        if (m.Msg == WM_MOUSEMOVE)
          {
            // Highlight the button at the current position, if it exists
            _dvdCtrl.SelectAtPosition(pt);
          }
        else if (m.Msg == WM_LBUTTONUP)
          {
            // Activate ("click") the button at the current position, if it exists
            _dvdCtrl.ActivateAtPosition(pt);
          }
      }
      catch{}
    }
    
    /// <summary>
    /// Handles DVD navigation.
    /// </summary>
    /// <param name="key">The key.</param>
    public void OnKeyPressed(ref Key key)
    {
      if (_dvdCtrl == null || !_menuOn || _buttonCount <= 0)
        return;

      if (key == Key.Up)
        _dvdCtrl.SelectRelativeButton(DvdRelativeButton.Upper);
      else if (key == Key.Down)
        _dvdCtrl.SelectRelativeButton(DvdRelativeButton.Lower);
      else if (key == Key.Left)
        _dvdCtrl.SelectRelativeButton(DvdRelativeButton.Left);
      else if (key == Key.Right)
        _dvdCtrl.SelectRelativeButton(DvdRelativeButton.Right);
      else if (_focusedButton > 0 && (key == Key.Ok || key == Key.Enter))
        _dvdCtrl.ActivateButton();
    }

    #endregion

    #region Audio stream handling

    /// <summary>
    /// Gets the available audio streams.
    /// </summary>
    public override string[] AudioStreams
    {
      get
      {
        int streamsAvailable, currentStream;
        _dvdInfo.GetCurrentAudio(out streamsAvailable, out currentStream);
        List<String> streams = new List<String>();
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
              StringBuilder currentAudio = new StringBuilder();
              currentAudio.AppendFormat("{0} ({1}/{2} ch/{3} KHz)",
                                        ci.EnglishName,
                                        attr.AudioFormat,
                                        attr.bNumberOfChannels, (attr.dwFrequency/1000));

              switch (attr.LanguageExtension)
              {
                case DvdAudioLangExt.NotSpecified:
                case DvdAudioLangExt.Captions:
                  break;
                case DvdAudioLangExt.VisuallyImpaired:
                case DvdAudioLangExt.DirectorComments1:
                case DvdAudioLangExt.DirectorComments2:
                  currentAudio.AppendFormat(" ({0})", 
                    ServiceRegistration.Get<ILocalization>().ToString("[Playback." + attr.LanguageExtension + "]")
                    );
                  break;
              }
              streams.Add(currentAudio.ToString());
            }
          }
        }
        return streams.ToArray();
      }
    }

    /// <summary>
    /// Sets the current audio stream.
    /// </summary>
    /// <param name="audioStream">audio stream</param>
    public override void SetAudioStream(string audioStream)
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      string[] audioStreams = AudioStreams;
      for (int i = 0; i < audioStreams.Length; ++i)
      {
        if (audioStreams[i] == audioStream)
        {
          ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: Select audio stream: {0}", audioStream);
          _dvdCtrl.SelectAudioStream(i, DvdCmdFlags.None, out _cmdOption);
          int audioLanguage;
          _dvdInfo.GetAudioLanguage(i, out audioLanguage);
          foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
          {
            if (ci.LCID == (audioLanguage & 0x3ff))
            {
              settings.AudioLanguage = ci.EnglishName;
              ServiceRegistration.Get<ISettingsManager>().Save(settings);
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
        string[] audioStreams = AudioStreams;
        return audioStreams[currentStream];
      }
    }
    
    #endregion

    #region Subtitle stream handling

    /// <summary>
    /// AddUnique adds a string to a list and avoids duplicates by adding a counting number.
    /// </summary>
    /// <param name="targetList">Target list</param>
    /// <param name="valueToAdd">String to add</param>
    private static void AddUnique(ICollection<string> targetList, String valueToAdd)
    {
      if (!targetList.Contains(valueToAdd))
        targetList.Add(valueToAdd);
      else
      {
        // Try a maximum of 2..5 numbers to append.
        for (int i = 2; i <= 5; i++)
        {
          String countedName = String.Format("{0} ({1})", valueToAdd, i);
          if (!targetList.Contains(countedName))
          {
            targetList.Add(countedName);
            return;
          }
        }
      }
    }

    public string[] Subtitles
    {
      get
      {
        // Accessing the list and count information can occure concurrently, this has to be avoided.
        lock (_guiPropertiesLock)
        {
          if (_subtitleStreams != null)
            return _subtitleStreams.ToArray();

          _subtitleStreams = new List<string>();
          if (_initialized && _dvdInfo != null)
          {
            int streamsAvailable;
            int currentStream;
            bool isDisabled;
            _dvdInfo.GetCurrentSubpicture(out streamsAvailable, out currentStream, out isDisabled);
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
                  String localizationTag = attr.LanguageExtension.ToString();
                  String currentSubtitle = String.Format("{0} {1}", ci.EnglishName,
                      ServiceRegistration.Get<ILocalization>().ToString(
                          "[Playback." + localizationTag + "]") ?? localizationTag
                    );
                  AddUnique(_subtitleStreams, currentSubtitle);
                  break;
                }
              }
            }
          }
          return _subtitleStreams.ToArray();
        }
      }
    }

    public void SetSubtitle(string subtitle)
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      string[] subtitles = Subtitles;
      for (int i = 0; i < subtitles.Length; ++i)
      {
        if (subtitles[i] == subtitle)
        {
          ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: Select subtitle:{0}", subtitle);
          _dvdCtrl.SelectSubpictureStream(i, DvdCmdFlags.None, out _cmdOption);
          _dvdCtrl.SetSubpictureState(true, DvdCmdFlags.None, out _cmdOption);
          int iLanguage;
          _dvdInfo.GetSubpictureLanguage(i, out iLanguage);
          foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
          {
            if (ci.LCID == (iLanguage & 0x3ff))
            {
              settings.SubtitleLanguage = ci.EnglishName;
              ServiceRegistration.Get<ISettingsManager>().Save(settings);
              break;
            }
          }
          return;
        }
      }
    }

    public void DisableSubtitle()
    {
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: Disable subtitles");
      _dvdCtrl.SetSubpictureState(false, DvdCmdFlags.None, out _cmdOption);
      settings.SubtitleLanguage = null;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    public string CurrentSubtitle
    {
      get
      {
        int streamsAvailable, currentStream;
        bool isDisabled;
        _dvdInfo.GetCurrentSubpicture(out streamsAvailable, out currentStream, out isDisabled);
        string[] subs = Subtitles;
        // Attention: currentStream can return invalid index, check if it's higher than the number of available subtitles
        if (isDisabled || currentStream >= subs.Length)
          return null;
        return subs[currentStream];
      }
    }

    #endregion

    #region DVD Titles handling

    /// <summary>
    /// Gets the available DVD titles.
    /// </summary>
    public string[] DvdTitles
    {
      get
      {
        // Attention: some DVDs are reporting the maximum of 99 titles, although having only one.
        DvdDiscSide side;
        int titleCount, numOfVolumes, volume;
        _dvdInfo.GetDVDVolumeInfo(out numOfVolumes, out volume, out side, out titleCount);
        string[] titles = new string[titleCount];
        for (int i = 0; i < titleCount; ++i)
        {
          titles[i] = String.Format("{0} {1}", ServiceRegistration.Get<ILocalization>().ToString("[Playback.Title]"), i + 1);
        }
        return titles;
      }
    }

    /// <summary>
    /// Sets the DVD title to play.
    /// </summary>
    /// <param name="title">DVD title</param>
    public void SetDvdTitle(string title)
    {
      string[] titles = DvdTitles;
      for (int i = 0; i < titles.Length; ++i)
      {
        if (title == titles[i])
        {
          _dvdCtrl.PlayTitle(1 + i, DvdCmdFlags.Flush, out _cmdOption);
          return;
        }
      }
    }

    /// <summary>
    /// Gets the current DVD title.
    /// </summary>
    public string CurrentDvdTitle
    {
      get
      {
        DvdPlaybackLocation2 location;
        _dvdInfo.GetCurrentLocation(out location);
        return String.Format("{0} {1}", ServiceRegistration.Get<ILocalization>().ToString("[Playback.Title]"), location.TitleNum);
      }
    }

    #endregion

    #region DVD Chapters handling

    /// <summary>
    /// Returns a localized chapter name.
    /// </summary>
    /// <param name="chapterNumber">0 based chapter</param>
    /// <returns>Chapter title</returns>
    private String ChapterName(Int32 chapterNumber)
    {
      //Idea: we could scrape chapter names and store them in MediaAspects. When they are available, return the full names here.
      return String.Format("{0} {1}", ServiceRegistration.Get<ILocalization>().ToString("[Playback.Chapter]"), chapterNumber);
    }

    /// <summary>
    /// Gets a list of available chapters.
    /// </summary>
    public string[] DvdChapters
    {
      get
      {
        lock (_guiPropertiesLock)
        {
          if (_chapters != null)
            return _chapters.ToArray();

          _chapters = new List<string>();
          if (_initialized && _dvdInfo != null)
          {
            int chapterCount;
            DvdPlaybackLocation2 location;
            _dvdInfo.GetCurrentLocation(out location);
            _dvdInfo.GetNumberOfChapters(location.TitleNum, out chapterCount);
            for (int i = 1; i <= chapterCount; ++i)
              _chapters.Add(ChapterName(i));
          }
        }
        return _chapters.ToArray();
      }
    }

    /// <summary>
    /// Sets the chapter to play.
    /// </summary>
    /// <param name="chapter">Chapter name</param>
    public void SetDvdChapter(string chapter)
    {
      string[] chapters = DvdChapters;
      for (int i = 0; i < chapters.Length; ++i)
      {
        if (chapter == chapters[i])
        {
          _dvdCtrl.PlayChapter(i + 1, DvdCmdFlags.Flush, out _cmdOption);
          return;
        }
      }
    }

    /// <summary>
    /// Gets the current chapter.
    /// </summary>
    public string CurrentDvdChapter
    {
      get
      {
        DvdPlaybackLocation2 location;
        _dvdInfo.GetCurrentLocation(out location);
        return ChapterName(location.ChapterNum);
      }
    }

    /// <summary>
    /// Indicate if chapters are available.
    /// </summary>
    public bool ChaptersAvailable
    {
      get { return DvdChapters.Length!= 0; }
    }

    /// <summary>
    /// Skip to next chapter.
    /// </summary>
    public void NextChapter()
    {
      IDvdCmd cmd;
      _dvdCtrl.PlayNextChapter(DvdCmdFlags.None, out cmd);
    }

    /// <summary>
    /// Skip to previous chapter.
    /// </summary>
    public void PrevChapter()
    {
      IDvdCmd cmd;
      _dvdCtrl.PlayPrevChapter(DvdCmdFlags.None, out cmd);
    }

    #endregion

    #region Time handling format conversions

    /// <summary>
    /// Converts a TimeSpan into a DvdHMSFTimeCode.
    /// </summary>
    /// <param name="newTime">TimeSpan</param>
    /// <returns>DvdHMSFTimeCode</returns>
    private static DvdHMSFTimeCode ToTimeCode(TimeSpan newTime)
    {
      int hours = newTime.Hours;
      int minutes = newTime.Minutes;
      int seconds = newTime.Seconds;
      DvdHMSFTimeCode timeCode = new DvdHMSFTimeCode
      {
        bHours = (byte)(hours & 0xff),
        bMinutes = (byte)(minutes & 0xff),
        bSeconds = (byte)(seconds & 0xff),
        bFrames = 0
      };
      return timeCode;
    }

    /// <summary>
    /// Converts a Double time stamp to TimeSpan.
    /// </summary>
    /// <param name="timeStamp">time stamp</param>
    /// <returns>TimeSpan</returns>
    private static TimeSpan ToTimeSpan(Double timeStamp)
    {
      return new TimeSpan(0, 0, 0, 0, (int)(timeStamp * 1000.0f));
    }

    /// <summary>
    /// Converts a DvdHMSFTimeCode to Double time stamp.
    /// </summary>
    /// <param name="timeCode">DvdHMSFTimeCode</param>
    /// <returns>Double</returns>
    private static Double ToDouble(DvdHMSFTimeCode timeCode)
    {
      Double result = timeCode.bHours * 3600d;
      result += (timeCode.bMinutes * 60d);
      result += timeCode.bSeconds;
      return result;
    }

    /// <summary>
    /// Convertes time and updates _currentTime.
    /// </summary>
    /// <param name="p1"></param>
    private void GetCurrentTime(IntPtr p1)
    {
      byte[] ati = BitConverter.GetBytes(p1.ToInt32());
      _currTime.bHours = ati[0];
      _currTime.bMinutes = ati[1];
      _currTime.bSeconds = ati[2];
      _currTime.bFrames = ati[3];
      _currentTime = ToDouble(_currTime);
    }

    /// <summary>
    /// Calculates the duration.
    /// </summary>
    private void CalculateDuration()
    {
      DvdHMSFTimeCode totaltime = new DvdHMSFTimeCode();
      DvdTimeCodeFlags ulTimeCodeFlags;
      _dvdInfo.GetTotalTitleTime(totaltime, out ulTimeCodeFlags);
      _duration = ToDouble(totaltime);
    }


    /// <summary>
    /// Returns the current play time.
    /// </summary>
    /// <value></value>
    public override TimeSpan CurrentTime
    {
      get { return ToTimeSpan(_currentTime); }
      set
      {
        ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: seek {0} / {1}", value.TotalSeconds, Duration.TotalSeconds);
        TimeSpan newTime = value;
        if (newTime.TotalSeconds < 0)
        {
          newTime = new TimeSpan();
        }
        if (newTime < Duration)
        {
          DvdHMSFTimeCode timeCode = ToTimeCode(newTime);
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

    /// <summary>
    /// Gets the duration of DVD title.
    /// </summary>
    public override TimeSpan Duration
    {
      get { return ToTimeSpan(_duration); }
    }

    /// <summary>
    /// Indicates if DVD menu is active.
    /// </summary>
    public bool InDvdMenu
    {
      get { return (_menuOn); }
    }

    #endregion
  }
}
