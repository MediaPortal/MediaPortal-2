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
using System.ComponentModel;
using SkinEngine.Controls.Brushes;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Visuals;
using SlimDX;

namespace SkinEngine.ElementRegistrations
{
  /// <summary>
  /// This class holds static methods for converting string representations
  /// into objects of a specific type.
  /// </summary>
  public class XamlTypeConverter
  {
    #region Public methods
    /// <summary>
    /// Converts a string into a <see cref="float"/>.
    /// </summary>
    /// <param name="floatString">A string containing a float. It doesn't matter if
    /// the string contains commas or dots as decimal point.</param>
    /// <returns>Float number parsed from the specified <paramref name="floatString"/>.</returns>
    public static float Convert2Float(string floatString)
    {
      floatString = BringFloatStringToCurrentCultureFormat(floatString);
      float f;
      float.TryParse(floatString, out f);
      return f;
    }

    /// <summary>
    /// Converts a string into a <see cref="double"/>.
    /// </summary>
    /// <param name="doubleString">A string containing a double. It doesn't matter if
    /// the string contains commas or dots as decimal point.</param>
    /// <returns>Double number parsed from the specified <paramref name="doubleString"/>.</returns>
    public static double Convert2Double(string doubleString)
    {
      doubleString = BringFloatStringToCurrentCultureFormat(doubleString);
      double f;
      double.TryParse(doubleString, out f);
      return f;
    }

    public static object ConvertType(Type type, object value)
    {
      if (type == typeof(Transform))
      {
        string v = value.ToString();
        string[] parts = v.Split(new char[] { ',' });
        if (parts.Length == 6)
        {
          float[] f = new float[parts.Length];
          for (int i = 0; i < parts.Length; ++i)
          {
            f[i] = XamlTypeConverter.Convert2Float(parts[i]);
          }
          System.Drawing.Drawing2D.Matrix matrix2d = new System.Drawing.Drawing2D.Matrix(f[0], f[1], f[2], f[3], f[4], f[5]);
          Static2dMatrix matrix = new Static2dMatrix();
          matrix.Set2DMatrix(matrix2d);
          return matrix;
        }
      }
      if (type == typeof(VisibilityEnum))
      {
        string v = value.ToString();
        if (v == "Collapsed") return VisibilityEnum.Collapsed;
        if (v == "Hidden") return VisibilityEnum.Hidden;
        if (v == "Visible") return VisibilityEnum.Visible;
      }
      if (type == typeof(HorizontalAlignmentEnum))
      {
        string v = value.ToString();
        if (v == "Left") return HorizontalAlignmentEnum.Left;
        if (v == "Right") return HorizontalAlignmentEnum.Right;
        if (v == "Center") return HorizontalAlignmentEnum.Center;
        if (v == "Stretch") return HorizontalAlignmentEnum.Stretch;
      }
      if (type == typeof(VerticalAlignmentEnum))
      {
        string v = value.ToString();
        if (v == "Bottom") return VerticalAlignmentEnum.Bottom;
        if (v == "Top") return VerticalAlignmentEnum.Top;
        if (v == "Center") return VerticalAlignmentEnum.Center;
        if (v == "Stretch") return VerticalAlignmentEnum.Stretch;
      }
      if (type == typeof(Vector2))
      {
        return Convert2Vector2(value.ToString());
      }
      else if (type == typeof(Vector3))
      {
        return Convert2Vector3(value.ToString());
      }
      else if (type == typeof(Vector4))
      {
        return Convert2Vector4(value.ToString());
      }
      else if (type == typeof(Brush))
      {
        SolidColorBrush b = new SolidColorBrush();
        b.Color = (System.Drawing.Color)TypeDescriptor.GetConverter(typeof(System.Drawing.Color)).ConvertFromString(value.ToString());
        return b;
      }
      else if (type == typeof(PointCollection))
      {
        PointCollection coll = new PointCollection();
        string text = value.ToString();
        string[] parts = text.Split(new char[] { ',', ' ' });
        for (int i = 0; i < parts.Length; i += 2)
        {
          System.Drawing.Point p = new System.Drawing.Point(Int32.Parse(parts[i]), Int32.Parse(parts[i + 1]));
          coll.Add(p);
        }
        return coll;
      }
      else if (type == typeof(GridLength))
      {
        string text = value.ToString();
        if (text == "Auto")
        {
          return new GridLength(GridUnitType.Star, 1.0);
        }
        else if (text.IndexOf('*') < 0)
        {
          double v = double.Parse(text);
          return new GridLength(GridUnitType.Pixel, v);
        }
        else
        {
          int pos = text.IndexOf('*');
          text = text.Substring(0, pos);
          double percent = XamlTypeConverter.Convert2Double(text);
          return new GridLength(GridUnitType.Star, percent);
        }
      }
      return value;
    }
    #endregion

    #region Private/protected methods
    protected static string BringFloatStringToCurrentCultureFormat(string floatString)
    {
      float test = 12.03f;
      string comma = test.ToString();
      // Bring the float string in a format which can be parsed with the current language settings
      bool langUsesComma = (comma.IndexOf(",") >= 0);
      if (langUsesComma)
      {
        floatString = floatString.Replace(".", ",");
      }
      else
      {
        floatString = floatString.Replace(",", ".");
      }
      return floatString;
    }

    /// <summary>
    /// Converts a string to a <see cref="Vector2"/>.
    /// </summary>
    /// <param name="coordsString">The coordinates in "0.2,0.4" format. This method
    /// will fill as many coordinates in the result vector as specified in the
    /// comma separated string. So the string "3.5,7.2" will result in a vector (3.5, 7.2),
    /// the string "5.6" will result in a vector (5.6, 0),
    /// an empty or a <code>null</code> string will result in a vector (0, 0).</param>
    /// <returns>New <see cref="Vector2"/> instance with the specified coordinates,
    /// never <code>null</code>.</returns>
    protected static Vector2 Convert2Vector2(string coordsString)
    {
      if (coordsString == null)
      {
        return new Vector2(0, 0);
      }
      Vector2 vec = new Vector2();
      string[] coords = coordsString.Split(new char[] { ',' });
      if (coords.Length > 0)
      {
        vec.X = XamlTypeConverter.Convert2Float(coords[0]);
      }
      if (coords.Length > 1)
      {
        vec.Y = XamlTypeConverter.Convert2Float(coords[1]);
      }
      return vec;
    }

    /// <summary>
    /// Converts a string to a <see cref="Vector3"/>.
    /// </summary>
    /// <param name="coordsString">The coordinates in "0.2,0.4,0.1" format. This method
    /// will fill as many coordinates in the result vector as specified in the
    /// comma separated string. So the string "3.5,7.2,5.2" will result in a vector (3.5, 7.2, 5.2),
    /// the string "5.6" will result in a vector (5.6, 0, 0),
    /// an empty or a <code>null</code> string will result in a vector (0, 0, 0).</param>
    /// <returns>New <see cref="Vector3"/> instance with the specified coordinates,
    /// never <code>null</code>.</returns>
    protected static Vector3 Convert2Vector3(string coordsString)
    {
      if (coordsString == null)
      {
        return new Vector3(0, 0, 0);
      }
      Vector3 vec = new Vector3();
      string[] coords = coordsString.Split(new char[] { ',' });
      if (coords.Length > 0)
      {
        vec.X = XamlTypeConverter.Convert2Float(coords[0]);
      }
      if (coords.Length > 1)
      {
        vec.Y = XamlTypeConverter.Convert2Float(coords[1]);
      }
      if (coords.Length > 2)
      {
        vec.Z = XamlTypeConverter.Convert2Float(coords[2]);
      }
      return vec;
    }

    /// <summary>
    /// Converts a string to a <see cref="Vector4"/>.
    /// </summary>
    /// <param name="coordsString">The coordinates in "0.2,0.4,0.1,0.6" format. This method
    /// will fill as many coordinates in the result vector as specified in the
    /// comma separated string. So the string "3.5,7.2,5.2,2.8" will result in a
    /// vector (3.5, 7.2, 5.2, 2.8),
    /// the string "5.6" will result in a vector (5.6, 0, 0, 0),
    /// an empty or a <code>null</code> string will result in a vector (0, 0, 0, 0).</param>
    /// <returns>New <see cref="Vector4"/> instance with the specified coordinates,
    /// never <code>null</code>.</returns>
    protected static Vector4 Convert2Vector4(string coordsString)
    {
      if (coordsString == null)
      {
        return new Vector4(0, 0, 0, 0);
      }
      Vector4 vec = new Vector4();
      string[] coords = coordsString.Split(new char[] { ',' });
      if (coords.Length > 0)
      {
        vec.X = XamlTypeConverter.Convert2Float(coords[0]);
      }
      if (coords.Length > 1)
      {
        vec.Y = XamlTypeConverter.Convert2Float(coords[1]);
      }
      if (coords.Length > 2)
      {
        vec.W = XamlTypeConverter.Convert2Float(coords[2]);
      }
      if (coords.Length > 3)
      {
        vec.Z = XamlTypeConverter.Convert2Float(coords[3]);
      }
      return vec;
    }
    #endregion
  }
}
