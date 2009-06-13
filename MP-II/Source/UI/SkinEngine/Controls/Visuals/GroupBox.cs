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
using System.Drawing;
using System.Drawing.Drawing2D;
using MediaPortal.Core.General;
using MediaPortal.SkinEngine.DirectX.Triangulate;
using MediaPortal.SkinEngine.SkinManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class GroupBox : Border
  {
    #region Consts

    /// <summary>
    /// Inset of the title between the border ends.
    /// </summary>
    public const float HEADER_INSET_LINE = 20f;
    public const float HEADER_INSET_SPACE = 10f;

    #endregion

    #region Protected fields

    protected Property _headerProperty;
    protected Property _headerColorProperty;
    protected Label _headerLabel;
    protected RectangleF _headerLabelRect;
    
    #endregion

    #region Ctor

    public GroupBox()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _headerProperty = new Property(typeof(string), string.Empty);
      _headerColorProperty = new Property(typeof(Color), Color.White);
      _headerLabel = new Label();
      _headerLabel.VisualParent = this;
    }

    void Attach()
    {
      _headerProperty.Attach(OnHeaderChanged);
      _headerColorProperty.Attach(OnHeaderChanged);
    }

    void Detach()
    {
      _headerProperty.Detach(OnHeaderChanged);
      _headerColorProperty.Detach(OnHeaderChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      GroupBox gb = (GroupBox) source;
      Header = copyManager.GetCopy(gb.Header);
      HeaderColor = copyManager.GetCopy(gb.HeaderColor);

      Attach();
    }

    #endregion

    void OnHeaderChanged(Property prop, object oldValue)
    {
      _headerLabel.Content = Header;
      _headerLabel.Color = HeaderColor;
      Invalidate();
    }

    #region Properties

    public Property HeaderProperty
    {
      get { return _headerProperty; }
    }

    public string Header
    {
      get { return (string) _headerProperty.GetValue(); }
      set { _headerProperty.SetValue(value); }
    }

    public Property HeaderColorProperty
    {
      get { return _headerColorProperty; }
    }

    public Color HeaderColor
    {
      get { return (Color) _headerColorProperty.GetValue(); }
      set { _headerColorProperty.SetValue(value); }
    }

    #endregion

    public override void AddChildren(System.Collections.Generic.ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      childrenOut.Add(_headerLabel);
    }

    protected override Thickness GetTotalBorderMargin()
    {
      Thickness result = base.GetTotalBorderMargin();
      float halfLabel = _headerLabel.TotalDesiredSize().Height/(2*SkinContext.Zoom.Height); // Value has to be adjusted by zoom because the label returns a zoomed value
      result.Top = halfLabel + Math.Max(halfLabel, result.Top);
      return result;
    }

    protected override void MeasureBorder(SizeF totalSize)
    {
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      float realHeaderInset = (HEADER_INSET_LINE + HEADER_INSET_SPACE)*SkinContext.Zoom.Width;
      float borderInsetX = GetBorderInsetX();
      SizeF headerSize = new SizeF(totalSize.Width - (borderInsetX + realHeaderInset) * 2, totalSize.Height);
      _headerLabel.Measure(ref headerSize);
      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();
      base.MeasureBorder(totalSize);
    }

    protected override void ArrangeBorder(RectangleF finalRect)
    {
      float totalHeaderLabelHeight = _headerLabel.TotalDesiredSize().Height;
      float halfLabelHeight = totalHeaderLabelHeight/2;
      RectangleF borderRect = new RectangleF(finalRect.X, finalRect.Y + halfLabelHeight,
          finalRect.Width, finalRect.Height - halfLabelHeight);
      base.ArrangeBorder(borderRect);
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      float realHeaderInset = (HEADER_INSET_LINE + HEADER_INSET_SPACE)*SkinContext.Zoom.Width;
      float borderInsetX = GetBorderInsetX();
      _headerLabelRect = new RectangleF(
          finalRect.X + borderInsetX + realHeaderInset, finalRect.Y,
          finalRect.Width - (borderInsetX + realHeaderInset) * 2, totalHeaderLabelHeight);
      if (_headerLabelRect.Width < 0)
        _headerLabelRect.Width = 0;
      if (_headerLabelRect.Height > finalRect.Height)
        _headerLabelRect.Height = finalRect.Height;

      _headerLabel.Arrange(_headerLabelRect);
      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();
    }

    protected override GraphicsPath CreateBorderRectPath(RectangleF baseRect)
    {
      ExtendedMatrix layoutTransform = _finalLayoutTransform ?? new ExtendedMatrix();
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        layoutTransform = layoutTransform.Multiply(em);
      }
      baseRect.Y += _headerLabel.DesiredSize.Height/2;
      return GraphicsPathHelper.CreateRoundedRectWithTitleRegionPath(baseRect,
          (float) CornerRadius * SkinContext.Zoom.Width, (float) CornerRadius * SkinContext.Zoom.Width, true,
          HEADER_INSET_LINE * SkinContext.Zoom.Width,
          _headerLabel.DesiredSize.Width + HEADER_INSET_SPACE * SkinContext.Zoom.Width * 2, layoutTransform);
    }

    public override void DoRender()
    {
      base.DoRender();

      SkinContext.AddOpacity(Opacity);
      _headerLabel.Render();
      SkinContext.RemoveOpacity();
    }

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      base.DoBuildRenderTree();
      _headerLabel.BuildRenderTree();
    }

    public override void DestroyRenderTree()
    {
      base.DestroyRenderTree();
      _headerLabel.DestroyRenderTree();
    }

  }
}