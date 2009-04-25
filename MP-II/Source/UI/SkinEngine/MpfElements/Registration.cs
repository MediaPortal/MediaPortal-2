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
using System.ComponentModel;
using System.Globalization;
using MediaPortal.Control.InputManager;
using MediaPortal.Core.Commands;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Commands;
using MediaPortal.SkinEngine.Controls.Brushes;
using MediaPortal.SkinEngine.Controls.Panels;
using MediaPortal.SkinEngine.Controls.Transforms;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.SkinEngine.MpfElements.Resources;
using SlimDX;
using TypeConverter = MediaPortal.SkinEngine.Xaml.TypeConverter;
using MediaPortal.SkinEngine.Controls.Visuals.Shapes;

namespace MediaPortal.SkinEngine.MpfElements
{                            
  /// <summary>
  /// This class holds a registration for all elements which can be instanciated
  /// by a XAML file. It also holds static methods for type conversions
  /// between special types and for copying instances.
  /// </summary>
  public class Registration
  {
    #region Variables

    protected static readonly NumberFormatInfo NUMBERFORMATINFO = CultureInfo.InvariantCulture.NumberFormat;

    /// <summary>
    /// Registration for all elements the loader can create from a XAML file.
    /// </summary>
    protected static IDictionary<string, Type> objectClassRegistrations = new Dictionary<string, Type>();
    static Registration()
    {                            
      // Panels
      objectClassRegistrations.Add("DockPanel", typeof(SkinEngine.Controls.Panels.DockPanel));
      objectClassRegistrations.Add("StackPanel", typeof(SkinEngine.Controls.Panels.StackPanel));
      objectClassRegistrations.Add("Canvas", typeof(SkinEngine.Controls.Panels.Canvas));
      objectClassRegistrations.Add("Grid", typeof(SkinEngine.Controls.Panels.Grid));
      objectClassRegistrations.Add("RowDefinition", typeof(SkinEngine.Controls.Panels.RowDefinition));
      objectClassRegistrations.Add("ColumnDefinition", typeof(SkinEngine.Controls.Panels.ColumnDefinition));
      objectClassRegistrations.Add("GridLength", typeof(SkinEngine.Controls.Panels.GridLength));
      objectClassRegistrations.Add("WrapPanel", typeof(SkinEngine.Controls.Panels.WrapPanel));

      // Visuals
      objectClassRegistrations.Add("Control", typeof(SkinEngine.Controls.Visuals.Control));
      objectClassRegistrations.Add("Border", typeof(SkinEngine.Controls.Visuals.Border));
      objectClassRegistrations.Add("GroupBox", typeof(SkinEngine.Controls.Visuals.GroupBox));
      objectClassRegistrations.Add("Image", typeof(SkinEngine.Controls.Visuals.Image));
      objectClassRegistrations.Add("Button", typeof(SkinEngine.Controls.Visuals.Button));
      objectClassRegistrations.Add("RadioButton", typeof(SkinEngine.Controls.Visuals.RadioButton));
      objectClassRegistrations.Add("CheckBox", typeof(SkinEngine.Controls.Visuals.CheckBox));
      objectClassRegistrations.Add("Label", typeof(SkinEngine.Controls.Visuals.Label));
      objectClassRegistrations.Add("ListView", typeof(SkinEngine.Controls.Visuals.ListView));
      objectClassRegistrations.Add("ListViewItem", typeof(SkinEngine.Controls.Visuals.ListViewItem));
      objectClassRegistrations.Add("ContentPresenter", typeof(SkinEngine.Controls.Visuals.ContentPresenter));
      objectClassRegistrations.Add("ScrollContentPresenter", typeof(SkinEngine.Controls.Visuals.ScrollContentPresenter));
      objectClassRegistrations.Add("ProgressBar", typeof(SkinEngine.Controls.Visuals.ProgressBar));
      objectClassRegistrations.Add("HeaderedItemsControl", typeof(SkinEngine.Controls.Visuals.HeaderedItemsControl));
      objectClassRegistrations.Add("TreeView", typeof(SkinEngine.Controls.Visuals.TreeView));
      objectClassRegistrations.Add("TreeViewItem", typeof(SkinEngine.Controls.Visuals.TreeViewItem));
      objectClassRegistrations.Add("ItemsPresenter", typeof(SkinEngine.Controls.Visuals.ItemsPresenter));
      objectClassRegistrations.Add("StyleSelector", typeof(SkinEngine.Controls.Visuals.StyleSelector));
      objectClassRegistrations.Add("ScrollViewer", typeof(SkinEngine.Controls.Visuals.ScrollViewer));
      objectClassRegistrations.Add("TextBox", typeof(SkinEngine.Controls.Visuals.TextBox));
      objectClassRegistrations.Add("TextControl", typeof(SkinEngine.Controls.Visuals.TextControl));
      objectClassRegistrations.Add("ContentControl", typeof(SkinEngine.Controls.Visuals.ContentControl));
      objectClassRegistrations.Add("KeyBinding", typeof(SkinEngine.Controls.Visuals.KeyBinding));
      objectClassRegistrations.Add("KeyBindingControl", typeof(SkinEngine.Controls.Visuals.KeyBindingControl));

      // Brushes
      objectClassRegistrations.Add("SolidColorBrush", typeof(SkinEngine.Controls.Brushes.SolidColorBrush));
      objectClassRegistrations.Add("LinearGradientBrush", typeof(SkinEngine.Controls.Brushes.LinearGradientBrush));
      objectClassRegistrations.Add("RadialGradientBrush", typeof(SkinEngine.Controls.Brushes.RadialGradientBrush));
      objectClassRegistrations.Add("ImageBrush", typeof(SkinEngine.Controls.Brushes.ImageBrush));
      objectClassRegistrations.Add("VisualBrush", typeof(SkinEngine.Controls.Brushes.VisualBrush));
      objectClassRegistrations.Add("VideoBrush", typeof(SkinEngine.Controls.Brushes.VideoBrush));
      objectClassRegistrations.Add("GradientBrush", typeof(SkinEngine.Controls.Brushes.GradientBrush));
      objectClassRegistrations.Add("GradientStop", typeof(SkinEngine.Controls.Brushes.GradientStop));

      // Shapes
      objectClassRegistrations.Add("Rectangle", typeof(SkinEngine.Controls.Visuals.Shapes.Rectangle));
      objectClassRegistrations.Add("Ellipse", typeof(SkinEngine.Controls.Visuals.Shapes.Ellipse));
      objectClassRegistrations.Add("Line", typeof(SkinEngine.Controls.Visuals.Shapes.Line));
      objectClassRegistrations.Add("Polygon", typeof(SkinEngine.Controls.Visuals.Shapes.Polygon));
      objectClassRegistrations.Add("Path", typeof(SkinEngine.Controls.Visuals.Shapes.Path));
      objectClassRegistrations.Add("Shape", typeof(SkinEngine.Controls.Visuals.Shapes.Shape));

      // Animations
      objectClassRegistrations.Add("ColorAnimation", typeof(SkinEngine.Controls.Animations.ColorAnimation));
      objectClassRegistrations.Add("DoubleAnimation", typeof(SkinEngine.Controls.Animations.DoubleAnimation));
      objectClassRegistrations.Add("PointAnimation", typeof(SkinEngine.Controls.Animations.PointAnimation));
      objectClassRegistrations.Add("Storyboard", typeof(SkinEngine.Controls.Animations.Storyboard));
      objectClassRegistrations.Add("ColorAnimationUsingKeyFrames", typeof(SkinEngine.Controls.Animations.ColorAnimationUsingKeyFrames));
      objectClassRegistrations.Add("DoubleAnimationUsingKeyFrames", typeof(SkinEngine.Controls.Animations.DoubleAnimationUsingKeyFrames));
      objectClassRegistrations.Add("PointAnimationUsingKeyFrames", typeof(SkinEngine.Controls.Animations.PointAnimationUsingKeyFrames));
      objectClassRegistrations.Add("SplineColorKeyFrame", typeof(SkinEngine.Controls.Animations.SplineColorKeyFrame));
      objectClassRegistrations.Add("SplineDoubleKeyFrame", typeof(SkinEngine.Controls.Animations.SplineDoubleKeyFrame));
      objectClassRegistrations.Add("SplinePointKeyFrame", typeof(SkinEngine.Controls.Animations.SplinePointKeyFrame));

      // Triggers
      objectClassRegistrations.Add("EventTrigger", typeof(SkinEngine.Controls.Visuals.Triggers.EventTrigger));
      objectClassRegistrations.Add("Trigger", typeof(SkinEngine.Controls.Visuals.Triggers.Trigger));
      objectClassRegistrations.Add("DataTrigger", typeof(SkinEngine.Controls.Visuals.Triggers.DataTrigger));
      objectClassRegistrations.Add("BeginStoryboard", typeof(SkinEngine.Controls.Visuals.Triggers.BeginStoryboard));
      objectClassRegistrations.Add("StopStoryboard", typeof(SkinEngine.Controls.Visuals.Triggers.StopStoryboard));
      objectClassRegistrations.Add("TriggerCommand", typeof(SkinEngine.Controls.Bindings.TriggerCommand));

      // Transforms
      objectClassRegistrations.Add("TransformGroup", typeof(SkinEngine.Controls.Transforms.TransformGroup));
      objectClassRegistrations.Add("ScaleTransform", typeof(SkinEngine.Controls.Transforms.ScaleTransform));
      objectClassRegistrations.Add("SkewTransform", typeof(SkinEngine.Controls.Transforms.SkewTransform));
      objectClassRegistrations.Add("RotateTransform", typeof(SkinEngine.Controls.Transforms.RotateTransform));
      objectClassRegistrations.Add("TranslateTransform", typeof(SkinEngine.Controls.Transforms.TranslateTransform));

      // Styles
      objectClassRegistrations.Add("Style", typeof(SkinEngine.Controls.Visuals.Styles.Style));
      objectClassRegistrations.Add("Setter", typeof(SkinEngine.Controls.Visuals.Styles.Setter));
      objectClassRegistrations.Add("BindingSetter", typeof(SkinEngine.Controls.Visuals.Styles.BindingSetter));
      objectClassRegistrations.Add("DataTemplate", typeof(SkinEngine.Controls.Visuals.Templates.DataTemplate));
      objectClassRegistrations.Add("HierarchicalDataTemplate", typeof(SkinEngine.Controls.Visuals.Templates.HierarchicalDataTemplate));
      objectClassRegistrations.Add("ControlTemplate", typeof(SkinEngine.Controls.Visuals.Templates.ControlTemplate));
      objectClassRegistrations.Add("ItemsPanelTemplate", typeof(SkinEngine.Controls.Visuals.ItemsPanelTemplate));

      // Resources/wrapper classes
      objectClassRegistrations.Add("ResourceDictionary", typeof(SkinEngine.MpfElements.Resources.ResourceDictionary));
      objectClassRegistrations.Add("Include", typeof(SkinEngine.MpfElements.Resources.Include));
      objectClassRegistrations.Add("LateBoundValue", typeof(SkinEngine.MpfElements.Resources.LateBoundValue));
      objectClassRegistrations.Add("ResourceWrapper", typeof(SkinEngine.MpfElements.Resources.ResourceWrapper));
      objectClassRegistrations.Add("BindingWrapper", typeof(SkinEngine.MpfElements.Resources.BindingWrapper));
      
      // Command
      objectClassRegistrations.Add("CommandList", typeof(SkinEngine.Commands.CommandList));
      objectClassRegistrations.Add("InvokeCommand", typeof(SkinEngine.Commands.InvokeCommand));

      // Converters

      objectClassRegistrations.Add("ReferenceNotNull_BoolConverter", typeof(SkinEngine.MpfElements.Converters.ReferenceNotNull_BoolConverter));
      objectClassRegistrations.Add("ExpressionMultiValueConverter", typeof(SkinEngine.MpfElements.Converters.ExpressionMultiValueConverter));
      
      // Markup extensions
      objectClassRegistrations.Add("StaticResource", typeof(SkinEngine.MarkupExtensions.StaticResourceMarkupExtension));
      objectClassRegistrations.Add("DynamicResource", typeof(SkinEngine.MarkupExtensions.DynamicResourceMarkupExtension));
      objectClassRegistrations.Add("ThemeResource", typeof(SkinEngine.MarkupExtensions.ThemeResourceMarkupExtension));
      objectClassRegistrations.Add("Binding", typeof(SkinEngine.MarkupExtensions.BindingMarkupExtension));
      objectClassRegistrations.Add("MultiBinding", typeof(SkinEngine.MarkupExtensions.MultiBindingMarkupExtension));
      objectClassRegistrations.Add("TemplateBinding", typeof(SkinEngine.MarkupExtensions.TemplateBindingMarkupExtension));
      objectClassRegistrations.Add("PickupBinding", typeof(SkinEngine.MarkupExtensions.PickupBindingMarkupExtension));
      objectClassRegistrations.Add("Command", typeof(SkinEngine.MarkupExtensions.CommandMarkupExtension));
      objectClassRegistrations.Add("CommandStencil", typeof(SkinEngine.MarkupExtensions.CommandStencilMarkupExtension));
      objectClassRegistrations.Add("Model", typeof(SkinEngine.MarkupExtensions.GetModelMarkupExtension));
      objectClassRegistrations.Add("Service", typeof(SkinEngine.MarkupExtensions.ServiceScopeMarkupExtension));
      objectClassRegistrations.Add("Color", typeof(SkinEngine.MarkupExtensions.ColorMarkupExtension));

      // Others
      objectClassRegistrations.Add("RelativeSource", typeof(SkinEngine.MarkupExtensions.RelativeSource));
    }

    #endregion

    #region Public properties

    public static IDictionary<string, Type> ObjectClassRegistrations
    { get { return objectClassRegistrations; } }

    #endregion

    #region Public methods
    
    public static bool ConvertType(object value, Type targetType, out object result)
    {
      result = value;
      if (value == null)
      {
        result = value;
        return true;
      }
      // Don't convert LateBoundValue (or superclass ValueWrapper) here... instances of
      // LateBoundValue must stay unchanged until some code part explicitly converts them!
      else if (typeof(ResourceWrapper).IsAssignableFrom(value.GetType()))
        return TypeConverter.Convert(((ResourceWrapper) value).Resource, targetType, out result);
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
      else if (targetType == typeof(Thickness))
      {
        Thickness t;
        float[] numberList = ParseFloatList(value.ToString());

        if (numberList.Length == 1)
        {
          t = new Thickness(numberList[0]);
        }
        else if (numberList.Length == 2)
        {
          t = new Thickness(numberList[0], numberList[1]);
        }
        else if (numberList.Length == 4)
        {
          t = new Thickness(numberList[0], numberList[1], numberList[2], numberList[3]);
        }
        else
        {
          throw new ArgumentException("Invalid # of parameters");
        }
        result = t;
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
          result = new GridLength(GridUnitType.Auto, 0.0);
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
          if (text.Length > 0)
          {
            object obj;
            TypeConverter.Convert(text, typeof(double), out obj);
            result = new GridLength(GridUnitType.Star, (double)obj);
          }
          else
          {
            result = new GridLength(GridUnitType.Star, 1.0);
          }
        }
        return true;
      }
      else if (targetType == typeof(string) && value is IResourceString)
      {
        result = ((IResourceString) value).Evaluate();
        return true;
      }
      else if (targetType.IsAssignableFrom(typeof(IExecutableCommand)) && value is ICommand)
      {
        result = new CommandBridge((ICommand) value);
        return true;
      }
      else if (targetType == typeof(Key) && value is string)
      {
        string str = (string) value;
        // Try a special key
        result = Key.GetSpecialKeyByName(str);
        if (result == null)
          if (str.Length != 1)
            throw new ArgumentException(string.Format("Cannot convert '{0}' to type Key", str));
          else
            result = new Key(str[0]);
        return true;
      }
      result = value;
      return false;
    }

    public static bool CopyMpfObject(object source, out object target)
    {
      target = null;
      if (source == null)
        return true;
      Type t = source.GetType();
      if (t == typeof(Vector2))
      {
        Vector2 vec = (Vector2) source;
        Vector2 result = new Vector2();
        result.X = vec.X;
        result.Y = vec.Y;
        target = result;
        return true;
      }
      else if (t == typeof(Vector3))
      {
        Vector3 vec = (Vector3) source;
        Vector3 result = new Vector3();
        result.X = vec.X;
        result.Y = vec.Y;
        result.Z = vec.Z;
        target = result;
        return true;
      }
      else if (t == typeof(Vector4))
      {
        Vector4 vec = (Vector4) source;
        Vector4 result = new Vector4();
        result.X = vec.X;
        result.Y = vec.Y;
        result.W = vec.W;
        result.Z = vec.Z;
        target = result;
        return true;
      }
      else if (source is Style)
      {
        // Style objects are unmodifyable
        target = source;
        return true;
      }
      // DataTemplates are modifiable, don't exclude them here from copying
      else if (source is ResourceWrapper && ((ResourceWrapper) source).Freezable)
      {
        target = source;
        return true;
      }
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

    /// <summary>
    /// Parses a comma separated list of floats.
    /// </summary>
    /// <param name="numbersString">The string representing the list of numbers.</param>
    /// <returns>Array of floats.</returns>
    /// <exception cref="ArgumentException">If the <paramref name="numbersString"/>
    /// is empty or if </exception>
    protected static float[] ParseFloatList(string numbersString)
    {
      string[] numbers = numbersString.Split(new char[] { ',' });
      if (numbers.Length == 0)
        throw new ArgumentException("Empty list");
      float[] result = new float[numbers.Length];
      for (int i = 0; i < numbers.Length; i++)
        result[i] = (float) TypeConverter.Convert(numbers[i], typeof(float));
      return result;
    }

    #endregion
  }
}
