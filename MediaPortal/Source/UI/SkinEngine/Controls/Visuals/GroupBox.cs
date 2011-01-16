#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Drawing;
using System.Drawing.Drawing2D;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
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

    protected AbstractProperty _headerProperty;
    protected AbstractProperty _headerColorProperty;
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
      _headerProperty = new SProperty(typeof(string), string.Empty);
      _headerColorProperty = new SProperty(typeof(Color), Color.White);
      _headerLabel = new Label {VisualParent = this};
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
      Header = gb.Header;
      HeaderColor = gb.HeaderColor;
      InitializeHeaderLabel();

      Attach();
    }

    #endregion

    void OnHeaderChanged(AbstractProperty prop, object oldValue)
    {
      InitializeHeaderLabel();
      InvalidateLayout(true, true);
    }

    protected void InitializeHeaderLabel()
    {
      _headerLabel.Content = Header;
      _headerLabel.Color = HeaderColor;
    }

    #region Properties

    public AbstractProperty HeaderProperty
    {
      get { return _headerProperty; }
    }

    public string Header
    {
      get { return (string) _headerProperty.GetValue(); }
      set { _headerProperty.SetValue(value); }
    }

    public AbstractProperty HeaderColorProperty
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
      float halfLabel = _headerLabel.DesiredSize.Height/2;
      result.Top = halfLabel + Math.Max(halfLabel, result.Top);
      return result;
    }

    protected override void MeasureBorder(SizeF totalSize)
    {
      const float realHeaderInset = HEADER_INSET_LINE + HEADER_INSET_SPACE;
      float borderInsetX = GetBorderInsetX();
      SizeF headerSize = new SizeF(totalSize.Width - (borderInsetX + realHeaderInset) * 2, totalSize.Height);
      _headerLabel.Measure(ref headerSize);
      base.MeasureBorder(totalSize);
    }

    protected override void ArrangeBorder(RectangleF finalRect)
    {
      float totalHeaderLabelHeight = _headerLabel.DesiredSize.Height;
      float halfLabelHeight = totalHeaderLabelHeight/2;
      RectangleF borderRect = new RectangleF(finalRect.X, finalRect.Y + halfLabelHeight,
          finalRect.Width, finalRect.Height - halfLabelHeight);
      base.ArrangeBorder(borderRect);
      const float realHeaderInset = HEADER_INSET_LINE + HEADER_INSET_SPACE;
      float borderInsetX = GetBorderInsetX();
      _headerLabelRect = new RectangleF(
          finalRect.X + borderInsetX + realHeaderInset, finalRect.Y,
          finalRect.Width - (borderInsetX + realHeaderInset) * 2, totalHeaderLabelHeight);
      if (_headerLabelRect.Width < 0)
        _headerLabelRect.Width = 0;
      if (_headerLabelRect.Height > finalRect.Height)
        _headerLabelRect.Height = finalRect.Height;

      _headerLabel.Arrange(_headerLabelRect);
    }

    protected override GraphicsPath CreateBorderRectPath(RectangleF baseRect)
    {
      SizeF desiredSize = _headerLabel.DesiredSize;
      baseRect.Y += desiredSize.Height/2;
      return GraphicsPathHelper.CreateRoundedRectWithTitleRegionPath(baseRect,
          (float) CornerRadius, (float) CornerRadius, true, HEADER_INSET_LINE,
          desiredSize.Width + HEADER_INSET_SPACE * 2);
    }

    public override void DoRender(RenderContext localRenderContext)
    {
      base.DoRender(localRenderContext);
      _headerLabel.Render(localRenderContext);
    }
  }
}
