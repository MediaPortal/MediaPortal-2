#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Collections;
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals;
using Rectangle = System.Drawing.Rectangle;

namespace SkinEngine.Controls.Panels
{
  public class Grid : Panel
  {
    Property _rowDefinitionsProperty;
    Property _columnDefinitionsProperty;

    double[] _colWidth;
    double[] _rowHeight;
    double[] _colOffset;
    double[] _rowOffset;

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

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(Size availableSize)
    {
      _desiredSize = new System.Drawing.Size((int)Width, (int)Height);
      if (Width == 0)
        _desiredSize.Width = (int)availableSize.Width - (int)(Margin.X + Margin.W);
      if (Height == 0)
        _desiredSize.Height = (int)availableSize.Height - (int)(Margin.Y + Margin.Z);

      double w = _desiredSize.Width;
      double h = _desiredSize.Height;


      if (ColumnDefinitions.Count == 0)
        ColumnDefinitions.Add(new ColumnDefinition());
      if (RowDefinitions.Count == 0)
        RowDefinitions.Add(new RowDefinition());
      _colOffset = new double[ColumnDefinitions.Count];
      _rowOffset = new double[RowDefinitions.Count];
      _colWidth = new double[ColumnDefinitions.Count];
      _rowHeight = new double[RowDefinitions.Count];
      foreach (FrameworkElement child in Children)
      {
        int col = child.Column;
        int row = child.Row;
        if (col >= ColumnDefinitions.Count) col = ColumnDefinitions.Count - 1;
        if (col < 0) col = 0;
        if (row >= RowDefinitions.Count) row = RowDefinitions.Count - 1;
        if (row < 0) row = 0;
        double widthPerCell = ((ColumnDefinition)(ColumnDefinitions[col])).Width.GetLength(w, ColumnDefinitions.Count);
        double heightPerCell = ((RowDefinition)(RowDefinitions[row])).Height.GetLength(h, RowDefinitions.Count);
        widthPerCell *= child.ColumnSpan;
        heightPerCell *= child.RowSpan;

        child.Measure(new Size((int)widthPerCell, (int)heightPerCell));

        float cw = child.DesiredSize.Width;
        cw /= ((float)child.ColumnSpan);
        if (child.DesiredSize.Width > _colWidth[col])
        {
          for (int i = 0; i < child.ColumnSpan; ++i)
            _colWidth[col + i] = cw;
        }
        float ch = child.DesiredSize.Height;
        ch /= ((float)child.RowSpan);
        if (child.DesiredSize.Height > _rowHeight[row])
        {
          for (int i = 0; i < child.RowSpan; ++i)
            _rowHeight[col + i] = ch;
        }

      }
      double totalWidth = 0;
      double totalHeight = 0;
      for (int i = 0; i < RowDefinitions.Count; ++i)
      {
        _rowOffset[i] = totalHeight;
        totalHeight += _rowHeight[i];
      }
      for (int i = 0; i < ColumnDefinitions.Count; ++i)
      {
        _colOffset[i] = totalWidth;
        totalWidth += _colWidth[i];
      }
      foreach (FrameworkElement child in Children)
      {
        int col = child.Column;
        int row = child.Row;
        if (col >= ColumnDefinitions.Count) col = ColumnDefinitions.Count - 1;
        if (col < 0) col = 0;
        if (row >= RowDefinitions.Count) row = RowDefinitions.Count - 1;
        if (row < 0) row = 0;
        child.Measure(new Size((int)(_colWidth[col] * child.ColumnSpan), (int)(_rowHeight[row] * child.RowSpan)));
      }
      _desiredSize.Width = (int)totalWidth;
      _desiredSize.Height = (int)totalHeight;

      if (Width > 0) _desiredSize.Width = (int)Width;
      if (Height > 0) _desiredSize.Height = (int)Height;
      _desiredSize.Width += (int)(Margin.X + Margin.W);
      _desiredSize.Height += (int)(Margin.Y + Margin.Z);
      base.Measure(availableSize);
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(Rectangle finalRect)
    {
      _availablePoint = new Point(finalRect.Location.X, finalRect.Location.Y);
      Rectangle layoutRect = new Rectangle(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (int)(Margin.X);
      layoutRect.Y += (int)(Margin.Y);
      layoutRect.Width -= (int)(Margin.X + Margin.W);
      layoutRect.Height -= (int)(Margin.Y + Margin.Z);
      ActualPosition = new Microsoft.DirectX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;

      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) continue;
        int col = child.Column;
        int row = child.Row;
        if (col >= ColumnDefinitions.Count) col = ColumnDefinitions.Count - 1;
        if (col < 0) col = 0;
        if (row >= RowDefinitions.Count) row = RowDefinitions.Count - 1;
        if (row < 0) row = 0;
        Point p = new Point((int)(this.ActualPosition.X + _colOffset[col]), (int)(this.ActualPosition.Y + _rowOffset[row]));
        ArrangeChild(child, ref p, (_colWidth[col] * child.ColumnSpan), (_rowHeight[row] * child.RowSpan));

        child.Arrange(new Rectangle(p, child.DesiredSize));
      }
      base.PerformLayout();
      base.Arrange(layoutRect);
    }

    protected void ArrangeChild(FrameworkElement child, ref System.Drawing.Point p, double widthPerCell, double heightPerCell)
    {
      if (VisualParent == null) return;

      if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center)
      {
        p.X += (int)((widthPerCell - child.DesiredSize.Width) / 2);
      }
      else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
      {
        p.X += (int)(widthPerCell - child.DesiredSize.Width);
      }
      if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
      {
        p.Y += (int)((heightPerCell - child.DesiredSize.Height) / 2);
      }
      else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
      {
        p.Y += (int)(heightPerCell - child.DesiredSize.Height);
      }
    }
  }
}

