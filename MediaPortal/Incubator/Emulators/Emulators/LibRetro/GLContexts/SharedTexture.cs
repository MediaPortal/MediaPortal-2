using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.GLContexts
{
  public class SharedTexture
  {
    public Texture Texture { get; set; }
    public IntPtr GLHandle { get; set; }
  }
}