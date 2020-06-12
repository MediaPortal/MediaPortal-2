using Emulators.LibRetro.Controllers;
using Emulators.LibRetro.Controllers.Mapping;
using Emulators.LibRetro.GLContexts;
using Emulators.LibRetro.Render;
using Emulators.LibRetro.Settings;
using Emulators.LibRetro.SoundProviders;
using Emulators.LibRetro.VideoProviders;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX.Direct3D9;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.IO;

namespace Emulators.LibRetro
{
  public class LibRetroFrontend : IDisposable
  {
    #region ILogger
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
    #endregion

    #region Protected Members
    protected readonly object _surfaceLock = new object();
    protected readonly object _audioLock = new object();

    protected LibRetroSettings _settings;
    protected LibRetroThread _retroThread;
    protected LibRetroEmulator _retroEmulator;
    protected LibRetroSaveStateHandler _saveHandler;
    protected RetroGLContext _glContext;
    protected ITextureProvider _textureProvider;
    protected ISoundOutput _soundOutput;
    protected ControllerWrapper _controllerWrapper;
    protected SynchronizationStrategy _synchronizationStrategy;    
    protected RenderDlgt _renderDlgt;    
    protected bool _guiInitialized;
    protected bool _isPaused;
    protected bool _hasAudio;

    protected string _corePath;
    protected string _gamePath;
    protected string _saveName;
    protected bool _autoSave;
    #endregion

    #region Ctor
    public LibRetroFrontend(string corePath, string gamePath, string saveName)
    {
      _corePath = corePath;
      _gamePath = gamePath;
      _saveName = saveName;
      _guiInitialized = true;
    }
    #endregion

    #region Public Properties
    public object SurfaceLock
    {
      get { return _surfaceLock; }
    }

    public bool Paused
    {
      get { return _isPaused; }
    }

    public Texture Texture
    {
      get
      {
        lock (_surfaceLock)
        {
          return _textureProvider != null ? _textureProvider.Texture : null;
        }
      }
    }

    public VideoInfo VideoInfo
    {
      get
      {
        LibRetroEmulator emulator = _retroEmulator;
        return emulator != null ? emulator.VideoInfo : null;
      }
    }
    #endregion

    #region Public Methods
    public bool Init()
    {
      _settings = ServiceRegistration.Get<ISettingsManager>().Load<LibRetroSettings>();
      InitializeControllerWrapper(); 
      _retroThread = new LibRetroThread();
      _retroThread.Initializing += RetroThreadInitializing;
      _retroThread.Started += RetroThreadStarted;
      _retroThread.Running += RetroThreadRunning;
      _retroThread.Finishing += RetroThreadFinishing;
      _retroThread.Finished += RetroThreadFinished;
      _retroThread.Paused += RetroThreadPaused;
      _retroThread.UnPaused += RetroThreadUnPaused;
      return _retroThread.Init();
    }

    public void Run()
    {
      if (_retroThread == null || !_retroThread.IsInit)
        return;
      _retroThread.Run();
    }

    public void Pause()
    {
      if (_isPaused)
        return;
      _isPaused = true;
      if (_retroThread != null)
        _retroThread.Pause();
    }

    public void Unpause()
    {
      if (!_isPaused)
        return;
      _isPaused = false;
      if (_retroThread != null)
        _retroThread.UnPause();
    }

    public void SetVolume(int volume)
    {
      lock (_audioLock)
        if (_soundOutput != null)
          _soundOutput.SetVolume(volume);
    }

    public bool SetRenderDelegate(RenderDlgt dlgt)
    {
      _renderDlgt = dlgt;
      return true;
    }

    public void ReallocGUIResources()
    {
      lock (_surfaceLock)
        _guiInitialized = true;
      _synchronizationStrategy.Update();
    }

    public void ReleaseGUIResources()
    {
      lock (_surfaceLock)
      {
        _guiInitialized = false;
        if (_textureProvider != null)
          _textureProvider.Release();
      }
    }

    public void SetStateIndex(int index)
    {
      if (_saveHandler != null)
        _saveHandler.StateIndex = index;
    }

    public void LoadState()
    {
      if (_retroThread != null && _saveHandler != null)
        _retroThread.EnqueueAction(_saveHandler.LoadState);
    }

    public void SaveState()
    {
      if (_retroThread != null && _saveHandler != null)
        _retroThread.EnqueueAction(_saveHandler.SaveState);
    }

    #endregion

    #region Init
    protected void InitializeAll()
    {
      try
      {
        InitializeLibRetro();
        if (!LoadGame())
          return;
        InitializeVideo();
        InitializeAudio();
        InitializeSaveStateHandler();
        _synchronizationStrategy = new SynchronizationStrategy(_retroEmulator.TimingInfo.FPS, _settings.SynchronizationType);
        _retroThread.IsInit = true;
      }
      catch (Exception ex)
      {
        Logger.Error("LibRetroFrontend: Error initialising Libretro core", ex);
        if (_retroEmulator != null)
        {
          _retroEmulator.Dispose();
          _retroEmulator = null;
        }
      }
    }

    protected void InitializeLibRetro()
    {
      _glContext = new RetroGLContext();
      _retroEmulator = new LibRetroEmulator(_corePath)
      {
        SaveDirectory = _settings.SavesDirectory,
        SystemDirectory = _settings.SystemDirectory,
        LogDelegate = RetroLogDlgt,
        Controller = _controllerWrapper,
        GLContext = _glContext
      };

      SetCoreVariables();
      _retroEmulator.VideoReady += OnVideoReady;
      _retroEmulator.FrameBufferReady += OnFrameBufferReady;
      _retroEmulator.AudioReady += OnAudioReady;
      _retroEmulator.AVInfoUpdated += OnAVInfoUpdated;
      _retroEmulator.Init();
      Logger.Debug("LibRetroFrontend: Libretro initialized");
    }

    protected void InitializeVideo()
    {
      lock (_surfaceLock)
        _textureProvider = new LibRetroTextureWrapper();
      Logger.Debug("LibRetroFrontend: Video initialized");
    }

    protected void InitializeAudio()
    {
      lock (_audioLock)
      {
        _soundOutput = new LibRetroDirectSound();
        if (!_soundOutput.Init(SkinContext.Form.Handle, _settings.AudioDeviceId, (int)_retroEmulator.TimingInfo.SampleRate, _settings.AudioBufferSize))
        {
          _soundOutput.Dispose();
          _soundOutput = null;
          return;
        }
      }
      Logger.Debug("LibRetroFrontend: Audio initialized");
    }

    protected void InitializeSaveStateHandler()
    {
      _autoSave = _settings.AutoSave;
      _saveHandler = new LibRetroSaveStateHandler(_retroEmulator, _saveName, _settings.SavesDirectory, _settings.AutoSaveInterval);
      _saveHandler.LoadSaveRam();
      Logger.Debug("LibRetroFrontend: Save handler Initialized");
    }

    protected void SetCoreVariables()
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      CoreSetting coreSetting;
      if (!sm.Load<LibRetroCoreSettings>().TryGetCoreSetting(_corePath, out coreSetting) || coreSetting.Variables == null)
        return;
      foreach (VariableDescription variable in coreSetting.Variables)
        _retroEmulator.Variables.AddOrUpdate(variable);
    }

    protected void InitializeControllerWrapper()
    {
      _controllerWrapper = new ControllerWrapper(_settings.MaxPlayers);
      DeviceProxy deviceProxy = new DeviceProxy();
      List<IMappableDevice> deviceList = deviceProxy.GetDevices(false);
      MappingProxy mappingProxy = new MappingProxy();

      foreach (PortMapping port in mappingProxy.PortMappings)
      {
        IMappableDevice device = deviceProxy.GetDevice(port.DeviceId, port.SubDeviceId, deviceList);
        if (device != null)
        {
          RetroPadMapping mapping = mappingProxy.GetDeviceMapping(device);
          device.Map(mapping);
          _controllerWrapper.AddController(device, port.Port);
          Logger.Debug("LibRetroFrontend: Mapped controller {0} to port {1}", device.DeviceName, port.Port);
        }
      }
    }

    protected bool LoadGame()
    {
      bool result;
      //A core can support running without a game as well as with a game.
      //There is currently no way to check which case is needed as we currently use a dummy game
      //to import/load standalone cores.
      //Checking ValidExtensions is currently a hack which works better than nothing
      if (_retroEmulator.SupportsNoGame && _retroEmulator.SystemInfo.ValidExtensions == null)
      {
        Logger.Debug("LibRetroFrontend: Loading no game");
        result = _retroEmulator.LoadGame(new retro_game_info());
      }
      else
      {
        Logger.Debug("LibRetroFrontend: Loading game '{0}', NeedsFullPath: {1}", _gamePath, _retroEmulator.SystemInfo.NeedsFullPath);
        byte[] gameData = _retroEmulator.SystemInfo.NeedsFullPath ? null : File.ReadAllBytes(_gamePath);
        result = _retroEmulator.LoadGame(_gamePath, gameData);
      }
      Logger.Debug("LibRetroFrontend: Load game {0}", result ? "succeeded" : "failed");
      return result;
    }
    #endregion

    #region Retro Thread
    private void RetroThreadInitializing(object sender, EventArgs e)
    {
      InitializeAll();
    }

    private void RetroThreadStarted(object sender, EventArgs e)
    {
      _controllerWrapper.Start();
      lock (_audioLock)
        if (_soundOutput != null)
          _soundOutput.Play();
      Logger.Debug("LibRetroFrontend: Libretro thread running");
    }

    private void RetroThreadRunning(object sender, EventArgs e)
    {
      RunEmulator();
      RenderFrame();
    }

    protected void RunEmulator()
    {
      //reset this every frame in case we get a frame without audio
      _hasAudio = false;
      _retroEmulator.Run();
      if (_autoSave)
        _saveHandler.AutoSave();
    }

    protected void RenderFrame()
    {
      RenderDlgt dlgt = _renderDlgt;
      bool wait = _synchronizationStrategy.SyncToAudio ? !_hasAudio : dlgt == null;
      if (wait)
        _synchronizationStrategy.Synchronize();
      if (dlgt != null)
        dlgt();
    }

    private void RetroThreadFinishing(object sender, EventArgs e)
    {
      Logger.Debug("LibRetroFrontend: Libretro thread finishing");
      _saveHandler.SaveSaveRam();
      _retroEmulator.UnloadGame();
      _retroEmulator.DeInit();
    }

    private void RetroThreadFinished(object sender, EventArgs e)
    {
      _retroEmulator.Dispose();
      _retroEmulator = null;
      Logger.Debug("LibRetroFrontend: Libretro thread finished");
    }

    private void RetroThreadPaused(object sender, EventArgs e)
    {
      lock (_audioLock)
        if (_soundOutput != null)
          _soundOutput.Pause();
    }

    private void RetroThreadUnPaused(object sender, EventArgs e)
    {
      lock (_audioLock)
        if (_soundOutput != null)
          _soundOutput.UnPause();
    }
    #endregion

    #region Audio/Video Output
    protected void OnVideoReady(object sender, EventArgs e)
    {
      lock (_surfaceLock)
      {
        if (_guiInitialized)
          _textureProvider.UpdateTexture(_retroEmulator.VideoBuffer, _retroEmulator.VideoInfo.Width, _retroEmulator.VideoInfo.Height, false);
      }
    }

    protected void OnFrameBufferReady(object sender, EventArgs e)
    {
      int width = _retroEmulator.VideoInfo.Width;
      int height = _retroEmulator.VideoInfo.Height;

      if (_glContext.HasDXContext)
      {
        lock (_surfaceLock)
        {
          if (_guiInitialized)
          {
            _glContext.UpdateCurrentTexture(width, height);
            _textureProvider.UpdateTexture(_glContext.Texture, width, height, _glContext.BottomLeftOrigin);
          }
        }
      }
      else
      {
        byte[] pixels = _glContext.ReadPixels(width, height);
        lock (_surfaceLock)
        {
          if (_guiInitialized)
            _textureProvider.UpdateTexture(pixels, width, height, _glContext.BottomLeftOrigin);
        }
      }
    }

    protected void OnAudioReady(object sender, EventArgs e)
    {
      lock (_audioLock)
      {
        if (_soundOutput != null)
        {
          _soundOutput.WriteSamples(_retroEmulator.AudioBuffer.Data, _retroEmulator.AudioBuffer.Length, _synchronizationStrategy.SyncToAudio);
          _hasAudio = true;
        }
      }
    }

    protected void OnAVInfoUpdated(object sender, EventArgs e)
    {
      Logger.Debug("LibRetroFrontend: AV info updated, reinitializing");
      lock(_audioLock)
      {
        if (_soundOutput != null)
          _soundOutput.Dispose();
        InitializeAudio();
        if (_soundOutput != null)
          _soundOutput.Play();
      }
    }
    #endregion

    #region LibRetro Logging
    protected void RetroLogDlgt(RETRO_LOG_LEVEL level, string message)
    {
      string format = "LibRetro: {0}";
      switch (level)
      {
        case RETRO_LOG_LEVEL.INFO:
          Logger.Info(format, message);
          break;
        case RETRO_LOG_LEVEL.DEBUG:
          Logger.Debug(format, message);
          break;
        case RETRO_LOG_LEVEL.WARN:
          Logger.Warn(format, message);
          break;
        case RETRO_LOG_LEVEL.ERROR:
          Logger.Error(format, message);
          break;
        default:
          Logger.Debug(format, message);
          break;
      }
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
      if (_retroThread != null)
      {
        _retroThread.Dispose();
        _retroThread = null;
      }
      _glContext = null;
      if (_controllerWrapper != null)
      {
        _controllerWrapper.Dispose();
        _controllerWrapper = null;
      }
      if (_textureProvider != null)
      {
        _textureProvider.Dispose();
        _textureProvider = null;
      }
      if (_soundOutput != null)
      {
        _soundOutput.Dispose();
        _soundOutput = null;
      }
      if (_synchronizationStrategy != null)
      {
        _synchronizationStrategy.Stop();
        _synchronizationStrategy = null;
      }
    }
    #endregion
  }
}