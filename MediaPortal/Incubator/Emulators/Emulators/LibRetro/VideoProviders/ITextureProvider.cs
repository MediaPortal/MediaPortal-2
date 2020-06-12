using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.VideoProviders
{
  public interface ITextureProvider : IDisposable
  {
    Texture Texture { get; }
    void UpdateTexture(int[] pixels, int width, int height, bool bottomLeftOrigin);
    void UpdateTexture(byte[] pixels, int width, int height, bool bottomLeftOrigin);
    void UpdateTexture(Texture source, int width, int height, bool bottomLeftOrigin);
    void Release();
  }
}
