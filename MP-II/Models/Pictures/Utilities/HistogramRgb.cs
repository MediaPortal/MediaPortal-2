#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace Models.Pictures.Utilities
{
  public class HistogramRgb
  {
    protected long[][] histogram;
    public long[][] HistogramValues
    {
      get
      {
        return this.histogram;
      }

      set
      {
        if (value.Length == this.histogram.Length && value[0].Length == this.histogram[0].Length)
        {
          this.histogram = value;
        }
        else
        {
          throw new ArgumentException("value muse be an array of arrays of matching size", "value");
        }
      }
    }

    public int Channels
    {
      get
      {
        return this.histogram.Length;
      }
    }

    public int Entries
    {
      get
      {
        return this.histogram[0].Length;
      }
    }

    protected internal HistogramRgb(int channels, int entries)
    {
      this.histogram = new long[channels][];

      for (int channel = 0; channel < channels; ++channel)
      {
        this.histogram[channel] = new long[entries];
      }
    }

    public long GetOccurrences(int channel, int val)
    {
      return histogram[channel][val];
    }

    public long GetMax()
    {
      long max = -1;

      foreach (long[] channelHistogram in histogram)
      {
        foreach (long i in channelHistogram)
        {
          if (i > max)
          {
            max = i;
          }
        }
      }

      return max;
    }

    public long GetMax(int channel)
    {
      long max = -1;

      foreach (long i in histogram[channel])
      {
        if (i > max)
        {
          max = i;
        }
      }

      return max;
    }

    public float[] GetMean()
    {
      float[] ret = new float[Channels];

      for (int channel = 0; channel < Channels; ++channel)
      {
        long[] channelHistogram = histogram[channel];
        long avg = 0;
        long sum = 0;

        for (int j = 0; j < channelHistogram.Length; j++)
        {
          avg += j * channelHistogram[j];
          sum += channelHistogram[j];
        }

        if (sum != 0)
        {
          ret[channel] = (float)avg / (float)sum;
        }
        else
        {
          ret[channel] = 0;
        }
      }

      return ret;
    }

    public int[] GetPercentile(float fraction)
    {
      int[] ret = new int[Channels];

      for (int channel = 0; channel < Channels; ++channel)
      {
        long[] channelHistogram = histogram[channel];
        long integral = 0;
        long sum = 0;

        for (int j = 0; j < channelHistogram.Length; j++)
        {
          sum += channelHistogram[j];
        }

        for (int j = 0; j < channelHistogram.Length; j++)
        {
          integral += channelHistogram[j];

          if (integral > sum * fraction)
          {
            ret[channel] = j;
            break;
          }
        }
      }

      return ret;
    }
    /// <summary>
    /// Sets the histogram to be all zeros.
    /// </summary>
    protected void Clear()
    {
      histogram.Initialize();
    }


    public void AddBitmap(Bitmap bmpPhoto)
    {
      BitmapData bmData = bmpPhoto.LockBits(new Rectangle(0, 0, bmpPhoto.Width, bmpPhoto.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

      long[] histogramB = histogram[0];
      long[] histogramG = histogram[1];
      long[] histogramR = histogram[2];

      System.IntPtr Scan0 = bmData.Scan0;
      byte red, green, blue;
      unsafe
      {
        byte* p = (byte*)(void*)Scan0;
        int stride = bmData.Stride;
        int nOffset = stride - bmpPhoto.Width * 3;
        int nWidth = bmpPhoto.Width * 3;

        for (int y = 0; y < bmpPhoto.Height; ++y)
        {
          for (int x = 0; x < bmpPhoto.Width; ++x)
          {
            blue = p[0];
            green = p[1];
            red = p[2];
            ++histogramB[blue];
            ++histogramG[green];
            ++histogramR[red];

            p += 3;
          }
          p += nOffset;
        }
      }
      bmpPhoto.UnlockBits(bmData);
    }

    public ColorBgra GetMeanColor()
    {
      float[] mean = GetMean();
      return ColorBgra.FromBgr((byte)(mean[0] + 0.5f), (byte)(mean[1] + 0.5f), (byte)(mean[2] + 0.5f));
    }

    public ColorBgra GetPercentileColor(float fraction)
    {
      int[] perc = GetPercentile(fraction);

      return ColorBgra.FromBgr((byte)(perc[0]), (byte)(perc[1]), (byte)(perc[2]));
    }
    public Level MakeLevelsAuto()
    {
      ColorBgra lo = GetPercentileColor(0.005f);
      ColorBgra md = GetMeanColor();
      ColorBgra hi = GetPercentileColor(0.995f);

      return AutoFromLoMdHi(lo, md, hi);
    }
    public Level AutoFromLoMdHi(ColorBgra lo, ColorBgra md, ColorBgra hi)
    {
      float[] gamma = new float[3];

      for (int i = 0; i < 3; i++)
      {
        if (lo[i] < md[i] && md[i] < hi[i])
        {
          gamma[i] = (float)Clamp(Math.Log(0.5, (float)(md[i] - lo[i]) / (float)(hi[i] - lo[i])), 0.1, 10.0);
        }
        else
        {
          gamma[i] = 1.0f;
        }
      }

      return new Level(lo, hi, gamma, ColorBgra.FromColor(Color.Black), ColorBgra.FromColor(Color.White));
    }
    double Clamp(double x, double min, double max)
    {
      if (x < min)
      {
        return min;
      }
      else if (x > max)
      {
        return max;
      }
      else
      {
        return x;
      }
    }
  }
}
