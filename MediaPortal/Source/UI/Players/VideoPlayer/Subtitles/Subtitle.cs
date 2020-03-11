using SharpDX.Direct2D1;
using System;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  public class Subtitle : IDisposable
  {
    public static int IdCount = 0;

    public Subtitle()
    {
      Id = IdCount++;
    }

    public uint Width;
    public uint Height;
    public double PresentTime;  // NOTE: in seconds
    public double TimeOut;      // NOTE: in seconds
    public int FirstScanLine;
    public long Id = 0;
    public bool ShouldDraw;
    public Int32 ScreenHeight; // Required for aspect ratio correction
    public Int32 ScreenWidth; // Required for aspect ratio correction
    public Int32 HorizontalPosition;
    public Bitmap1 SubTexture;

    public override string ToString()
    {
      return "Subtitle " + Id + " meta data: Timeout=" + TimeOut + " timestamp=" + PresentTime;
    }

    public void Dispose()
    {
      if (SubTexture != null)
        SubTexture.Dispose();
      SubTexture = null;
    }
  }
}
