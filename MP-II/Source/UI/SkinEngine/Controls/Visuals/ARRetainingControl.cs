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
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Core.General;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

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
      _contentProperty.Attach(OnLayoutPropertyChanged);
      _aspectRatioProperty.Attach(OnLayoutPropertyChanged);
    }

    void Detach()
    {
      _contentProperty.Detach(OnLayoutPropertyChanged);
      _aspectRatioProperty.Detach(OnLayoutPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ARRetainingControl c = (ARRetainingControl) source;
      Content = copyManager.GetCopy(c.Content);
      AspectRatio = copyManager.GetCopy(c.AspectRatio);
      Attach();
      OnLayoutPropertyChanged(null, null);
    }

    #endregion

    void OnLayoutPropertyChanged(AbstractProperty property, object oldValue)
    {
      Invalidate();
    }

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

    protected SizeF CalculateSize(SizeF availableSize)
    {
      // Calculate constraints
      SizeF result = new SizeF((float) Width * SkinContext.Zoom.Width, (float) Height * SkinContext.Zoom.Height);
      if (double.IsNaN(result.Width))
        result.Width = availableSize.Width;
      if (double.IsNaN(result.Height))
        result.Height = availableSize.Height;
      float desiredWidthFromHeight = result.Height * AspectRatio;
      if (result.Width < desiredWidthFromHeight)
        // Adapt height
        result.Height = result.Width / AspectRatio;
      else
        // Adapt width
        result.Width = desiredWidthFromHeight;
      return result;
    }

    public override void Measure(ref SizeF totalSize)
    {
      RemoveMargin(ref totalSize);

      _desiredSize = CalculateSize(totalSize);

      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("ARRetainingControl.Measure: {0} returns {1}x{2}", Name, (int) totalSize.Width, (int) totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("ARRetainingControl.Arrange: {0} X {1}, Y {2} W {4} H {5}", Name, (int) finalRect.X, (int) finalRect.Y, (int) finalRect.Width, (int) finalRect.Height));
  
      RemoveMargin(ref finalRect);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      FrameworkElement content = Content;
      if (content != null)
      {
        PointF position = new PointF(finalRect.X, finalRect.Y);
        SizeF availableSize = CalculateSize(finalRect.Size);
        ArrangeChild(content, ref position, ref availableSize);
        RectangleF childRect = new RectangleF(position, availableSize);
        content.Arrange(childRect);
      }

      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();

      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      base.Arrange(finalRect);
    }

    public override void DoRender()
    {
      base.DoRender();
      FrameworkElement content = Content;
      if (content != null)
      {
        SkinContext.AddOpacity(Opacity);
        content.Render();
        SkinContext.RemoveOpacity();
      }
    }

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      FrameworkElement content = Content;
      if (content != null)
        content.BuildRenderTree();
    }

    public override void DestroyRenderTree()
    {
      FrameworkElement content = Content;
      if (content != null)
        content.DestroyRenderTree();
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
