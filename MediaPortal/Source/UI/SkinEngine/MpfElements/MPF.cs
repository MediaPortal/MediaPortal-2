#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Localization;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.Controls.Brushes;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Transforms;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities;
using SharpDX;
using TypeConverter = MediaPortal.UI.SkinEngine.Xaml.TypeConverter;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes;
using Brush = MediaPortal.UI.SkinEngine.Controls.Brushes.Brush;

namespace MediaPortal.UI.SkinEngine.MpfElements
{
  /// <summary>
  /// This class holds a registration for all elements which can be instantiated  by a XAML file. It also provides
  /// static methods for type conversions between special types and for copying instances.
  /// </summary>
  public class MPF
  {
    protected static readonly IEnumerable<IBinding> EMPTY_BINDING_ENUMERATION = new List<IBinding>();

    #region Variables

    protected static readonly NumberFormatInfo NUMBERFORMATINFO = CultureInfo.InvariantCulture.NumberFormat;

    /// <summary>
    /// Registration for all elements the loader can create from a XAML file.
    /// </summary>
    protected static IDictionary<string, Type> _objectClassRegistrations = new Dictionary<string, Type>();
    static MPF()
    {
      // dependency objects with abstract base classes (needed for Qualified event names in XAML)
      RegisterObjectClasses(typeof(DependencyObject).Assembly, typeof(DependencyObject), true);
      // markup extensions 
      //TODO: add the next line when merged with latest changes as in Weekly
      //RegisterObjectClasses(typeof(MPFExtensionBase).Assembly, typeof(MPFExtensionBase), false);
      // this covers several more types
      RegisterObjectClasses(typeof(ISkinEngineManagedObject).Assembly, typeof(ISkinEngineManagedObject), false);
      // remaining types
      MPF._objectClassRegistrations.Add("Thickness", typeof(Thickness));
      // Custom type "Vector2" to be used as "Point"
      MPF._objectClassRegistrations.Add("Point", typeof(Vector2));

      // uncomment this block to compare automatic class collection with the old manual one. The missing list at the end must be empty.
      /*var _objectClassRegistrations = new Dictionary<string, Type>();

      _objectClassRegistrations.Add("Screen", typeof(SkinEngine.ScreenManagement.Screen));

      // Panels
      _objectClassRegistrations.Add("DockPanel", typeof(SkinEngine.Controls.Panels.DockPanel));
      _objectClassRegistrations.Add("StackPanel", typeof(SkinEngine.Controls.Panels.StackPanel));
      _objectClassRegistrations.Add("VirtualizingStackPanel", typeof(SkinEngine.Controls.Panels.VirtualizingStackPanel));
      _objectClassRegistrations.Add("Canvas", typeof(SkinEngine.Controls.Panels.Canvas));
      _objectClassRegistrations.Add("Grid", typeof(SkinEngine.Controls.Panels.Grid));
      _objectClassRegistrations.Add("RowDefinition", typeof(SkinEngine.Controls.Panels.RowDefinition));
      _objectClassRegistrations.Add("ColumnDefinition", typeof(SkinEngine.Controls.Panels.ColumnDefinition));
      _objectClassRegistrations.Add("GridLength", typeof(SkinEngine.Controls.Panels.GridLength));
      _objectClassRegistrations.Add("WrapPanel", typeof(SkinEngine.Controls.Panels.WrapPanel));
      _objectClassRegistrations.Add("VirtualizingWrapPanel", typeof(SkinEngine.Controls.Panels.VirtualizingWrapPanel));
      _objectClassRegistrations.Add("UniformGrid", typeof(SkinEngine.Controls.Panels.UniformGrid));
      _objectClassRegistrations.Add("StarRatingPanel", typeof(SkinEngine.Controls.Panels.StarRatingPanel));

      // Visuals
      _objectClassRegistrations.Add("ARRetainingControl", typeof(SkinEngine.Controls.Visuals.ARRetainingControl));
      _objectClassRegistrations.Add("BackgroundEffect", typeof(SkinEngine.Controls.Visuals.BackgroundEffect));
      _objectClassRegistrations.Add("Control", typeof(SkinEngine.Controls.Visuals.Control));
      _objectClassRegistrations.Add("ContentControl", typeof(SkinEngine.Controls.Visuals.ContentControl));
      _objectClassRegistrations.Add("Border", typeof(SkinEngine.Controls.Visuals.Border));
      _objectClassRegistrations.Add("GroupBox", typeof(SkinEngine.Controls.Visuals.GroupBox));
      _objectClassRegistrations.Add("Image", typeof(SkinEngine.Controls.Visuals.Image));
      _objectClassRegistrations.Add("Button", typeof(SkinEngine.Controls.Visuals.Button));
      _objectClassRegistrations.Add("RadioButton", typeof(SkinEngine.Controls.Visuals.RadioButton));
      _objectClassRegistrations.Add("CheckBox", typeof(SkinEngine.Controls.Visuals.CheckBox));
      _objectClassRegistrations.Add("Label", typeof(SkinEngine.Controls.Visuals.Label));
      _objectClassRegistrations.Add("ListView", typeof(SkinEngine.Controls.Visuals.ListView));
      _objectClassRegistrations.Add("ListViewItem", typeof(SkinEngine.Controls.Visuals.ListViewItem));
      _objectClassRegistrations.Add("ContentPresenter", typeof(SkinEngine.Controls.Visuals.ContentPresenter));
      _objectClassRegistrations.Add("ScrollContentPresenter", typeof(SkinEngine.Controls.Visuals.ScrollContentPresenter));
      _objectClassRegistrations.Add("ProgressBar", typeof(SkinEngine.Controls.Visuals.ProgressBar));
      _objectClassRegistrations.Add("HeaderedItemsControl", typeof(SkinEngine.Controls.Visuals.HeaderedItemsControl));
      _objectClassRegistrations.Add("TreeView", typeof(SkinEngine.Controls.Visuals.TreeView));
      _objectClassRegistrations.Add("TreeViewItem", typeof(SkinEngine.Controls.Visuals.TreeViewItem));
      _objectClassRegistrations.Add("ItemsPresenter", typeof(SkinEngine.Controls.Visuals.ItemsPresenter));
      _objectClassRegistrations.Add("ScrollViewer", typeof(SkinEngine.Controls.Visuals.ScrollViewer));
      _objectClassRegistrations.Add("TextBox", typeof(SkinEngine.Controls.Visuals.TextBox));
      _objectClassRegistrations.Add("TextControl", typeof(SkinEngine.Controls.Visuals.TextControl));
      _objectClassRegistrations.Add("KeyBinding", typeof(SkinEngine.Controls.Visuals.KeyBinding));
      _objectClassRegistrations.Add("KeyBindingControl", typeof(SkinEngine.Controls.Visuals.KeyBindingControl));
      _objectClassRegistrations.Add("VirtualKeyboardControl", typeof(SkinEngine.Controls.Visuals.VirtualKeyboardControl));
      _objectClassRegistrations.Add("VirtualKeyboardPresenter", typeof(SkinEngine.Controls.Visuals.VirtualKeyboardPresenter));
      _objectClassRegistrations.Add("Thickness", typeof(SkinEngine.Controls.Visuals.Thickness));

      // Image Sources
      _objectClassRegistrations.Add("BitmapImageSource", typeof(SkinEngine.Controls.ImageSources.BitmapImageSource));
      _objectClassRegistrations.Add("MultiImageSource", typeof(SkinEngine.Controls.ImageSources.MultiImageSource));
      _objectClassRegistrations.Add("ImagePlayerImageSource", typeof(SkinEngine.Controls.ImageSources.ImagePlayerImageSource));

      // Brushes
      _objectClassRegistrations.Add("SolidColorBrush", typeof(SkinEngine.Controls.Brushes.SolidColorBrush));
      _objectClassRegistrations.Add("LinearGradientBrush", typeof(SkinEngine.Controls.Brushes.LinearGradientBrush));
      _objectClassRegistrations.Add("RadialGradientBrush", typeof(SkinEngine.Controls.Brushes.RadialGradientBrush));
      _objectClassRegistrations.Add("ImageBrush", typeof(SkinEngine.Controls.Brushes.ImageBrush));
      _objectClassRegistrations.Add("VisualBrush", typeof(SkinEngine.Controls.Brushes.VisualBrush));
      _objectClassRegistrations.Add("VideoBrush", typeof(SkinEngine.Controls.Brushes.VideoBrush));
      _objectClassRegistrations.Add("GradientBrush", typeof(SkinEngine.Controls.Brushes.GradientBrush));
      _objectClassRegistrations.Add("GradientStopCollection", typeof(SkinEngine.Controls.Brushes.GradientStopCollection));
      _objectClassRegistrations.Add("GradientStop", typeof(SkinEngine.Controls.Brushes.GradientStop));

      // Shapes
      _objectClassRegistrations.Add("Rectangle", typeof(SkinEngine.Controls.Visuals.Shapes.Rectangle));
      _objectClassRegistrations.Add("Ellipse", typeof(SkinEngine.Controls.Visuals.Shapes.Ellipse));
      _objectClassRegistrations.Add("Line", typeof(SkinEngine.Controls.Visuals.Shapes.Line));
      _objectClassRegistrations.Add("Polygon", typeof(SkinEngine.Controls.Visuals.Shapes.Polygon));
      _objectClassRegistrations.Add("Path", typeof(SkinEngine.Controls.Visuals.Shapes.Path));
      _objectClassRegistrations.Add("Shape", typeof(SkinEngine.Controls.Visuals.Shapes.Shape));

      // Custom type "Vector2" to be used as "Point"
      _objectClassRegistrations.Add("Point", typeof(SharpDX.Vector2));

      // Animations
      _objectClassRegistrations.Add("ColorAnimation", typeof(SkinEngine.Controls.Animations.ColorAnimation));
      _objectClassRegistrations.Add("DoubleAnimation", typeof(SkinEngine.Controls.Animations.DoubleAnimation));
      _objectClassRegistrations.Add("PointAnimation", typeof(SkinEngine.Controls.Animations.PointAnimation));
      _objectClassRegistrations.Add("Storyboard", typeof(SkinEngine.Controls.Animations.Storyboard));
      _objectClassRegistrations.Add("ColorAnimationUsingKeyFrames", typeof(SkinEngine.Controls.Animations.ColorAnimationUsingKeyFrames));
      _objectClassRegistrations.Add("DoubleAnimationUsingKeyFrames", typeof(SkinEngine.Controls.Animations.DoubleAnimationUsingKeyFrames));
      _objectClassRegistrations.Add("PointAnimationUsingKeyFrames", typeof(SkinEngine.Controls.Animations.PointAnimationUsingKeyFrames));
      _objectClassRegistrations.Add("SplineColorKeyFrame", typeof(SkinEngine.Controls.Animations.SplineColorKeyFrame));
      _objectClassRegistrations.Add("SplineDoubleKeyFrame", typeof(SkinEngine.Controls.Animations.SplineDoubleKeyFrame));
      _objectClassRegistrations.Add("SplinePointKeyFrame", typeof(SkinEngine.Controls.Animations.SplinePointKeyFrame));
      _objectClassRegistrations.Add("ObjectAnimationUsingKeyFrames", typeof(SkinEngine.Controls.Animations.ObjectAnimationUsingKeyFrames));
      _objectClassRegistrations.Add("DiscreteObjectKeyFrame", typeof(SkinEngine.Controls.Animations.DiscreteObjectKeyFrame));

      // Triggers
      _objectClassRegistrations.Add("EventTrigger", typeof(SkinEngine.Controls.Visuals.Triggers.EventTrigger));
      _objectClassRegistrations.Add("Trigger", typeof(SkinEngine.Controls.Visuals.Triggers.Trigger));
      _objectClassRegistrations.Add("DataTrigger", typeof(SkinEngine.Controls.Visuals.Triggers.DataTrigger));
      _objectClassRegistrations.Add("BeginStoryboard", typeof(SkinEngine.Controls.Visuals.Triggers.BeginStoryboard));
      _objectClassRegistrations.Add("StopStoryboard", typeof(SkinEngine.Controls.Visuals.Triggers.StopStoryboard));
      _objectClassRegistrations.Add("TriggerCommand", typeof(SkinEngine.Controls.Visuals.Triggers.TriggerCommand));
      _objectClassRegistrations.Add("SoundPlayerAction", typeof(SkinEngine.Controls.Visuals.Triggers.SoundPlayerAction));

      // Transforms
      _objectClassRegistrations.Add("TransformGroup", typeof(SkinEngine.Controls.Transforms.TransformGroup));
      _objectClassRegistrations.Add("ScaleTransform", typeof(SkinEngine.Controls.Transforms.ScaleTransform));
      _objectClassRegistrations.Add("SkewTransform", typeof(SkinEngine.Controls.Transforms.SkewTransform));
      _objectClassRegistrations.Add("RotateTransform", typeof(SkinEngine.Controls.Transforms.RotateTransform));
      _objectClassRegistrations.Add("TranslateTransform", typeof(SkinEngine.Controls.Transforms.TranslateTransform));
      _objectClassRegistrations.Add("MatrixTransform", typeof(SkinEngine.Controls.Transforms.MatrixTransform));

      // Styles
      _objectClassRegistrations.Add("Style", typeof(SkinEngine.Controls.Visuals.Styles.Style));
      _objectClassRegistrations.Add("Setter", typeof(SkinEngine.Controls.Visuals.Styles.Setter));
      _objectClassRegistrations.Add("BindingSetter", typeof(SkinEngine.Controls.Visuals.Styles.BindingSetter));
      _objectClassRegistrations.Add("ControlTemplate", typeof(SkinEngine.Controls.Visuals.Templates.ControlTemplate));
      _objectClassRegistrations.Add("ItemsPanelTemplate", typeof(SkinEngine.Controls.Visuals.Templates.ItemsPanelTemplate));
      _objectClassRegistrations.Add("DataTemplate", typeof(SkinEngine.Controls.Visuals.Templates.DataTemplate));
      _objectClassRegistrations.Add("DataStringProvider", typeof(SkinEngine.Controls.Visuals.Templates.DataStringProvider));
      _objectClassRegistrations.Add("SubItemsProvider", typeof(SkinEngine.Controls.Visuals.Templates.SubItemsProvider));

      // Resources/wrapper classes
      _objectClassRegistrations.Add("ResourceDictionary", typeof(SkinEngine.MpfElements.Resources.ResourceDictionary));
      _objectClassRegistrations.Add("Include", typeof(SkinEngine.MpfElements.Resources.Include));
      _objectClassRegistrations.Add("LateBoundValue", typeof(SkinEngine.MpfElements.Resources.LateBoundValue));
      _objectClassRegistrations.Add("ResourceWrapper", typeof(SkinEngine.MpfElements.Resources.ResourceWrapper));
      _objectClassRegistrations.Add("BindingWrapper", typeof(SkinEngine.MpfElements.Resources.BindingWrapper));

      // Command
      _objectClassRegistrations.Add("CommandList", typeof(SkinEngine.Commands.CommandList));
      _objectClassRegistrations.Add("InvokeCommand", typeof(SkinEngine.Commands.InvokeCommand));
      _objectClassRegistrations.Add("CommandBridge", typeof(SkinEngine.Commands.CommandBridge));

      // Converters
      _objectClassRegistrations.Add("ReferenceNotNull_BoolConverter", typeof(SkinEngine.MpfElements.Converters.ReferenceNotNull_BoolConverter));
      _objectClassRegistrations.Add("EmptyString2FalseConverter", typeof(SkinEngine.MpfElements.Converters.EmptyString2FalseConverter));
      _objectClassRegistrations.Add("ExpressionMultiValueConverter", typeof(SkinEngine.MpfElements.Converters.ExpressionMultiValueConverter));
      _objectClassRegistrations.Add("ExpressionValueConverter", typeof(SkinEngine.MpfElements.Converters.ExpressionValueConverter));
      _objectClassRegistrations.Add("CommaSeparatedValuesConverter", typeof(SkinEngine.MpfElements.Converters.CommaSeparatedValuesConverter));
      _objectClassRegistrations.Add("DateFormatConverter", typeof(SkinEngine.MpfElements.Converters.DateFormatConverter));
      _objectClassRegistrations.Add("DurationConverter", typeof(SkinEngine.MpfElements.Converters.DurationConverter));
      _objectClassRegistrations.Add("PriorityBindingConverter", typeof(SkinEngine.MpfElements.Converters.PriorityBindingConverter));
      _objectClassRegistrations.Add("StringFormatConverter", typeof(SkinEngine.MpfElements.Converters.StringFormatConverter));

      // Markup extensions
      _objectClassRegistrations.Add("StaticResource", typeof(SkinEngine.MarkupExtensions.StaticResourceExtension));
      _objectClassRegistrations.Add("DynamicResource", typeof(SkinEngine.MarkupExtensions.DynamicResourceExtension));
      _objectClassRegistrations.Add("ThemeResource", typeof(SkinEngine.MarkupExtensions.ThemeResourceExtension));
      _objectClassRegistrations.Add("Binding", typeof(SkinEngine.MarkupExtensions.BindingExtension));
      _objectClassRegistrations.Add("MultiBinding", typeof(SkinEngine.MarkupExtensions.MultiBindingExtension));
      _objectClassRegistrations.Add("TemplateBinding", typeof(SkinEngine.MarkupExtensions.TemplateBindingExtension));
      _objectClassRegistrations.Add("PickupBinding", typeof(SkinEngine.MarkupExtensions.PickupBindingExtension));
      _objectClassRegistrations.Add("Command", typeof(SkinEngine.MarkupExtensions.CommandExtension));
      _objectClassRegistrations.Add("CommandStencil", typeof(SkinEngine.MarkupExtensions.CommandStencilExtension));
      _objectClassRegistrations.Add("Model", typeof(SkinEngine.MarkupExtensions.ModelExtension));
      _objectClassRegistrations.Add("Service", typeof(SkinEngine.MarkupExtensions.ServiceExtension));
      _objectClassRegistrations.Add("Color", typeof(SkinEngine.MarkupExtensions.ColorExtension));

      // Others
      _objectClassRegistrations.Add("RelativeSource", typeof(SkinEngine.MarkupExtensions.RelativeSourceExtension));

      // Effects
      // Image effects based on ImageContext
      _objectClassRegistrations.Add("SimpleImageEffect", typeof(MediaPortal.UI.SkinEngine.Controls.Visuals.Effects.SimpleImageEffect));
      _objectClassRegistrations.Add("ZoomBlurEffect", typeof(MediaPortal.UI.SkinEngine.Controls.Visuals.Effects.ZoomBlurEffect));
      _objectClassRegistrations.Add("PixelateEffect", typeof(MediaPortal.UI.SkinEngine.Controls.Visuals.Effects.PixelateEffect));

      // Generic shader effects based on EffectContext
      _objectClassRegistrations.Add("SimpleShaderEffect", typeof(MediaPortal.UI.SkinEngine.Controls.Visuals.Effects.SimpleShaderEffect));
      // ReSharper restore RedundantNameQualifier

      var missig = new List<string>();
      foreach (var key in _objectClassRegistrations.Keys)
      {
        if (!MPF._objectClassRegistrations.ContainsKey(key))
        {
          missig.Add(key);
        }
      }
      var m = missig;
      */
    }

    #endregion

    #region Public properties

    public static IDictionary<string, Type> ObjectClassRegistrations
    { get { return _objectClassRegistrations; } }

    #endregion

    #region Public methods

    public static bool ConvertCollectionType(object value, Type targetType, out object result)
    {
      result = value;
      if (value == null)
        return true;
      if (targetType == typeof(PointCollection))
      {
        PointCollection coll = new PointCollection();
        string text = value.ToString();
        string[] parts = text.Split(new[] { ',', ' ' });
        for (int i = 0; i < parts.Length; i += 2)
        {
          Point p = new Point(Int32.Parse(parts[i]), Int32.Parse(parts[i + 1]));
          coll.Add(p);
        }
        result = coll;
        return true;
      }
      return false;
    }

    public static bool ConvertType(object value, Type targetType, out object result)
    {
      result = value;
      if (value == null)
        return true;
      if (value is string && targetType == typeof(Type))
      {
        string typeName = (string)value;
        Type type;
        if (!_objectClassRegistrations.TryGetValue(typeName, out type))
          type = Type.GetType(typeName);
        if (type != null)
        {
          result = type;
          return true;
        }
      }
      // Don't convert LateBoundValue (or superclass ValueWrapper) here... instances of
      // LateBoundValue must stay unchanged until some code part explicitly converts them!
      if (value is ResourceWrapper)
      {
        object resource = ((ResourceWrapper)value).Resource;
        if (TypeConverter.Convert(resource, targetType, out result))
        {
          if (ReferenceEquals(resource, result))
          {
            // Resource must be copied because setters and other controls most probably need a copy of the resource.
            // If we don't copy it, Setter is not able to check if we already return a copy because our input value differs
            // from the output value, even if we didn't do a copy here.
            result = MpfCopyManager.DeepCopyCutLVPs(result);
          }
          return true;
        }
      }
      if (value is string && targetType == typeof(FrameworkElement))
      {
        // It doesn't suffice to have an implicit data template declaration which returns a label for a string.
        // If you try to build a ResourceWrapper with a string and assign that ResourceWrapper to a Button's Content property
        // with a StaticResource, for example, the ResourceWrapper will be assigned directly without the data template being
        // applied. To make it sill work, we need this explicit type conversion here.
        result = new Label { Content = (string)value, Color = Color.White };
        return true;
      }
      if (targetType == typeof(Transform))
      {
        string v = value.ToString();
        string[] parts = v.Split(new[] { ',' });
        if (parts.Length == 6)
        {
          float[] f = new float[parts.Length];
          for (int i = 0; i < parts.Length; ++i)
          {
            object obj;
            TypeConverter.Convert(parts[i], typeof(double), out obj);
            f[i] = (float)obj;
          }
          System.Drawing.Drawing2D.Matrix matrix2D = new System.Drawing.Drawing2D.Matrix(f[0], f[1], f[2], f[3], f[4], f[5]);
          Static2dMatrix matrix = new Static2dMatrix();
          matrix.Set2DMatrix(matrix2D);
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
      else if (targetType == typeof(Color))
      {
        Color color;
        if (ColorConverter.ConvertColor(value, out color))
        {
          result = color;
          return true;
        }
      }
      else if (targetType == typeof(Brush) && value is string || value is Color)
      {
        try
        {
          Color color;
          if (ColorConverter.ConvertColor(value, out color))
          {
            SolidColorBrush b = new SolidColorBrush
            {
              Color = color
            };
            result = b;
            return true;
          }
        }
        catch (Exception)
        {
          return false;
        }
      }
      else if (targetType == typeof(GridLength))
      {
        string text = value.ToString();
        if (text == "Auto")
          result = new GridLength(GridUnitType.Auto, 0.0);
        else if (text == "AutoStretch")
          result = new GridLength(GridUnitType.AutoStretch, 1.0);
        else if (text.IndexOf('*') >= 0)
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
            result = new GridLength(GridUnitType.Star, 1.0);
        }
        else
        {
          double v = double.Parse(text);
          result = new GridLength(GridUnitType.Pixel, v);
        }
        return true;
      }
      else if (targetType == typeof(string) && value is IResourceString)
      {
        result = ((IResourceString)value).Evaluate();
        return true;
      }
      else if (targetType.IsAssignableFrom(typeof(IExecutableCommand)) && value is ICommand)
      {
        result = new CommandBridge((ICommand)value);
        return true;
      }
      else if (targetType == typeof(Key) && value is string)
      {
        string str = (string)value;
        // Try a special key
        result = Key.GetSpecialKeyByName(str);
        if (result == null)
          if (str.Length != 1)
            throw new ArgumentException(string.Format("Cannot convert '{0}' to type Key", str));
          else
            result = new Key(str[0]);
        return true;
      }
      else if (targetType == typeof(string) && value is IEnumerable)
      {
        result = StringUtils.Join(", ", (IEnumerable)value);
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
        Vector2 vec = (Vector2)source;
        Vector2 result = new Vector2 { X = vec.X, Y = vec.Y };
        target = result;
        return true;
      }
      if (t == typeof(Vector3))
      {
        Vector3 vec = (Vector3)source;
        Vector3 result = new Vector3 { X = vec.X, Y = vec.Y, Z = vec.Z };
        target = result;
        return true;
      }
      if (t == typeof(Vector4))
      {
        Vector4 vec = (Vector4)source;
        Vector4 result = new Vector4 { X = vec.X, Y = vec.Y, W = vec.W, Z = vec.Z };
        target = result;
        return true;
      }
      if (source is IUnmodifiableResource)
      {
        IUnmodifiableResource resource = (IUnmodifiableResource)source;
        if (resource.Owner != null)
        {
          target = source;
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Method to cleanup resources for callers which don't register themselves as owner.
    /// </summary>
    /// <param name="maybeUIElementOrDisposable">Element to be cleaned up or disposed.</param>
    public static void TryCleanupAndDispose(object maybeUIElementOrDisposable)
    {
      IUnmodifiableResource resource = maybeUIElementOrDisposable as IUnmodifiableResource;
      if (resource != null && resource.Owner != null)
        // Optimize disposal for unmodifiable resources: They are only disposed by their parent ResourceDictionary
        return;
      TryCleanupAndDispose_NoCheckOwner(maybeUIElementOrDisposable);
    }

    protected static void TryCleanupAndDispose_NoCheckOwner(object maybeUIElementOrDisposable)
    {
      if (!(maybeUIElementOrDisposable is ISkinEngineManagedObject))
        // Don't dispose external resources
        return;
      UIElement u = maybeUIElementOrDisposable as UIElement;
      if (u != null)
      {
        u.CleanupAndDispose();
        return;
      }
      IDisposable d = maybeUIElementOrDisposable as IDisposable;
      if (d == null)
        return;
      d.Dispose();
    }

    /// <summary>
    /// Sets the owner of the given resource to the given <paramref name="owner"/>, if the given resource implements the
    /// <see cref="IUnmodifiableResource"/> interface. The owner is only set if no owner is set yet, except if <paramref name="force"/> is
    /// set to <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Containers like <see cref="ResourceDictionary"/> or <see cref="Setter"/> set themselves as owner of their contents.
    /// That has two implications:
    /// <list type="bullet">
    /// <item>The owner of a resource is responsible for the disposal of the resource. A resource with an owner cannot live longer than its owner.</item>
    /// <item>A resource with an owner is not copied. Instead, the <see cref="CopyMpfObject"/> method will return the original reference.</item>
    /// </list>
    /// That works only for resources which implement the <see cref="IUnmodifiableResource"/> interface. Those resources are unmodifiable, i.e.
    /// they are not "personalized" to their owner so it is safe to reuse their reference.
    /// So if a container sets itself as owner of its contents, it can optimize the performance if the contents are very often of type
    /// <see cref="IUnmodifiableResource"/>.
    /// </remarks>
    /// <param name="res">Resource to set the owner.</param>
    /// <param name="owner">Owner to be set.</param>
    /// <param name="force">If set to <c>false</c> and the <paramref name="res">resource</paramref> has already an owner, nothing happens.
    /// Else, the owner of the resource will be set.</param>
    public static void SetOwner(object res, object owner, bool force)
    {
      IUnmodifiableResource resource = res as IUnmodifiableResource;
      if (resource != null && (resource.Owner == null || force))
        resource.Owner = owner;
    }

    /// <summary>
    /// Method to cleanup resources for callers which register themselves as owner.
    /// </summary>
    /// <param name="res">Element to be cleaned up or disposed.</param>
    /// <param name="checkOwner">Owner reference to check for. This method will only clean up the given element if the
    /// specified <paramref name="checkOwner"/> is the owner of the given element or if the element doesn't have an owner.</param>
    public static void CleanupAndDisposeResourceIfOwner(object res, object checkOwner)
    {
      IUnmodifiableResource resource = res as IUnmodifiableResource;
      if (resource == null || resource.Owner == null || ReferenceEquals(resource.Owner, checkOwner))
        TryCleanupAndDispose_NoCheckOwner(res);
    }

    #endregion

    #region Private/protected methods

    private static void RegisterObjectClasses(Assembly assembly, Type baseType, bool addAbstractClasses)
    {
      var baseTypeFullName = baseType.FullName;
      foreach (var type in assembly.GetTypes())
      {
        if (type.IsClass &&
            (addAbstractClasses || !type.IsAbstract) &&
            (
              type == baseType ||
              (baseType.IsClass && type.IsSubclassOf(baseType)) ||
              (baseType.IsInterface && type.GetInterface(baseTypeFullName) != null)
            )
          )
        {
          var name = type.Name;
          if (name.EndsWith("Extension") && !type.IsAbstract)
          {
            name = name.Substring(0, name.Length - 9);
          }
          if (!_objectClassRegistrations.ContainsKey(name))
          {
            _objectClassRegistrations.Add(name, type);
          }
        }
      }
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
      string[] coords = coordsString.Split(new[] { ',' });
      object obj;
      if (coords.Length > 0)
      {
        TypeConverter.Convert(coords[0], typeof(float), out obj);
        vec.X = (float)obj;
      }
      if (coords.Length > 1)
      {
        TypeConverter.Convert(coords[1], typeof(float), out obj);
        vec.Y = (float)obj;
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
      string[] coords = coordsString.Split(new[] { ',' });
      object obj;
      if (coords.Length > 0)
      {
        TypeConverter.Convert(coords[0], typeof(float), out obj);
        vec.X = (float)obj;
      }
      if (coords.Length > 1)
      {
        TypeConverter.Convert(coords[1], typeof(float), out obj);
        vec.Y = (float)obj;
      }
      if (coords.Length > 2)
      {
        TypeConverter.Convert(coords[2], typeof(float), out obj);
        vec.Z = (float)obj;
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
      string[] coords = coordsString.Split(new[] { ',' });
      object obj;
      if (coords.Length > 0)
      {
        TypeConverter.Convert(coords[0], typeof(float), out obj);
        vec.X = (float)obj;
      }
      if (coords.Length > 1)
      {
        TypeConverter.Convert(coords[1], typeof(float), out obj);
        vec.Y = (float)obj;
      }
      if (coords.Length > 2)
      {
        TypeConverter.Convert(coords[2], typeof(float), out obj);
        vec.Z = (float)obj;
      }
      if (coords.Length > 3)
      {
        TypeConverter.Convert(coords[3], typeof(float), out obj);
        vec.W = (float)obj;
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
      string[] numbers = numbersString.Split(new[] { ',' });
      if (numbers.Length == 0)
        throw new ArgumentException("Empty list");
      float[] result = new float[numbers.Length];
      for (int i = 0; i < numbers.Length; i++)
        result[i] = (float)TypeConverter.Convert(numbers[i], typeof(float));
      return result;
    }

    #endregion
  }
}
