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

namespace Presentation.SkinEngine.ElementRegistrations
{                            
  /// <summary>
  /// This class holds a registration for all elements which can be instanciated
  /// by a XAML file.
  /// </summary>
  public class MseElements
  {
    #region Variables
    /// <summary>
    /// Registration for all elements the loader can create from a XAML file.
    /// </summary>
    protected static IDictionary<string, Type> objectClassRegistrations = new Dictionary<string, Type>();
    static MseElements()
    {                            
      // Panels
      objectClassRegistrations.Add("DockPanel", typeof(SkinEngine.Controls.Panels.DockPanel));
      objectClassRegistrations.Add("StackPanel", typeof(SkinEngine.Controls.Panels.StackPanel));
      objectClassRegistrations.Add("VirtualizingStackPanel", typeof(SkinEngine.Controls.Panels.VirtualizingStackPanel));
      objectClassRegistrations.Add("Canvas", typeof(SkinEngine.Controls.Panels.Canvas));
      objectClassRegistrations.Add("Grid", typeof(SkinEngine.Controls.Panels.Grid));
      objectClassRegistrations.Add("RowDefinition", typeof(SkinEngine.Controls.Panels.RowDefinition));
      objectClassRegistrations.Add("ColumnDefinition", typeof(SkinEngine.Controls.Panels.ColumnDefinition));
      objectClassRegistrations.Add("GridLength", typeof(SkinEngine.Controls.Panels.GridLength));
      objectClassRegistrations.Add("WrapPanel", typeof(SkinEngine.Controls.Panels.WrapPanel));

      // Visuals
      objectClassRegistrations.Add("Border", typeof(SkinEngine.Controls.Visuals.Border));
      objectClassRegistrations.Add("Image", typeof(SkinEngine.Controls.Visuals.Image));
      objectClassRegistrations.Add("Button", typeof(SkinEngine.Controls.Visuals.Button));
      objectClassRegistrations.Add("TextBox", typeof(SkinEngine.Controls.Visuals.TextBox));
      objectClassRegistrations.Add("CheckBox", typeof(SkinEngine.Controls.Visuals.CheckBox));
      objectClassRegistrations.Add("Label", typeof(SkinEngine.Controls.Visuals.Label));
      objectClassRegistrations.Add("ListView", typeof(SkinEngine.Controls.Visuals.ListView));
      objectClassRegistrations.Add("ContentPresenter", typeof(SkinEngine.Controls.Visuals.ContentPresenter));
      objectClassRegistrations.Add("ScrollContentPresenter", typeof(SkinEngine.Controls.Visuals.ScrollContentPresenter));
      objectClassRegistrations.Add("ProgressBar", typeof(SkinEngine.Controls.Visuals.ProgressBar));
      objectClassRegistrations.Add("KeyBinding", typeof(SkinEngine.Controls.Visuals.KeyBinding));
      objectClassRegistrations.Add("TreeView", typeof(SkinEngine.Controls.Visuals.TreeView));
      objectClassRegistrations.Add("TreeViewItem", typeof(SkinEngine.Controls.Visuals.TreeViewItem));
      objectClassRegistrations.Add("ItemsPresenter", typeof(SkinEngine.Controls.Visuals.ItemsPresenter));
      objectClassRegistrations.Add("DataTemplate", typeof(SkinEngine.Controls.Visuals.DataTemplate));
      objectClassRegistrations.Add("StyleSelector", typeof(SkinEngine.Controls.Visuals.StyleSelector));
      objectClassRegistrations.Add("DataTemplateSelector", typeof(SkinEngine.Controls.Visuals.DataTemplateSelector));
      objectClassRegistrations.Add("ScrollViewer", typeof(SkinEngine.Controls.Visuals.ScrollViewer));

      // Resources
      objectClassRegistrations.Add("ResourceDictionary", typeof(SkinEngine.Controls.Resources.ResourceDictionary));
      
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
      objectClassRegistrations.Add("BeginStoryboard", typeof(SkinEngine.Controls.Visuals.Triggers.BeginStoryboard));
      objectClassRegistrations.Add("StopStoryboard", typeof(SkinEngine.Controls.Visuals.Triggers.StopStoryboard));

      // Transforms
      objectClassRegistrations.Add("TransformGroup", typeof(SkinEngine.Controls.Transforms.TransformGroup));
      objectClassRegistrations.Add("ScaleTransform", typeof(SkinEngine.Controls.Transforms.ScaleTransform));
      objectClassRegistrations.Add("SkewTransform", typeof(SkinEngine.Controls.Transforms.SkewTransform));
      objectClassRegistrations.Add("RotateTransform", typeof(SkinEngine.Controls.Transforms.RotateTransform));
      objectClassRegistrations.Add("TranslateTransform", typeof(SkinEngine.Controls.Transforms.TranslateTransform));

      // Styles
      objectClassRegistrations.Add("Style", typeof(SkinEngine.Controls.Visuals.Styles.Style));
      objectClassRegistrations.Add("Setter", typeof(SkinEngine.Controls.Visuals.Styles.Setter));
      objectClassRegistrations.Add("ControlTemplate", typeof(SkinEngine.Controls.Visuals.Styles.ControlTemplate));
      objectClassRegistrations.Add("ItemsPanelTemplate", typeof(SkinEngine.Controls.Visuals.ItemsPanelTemplate));

      // Commands
      objectClassRegistrations.Add("CommandGroup", typeof(SkinEngine.Controls.Bindings.CommandGroup));
      objectClassRegistrations.Add("InvokeCommand", typeof(SkinEngine.Controls.Bindings.InvokeCommand));

      // Include
      objectClassRegistrations.Add("Include", typeof(SkinEngine.Controls.Resources.Include));

      // Markup extensions
      objectClassRegistrations.Add("StaticResource", typeof(SkinEngine.MarkupExtensions.StaticResourceMarkupExtension));
      objectClassRegistrations.Add("Command", typeof(SkinEngine.MarkupExtensions.CommandMarkupExtension));
      objectClassRegistrations.Add("Binding", typeof(SkinEngine.MarkupExtensions.BindingMarkupExtension));
      objectClassRegistrations.Add("TemplateBinding", typeof(SkinEngine.MarkupExtensions.TemplateBindingMarkupExtension));
      objectClassRegistrations.Add("Model", typeof(SkinEngine.MarkupExtensions.GetModelMarkupExtension));
      objectClassRegistrations.Add("Service", typeof(SkinEngine.MarkupExtensions.ServiceScopeMarkupExtension));
      objectClassRegistrations.Add("RelativeSource", typeof(SkinEngine.MarkupExtensions.RelativeSource));
    }

    #endregion

    #region Properties           
    public static IDictionary<string, Type> ObjectClassRegistrations
    { get { return objectClassRegistrations; } }
    #endregion
  }
}
