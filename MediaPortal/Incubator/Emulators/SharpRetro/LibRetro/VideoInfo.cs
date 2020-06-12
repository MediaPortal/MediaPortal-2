using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.LibRetro
{
  public class VideoInfo
  {
    public VideoInfo(int width, int height, float dar)
    {
      Width = width;
      Height = height;
      DAR = dar;
    }

    public int Width { get; set; }
    public int Height { get; set; }
    public float DAR { get; set; }

    public int VirtualWidth
    {
      get
      {
        if (DAR <= 0)
          return Width;
        else if (DAR > 1.0f)
          return (int)(Height * DAR);
        else
          return Width;
      }
    }

    public int VirtualHeight
    {
      get
      {
        if (DAR <= 0)
          return Height;
        if (DAR < 1.0f)
          return (int)(Width / DAR);
        else
          return Height;
      }
    }
  }
}
