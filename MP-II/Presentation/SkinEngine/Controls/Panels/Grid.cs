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
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Drawing;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Rendering;
using Rectangle = System.Drawing.Rectangle;

namespace Presentation.SkinEngine.Controls.Panels
{
  public class Grid : Panel
  {
    Property _rowDefinitionsProperty;
    Property _columnDefinitionsProperty;

    #region ctor
    public Grid()
    {
      Init();
    }
    public Grid(Grid v)
      : base(v)
    {
      Init();
      foreach (RowDefinition row in v.RowDefinitions)
      {
        RowDefinitions.Add(row);
      }

      foreach (ColumnDefinition row in v.ColumnDefinitions)
      {
        ColumnDefinitions.Add(row);
      }
    }

    public override object Clone()
    {
      return new Grid(this);
    }

    void Init()
    {
      _rowDefinitionsProperty = new Property(new RowDefinitionsCollection());
      _columnDefinitionsProperty = new Property(new ColumnDefinitionsCollection());
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the row definitions property.
    /// </summary>
    /// <value>The row definitions property.</value>
    public Property RowDefinitionsProperty
    {
      get
      {
        return _rowDefinitionsProperty;
      }
      set
      {
        _rowDefinitionsProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the row definitions.
    /// </summary>
    /// <value>The row definitions.</value>
    public RowDefinitionsCollection RowDefinitions
    {
      get
      {
        return _rowDefinitionsProperty.GetValue() as RowDefinitionsCollection;
      }
    }

    /// <summary>
    /// Gets or sets the column definitions property.
    /// </summary>
    /// <value>The column definitions property.</value>
    public Property ColumnDefinitionsProperty
    {
      get
      {
        return _columnDefinitionsProperty;
      }
      set
      {
        _columnDefinitionsProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the column definitions.
    /// </summary>
    /// <value>The column definitions.</value>
    public ColumnDefinitionsCollection ColumnDefinitions
    {
      get
      {
        return _columnDefinitionsProperty.GetValue() as ColumnDefinitionsCollection;
      }
    }
    #endregion

    #region measure & arrange
    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(SizeF availableSize)
    {
      float marginWidth = (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      float marginHeight = (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
      _desiredSize = new System.Drawing.SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);
      if (Width <= 0)
        _desiredSize.Width = (float)(availableSize.Width - marginWidth);
      if (Height <= 0)
        _desiredSize.Height = (float)(availableSize.Height - marginHeight);

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      double w = _desiredSize.Width;
      double h = _desiredSize.Height;

      if (ColumnDefinitions.Count == 0)
        ColumnDefinitions.Add(new ColumnDefinition());
      if (RowDefinitions.Count == 0)
        RowDefinitions.Add(new RowDefinition());
      ColumnDefinitions.SetAvailableSize(w);
      RowDefinitions.SetAvailableSize(h);

      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) continue;
        int col = child.Column;
        int row = child.Row;
        if (col >= ColumnDefinitions.Count) col = ColumnDefinitions.Count - 1;
        if (col < 0) col = 0;
        if (row >= RowDefinitions.Count) row = RowDefinitions.Count - 1;
        if (row < 0) row = 0;

        child.Measure(new SizeF(ColumnDefinitions.GetWidth(col, child.ColumnSpan), RowDefinitions.GetHeight(row, child.RowSpan)));

        ColumnDefinitions.SetWidth(col, child.ColumnSpan, child.DesiredSize.Width);
        RowDefinitions.SetHeight(row, child.RowSpan, child.DesiredSize.Height);
      }


      _desiredSize.Width = (float)ColumnDefinitions.TotalWidth;
      _desiredSize.Height = (float)RowDefinitions.TotalHeight;

      if (Width > 0) _desiredSize.Width = (float)Width * SkinContext.Zoom.Width;
      if (Height > 0) _desiredSize.Height = (float)Height * SkinContext.Zoom.Height;


      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      _desiredSize.Width += marginWidth;
      _desiredSize.Height += marginHeight;
      _originalSize = _desiredSize;

      float d = (float)Math.Abs(_desiredSize.Width - 480.0 * SkinContext.Zoom.Width);

      base.Measure(availableSize);
      //Trace.WriteLine(String.Format("Grid.measure :{0} {1}x{2} returns {3}x{4}", this.Name, (int)availableSize.Width, (int)availableSize.Height, (int)_desiredSize.Width, (int)_desiredSize.Height));
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("Grid.arrange :{0} X {1}, Y {2} W{3}x H{4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));
      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X * SkinContext.Zoom.Width);
      layoutRect.Y += (float)(Margin.Y * SkinContext.Zoom.Height);
      layoutRect.Width -= (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      layoutRect.Height -= (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (ColumnDefinitions.Count == 0)
        ColumnDefinitions.Add(new ColumnDefinition());
      if (RowDefinitions.Count == 0)
        RowDefinitions.Add(new RowDefinition());

      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) continue;
        int col = child.Column;
        int row = child.Row;
        if (col >= ColumnDefinitions.Count) col = ColumnDefinitions.Count - 1;
        if (col < 0) col = 0;
        if (row >= RowDefinitions.Count) row = RowDefinitions.Count - 1;
        if (row < 0) row = 0;

        PointF p = new PointF((float)(this.ActualPosition.X + ColumnDefinitions.GetOffset(col)), (float)(this.ActualPosition.Y + RowDefinitions.GetOffset(row)));
        float w = ColumnDefinitions.GetWidth(col, child.ColumnSpan);
        float h = RowDefinitions.GetHeight(row, child.RowSpan);
        ArrangeChild(child, ref p, w, h);

        child.Arrange(new RectangleF(p, child.DesiredSize));
      }
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
        if (Window!=null) Window.Invalidate(this);
      }
      base.Arrange(layoutRect);
    }

    protected void ArrangeChild(FrameworkElement child, ref System.Drawing.PointF p, double widthPerCell, double heightPerCell)
    {
      if (VisualParent == null) return;

      if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center)
      {

        p.X += (float)((widthPerCell - child.DesiredSize.Width) / 2);
      }
      else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
      {
        p.X += (float)(widthPerCell - child.DesiredSize.Width);
      }
      if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
      {
        p.Y += (float)((heightPerCell - child.DesiredSize.Height) / 2);
      }
      else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
      {
        p.Y += (float)(heightPerCell - child.DesiredSize.Height);
      }
    }
    #endregion
  }
}

