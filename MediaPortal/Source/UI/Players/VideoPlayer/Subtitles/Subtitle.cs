using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Players.Video.Tools;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  public class Subtitle : IDisposable
  {
    public static int IdCount = 0;
    protected DeviceEx _device;

    public Subtitle(DeviceEx device)
    {
      _device = device;
      Id = IdCount++;
    }

    public Bitmap SubBitmap;
    public string Text;
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
    public Texture SubTexture;

    /// <summary>
    /// Update the subtitle texture from a Bitmap.
    /// </summary>
    public bool Allocate()
    {
      if (SubTexture != null)
        return true;

      try
      {
        if (SubBitmap != null)
        {
          using (MemoryStream stream = new MemoryStream())
          {
            ImageInformation imageInformation;
            SubBitmap.Save(stream, ImageFormat.Bmp);
            stream.Position = 0;
            SubTexture = Texture.FromStream(_device, stream, (int)stream.Length, (int)Width,
              (int)Height, 1,
              Usage.Dynamic, Format.A8R8G8B8, Pool.Default, Filter.None, Filter.None, 0,
              out imageInformation);
          }
          // Free bitmap
          FilterGraphTools.TryDispose(ref SubBitmap);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("SubtitleRenderer: Failed to create subtitle texture!!!", e);
        return false;
      }
      return true;
    }

    public override string ToString()
    {
      return "Subtitle " + Id + " meta data: Timeout=" + TimeOut + " timestamp=" + PresentTime;
    }

    public void Dispose()
    {
      SubTexture?.Dispose();
      SubTexture = null;
    }
  }
}
