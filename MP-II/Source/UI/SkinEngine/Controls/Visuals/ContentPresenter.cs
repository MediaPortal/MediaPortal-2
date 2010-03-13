#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.Xaml;
using SlimDX;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class ContentPresenter : FrameworkElement
  {
    #region Protected fields

    protected AbstractProperty _contentProperty;
    protected AbstractProperty _contentTemplateProperty;
    protected FrameworkElement _templateControl = null;

    #endregion

    #region Ctor

    public ContentPresenter()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _contentProperty = new SProperty(typeof(object), null);
      _contentTemplateProperty = new SProperty(typeof(DataTemplate), null);
    }

    void Attach()
    {
      _contentProperty.Attach(OnContentChanged);
      _contentTemplateProperty.Attach(OnContentTemplateChanged);
    }

    void Detach()
    {
      _contentProperty.Detach(OnContentChanged);
      _contentTemplateProperty.Detach(OnContentTemplateChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ContentPresenter p = (ContentPresenter) source;
      Content = copyManager.GetCopy(p.Content);
      ContentTemplate = copyManager.GetCopy(p.ContentTemplate);
      Attach();
      OnContentTemplateChanged(_contentTemplateProperty, false);
    }

    #endregion

    void OnContentChanged(AbstractProperty property, object oldValue)
    {
      if (_templateControl == null)
      { // No ContentTemplate set
        FindAutomaticContentDataTemplate();
      }
      if (_templateControl != null)
        // The controls in the DataTemplate access their "data" via their data context, so we must assign it
        _templateControl.Context = Content;
    }

    /// <summary>
    /// Does an automatic search for an approppriate data template for our content, i.e. looks
    /// in our resources for a resource with the Content's type as key.
    /// </summary>
    void FindAutomaticContentDataTemplate()
    {
      if (Content == null)
        return;
      DataTemplate dt = FindResource(Content.GetType()) as DataTemplate;
      if (dt != null)
      {
        SetTemplateControl(dt.LoadContent() as FrameworkElement);
        return;
      }
      object templateControl;
      if (TypeConverter.Convert(Content, typeof(FrameworkElement), out templateControl))
      {
        SetTemplateControl((FrameworkElement) templateControl);
        return;
      }
    }

    void SetTemplateControl(FrameworkElement templateControl)
    {
      if (templateControl == null)
        return;
      _templateControl = templateControl;
      _templateControl.Context = Content;
      _templateControl.VisualParent = this;
      _templateControl.SetScreen(Screen);
    }

    void OnContentTemplateChanged(AbstractProperty property, object oldValue)
    {
      if (ContentTemplate == null)
      {
        FindAutomaticContentDataTemplate();
        return;
      }
      SetTemplateControl(ContentTemplate.LoadContent() as FrameworkElement);
    }

    public FrameworkElement TemplateControl
    {
      get { return _templateControl; }
    }

    public AbstractProperty ContentProperty
    {
      get { return _contentProperty; }
    }

    public object Content
    {
      get { return _contentProperty.GetValue(); }
      set { _contentProperty.SetValue(value); }
    }

    public AbstractProperty ContentTemplateProperty
    {
      get { return _contentTemplateProperty; }
    }

    public DataTemplate ContentTemplate
    {
      get { return _contentTemplateProperty.GetValue() as DataTemplate; }
      set { _contentTemplateProperty.SetValue(value); }
    }

    public override void Measure(ref SizeF totalSize)
    {
      RemoveMargin(ref totalSize);

      if (!double.IsNaN(Width))
        totalSize.Width = (float) Width*SkinContext.Zoom.Width;
      if (!double.IsNaN(Height))
        totalSize.Height = (float) Height*SkinContext.Zoom.Height;

      SizeF childSize;

      if (_templateControl != null)
      {
        childSize = new SizeF(totalSize.Width, totalSize.Height);
        if (LayoutTransform != null)
        {
          ExtendedMatrix m;
          LayoutTransform.GetTransform(out m);
          SkinContext.AddLayoutTransform(m);
        }

        // Measure the child
        _templateControl.Measure(ref childSize);

        if (LayoutTransform != null)
          SkinContext.RemoveLayoutTransform();
      }
      else
        childSize = new SizeF();

      _desiredSize = new SizeF((float) Width * SkinContext.Zoom.Width, (float) Height * SkinContext.Zoom.Height);

      if (double.IsNaN(Width))
        _desiredSize.Width = childSize.Width;

      if (double.IsNaN(Height))
        _desiredSize.Height = childSize.Height;

      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("ContentPresenter.Measure: {0} returns {1}x{2}", Name, (int) totalSize.Width, (int) totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("ContentPresenter.Arrange: {0} X {1}, Y {2} W {4} H {5}", Name, (int) finalRect.X, (int) finalRect.Y, (int) finalRect.Width, (int) finalRect.Height));
  
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

      if (_templateControl != null)
      {
        PointF position = new PointF(finalRect.X, finalRect.Y);
        SizeF availableSize = new SizeF(finalRect.Width, finalRect.Height);
        ArrangeChild(_templateControl, ref position, ref availableSize);
        RectangleF childRect = new RectangleF(position, availableSize);
        _templateControl.Arrange(childRect);
      }

      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();

      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      base.Arrange(finalRect);
    }

    public override void DoRender()
    {
      base.DoRender();
      if (_templateControl != null)
      {
        SkinContext.AddOpacity(Opacity);
        _templateControl.Render();
        SkinContext.RemoveOpacity();
      }
    }

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      if (_templateControl != null)
        _templateControl.BuildRenderTree();
    }

    public override void DestroyRenderTree()
    {
      if (_templateControl != null)
        _templateControl.DestroyRenderTree();
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      if (_templateControl != null)
        childrenOut.Add(_templateControl);
    }
  }
}
