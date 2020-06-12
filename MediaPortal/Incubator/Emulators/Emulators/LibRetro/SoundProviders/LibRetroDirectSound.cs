using MediaPortal.Common;
using MediaPortal.Common.Logging;
using SharpDX.DirectSound;
using SharpDX.Multimedia;
using System;
using System.Threading;

namespace Emulators.LibRetro.SoundProviders
{
  public class LibRetroDirectSound : ISoundOutput
  {
    public const double DEFAULT_BUFFER_SIZE_SECONDS = 0.4;

    protected DirectSound _directSound;
    protected SecondarySoundBuffer _secondaryBuffer;
    protected int _bufferBytes;
    protected int _nextWrite;
    protected double _samplesPerMs;

    public bool Init(IntPtr windowHandle, Guid audioDeviceId, int sampleRate, double bufferSizeSeconds)
    {
      try
      {
        InitializeDirectSound(windowHandle, audioDeviceId);
        InitializeAudio(sampleRate, bufferSizeSeconds > 0 ? bufferSizeSeconds : DEFAULT_BUFFER_SIZE_SECONDS);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("LibRetroDirectSound: Failed to initialise device {0}", ex, audioDeviceId);
        return false;
      }
    }

    void InitializeDirectSound(IntPtr windowHandle, Guid audioDeviceId)
    {
      _directSound = new DirectSound(audioDeviceId);
      // Set the cooperative level to priority so the format of the primary sound buffer can be modified.
      _directSound.SetCooperativeLevel(windowHandle, CooperativeLevel.Priority);
    }

    protected void InitializeAudio(int sampleRate, double bufferSizeSeconds)
    {
      var format = new WaveFormat(sampleRate, 16, 2);
      var buffer = new SoundBufferDescription();
      buffer.Flags = BufferFlags.GlobalFocus | BufferFlags.ControlVolume;
      buffer.BufferBytes = (int)(format.AverageBytesPerSecond * bufferSizeSeconds);
      buffer.Format = format;
      buffer.AlgorithmFor3D = Guid.Empty;
      // Create a temporary sound buffer with the specific buffer settings.
      _secondaryBuffer = new SecondarySoundBuffer(_directSound, buffer);
      _bufferBytes = _secondaryBuffer.Capabilities.BufferBytes;
      _samplesPerMs = format.AverageBytesPerSecond / (sizeof(short) * 1000d);
    }
    
    public void WriteSamples(short[] samples, int count, bool synchronise)
    {
      if (count == 0)
        return;
      if (_secondaryBuffer.Status == (int)BufferStatus.BufferLost)
        _secondaryBuffer.Restore();
      //If synchronise wait until there is enough free space
      if (synchronise)
        Synchronize(count);
      int samplesNeeded = GetSamplesNeeded();
      if (samplesNeeded < 1)
        return;
      if (count > samplesNeeded)
        count = samplesNeeded;
      _secondaryBuffer.Write(samples, 0, count, _nextWrite, LockFlags.None);
      IncrementWritePosition(count * 2);
    }

    public bool Play()
    {
      try
      {
        // Set the position at the beginning of the sound buffer.
        _secondaryBuffer.CurrentPosition = 0;
        // Set volume of the buffer to 100%
        _secondaryBuffer.Volume = 0;
        // Play the contents of the secondary sound buffer.
        _secondaryBuffer.Play(0, PlayFlags.Looping);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("LibRetroDirectSound: Failed to start playback", ex);
        return false;
      }
      return true;
    }

    public void Pause()
    {
      if (_secondaryBuffer != null)
        _secondaryBuffer.Stop();
    }

    public void UnPause()
    {
      if (_secondaryBuffer != null)
        _secondaryBuffer.Play(0, PlayFlags.Looping);
    }

    public void SetVolume(int volume)
    {
      if (_secondaryBuffer != null)
        _secondaryBuffer.Volume = volume;
    }

    protected void IncrementWritePosition(int count)
    {
      _nextWrite = (_nextWrite + count) % _bufferBytes;
    }

    protected void Synchronize(int count)
    {
      int samplesNeeded = GetSamplesNeeded();
      while (samplesNeeded < count)
      {
        int sleepTime = (int)((count - samplesNeeded) / _samplesPerMs);
        Thread.Sleep(sleepTime / 2);
        samplesNeeded = GetSamplesNeeded();
      }
    }

    protected int GetSamplesNeeded()
    {
      return GetBytesNeeded() / sizeof(short);
    }

    protected int GetBytesNeeded()
    {
      int pPos;
      int wPos;
      _secondaryBuffer.GetCurrentPosition(out pPos, out wPos);
      return wPos < _nextWrite ? wPos + _bufferBytes - _nextWrite : wPos - _nextWrite;
    }

    public void Dispose()
    {
      if (_secondaryBuffer != null)
      {
        _secondaryBuffer.Dispose();
        _secondaryBuffer = null;
      }
      if (_directSound != null)
      {
        _directSound.Dispose();
        _directSound = null;
      }
    }
  }
}
