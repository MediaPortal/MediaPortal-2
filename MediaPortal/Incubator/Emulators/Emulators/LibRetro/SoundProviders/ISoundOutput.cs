using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.SoundProviders
{
  public interface ISoundOutput : IDisposable
  {
    bool Init(IntPtr windowHandle, Guid audioDeviceId, int sampleRate, double bufferSizeSeconds);
    bool Play();
    void Pause();
    void UnPause();
    void SetVolume(int volume);
    void WriteSamples(short[] samples, int count, bool synchronise);
  }
}
