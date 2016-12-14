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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using DirectShow;
using DirectShow.Dvd;
using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.General;
using MediaPortal.UI.Players.Video.Interfaces;
using MediaPortal.UI.Players.Video.Settings;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Players.ResumeState;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UI.Players.Video
{
  public class DvdPlayer : VideoPlayer, IDVDPlayer
  {
    #region Constants

    protected const int WM_DVD_EVENT = 0x00008002; // message from dvd graph
    protected const int WS_CHILD = 0x40000000; // attributes for video window
    protected const int WS_CLIPCHILDREN = 0x02000000;
    protected const int WS_CLIPSIBLINGS = 0x04000000;
    protected const int WM_MOUSEMOVE = 0x0200;
    protected const int WM_LBUTTONUP = 0x0202;

    public const string RES_PLAYBACK_TITLE = "[Playback.Title]";
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

    protected DvdHMSFTimeCode _currTime; // copy of current playback states, see OnDvdEvent()
    protected string[] _titles;
    protected int _currTitle = 0;
    protected int _currChapter = 0;
    protected int _buttonCount = 0;
    protected int _focusedButton = 0;
    protected DvdDomain _currDomain;

    protected bool _handlesInput = false;
    protected ValidUOPFlag _UOPs;
    protected double _duration;
    protected double _currentTime = 0;

    private const string DVD_NAVIGATOR = "DVD Navigator";

    protected DvdPreferredDisplayMode _videoPref = DvdPreferredDisplayMode.DisplayContentDefault;
    protected AspectRatioMode _arMode = AspectRatioMode.Stretched;
    protected DvdVideoAttributes _videoAttr;

    private readonly object _dvdStopSyncObj = new object();
    private bool _stopping = false;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a DVDPlayer player object.
    /// </summary>
    public DvdPlayer()
    {
      PlayerTitle = "DVDPlayer"; // for logging
    }

    #endregion

    #region Graphbuilding overrides

    protected override void CreateGraphBuilder()
    {
      _dvdGraph = (IDvdGraphBuilder) new DvdGraphBuilder();
      new HRESULT(_dvdGraph.GetFiltergraph(out _graphBuilder)).Throw();
      _streamCount = 3; // Allow Video, CC, and Subtitle
    }

    protected override void CreateResourceAccessor()
    {
      // DvdPlayer needs an ILocalFSResourceAccessor
      ILocalFsResourceAccessor lfsra;
      if(!_resourceLocator.TryCreateLocalFsAccessor(out lfsra))
        throw new IllegalCallException("The DVDPlayer can only play local file system resources");
      _resourceAccessor = lfsra;
    }

    /// <summary>
    /// Adds the DVDNavigator filter to the graph and sets the input path.
    /// </summary>
    protected override void AddSourceFilter()
    {
      ServiceRegistration.Get<ILogger>().Debug("DvdPlayer.AddSourceFilter");
      _pendingCmd = true;

      _dvdbasefilter = (IBaseFilter) new DVDNavigator();

      if (_dvdbasefilter == null)
        throw new Exception("Failed to add DVD Navigator!");

      _graphBuilder.AddFilter(_dvdbasefilter, DVD_NAVIGATOR);

      _dvdInfo = _dvdbasefilter as IDvdInfo2;
      if (_dvdInfo == null)
        throw new Exception("Failed to get IDvdInfo2 from DVDNavigator!");

      _dvdCtrl = _dvdbasefilter as IDvdControl2;

      if (_dvdCtrl == null)
        throw new Exception("Failed to get IDvdControl2 from DVDNavigator!");

      if (!IsLocalFilesystemResource)
        throw new IllegalCallException("The DVDPlayer can only play local file system resources");
      using (((ILocalFsResourceAccessor)_resourceAccessor).EnsureLocalFileSystemAccess())
      {
        string path = ((ILocalFsResourceAccessor)_resourceAccessor).LocalFileSystemPath;

        // check if path is a drive root (like D:), otherwise append VIDEO_TS 
        // MediaItem always contains the parent folder. Add the required VIDEO_TS subfolder.
        if (!String.IsNullOrEmpty(path) && !path.EndsWith(Path.VolumeSeparatorChar.ToString()))
          path = Path.Combine(path, "VIDEO_TS");

        int hr = _dvdCtrl.SetDVDDirectory(path);
        if (hr != 0)
          throw new Exception("Failed to set DVD directory!");
      }
      _dvdCtrl.SetOption(DvdOptionFlag.HMSFTimeCodeEvents, true); // use new HMSF timecode format
      _dvdCtrl.SetOption(DvdOptionFlag.ResetOnStop, false);

      _mediaEvt = _graphBuilder as IMediaEventEx;
      if (_mediaEvt != null)
      {
        IScreenControl screenControl = ServiceRegistration.Get<IScreenControl>();
        _mediaEvt.SetNotifyWindow(screenControl.MainWindowHandle, WM_DVD_EVENT, _instancePtr);
      }

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
      _currTime = new DvdHMSFTimeCode();
    }

    protected override void OnGraphRunning()
    {
      base.OnGraphRunning();
      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();

      int hr = _dvdCtrl.SelectVideoModePreference(_videoPref);
      if (hr != 0)
        ServiceRegistration.Get<ILogger>().Error("DVDPlayer: Unable to set DVD video mode preference 0x{0:X}", hr);

      hr = _dvdInfo.GetCurrentVideoAttributes(out _videoAttr);
      if (hr != 0)
        ServiceRegistration.Get<ILogger>().Error("DVDPlayer: Unable to get DVD video attributes 0x{0:X}", hr);

      DvdDiscSide side;
      int titles, numOfVolumes, volume;
      hr = _dvdInfo.GetDVDVolumeInfo(out numOfVolumes, out volume, out side, out titles);
      if (hr < 0)
        ServiceRegistration.Get<ILogger>().Error("DVDPlayer: Unable to get DVD volume info 0x{0:X}", hr);

      if (titles <= 0)
        ServiceRegistration.Get<ILogger>().Error("DVDPlayer: DVD does not contain any titles? (# titles = {0})", titles);

      _pendingCmd = false;

      _dvdCtrl.SetSubpictureState(settings.EnableSubtitles, DvdCmdFlags.None, out _cmdOption);

      _line21Decoder = FilterGraphTools.FindFilterByInterface<IAMLine21Decoder>(_graphBuilder);
      if (_line21Decoder != null)
      {
        AMLine21CCState state = settings.EnableClosedCaption ? AMLine21CCState.On : AMLine21CCState.Off;
        if (_line21Decoder.SetServiceState(state) == 0)
          ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: {0} Closed Captions", settings.EnableClosedCaption ? "Enabled" : "Disabled");
        else
          ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: Failed to set Closed Captions state.");
      }
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
      int setError = _dvdCtrl.SelectDefaultAudioLanguage(settings.PreferredAudioLanguage, DvdAudioLangExt.NotSpecified);
      string errorText = GetErrorText(setError);
      ServiceRegistration.Get<ILogger>().Info("DVDPlayer: Set default language to: {0}. {1}", settings.PreferredAudioLanguage, errorText);

      setError = _dvdCtrl.SelectDefaultMenuLanguage(settings.PreferredMenuLanguage);
      errorText = GetErrorText(setError);
      ServiceRegistration.Get<ILogger>().Info("DVDPlayer: Set default menu language to:{0}. {1}", settings.PreferredMenuLanguage, errorText);

      setError = _dvdCtrl.SelectDefaultSubpictureLanguage(settings.PreferredSubtitleLanguage, DvdSubPictureLangExt.NotSpecified);
      errorText = GetErrorText(setError);
      ServiceRegistration.Get<ILogger>().Info("DVDPlayer: Set default subtitle language:{0}. {1}", settings.PreferredSubtitleLanguage, errorText);

      _dvdCtrl.SetSubpictureState(settings.EnableSubtitles, DvdCmdFlags.None, out _cmdOption);
    }

    /// <summary>
    /// Gets a DVD navigator related error text for passed error code.
    /// </summary>
    /// <param name="errorCode">Error code returned by any method of the DVD control.</param>
    /// <returns>Error message.</returns>
    private static string GetErrorText(int errorCode)
    {
      string result;
      switch (errorCode)
      {
        case 0:
          result = "Success.";
          break;
        case 631:
          result = "The DVD Navigator filter is not in a valid domain.";
          break;
        default:
          result = "Unknown Error: " + errorCode;
          break;
      }
      return result;
    }

    /// <summary>
    /// Gets the current DVD playback state as byte array. The resume state can be set later by calling
    /// <see cref="SetResumeState(byte[])"/> with the returned byte array.
    /// </summary>
    /// <param name="resumeData">The resume data.</param>
    /// <returns><c>true</c>, if the resume state could successfully retrieved. In that case <paramref name="resumeData"/>
    /// contains the resume data. <c>false</c>, if the resume state could not be retrieved. In that case,
    /// <paramref name="resumeData"/> is <c>null</c>.</returns>
    private bool GetResumeState(out byte[] resumeData)
    {
      byte[] result = null;
      try
      {
        IDvdState dvdState;
        int hr = _dvdInfo.GetState(out dvdState);
        if (hr != 0)
          return false;

        IPersistMemory dvdStatePersistMemory = (IPersistMemory) dvdState;
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
        IntPtr stateData = Marshal.AllocCoTaskMem((int) resumeSize);

        try
        {
          dvdStatePersistMemory.Save(stateData, true, resumeSize);
          result = new byte[resumeSize];
          Marshal.Copy(stateData, result, 0, (int) resumeSize);
        }
        catch
        {
          return false;
        }

        Marshal.FreeCoTaskMem(stateData);
        Marshal.ReleaseComObject(dvdStatePersistMemory);
        Marshal.ReleaseComObject(dvdState);
      }
      catch
      {
        return false;
      }
      finally
      {
        resumeData = result;
      }
      return true;
    }

    /// <summary>
    /// Restores the DVD playback state from a formerly saved resume state.
    /// </summary>
    /// <param name="resumeData">The resume data which was retrieved by calling <see cref="GetResumeState(out byte[])"/>.</param>
    private void SetResumeState(byte[] resumeData)
    {
      if ((resumeData != null) && (resumeData.Length > 0))
      {
        IDvdState dvdState;

        int hr = _dvdInfo.GetState(out dvdState);
        if (hr < 0)
          return;
        IPersistMemory dvdStatePersistMemory = (IPersistMemory) dvdState;
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
    }

    /// <summary>
    /// Gets a unique file name for the current DVD which contains the disc's ID.
    /// </summary>
    protected string GetResumeFilename()
    {
      long discId = 0;

      IBaseFilter dvdbasefilter = (IBaseFilter) new DVDNavigator();
      IDvdInfo2 dvdInfo = dvdbasefilter as IDvdInfo2;

      StringBuilder path = new StringBuilder(1024);
      if (dvdInfo != null)
      {
        int actualSize;
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
    /// Stops the playback and the input handling.
    /// </summary>
    public override void Stop()
    {
      lock (_dvdStopSyncObj)
      {
        if (_stopping == false)
          _stopping = true;
        else
          return;
      }

      ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: Stop");
      if (_dvdCtrl != null)
        _dvdCtrl.Stop();

      base.Stop();
      
      lock(_dvdStopSyncObj)
      {
        _stopping = false;
      }
    }

    #endregion

    #region DVD menu handling

    public bool IsHandlingUserInput
    {
      get
      {
        return _handlesInput;
      }
    }

    public void ShowDvdMenu()
    {
      ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: ShowDvdMenu");
      _dvdCtrl.ShowMenu(DvdMenuId.Root, DvdCmdFlags.None, out _cmdOption);
    }

    public void OnMouseMove(float x, float y)
    {
      if (_dvdCtrl == null || !_handlesInput || _buttonCount <= 0)
        return;

      // Scale to video's size
      x *= _videoAttr.sourceResolutionX;
      y *= _videoAttr.sourceResolutionY;

      Point pt = new Point((int) x, (int) y);

      // Highlight the button at the current position, if it exists
      _dvdCtrl.SelectAtPosition(pt);
    }

    public void OnMouseClick(float x, float y)
    {
      if (_dvdCtrl == null || !_handlesInput || _buttonCount <= 0)
        return;

      // Scale to video's size
      x *= _videoAttr.sourceResolutionX;
      y *= _videoAttr.sourceResolutionY;

      Point pt = new Point((int) x, (int) y);

      // Activate ("click") the button at the current position, if it exists
      _dvdCtrl.ActivateAtPosition(pt);
    }

    public void OnKeyPress(Key key)
    {
      if (_dvdCtrl == null || !_handlesInput || _buttonCount <= 0)
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

    #region DVD Event handling

    /// <summary>
    /// Called to handle DVD event window messages.
    /// </summary>
    private void OnDvdEvent()
    {
      if (_mediaEvt == null || _dvdCtrl == null || _stopping)
        return;

      int p1, p2;
      try
      {
        int hr;
        do
        {
          bool needStop = false;

          IMediaEventEx eventEx = (IMediaEventEx) _graphBuilder;
          EventCode code;
          hr = eventEx.GetEvent(out code, out p1, out p2, 0);
          if (hr < 0)
            break;

          switch (code)
          {
            case EventCode.DvdPlaybackStopped:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer DvdPlaybackStopped event: {0:X} {1:X}", p1, p2);
              break;
            case EventCode.DvdError:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer DvdError event: {0:X} {1:X}", p1, p2);
              break;
            case EventCode.VMRReconnectionFailed:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer VMRReconnectionFailed event: {0:X} {1:X}", p1, p2);
              break;
            case EventCode.DvdWarning:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer DVD warning: {0} {1}", p1, p2);
              break;
            case EventCode.DvdSubPicictureStreamChange:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdSubPicture Changed to: {0} Enabled: {1}", p1, p2);
              break;
            case EventCode.DvdCurrentHmsfTime:
              SetCurrentTime(p1);
              break;

            case EventCode.DvdChapterStart:
              {
                ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdChaptStart: {0}", p1);
                lock (SyncObj)
                {
                  _currChapter = p1;
                  CalculateDuration();
                }
                ServiceRegistration.Get<ILogger>().Debug("  _duration: {0}", _currentTime);
                break;
              }

            case EventCode.DvdTitleChange:
              {
                OnTitleSelect(p1);
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
                ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdStillOn: {0}", p1);
                break;
              }

            case EventCode.DvdStillOff:
              {
                ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdStillOff: {0}", p1);
                break;
              }

            case EventCode.DvdButtonChange:
              {
                _buttonCount = p1;
                _focusedButton = p2;
                ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdButtonChange: buttons: {0}, focused: {1}", _buttonCount, _focusedButton);
                break;
              }

            case EventCode.DvdNoFpPgc:
              {
                ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdNoFpPgc: {0}", p1);
                hr = _dvdCtrl.PlayTitle(1, DvdCmdFlags.None, out _cmdOption);
                break;
              }

            case EventCode.DvdAudioStreamChange:
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdAudioStreamChange: {0}", p1);
              break;

            case EventCode.DvdValidUopsChange:
              _UOPs = (ValidUOPFlag) p1;
              ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: DvdValidUopsChange: {0}", _UOPs);
              break;

            case EventCode.DvdDomainChange:
              {
                _currDomain = (DvdDomain) p1;
                switch (_currDomain)
                {
                  case DvdDomain.FirstPlay:
                    ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: Domain=FirstPlay");
                    _handlesInput = false;
                    break;
                  case DvdDomain.Stop:
                    ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: Domain=Stop");
                    needStop = true;
                    break;
                  case DvdDomain.VideoManagerMenu:
                    ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: Domain=VideoManagerMenu (menu)");
                    _handlesInput = true;
                    break;
                  case DvdDomain.VideoTitleSetMenu:
                    ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: Domain=VideoTitleSetMenu (menu)");
                    _handlesInput = true;
                    break;
                  case DvdDomain.Title:
                    _handlesInput = false;
                    break;
                  default:
                    ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: Unhandled event DvdDomainChange: {0}", p1);
                    break;
                }
                break;
              }
          }
          eventEx.FreeEventParams(code, p1, p2);

          if(needStop)
          {
            Stop();
            break;
          }
        } while (hr == 0);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("DVDPlayer: Exception in OnDvdEvent()", ex);
      }
    }

    protected void OnTitleSelect(int title)
    {
      ServiceRegistration.Get<ILogger>().Debug("EVT: DvdTitleChange: {0}", title);
      lock (SyncObj)
      {
        _chapterNames = null; // Invalidate chapters - they are different per title
        _currTitle = title;
      }
      EnumerateChapters();
      EnumerateStreams(true);
      CalculateDuration();
      ServiceRegistration.Get<ILogger>().Debug("  _duration: {0}", _currentTime);
    }

    /// <summary>
    /// Called when an asynchronous DVD command is completed.
    /// </summary>
    /// <param name="p1">The command event handle.</param>
    private void OnCmdComplete(int p1)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("DVD OnCmdComplete");
        if (!_pendingCmd || _dvdInfo == null)
          return;

        IDvdCmd cmd;
        int hr = _dvdInfo.GetCmdFromEvent(p1, out cmd);
        if (hr != 0 || cmd == null)
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
        Message m = (Message) message.MessageData[WindowsMessaging.MESSAGE];
        if (m.Msg == WM_DVD_EVENT)
          OnDvdEvent();
      }
    }

    #endregion

    #region Audio stream handling

    protected override bool EnumerateStreams(bool forceRefresh)
    {
      if (_dvdInfo == null || !_initialized)
        return false;

      BaseStreamInfoHandler audioStreams;
      BaseStreamInfoHandler subtitleStreams;
      lock (SyncObj)
      {
        audioStreams = _streamInfoAudio;
        subtitleStreams = _streamInfoSubtitles;
      }

      if (forceRefresh || audioStreams == null || subtitleStreams == null)
      {
        audioStreams = new StreamInfoHandler();
        subtitleStreams = new StreamInfoHandler();

        int streamsAvailable, currentStream;
        _dvdInfo.GetCurrentAudio(out streamsAvailable, out currentStream);
        for (int i = 0; i < streamsAvailable; ++i)
        {
          int audioLanguage;
          DvdAudioAttributes attr;
          _dvdInfo.GetAudioLanguage(i, out audioLanguage);
          _dvdInfo.GetAudioAttributes(i, out attr);

          int currentLCID = (audioLanguage & 0x3ff);
          string languageName = GetLanguageName(currentLCID);

          StringBuilder currentAudio = new StringBuilder();
          currentAudio.AppendFormat("{0} ({1}/{2} ch/{3} KHz)", languageName, attr.AudioFormat, attr.bNumberOfChannels, (attr.dwFrequency / 1000));

          switch (attr.LanguageExtension)
          {
            case DvdAudioLangExt.NotSpecified:
            case DvdAudioLangExt.Captions:
              break;
            case DvdAudioLangExt.VisuallyImpaired:
            case DvdAudioLangExt.DirectorComments1:
            case DvdAudioLangExt.DirectorComments2:
              currentAudio.AppendFormat(" ({0})", ServiceRegistration.Get<ILocalization>().ToString("[Playback." + attr.LanguageExtension + "]"));
              break;
          }
          audioStreams.AddUnique(new StreamInfo(null, i, currentAudio.ToString(), currentLCID));
        }

        bool isDisabled;
        _dvdInfo.GetCurrentSubpicture(out streamsAvailable, out currentStream, out isDisabled);
        for (int i = 0; i < streamsAvailable; ++i)
        {
          DvdSubpictureAttributes attr;
          int iLanguage;
          _dvdInfo.GetSubpictureLanguage(i, out iLanguage);
          _dvdInfo.GetSubpictureAttributes(i, out attr);
          int currentLCID = (iLanguage & 0x3ff);
          string languageName = GetLanguageName(currentLCID);

          String localizationTag = attr.LanguageExtension.ToString();
          String currentSubtitle = String.Format("{0} {1}", languageName,
            ServiceRegistration.Get<ILocalization>().ToString("[Playback." + localizationTag + "]") ?? localizationTag);

          subtitleStreams.AddUnique(new StreamInfo(null, i, currentSubtitle, currentLCID));
        }

        lock (SyncObj)
        {
          _streamInfoAudio = audioStreams;
          _streamInfoSubtitles = subtitleStreams;
        }
        return true; // refreshed
      }
      return false;
    }

    /// <summary>
    /// Sets the current audio stream.
    /// </summary>
    /// <param name="audioStream">audio stream</param>
    public override void SetAudioStream(string audioStream)
    {
      BaseStreamInfoHandler audioStreams;
      lock (SyncObj)
        audioStreams = _streamInfoAudio;

      if (audioStreams == null)
        return;

      StreamInfo selectedAudio = audioStreams.FindStream(audioStream);
      if (selectedAudio == null)
        return;

      int audioLanguage;
      ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: Select audio stream: {0}", audioStream);
      _dvdCtrl.SelectAudioStream(selectedAudio.StreamIndex, DvdCmdFlags.None, out _cmdOption);
      _dvdInfo.GetAudioLanguage(selectedAudio.StreamIndex, out audioLanguage);

      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      settings.PreferredAudioLanguage = audioLanguage;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
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
        return (currentStream >= 0 && audioStreams.Length > currentStream) ? audioStreams[currentStream] : null;
      }
    }

    #endregion

    #region Subtitle stream handling

    /// <summary>
    /// Gets the DisplayName for the given LCID. If LCID is not valid, "unknown" is returned.
    /// </summary>
    /// <param name="lcid">Valid LCID.</param>
    /// <returns>DisplayName of the culture to the given <paramref name="lcid"/>.</returns>
    private static string GetLanguageName(int lcid)
    {
      if (lcid == 0)
        return "unknown";
      try
      {
        CultureInfo ci = new CultureInfo(lcid);
        return ci.DisplayName; // use localized name.
      }
      catch (Exception)
      {
        return "unknown";
      }
    }

    public override void SetSubtitle(string subtitle)
    {
      BaseStreamInfoHandler subtitleStreams;
      lock (SyncObj)
        subtitleStreams = _streamInfoSubtitles;

      if (subtitleStreams == null)
        return;

      StreamInfo selectedSubtitle = subtitleStreams.FindStream(subtitle);
      if (selectedSubtitle == null)
        return;

      int iLanguage;
      ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: Select subtitle: {0}", subtitle);
      _dvdCtrl.SelectSubpictureStream(selectedSubtitle.StreamIndex, DvdCmdFlags.None, out _cmdOption);
      _dvdCtrl.SetSubpictureState(true, DvdCmdFlags.None, out _cmdOption);
      _dvdInfo.GetSubpictureLanguage(selectedSubtitle.StreamIndex, out iLanguage);

      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      settings.PreferredSubtitleLanguage = iLanguage;
      settings.EnableSubtitles = true;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    public override void DisableSubtitle()
    {
      ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: Disable subtitles");
      _dvdCtrl.SetSubpictureState(false, DvdCmdFlags.None, out _cmdOption);

      VideoSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoSettings>();
      settings.EnableSubtitles = false;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);

    }

    public override string CurrentSubtitle
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

    #region DVD Titles handling (ITitlesPlayer members)

    /// <summary>
    /// Gets the available DVD titles.
    /// </summary>
    public override string[] Titles
    {
      get
      {
        if (_titles != null)
          return _titles;

        // Attention: some DVDs are reporting the maximum of 99 titles, although having only one.
        DvdDiscSide side;
        int titleCount, numOfVolumes, volume;
        _dvdInfo.GetDVDVolumeInfo(out numOfVolumes, out volume, out side, out titleCount);
        _titles = new string[titleCount];
        for (int i = 0; i < titleCount; ++i)
          _titles[i] = ServiceRegistration.Get<ILocalization>().ToString(RES_PLAYBACK_TITLE, i + 1);
        return _titles;
      }
    }

    /// <summary>
    /// Sets the DVD title to play.
    /// </summary>
    /// <param name="title">DVD title</param>
    public override void SetTitle(string title)
    {
      string[] titles = Titles;
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
    public override string CurrentTitle
    {
      get
      {
        DvdPlaybackLocation2 location;
        _dvdInfo.GetCurrentLocation(out location);
        return ServiceRegistration.Get<ILocalization>().ToString(RES_PLAYBACK_TITLE, location.TitleNum);
      }
    }

    #endregion

    #region DVD Chapters handling

    /// <summary>
    /// Enumerates available chapters. Needs to be executed after title changes.
    /// </summary>
    protected override void EnumerateChapters()
    {
      if (!_initialized || _dvdInfo == null)
        return;
      List<string> chapters = new List<string>();
      DvdPlaybackLocation2 location;
      int hr = _dvdInfo.GetCurrentLocation(out location);
      if (hr == 0)
      {
        int chapterCount;
        _dvdInfo.GetNumberOfChapters(location.TitleNum, out chapterCount);
        for (int i = 1; i <= chapterCount; ++i)
          chapters.Add(GetChapterName(i));
      }
      lock (SyncObj)
        _chapterNames = chapters.ToArray();
    }

    /// <summary>
    /// Sets the chapter to play.
    /// </summary>
    /// <param name="chapter">Chapter name</param>
    public override void SetChapter(string chapter)
    {
      string[] chapters = Chapters;
      for (int i = 0; i < chapters.Length; i++)
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
    public override string CurrentChapter
    {
      get
      {
        DvdPlaybackLocation2 location;
        _dvdInfo.GetCurrentLocation(out location);
        return GetChapterName(location.ChapterNum);
      }
    }

    /// <summary>
    /// Skip to next chapter.
    /// </summary>
    public override void NextChapter()
    {
      IDvdCmd cmd;
      _dvdCtrl.PlayNextChapter(DvdCmdFlags.None, out cmd);
    }

    /// <summary>
    /// Skip to previous chapter.
    /// </summary>
    public override void PrevChapter()
    {
      IDvdCmd cmd;
      _dvdCtrl.PlayPrevChapter(DvdCmdFlags.None, out cmd);
    }

    #endregion

    #region Time handling format conversions

    /// <summary>
    /// Converts a <see cref="TimeSpan"/> into a <see cref="DvdHMSFTimeCode"/>.
    /// </summary>
    /// <param name="newTime">TimeSpan to convert.</param>
    /// <returns><see cref="DvdHMSFTimeCode"/> instance.</returns>
    private static DvdHMSFTimeCode ToTimeCode(TimeSpan newTime)
    {
      int hours = newTime.Hours;
      int minutes = newTime.Minutes;
      int seconds = newTime.Seconds;
      DvdHMSFTimeCode timeCode = new DvdHMSFTimeCode
      {
        bHours = (byte) (hours & 0xff),
        bMinutes = (byte) (minutes & 0xff),
        bSeconds = (byte) (seconds & 0xff),
        bFrames = 0
      };
      return timeCode;
    }

    /// <summary>
    /// Converts a <see cref="double"/> time stamp to <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="timeStamp">Time stamp to convert.</param>
    /// <returns>TimeSpan instance.</returns>
    private static TimeSpan ToTimeSpan(double timeStamp)
    {
      return new TimeSpan(0, 0, 0, 0, (int) (timeStamp * 1000.0f));
    }

    /// <summary>
    /// Converts a <see cref="DvdHMSFTimeCode"/> to <see cref="double"/> time stamp.
    /// </summary>
    /// <param name="timeCode">DvdHMSFTimeCode</param>
    /// <returns>Double</returns>
    private static double ToDouble(DvdHMSFTimeCode timeCode)
    {
      Double result = timeCode.bHours * 3600d;
      result += (timeCode.bMinutes * 60d);
      result += timeCode.bSeconds;
      return result;
    }

    /// <summary>
    /// Convertes time and updates _currentTime.
    /// </summary>
    /// <param name="p1">Packed time code.</param>
    private void SetCurrentTime(int p1)
    {
      byte[] ati = BitConverter.GetBytes(p1);
      lock (SyncObj)
      {
        _currTime.bHours = ati[0];
        _currTime.bMinutes = ati[1];
        _currTime.bSeconds = ati[2];
        _currTime.bFrames = ati[3];
        _currentTime = ToDouble(_currTime);
      }
    }

    /// <summary>
    /// Calculates the duration.
    /// </summary>
    private void CalculateDuration()
    {
      DvdHMSFTimeCode totaltime = new DvdHMSFTimeCode();
      DvdTimeCodeFlags ulTimeCodeFlags;
      _dvdInfo.GetTotalTitleTime(totaltime, out ulTimeCodeFlags);
      lock (SyncObj)
        _duration = ToDouble(totaltime);
    }


    /// <summary>
    /// Returns the current play time.
    /// </summary>
    /// <value></value>
    public override TimeSpan CurrentTime
    {
      get
      {
        return ToTimeSpan(_currentTime);
      }
      set
      {
        ServiceRegistration.Get<ILogger>().Debug("DvdPlayer: seek {0} / {1}", value.TotalSeconds, Duration.TotalSeconds);
        TimeSpan newTime = value;
        if (newTime.TotalSeconds < 0)
          newTime = new TimeSpan();
        if (newTime > Duration)
          return;
        DvdHMSFTimeCode timeCode = ToTimeCode(newTime);
        DvdPlaybackLocation2 loc;
        int hr = _dvdInfo.GetCurrentLocation(out loc);
        if (hr == 0)
        {
          _currTitle = loc.TitleNum;
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
      get
      {
        lock (SyncObj)
          return ToTimeSpan(_duration);
      }
    }

    #endregion

    #region Implementation of IResumablePlayer

    /// <summary>
    /// Gets a <see cref="IResumeState"/> from the player.
    /// </summary>
    /// <param name="state">Outputs resume state.</param>
    /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
    public override bool GetResumeState(out IResumeState state)
    {
      byte[] data;
      if (GetResumeState(out data))
      {
        state = new BinaryResumeState { ResumeData = data };
        return true;
      }
      state = null;
      return false;
    }

    /// <summary>
    /// Sets a <see cref="IResumeState"/> to the player. The player is responsible to make the required initializations.
    /// </summary>
    /// <param name="state">Resume state.</param>
    /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
    public override bool SetResumeState(IResumeState state)
    {
      BinaryResumeState binaryResumeState = state as BinaryResumeState;
      if (binaryResumeState == null)
        return false;
      SetResumeState(binaryResumeState.ResumeData);
      return true;
    }

    #endregion
  }
}
