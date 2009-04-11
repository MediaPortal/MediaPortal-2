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

using System.Drawing;
using System.Drawing.Drawing2D;
using MediaPortal.Presentation.DataObjects;
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
      _headerLabel = new Label();
      _headerLabel.VisualParent = this;
    }

    void Attach()
    {
      _headerProperty.Attach(OnHeaderChanged);
    }

    void Detach()
    {
      _headerProperty.Detach(OnHeaderChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      GroupBox gb = (GroupBox) source;
      Header = copyManager.GetCopy(gb.Header);

      Attach();
    }

    #endregion

    void OnHeaderChanged(Property prop, object oldValue)
    {
      _headerLabel.Content = Header;
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

    #endregion

    public override void AddChildren(System.Collections.Generic.ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      childrenOut.Add(_headerLabel);
    }

    public override void Measure(ref SizeF totalSize)
    {
      base.Measure(ref totalSize);
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      float borderInset = GetBorderInset();
      SizeF headerSize = new SizeF(totalSize.Width - (borderInset + HEADER_INSET_LINE + HEADER_INSET_SPACE) * 2,
          totalSize.Height);
      _headerLabel.Measure(ref headerSize);
      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();
    }

    public override void Arrange(RectangleF finalRect)
    {
      RectangleF borderRect = new RectangleF(finalRect.X, finalRect.Y + _headerLabel.DesiredSize.Height / 2,
          finalRect.Width, finalRect.Height - _headerLabel.DesiredSize.Height / 2);
      base.Arrange(borderRect);
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      float borderInset = GetBorderInset();
      _headerLabelRect = new RectangleF(finalRect.X + borderInset + HEADER_INSET_LINE + HEADER_INSET_SPACE, finalRect.Y,
          finalRect.Width - (borderInset + HEADER_INSET_LINE + HEADER_INSET_SPACE) * 2, _headerLabel.DesiredSize.Height);
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
      return GraphicsPathHelper.CreateRoundedRectWithTitleRegionPath(baseRect,
          (float) CornerRadius, (float) CornerRadius, true,
          HEADER_INSET_LINE, _headerLabel.DesiredSize.Width + HEADER_INSET_SPACE * 2, layoutTransform);
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