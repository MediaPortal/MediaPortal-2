using MediaPortal.Common;
using MediaPortal.Common.Logging;
using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro
{
  public class LibRetroSaveStateHandler
  {
    protected const string SAVE_RAM_EXTENSION = ".srm";
    protected const string STATE_EXTENSION = ".state";
    protected int _autoSaveInterval;
    protected LibRetroEmulator _retroEmulator;
    protected string _saveName;
    protected string _saveDirectory;
    protected DateTime _lastSaveTime = DateTime.MinValue;
    protected byte[] _currentSaveRam;
    protected byte[] _lastSaveRam;
    protected int _stateIndex;

    public LibRetroSaveStateHandler(LibRetroEmulator retroEmulator, string saveName, string saveDirectory, int autoSaveInterval)
    {
      _retroEmulator = retroEmulator;
      _saveName = saveName;
      _saveDirectory = saveDirectory;
      _autoSaveInterval = autoSaveInterval;
    }

    public int StateIndex
    {
      get { return _stateIndex; }
      set { _stateIndex = value; }
    }

    public void LoadSaveRam()
    {
      string saveFile = GetSaveFile(SAVE_RAM_EXTENSION);
      byte[] saveRam;
      if (TryReadFromFile(saveFile, out saveRam))
      {
        _retroEmulator.SetMemoryData(RETRO_MEMORY.SAVE_RAM, saveRam);
        _lastSaveRam = saveRam;
      }
    }

    public void SaveSaveRam()
    {
      byte[] saveRam = _retroEmulator.GetMemoryData(RETRO_MEMORY.SAVE_RAM);
      if (saveRam == null)
        return;
      TryWriteToFile(GetSaveFile(SAVE_RAM_EXTENSION), saveRam);
    }

    public void LoadState()
    {
      string stateFile = GetSaveFile(STATE_EXTENSION);
      byte[] state;
      if (TryReadFromFile(stateFile, out state))
        _retroEmulator.Unserialize(state);
    }

    public void SaveState()
    {
      byte[] state = _retroEmulator.Serialize();
      if (state != null)
        TryWriteToFile(GetSaveFile(STATE_EXTENSION), state);
    }

    public void AutoSave()
    {
      DateTime now = DateTime.Now;
      if ((now - _lastSaveTime).TotalSeconds < _autoSaveInterval)
        return;
      _lastSaveTime = now;
      CheckSaveBuffer();
      if (!_retroEmulator.GetMemoryData(RETRO_MEMORY.SAVE_RAM, _currentSaveRam) || !ShouldSave(_lastSaveRam, _currentSaveRam))
        return;
      string savePath = GetSaveFile(SAVE_RAM_EXTENSION);
      if (TryWriteToFile(savePath, _currentSaveRam))
      {
        ServiceRegistration.Get<ILogger>().Debug("LibRetroSaveStateHandler: Auto saved to '{0}'", GetSaveFile(SAVE_RAM_EXTENSION));
        SwitchBuffers();
      }
    }

    protected void CheckSaveBuffer()
    {
      int size = _retroEmulator.GetMemorySize(RETRO_MEMORY.SAVE_RAM);
      if (_currentSaveRam == null || _currentSaveRam.Length < size)
        _currentSaveRam = new byte[size];
    }

    protected void SwitchBuffers()
    {
      byte[] dummy = _lastSaveRam;
      _lastSaveRam = _currentSaveRam;
      _currentSaveRam = dummy;
    }

    protected bool TryReadFromFile(string path, out byte[] fileBytes)
    {
      try
      {
        if (File.Exists(path))
        {
          fileBytes = File.ReadAllBytes(path);
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("LibRetroSaveStateHandler: Error reading from path '{0}':", ex, path);
      }
      fileBytes = null;
      return false;
    }

    protected bool TryWriteToFile(string path, byte[] fileBytes)
    {
      try
      {
        DirectoryInfo directory = new DirectoryInfo(Path.GetDirectoryName(path));
        if (!directory.Exists)
          directory.Create();
        File.WriteAllBytes(path, fileBytes);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("LibRetroSaveStateHandler: Error writing to path '{0}':", ex, path);
      }
      return false;
    }

    protected string GetSaveFile(string extension)
    {
      string indexString = _stateIndex > 0 ? _stateIndex.ToString() : string.Empty;
      return Path.Combine(_saveDirectory, _saveName + extension + indexString);
    }

    protected static bool ShouldSave(byte[] original, byte[] updated)
    {
      if (updated == null || updated.Length == 0)
        return false;
      if (original == null || original.Length == 0)
        return true;
      if (original.Length != updated.Length)
        return true;
      for (int i = 0; i < original.Length; i++)
        if (original[i] != updated[i])
          return true;
      return false;
    }
  }
}
