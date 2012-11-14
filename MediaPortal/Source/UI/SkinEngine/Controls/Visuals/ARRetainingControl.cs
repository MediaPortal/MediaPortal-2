#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class ARRetainingControl : FrameworkElement
  {
    #region Protected fields

    protected AbstractProperty _contentProperty;
    protected AbstractProperty _aspectRatioProperty;

    #endregion

    #region Ctor

    public ARRetainingControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _contentProperty = new SProperty(typeof(FrameworkElement), null);
      _aspectRatioProperty = new SProperty(typeof(float), 1.0f);
    }

    void Attach()
    {
      _contentProperty.Attach(OnCompleteLayoutGetsInvalid);
      _aspectRatioProperty.Attach(OnCompleteLayoutGetsInvalid);
    }

    void Detach()
    {
      _contentProperty.Detach(OnCompleteLayoutGetsInvalid);
      _aspectRatioProperty.Detach(OnCompleteLayoutGetsInvalid);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ARRetainingControl c = (ARRetainingControl) source;
      Content = copyManager.GetCopy(c.Content);
      AspectRatio = c.AspectRatio;
      Attach();
    }

    #endregion

    public AbstractProperty AspectRatioProperty
    {
      get { return _aspectRatioProperty; }
    }

    public float AspectRatio
    {
      get { return (float) _aspectRatioProperty.GetValue(); }
      set { _aspectRatioProperty.SetValue(value); }
    }

    public AbstractProperty ContentProperty
    {
      get { return _contentProperty; }
    }

    public FrameworkElement Content
    {
      get { return (FrameworkElement) _contentProperty.GetValue(); }
      set { _contentProperty.SetValue(value); }
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      // Calculate constraints
      SizeF result = new SizeF(totalSize.Width, totalSize.Height);
      float desiredWidthFromHeight = result.Height * AspectRatio;
      if (result.Width < desiredWidthFromHeight)
        // Adapt height
        result.Height = result.Width / AspectRatio;
      else
        // Adapt width
        result.Width = desiredWidthFromHeight;
      return result;
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      FrameworkElement content = Content;
      if (content != null)
      {
        PointF position = new PointF(_innerRect.X, _innerRect.Y);
        SizeF availableSize = CalculateInnerDesiredSize(_innerRect.Size);
        ArrangeChild(content, content.HorizontalAlignment, content.VerticalAlignment, ref position, ref availableSize);
        RectangleF childRect = new RectangleF(position, availableSize);
        content.Arrange(childRect);
      }
    }

    public override void RenderOverride(RenderContext localRenderContext)
    {
      base.RenderOverride(localRenderContext);
      FrameworkElement content = Content;
      if (content != null)
        content.Render(localRenderContext);
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      FrameworkElement content = Content;
      if (content != null)
        childrenOut.Add(content);
    }
  }
}
