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
  [Serializable]
  public class Level : ChannelCurve
  {
    private ColorBgra colorInLow;
    public ColorBgra ColorInLow
    {
      get
      {
        return colorInLow;
      }

      set
      {
        if (value.R == 255)
        {
          value.R = 254;
        }

        if (value.G == 255)
        {
          value.G = 254;
        }

        if (value.B == 255)
        {
          value.B = 254;
        }

        if (colorInHigh.R < value.R + 1)
        {
          colorInHigh.R = (byte)(value.R + 1);
        }

        if (colorInHigh.G < value.G + 1)
        {
          colorInHigh.G = (byte)(value.R + 1);
        }

        if (colorInHigh.B < value.B + 1)
        {
          colorInHigh.B = (byte)(value.R + 1);
        }

        colorInLow = value;
        UpdateLookupTable();
      }
    }

    private ColorBgra colorInHigh;
    public ColorBgra ColorInHigh
    {
      get
      {
        return colorInHigh;
      }

      set
      {
        if (value.R == 0)
        {
          value.R = 1;
        }

        if (value.G == 0)
        {
          value.G = 1;
        }

        if (value.B == 0)
        {
          value.B = 1;
        }

        if (colorInLow.R > value.R - 1)
        {
          colorInLow.R = (byte)(value.R - 1);
        }

        if (colorInLow.G > value.G - 1)
        {
          colorInLow.G = (byte)(value.R - 1);
        }

        if (colorInLow.B > value.B - 1)
        {
          colorInLow.B = (byte)(value.R - 1);
        }

        colorInHigh = value;
        UpdateLookupTable();
      }
    }

    private ColorBgra colorOutLow;
    public ColorBgra ColorOutLow
    {
      get
      {
        return colorOutLow;
      }

      set
      {
        if (value.R == 255)
        {
          value.R = 254;
        }

        if (value.G == 255)
        {
          value.G = 254;
        }

        if (value.B == 255)
        {
          value.B = 254;
        }

        if (colorOutHigh.R < value.R + 1)
        {
          colorOutHigh.R = (byte)(value.R + 1);
        }

        if (colorOutHigh.G < value.G + 1)
        {
          colorOutHigh.G = (byte)(value.G + 1);
        }

        if (colorOutHigh.B < value.B + 1)
        {
          colorOutHigh.B = (byte)(value.B + 1);
        }

        colorOutLow = value;
        UpdateLookupTable();
      }
    }

    private ColorBgra colorOutHigh;
    public ColorBgra ColorOutHigh
    {
      get
      {
        return colorOutHigh;
      }

      set
      {
        if (value.R == 0)
        {
          value.R = 1;
        }

        if (value.G == 0)
        {
          value.G = 1;
        }

        if (value.B == 0)
        {
          value.B = 1;
        }

        if (colorOutLow.R > value.R - 1)
        {
          colorOutLow.R = (byte)(value.R - 1);
        }

        if (colorOutLow.G > value.G - 1)
        {
          colorOutLow.G = (byte)(value.G - 1);
        }

        if (colorOutLow.B > value.B - 1)
        {
          colorOutLow.B = (byte)(value.B - 1);
        }

        colorOutHigh = value;
        UpdateLookupTable();
      }
    }

    private float[] gamma = new float[3];
    public float GetGamma(int index)
    {
      if (index < 0 || index >= 3)
      {
        throw new ArgumentOutOfRangeException("index", index, "Index must be between 0 and 2");
      }

      return gamma[index];
    }

    public void SetGamma(int index, float val)
    {
      if (index < 0 || index >= 3)
      {
        throw new ArgumentOutOfRangeException("index", index, "Index must be between 0 and 2");
      }

      gamma[index] = Clamp(val, 0.1f, 10.0f);
      UpdateLookupTable();
    }

    static double Clamp(double x, double min, double max)
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
    static float Clamp(float x, float min, float max)
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
    public bool isValid = true;

    public static Level AutoFromLoMdHi(ColorBgra lo, ColorBgra md, ColorBgra hi)
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

    private void UpdateLookupTable()
    {
      for (int i = 0; i < 3; i++)
      {
        if (colorOutHigh[i] < colorOutLow[i] ||
            colorInHigh[i] <= colorInLow[i] ||
            gamma[i] < 0)
        {
          isValid = false;
          return;
        }

        for (int j = 0; j < 256; j++)
        {
          ColorBgra col = Apply(j, j, j);
          CurveB[j] = col.B;
          CurveG[j] = col.G;
          CurveR[j] = col.R;
        }
      }
    }

    public Level()
      : this(ColorBgra.FromColor(Color.Black),
               ColorBgra.FromColor(Color.White),
               new float[] { 1, 1, 1 },
               ColorBgra.FromColor(Color.Black),
               ColorBgra.FromColor(Color.White))
    {
    }

    public Level(ColorBgra in_lo, ColorBgra in_hi, float[] gamma, ColorBgra out_lo, ColorBgra out_hi)
    {
      colorInLow = in_lo;
      colorInHigh = in_hi;
      colorOutLow = out_lo;
      colorOutHigh = out_hi;

      if (gamma.Length != 3)
      {
        throw new ArgumentException("gamma", "gamma must be a float[3]");
      }

      this.gamma = gamma;
      UpdateLookupTable();
    }

    public ColorBgra Apply(float r, float g, float b)
    {
      ColorBgra ret = new ColorBgra();
      float[] input = new float[] { b, g, r };

      for (int i = 0; i < 3; i++)
      {
        float v = (input[i] - colorInLow[i]);

        if (v < 0)
        {
          ret[i] = colorOutLow[i];
        }
        else if (v + colorInLow[i] >= colorInHigh[i])
        {
          ret[i] = colorOutHigh[i];
        }
        else
        {
          ret[i] = (byte)Clamp(
              colorOutLow[i] + (colorOutHigh[i] - colorOutLow[i]) * Math.Pow(v / (colorInHigh[i] - colorInLow[i]), gamma[i]),
              0.0f,
              255.0f);
        }
      }

      return ret;
    }

    public void UnApply(ColorBgra after, float[] beforeOut, float[] slopesOut)
    {
      if (beforeOut.Length != 3)
      {
        throw new ArgumentException("before must be a float[3]", "before");
      }

      if (slopesOut.Length != 3)
      {
        throw new ArgumentException("slopes must be a float[3]", "slopes");
      }

      for (int i = 0; i < 3; i++)
      {
        beforeOut[i] = colorInLow[i] + (colorInHigh[i] - colorInLow[i]) *
            (float)Math.Pow((float)(after[i] - colorOutLow[i]) / (colorOutHigh[i] - colorOutLow[i]), 1 / gamma[i]);

        slopesOut[i] = (float)(colorInHigh[i] - colorInLow[i]) / ((colorOutHigh[i] - colorOutLow[i]) * gamma[i]) *
            (float)Math.Pow((float)(after[i] - colorOutLow[i]) / (colorOutHigh[i] - colorOutLow[i]), 1 / gamma[i] - 1);

        if (float.IsInfinity(slopesOut[i]) || float.IsNaN(slopesOut[i]))
        {
          slopesOut[i] = 0;
        }
      }
    }

    public object Clone()
    {
      Level copy = new Level(colorInLow, colorInHigh, (float[])gamma.Clone(), colorOutLow, colorOutHigh);

      copy.CurveB = (byte[])this.CurveB.Clone();
      copy.CurveG = (byte[])this.CurveG.Clone();
      copy.CurveR = (byte[])this.CurveR.Clone();

      return copy;
    }
  }


  [Serializable]
  public class ChannelCurve
  {
    public byte[] CurveB = new byte[256];
    public byte[] CurveG = new byte[256];
    public byte[] CurveR = new byte[256];

    public ChannelCurve()
    {
      for (int i = 0; i < 256; ++i)
      {
        CurveB[i] = (byte)i;
        CurveG[i] = (byte)i;
        CurveR[i] = (byte)i;
      }
    }

    public unsafe void Apply(ColorBgra* dst, ColorBgra* src, int length)
    {
      while (--length >= 0)
      {
        dst->B = CurveB[src->B];
        dst->G = CurveG[src->G];
        dst->R = CurveR[src->R];
        dst->A = src->A;

        ++dst;
        ++src;
      }
    }

    public unsafe void Apply(ColorBgra* ptr, int length)
    {
      while (--length >= 0)
      {
        ptr->B = CurveB[ptr->B];
        ptr->G = CurveG[ptr->G];
        ptr->R = CurveR[ptr->R];

        ++ptr;
      }
    }

    public ColorBgra Apply(ColorBgra color)
    {
      return ColorBgra.FromBgra(CurveB[color.B], CurveG[color.G], CurveR[color.R], color.A);
    }

    public void Apply(Bitmap bmpPhoto)
    {
      BitmapData bmData = bmpPhoto.LockBits(new Rectangle(0, 0, bmpPhoto.Width, bmpPhoto.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
      System.IntPtr Scan0 = bmData.Scan0;

      unsafe
      {
        byte* p = (byte*)(void*)Scan0;
        int stride = bmData.Stride;
        for (int y = 0; y < bmpPhoto.Height; ++y)
        {
          Apply(p, bmpPhoto.Width);
          p += stride;
        }
      }
      bmpPhoto.UnlockBits(bmData);
    }

    public unsafe virtual void Apply(byte* ptr, int length)
    {
      unsafe
      {
        while (length > 0)
        {
          byte blue = ptr[0];
          byte green = ptr[1];
          byte red = ptr[2];
          ColorBgra col = Apply(ColorBgra.FromBgr(blue, green, red));
          ptr[0] = col.B;
          ptr[1] = col.G;
          ptr[2] = col.R;
          ptr += 3;          
          --length;
        }
      }
    }
  }

}
