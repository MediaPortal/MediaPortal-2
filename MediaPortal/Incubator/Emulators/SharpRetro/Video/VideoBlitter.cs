using SharpRetro.LibRetro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.Video
{
  public static unsafe class VideoBlitter
  {
    public static void Blit(RETRO_PIXEL_FORMAT pixelFormat, IntPtr src, int* dst, int width, int height, int pitch)
    {
      switch (pixelFormat)
      {
        case RETRO_PIXEL_FORMAT.XRGB8888:
          Blit888((int*)src, dst, width, height, pitch / 4);
          break;
        case RETRO_PIXEL_FORMAT.RGB565:
          Blit565((short*)src, dst, width, height, pitch / 2);
          break;
        case RETRO_PIXEL_FORMAT.XRGB1555:
          Blit555((short*)src, dst, width, height, pitch / 2);
          break;
      }
    }

    public static void Blit555(short* src, int* dst, int width, int height, int pitch)
    {
      for (int j = 0; j < height; j++)
      {
        short* row = src;
        for (int i = 0; i < width; i++)
        {
          short ci = *row;
          int r = ci & 0x001f;
          int g = ci & 0x03e0;
          int b = ci & 0x7c00;

          r = (r << 3) | (r >> 2);
          g = (g >> 2) | (g >> 7);
          b = (b >> 7) | (b >> 12);
          int co = (b << 16) | (g << 8) | r;

          *dst = co;
          dst++;
          row++;
        }
        src += pitch;
      }
    }

    public static void Blit565(short* src, int* dst, int width, int height, int pitch)
    {
      for (int j = 0; j < height; j++)
      {
        short* row = src;
        for (int i = 0; i < width; i++)
        {
          short ci = *row;
          int r = ci & 0x001f;
          int g = (ci & 0x07e0) >> 5;
          int b = (ci & 0xf800) >> 11;

          r = (r << 3) | (r >> 2);
          g = (g << 2) | (g >> 4);
          b = (b << 3) | (b >> 2);
          int co = (b << 16) | (g << 8) | r;

          *dst = co;
          dst++;
          row++;
        }
        src += pitch;
      }
    }

    public static void Blit888(int* src, int* dst, int width, int height, int pitch)
    {
      for (int j = 0; j < height; j++)
      {
        int* row = src;
        for (int i = 0; i < width; i++)
        {
          int ci = *row;
          int co = ci | unchecked((int)0xff000000);
          *dst = co;
          dst++;
          row++;
        }
        src += pitch;
      }
    }
  }
}
