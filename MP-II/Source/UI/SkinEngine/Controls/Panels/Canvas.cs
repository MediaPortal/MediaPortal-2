#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnrenu.org/licenses/>.
*/

#endregion

using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  /// <summary>
  /// Defines an area within which you can explicitly position child elements by using
  /// coordinates that are relative to the Canvas area.
  /// </summary>
  public class Canvas : Panel
  {
    protected const string LEFT_ATTACHED_PROPERTY = "Canvas.Left";
    protected const string RIGHT_ATTACHED_PROPERTY = "Canvas.Right";
    protected const string TOP_ATTACHED_PROPERTY = "Canvas.Top";
    protected const string BOTTOM_ATTACHED_PROPERTY = "Canvas.Bottom";

    protected override SizeF CalculateDesiredSize(SizeF totalSize)
    {
      RectangleF rect = new RectangleF(0, 0, 0, 0);
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible)
          continue;
        SizeF childSize = new SizeF(totalSize.Width, totalSize.Height);
        child.Measure(ref childSize);
        rect = RectangleF.Union(rect, new RectangleF(new PointF((float) GetLeft(child), (float) GetTop(child)),
            new SizeF(childSize.Width, childSize.Height)));
      }

      return new SizeF(rect.Right, rect.Bottom);
    }

    protected override void ArrangeOverride(RectangleF finalRect)
    {
      base.ArrangeOverride(finalRect);
      float x = finalRect.Location.X;
      float y = finalRect.Location.Y;

      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible)
          continue;
        // Get the coordinates relative to the canvas area.
        PointF point = new PointF(((float) GetLeft(child) * SkinContext.Zoom.Width),
                                  ((float) GetTop(child) * SkinContext.Zoom.Height));

        SkinContext.FinalLayoutTransform.TransformPoint(ref point);
        point.X += x;
        point.Y += y;

        // Get the child size
        SizeF childSize = child.DesiredSize;

        // Arrange the child
        child.Arrange(new RectangleF(point, childSize));
      }
      UpdateRenderOrder();
    }

    #region Attached properties

    /// <summary>
    /// Getter method for the attached property <c>Left</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>Left</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static double GetLeft(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<double>(LEFT_ATTACHED_PROPERTY, 0.0);
    }

    /// <summary>
    /// Setter method for the attached property <c>Left</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>Left</c> property on the
    /// <paramref name="targetObject"/> to be set.</returns>
    public static void SetLeft(DependencyObject targetObject, double value)
    {
      targetObject.SetAttachedPropertyValue<double>(LEFT_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>Left</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>Left</c> property.</returns>
    public static AbstractProperty GetLeftAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<double>(LEFT_ATTACHED_PROPERTY, 0.0);
    }

    /// <summary>
    /// Getter method for the attached property <c>Right</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>Right</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static double GetRight(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<double>(RIGHT_ATTACHED_PROPERTY, 0.0);
    }

    /// <summary>
    /// Setter method for the attached property <c>Right</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>Right</c> property on the
    /// <paramref name="targetObject"/> to be set.</returns>
    public static void SetRight(DependencyObject targetObject, double value)
    {
      targetObject.SetAttachedPropertyValue<double>(RIGHT_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>Right</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>Right</c> property.</returns>
    public static AbstractProperty GetRightAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<double>(RIGHT_ATTACHED_PROPERTY, 0.0);
    }

    /// <summary>
    /// Getter method for the attached property <c>Top</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>Top</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static double GetTop(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<double>(TOP_ATTACHED_PROPERTY, 0.0);
    }

    /// <summary>
    /// Setter method for the attached property <c>Top</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>Top</c> property on the
    /// <paramref name="targetObject"/> to be set.</returns>
    public static void SetTop(DependencyObject targetObject, double value)
    {
      targetObject.SetAttachedPropertyValue<double>(TOP_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>Top</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>Top</c> property.</returns>
    public static AbstractProperty GetTopAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<double>(TOP_ATTACHED_PROPERTY, 0.0);
    }

    /// <summary>
    /// Getter method for the attached property <c>Bottom</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>Bottom</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static double GetBottom(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<double>(BOTTOM_ATTACHED_PROPERTY, 0.0);
    }

    /// <summary>
    /// Setter method for the attached property <c>Bottom</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>Bottom</c> property on the
    /// <paramref name="targetObject"/> to be set.</returns>
    public static void SetBottom(DependencyObject targetObject, double value)
    {
      targetObject.SetAttachedPropertyValue<double>(BOTTOM_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>Bottom</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>Bottom</c> property.</returns>
    public static AbstractProperty GetBottomAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<double>(BOTTOM_ATTACHED_PROPERTY, 0.0);
    }

    #endregion
  }
}
