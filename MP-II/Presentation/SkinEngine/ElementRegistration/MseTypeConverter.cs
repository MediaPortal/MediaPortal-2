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
using Presentation.SkinEngine.Controls.Brushes;
using Presentation.SkinEngine.Controls.Panels;
using Presentation.SkinEngine.Controls.Transforms;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Controls.Visuals.Shapes;
using Presentation.SkinEngine.Exceptions;
using SlimDX;
using TypeConverter = Presentation.SkinEngine.XamlParser.TypeConverter;

namespace Presentation.SkinEngine.ElementRegistrations
{
  
  /// <summary>
  /// This class holds static methods for converting string representations
  /// into objects of a specific type.
  /// </summary>
  public class MseTypeConverter
  {
    #region Public methods
    
    public static object ConvertType(object value, Type targetType)
    {
      object result;
      if (ConvertType(value, targetType, out result))
        return result;
      else
        throw new ConvertException("Could not convert object '{0}' to type '{1}'", value, targetType.Name);
    }

    public static bool ConvertType(object value, Type targetType, out object result)
    {
      result = value;
      if (value == null)
      {
        result = value;
        return true;
      }
      else if (targetType == typeof(Transform))
      {
        string v = value.ToString();
        string[] parts = v.Split(new char[] { ',' });
        if (parts.Length == 6)
        {
          float[] f = new float[parts.Length];
          for (int i = 0; i < parts.Length; ++i)
          {
            object obj;
            TypeConverter.Convert(parts[i], typeof(double), out obj);
            f[i] = (float) obj;
          }
          System.Drawing.Drawing2D.Matrix matrix2d = new System.Drawing.Drawing2D.Matrix(f[0], f[1], f[2], f[3], f[4], f[5]);
          Static2dMatrix matrix = new Static2dMatrix();
          matrix.Set2DMatrix(matrix2d);
          result = matrix;
          return true;
        }
      }
      else if (targetType == typeof(VisibilityEnum))
      {
        string v = value.ToString();
        if (v == "Collapsed")
        {
          result = VisibilityEnum.Collapsed;
          return true;
        }
        if (v == "Hidden")
        {
          result = VisibilityEnum.Hidden;
          return true;
        }
        if (v == "Visible")
        {
          result = VisibilityEnum.Visible;
          return true;
        }
      }
      else if (targetType == typeof(HorizontalAlignmentEnum))
      {
        string v = value.ToString();
        if (v == "Left")
        {
          result = HorizontalAlignmentEnum.Left;
          return true;
        }
        if (v == "Right")
        {
          result = HorizontalAlignmentEnum.Right;
          return true;
        }
        if (v == "Center")
        {
          result = HorizontalAlignmentEnum.Center;
          return true;
        }
        if (v == "Stretch")
        {
          result = HorizontalAlignmentEnum.Stretch;
          return true;
        }
      }
      else if (targetType == typeof(VerticalAlignmentEnum))
      {
        string v = value.ToString();
        if (v == "Bottom")
        {
          result = VerticalAlignmentEnum.Bottom;
          return true;
        }
        if (v == "Top")
        {
          result = VerticalAlignmentEnum.Top;
          return true;
        }
        if (v == "Center")
        {
          result = VerticalAlignmentEnum.Center;
          return true;
        }
        if (v == "Stretch")
        {
          result = VerticalAlignmentEnum.Stretch;
          return true;
        }
      }
      else if (targetType == typeof(Vector2))
      {
        result = Convert2Vector2(value.ToString());
        return true;
      }
      else if (targetType == typeof(Vector3))
      {
        result = Convert2Vector3(value.ToString());
        return true;
      }
      else if (targetType == typeof(Vector4))
      {
        result = Convert2Vector4(value.ToString());
        return true;
      }
      else if (targetType == typeof(Brush))
      {
        SolidColorBrush b = new SolidColorBrush();
        b.Color = (System.Drawing.Color)TypeDescriptor.GetConverter(typeof(System.Drawing.Color)).ConvertFromString(value.ToString());
        result = b;
        return true;
      }
      else if (targetType == typeof(PointCollection))
      {
        PointCollection coll = new PointCollection();
        string text = value.ToString();
        string[] parts = text.Split(new char[] { ',', ' ' });
        for (int i = 0; i < parts.Length; i += 2)
        {
          System.Drawing.Point p = new System.Drawing.Point(Int32.Parse(parts[i]), Int32.Parse(parts[i + 1]));
          coll.Add(p);
        }
        result = coll;
        return true;
      }
      else if (targetType == typeof(GridLength))
      {
        string text = value.ToString();
        if (text == "Auto")
        {
          result = new GridLength(GridUnitType.Star, 1.0);
        }
        else if (text.IndexOf('*') < 0)
        {
          double v = double.Parse(text);
          result = new GridLength(GridUnitType.Pixel, v);
        }
        else
        {
          int pos = text.IndexOf('*');
          text = text.Substring(0, pos);
          object obj;
          TypeConverter.Convert(text, typeof(double), out obj);
          result = new GridLength(GridUnitType.Star, (double) obj);
        }
        return true;
      }
      else if (targetType.IsAssignableFrom(typeof(FrameworkElement)) && value is string)
      {
        Label resultLabel = new Label();
        resultLabel.Text = (string)value;
        resultLabel.Font = "font12";
        result = resultLabel;
        return true;
      }
      result = value;
      return false;
    }

    #endregion

    #region Private/protected methods

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
      object obj;
      if (coords.Length > 0)
      {
        TypeConverter.Convert(coords[0], typeof(float), out obj);
        vec.X = (float) obj;
      }
      if (coords.Length > 1)
      {
        TypeConverter.Convert(coords[1], typeof(float), out obj);
        vec.Y = (float) obj;
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
      object obj;
      if (coords.Length > 0)
      {
        TypeConverter.Convert(coords[0], typeof(float), out obj);
        vec.X = (float) obj;
      }
      if (coords.Length > 1)
      {
        TypeConverter.Convert(coords[1], typeof(float), out obj);
        vec.Y = (float) obj;
      }
      if (coords.Length > 2)
      {
        TypeConverter.Convert(coords[2], typeof(float), out obj);
        vec.Z = (float) obj;
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
      object obj;
      if (coords.Length > 0)
      {
        TypeConverter.Convert(coords[0], typeof(float), out obj);
        vec.X = (float) obj;
      }
      if (coords.Length > 1)
      {
        TypeConverter.Convert(coords[1], typeof(float), out obj);
        vec.Y = (float) obj;
      }
      if (coords.Length > 2)
      {
        TypeConverter.Convert(coords[2], typeof(float), out obj);
        vec.W = (float) obj;
      }
      if (coords.Length > 3)
      {
        TypeConverter.Convert(coords[3], typeof(float), out obj);
        vec.Z = (float) obj;
      }
      return vec;
    }
    #endregion
  }
}
