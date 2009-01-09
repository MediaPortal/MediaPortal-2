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
using System.Drawing.Drawing2D;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.Controls.Visuals.Templates;
using SlimDX.Direct3D9;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.DirectX;
using MediaPortal.SkinEngine.Rendering;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using MediaPortal.SkinEngine.Controls.Visuals.Shapes;
using MediaPortal.SkinEngine.Controls.Brushes;
using MediaPortal.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class Control : FrameworkElement, IUpdateEventHandler
  {
    #region Private/protected fields

    Property _templateProperty;
    Property _templateControlProperty;
    Property _backgroundProperty;
    Property _borderProperty;
    Property _borderThicknessProperty;
    Property _cornerRadiusProperty;
    Property _fontSizeProperty;
    Property _fontFamilyProperty;
    VisualAssetContext _backgroundAsset;
    VisualAssetContext _borderAsset;
    protected bool _performLayout;
    int _verticesCountBackground;
    int _verticesCountBorder;
    PrimitiveContext _backgroundContext;
    PrimitiveContext _borderContext;
    protected UIEvent _lastEvent = UIEvent.None;
    protected bool _hidden = false;

    #endregion

    #region Ctor

    public Control()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _templateProperty = new Property(typeof(ControlTemplate), null);
      _templateControlProperty = new Property(typeof(FrameworkElement), null);
      _borderProperty = new Property(typeof(Brush), null);
      _backgroundProperty = new Property(typeof(Brush), null);
      _borderThicknessProperty = new Property(typeof(double), 1.0);
      _cornerRadiusProperty = new Property(typeof(double), 0.0);
      _fontFamilyProperty = new Property(typeof(string), String.Empty);
      _fontSizeProperty = new Property(typeof(int), 0);
    }

    void Attach()
    {
      _borderProperty.Attach(OnPropertyChanged);
      _backgroundProperty.Attach(OnPropertyChanged);
      _borderThicknessProperty.Attach(OnPropertyChanged);
      _templateProperty.Attach(OnTemplateChanged);
      _templateControlProperty.Attach(OnTemplateControlChanged);
      _cornerRadiusProperty.Attach(OnPropertyChanged);
      _fontFamilyProperty.Attach(OnFontChanged);
      _fontSizeProperty.Attach(OnFontChanged);
    }

    void Detach()
    {
      _borderProperty.Detach(OnPropertyChanged);
      _backgroundProperty.Detach(OnPropertyChanged);
      _borderThicknessProperty.Detach(OnPropertyChanged);
      _templateProperty.Detach(OnTemplateChanged);
      _templateControlProperty.Detach(OnTemplateControlChanged);
      _cornerRadiusProperty.Detach(OnPropertyChanged);
      _fontFamilyProperty.Detach(OnFontChanged);
      _fontSizeProperty.Detach(OnFontChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Control c = (Control) source;
      BorderBrush = copyManager.GetCopy(c.BorderBrush);
      Background = copyManager.GetCopy(c.Background);
      BorderThickness = copyManager.GetCopy(c.BorderThickness);
      CornerRadius = copyManager.GetCopy(c.CornerRadius);
      Template = copyManager.GetCopy(c.Template);
      TemplateControl = copyManager.GetCopy(c.TemplateControl);
      Attach();
    }

    #endregion

    #region Change handlers

    protected void OnTemplateChanged(Property property, object oldValue)
    {
      if (Template != null)
      {
        Resources.Merge(Template.Resources);
        foreach (TriggerBase t in Template.Triggers)
          Triggers.Add(t);
        ///@optimize: 
        TemplateControl = Template.LoadContent() as FrameworkElement;
      }
      else
        TemplateControl = null;
    }

    protected void OnTemplateControlChanged(Property property, object oldValue)
    {
      FrameworkElement oldTemplateControl = oldValue as FrameworkElement;
      if (oldTemplateControl != null)
        oldTemplateControl.VisualParent = null;

      FrameworkElement element = property.GetValue() as FrameworkElement;
      if (element != null)
      {
        element.VisualParent = this;
        element.SetScreen(Screen);
      }
      Invalidate();
    }

    void OnPropertyChanged(Property property, object oldValue)
    {
      _performLayout = true;
      if (Screen != null) Screen.Invalidate(this);
    }

    protected virtual void OnFontChanged(Property property, object oldValue)
    {
    }

    #endregion

    #region Public properties

    public Property TemplateControlProperty
    {
      get { return _templateControlProperty; }
    }

    public FrameworkElement TemplateControl
    {
      get { return (FrameworkElement)_templateControlProperty.GetValue(); }
      set { _templateControlProperty.SetValue(value); }
    }

    public Property BackgroundProperty
    {
      get { return _backgroundProperty; }
    }

    public Brush Background
    {
      get { return (Brush) _backgroundProperty.GetValue(); }
      set { _backgroundProperty.SetValue(value); }
    }

    public Brush BorderBrush
    {
      get { return (Brush) _borderProperty.GetValue(); }
      set { _borderProperty.SetValue(value); }
    }

    public Property BorderThicknessProperty
    {
      get { return _borderThicknessProperty; }
    }

    public double BorderThickness
    {
      get { return (double) _borderThicknessProperty.GetValue(); }
      set { _borderThicknessProperty.SetValue(value); }
    }

    public Property CornerRadiusProperty
    {
      get { return _cornerRadiusProperty; }
    }

    public double CornerRadius
    {
      get { return (double) _cornerRadiusProperty.GetValue(); }
      set { _cornerRadiusProperty.SetValue(value); }
    }

    public Property TemplateProperty
    {
      get { return _templateProperty; }
    }

    public ControlTemplate Template
    {
      get { return (ControlTemplate) _templateProperty.GetValue(); }
      set { _templateProperty.SetValue(value); }
    }

    public Property FontFamilyProperty
    {
      get { return _fontFamilyProperty; }
    }

    public string FontFamily
    {
      get { return (string) _fontFamilyProperty.GetValue(); }
      set { _fontFamilyProperty.SetValue(value); }
    }

    public Property FontSizeProperty
    {
      get { return _fontSizeProperty; }
    }

    public int FontSize
    {
      get { return (int) _fontSizeProperty.GetValue(); }
      set { _fontSizeProperty.SetValue(value); }
    }

    #endregion

    #region Rendering

    void SetupBrush(UIEvent uiEvent)
    {
      if ((uiEvent & UIEvent.OpacityChange) != 0 || (uiEvent & UIEvent.FillChange) != 0)
      {
        if (Background != null && _backgroundContext != null)
        {
          RenderPipeline.Instance.Remove(_backgroundContext);
          Background.SetupPrimitive(_backgroundContext);
          RenderPipeline.Instance.Add(_backgroundContext);
        }
      }
      if ((uiEvent & UIEvent.OpacityChange) != 0 || (uiEvent & UIEvent.StrokeChange) != 0)
      {
        if (BorderBrush != null && _borderContext != null)
        {
          RenderPipeline.Instance.Remove(_borderContext);
          BorderBrush.SetupPrimitive(_borderContext);
          RenderPipeline.Instance.Add(_borderContext);
        }
      }
    }

    public virtual void Update()
    {
      if (_hidden)
      {
        if ((_lastEvent & UIEvent.Visible) != 0)
        {
          _hidden = false;
          BecomesVisible();
        }
      }
      if (_hidden)
      {
        _lastEvent = UIEvent.None;
        return;
      }
      if ((_lastEvent & UIEvent.Hidden) != 0)
      {
        RenderPipeline.Instance.Remove(_backgroundContext);
        RenderPipeline.Instance.Remove(_borderContext);
        _backgroundContext = null;
        _borderContext = null;
        _performLayout = true;
        if (_hidden == false)
        {
          _hidden = true;
          BecomesHidden();
        }
        _lastEvent = UIEvent.None;
        return;
      }

      UpdateLayout();
      if (_performLayout)
      {
        PerformLayout();
        _lastEvent = UIEvent.None;
      }
      else if (_lastEvent != UIEvent.None)
      {
        if (!_hidden)
          SetupBrush(_lastEvent);
        _lastEvent = UIEvent.None;
      }
    }

    void RenderBorder()
    {
      if (!IsVisible) return;
      if (Background != null || (BorderBrush != null && BorderThickness > 0))
      {
        if (Background != null && _backgroundAsset != null && _backgroundAsset.IsAllocated == false)
          _performLayout = true;
        if (BorderBrush != null && _borderAsset != null && _borderAsset.IsAllocated == false)
          _performLayout = true;
        PerformLayout();
        SkinContext.AddOpacity(Opacity);
        //ExtendedMatrix m = new ExtendedMatrix();
        //m.Matrix = Matrix.Translation(new Vector3((float)ActualPosition.X, (float)ActualPosition.Y, (float)ActualPosition.Z));
        //SkinContext.AddTransform(m);
        if (Background != null)
        {
          //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
          //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
          if (Background.BeginRender(_backgroundAsset.VertexBuffer, _verticesCountBackground, PrimitiveType.TriangleList))
          {
            GraphicsDevice.Device.SetStreamSource(0, _backgroundAsset.VertexBuffer, 0, PositionColored2Textured.StrideSize);
            GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _verticesCountBackground);
            Background.EndRender();
          }
          _backgroundAsset.LastTimeUsed = SkinContext.Now;
        }

        if (BorderBrush != null && BorderThickness > 0)
        {
          //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
          if (BorderBrush.BeginRender(_borderAsset.VertexBuffer, _verticesCountBorder, PrimitiveType.TriangleList))
          {
            GraphicsDevice.Device.SetStreamSource(0, _borderAsset.VertexBuffer, 0, PositionColored2Textured.StrideSize);
            GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _verticesCountBorder);
            BorderBrush.EndRender();
          }
          _borderAsset.LastTimeUsed = SkinContext.Now;
        }
        //SkinContext.RemoveTransform();
        SkinContext.RemoveOpacity();
      }
    }

    public override void DoRender()
    {
      RenderBorder();
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
      {
        SkinContext.AddOpacity(Opacity);
        templateControl.Render();
        SkinContext.RemoveOpacity();
      }
    }

    #endregion

    #region Measure&Arrange

    public override void Measure(ref SizeF totalSize)
    {
      RemoveMargin(ref totalSize);

      if (!double.IsNaN(Width))
        totalSize.Width = (float) Width;
      if (!double.IsNaN(Height))
        totalSize.Height = (float) Height;

      FrameworkElement templateControl = TemplateControl;
      SizeF childSize;

      if (templateControl != null)
      {
        childSize = new SizeF(totalSize.Width, totalSize.Height);
        if (LayoutTransform != null)
        {
          ExtendedMatrix m;
          LayoutTransform.GetTransform(out m);
          SkinContext.AddLayoutTransform(m);
        }

        templateControl.Measure(ref childSize);

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

      //Trace.WriteLine(String.Format("Control.Measure returns '{0}' {1}x{2}", this.Name, totalSize.Width, totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      FrameworkElement templateControl = TemplateControl;
      //Trace.WriteLine(String.Format("Control.Arrange :{0} X {1},Y {2} W {3}xH {4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));
      RemoveMargin(ref finalRect);

      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);

      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (templateControl != null)
      {
        templateControl.Arrange(layoutRect);
        ActualPosition = templateControl.ActualPosition;
        ActualWidth = templateControl.ActualWidth;
        ActualHeight = templateControl.ActualHeight;
      }

      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();

      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        if (Screen != null) Screen.Invalidate(this);
        _finalRect = new RectangleF(finalRect.Location, finalRect.Size);
      }
      base.Arrange(finalRect);
    }

    #endregion

    public override void FireEvent(string eventName)
    {
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
      {
        templateControl.FireEvent(eventName);
      }
      base.FireEvent(eventName);
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        childrenOut.Add(templateControl);
    }

    #region Input handling

    public override void FireUIEvent(UIEvent eventType, UIElement source)
    {
      if ((_lastEvent & UIEvent.Hidden) != 0 && eventType == UIEvent.Visible)
      {
        _lastEvent = UIEvent.None;
      }
      if ((_lastEvent & UIEvent.Visible) != 0 && eventType == UIEvent.Hidden)
      {
        _lastEvent = UIEvent.None;
      }
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        templateControl.FireUIEvent(eventType, source);

      if (SkinContext.UseBatching)
      {
        _lastEvent |= eventType;
        if (Screen != null) Screen.Invalidate(this);
      }
      base.FireUIEvent(eventType, source);
    }

    public override void OnMouseMove(float x, float y)
    {
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        templateControl.OnMouseMove(x, y);
      base.OnMouseMove(x, y);
    }

    public override void OnKeyPressed(ref Key key)
    {
      FrameworkElement templateControl = TemplateControl;
      base.OnKeyPressed(ref key);
      if (templateControl != null)
        templateControl.OnKeyPressed(ref key);
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (BorderBrush != null)
        BorderBrush.Deallocate();
      if (Background != null)
        Background.Deallocate();
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        templateControl.Deallocate();
      if (_borderAsset != null)
      {
        _borderAsset.Free(true);
        ContentManager.Remove(_borderAsset);
        _borderAsset = null;
      }
      if (_backgroundAsset != null)
      {
        _backgroundAsset.Free(true);
        ContentManager.Remove(_backgroundAsset);
        _backgroundAsset = null;
      }
      if (_backgroundContext != null)
      {
        RenderPipeline.Instance.Remove(_backgroundContext);
        _backgroundContext = null;
      }
      if (_borderContext != null)
      {
        RenderPipeline.Instance.Remove(_borderContext);
        _borderContext = null;
      }
      _performLayout = true;
    }

    public override void Allocate()
    {
      base.Allocate();
      if (BorderBrush != null)
        BorderBrush.Allocate();
      if (Background != null)
        Background.Allocate();
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        templateControl.Allocate();
    }

    #endregion

    #region Layouting

    void PerformLayout()
    {
      _performLayout = false;
      //Trace.WriteLine("Border.PerformLayout() " + this.Name);

      double w = ActualWidth;
      double h = ActualHeight;
      SizeF rectSize = new SizeF((float)w, (float)h);

      ExtendedMatrix m = new ExtendedMatrix();
      if (_finalLayoutTransform != null)
        m.Matrix *= _finalLayoutTransform.Matrix;
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        m.Matrix *= em.Matrix;
      }
      m.InvertSize(ref rectSize);
      RectangleF rect = new RectangleF(-0.5f, -0.5f, rectSize.Width + 0.5f, rectSize.Height + 0.5f);
      rect.X += ActualPosition.X;
      rect.Y += ActualPosition.Y;
      PositionColored2Textured[] verts;
      if (Background != null || (BorderBrush != null && BorderThickness > 0))
      {
        GraphicsPath path;
        using (path = GetRoundedRect(rect, (float)CornerRadius))
        {
          float centerX, centerY;
          Shape.CalcCentroid(path, out centerX, out centerY);
          if (Background != null)
          {
            if (SkinContext.UseBatching == false)
            {
              if (_backgroundAsset == null)
              {
                _backgroundAsset = new VisualAssetContext("Border._backgroundAsset:" + Name);
                ContentManager.Add(_backgroundAsset);
              }
              _backgroundAsset.VertexBuffer = Shape.ConvertPathToTriangleFan(path, centerX, centerY, out verts);
              if (_backgroundAsset.VertexBuffer != null)
              {
                Background.SetupBrush(this, ref verts);

                PositionColored2Textured.Set(_backgroundAsset.VertexBuffer, ref verts);
                _verticesCountBackground = (verts.Length / 3);

              }
            }
            else
            {
              Shape.PathToTriangleList(path, centerX, centerY, out verts);
              _verticesCountBackground = (verts.Length / 3);
              Background.SetupBrush(this, ref verts);
              if (_backgroundContext == null)
              {
                _backgroundContext = new PrimitiveContext(_verticesCountBackground, ref verts);
                Background.SetupPrimitive(_backgroundContext);
                RenderPipeline.Instance.Add(_backgroundContext);
              }
              else
              {
                _backgroundContext.OnVerticesChanged(_verticesCountBackground, ref verts);
              }
            }
          }

          if (BorderBrush != null && BorderThickness > 0)
          {
            if (SkinContext.UseBatching == false)
            {
              if (_borderAsset == null)
              {
                _borderAsset = new VisualAssetContext("Border._borderAsset:" + Name);
                ContentManager.Add(_borderAsset);
              }
              _borderAsset.VertexBuffer = Shape.ConvertPathToTriangleStrip(path, (float)BorderThickness, true, out verts, _finalLayoutTransform, false);
              if (_borderAsset.VertexBuffer != null)
              {
                BorderBrush.SetupBrush(this, ref verts);

                PositionColored2Textured.Set(_borderAsset.VertexBuffer, ref verts);
                _verticesCountBorder = (verts.Length / 3);

              }
            }
            else
            {
              Shape.PathToTriangleStrip(path, (float)BorderThickness, true, out verts, _finalLayoutTransform);
              BorderBrush.SetupBrush(this, ref verts);
              _verticesCountBorder = (verts.Length / 3);
              if (_borderContext == null)
              {
                _borderContext = new PrimitiveContext(_verticesCountBorder, ref verts);
                BorderBrush.SetupPrimitive(_borderContext);
                RenderPipeline.Instance.Add(_borderContext);
              }
              else
              {
                _borderContext.OnVerticesChanged(_verticesCountBorder, ref verts);
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Get the desired Rounded Rectangle path.
    /// </summary>
    private GraphicsPath GetRoundedRect(RectangleF baseRect, float cornerRadius)
    {
      // if corner radius is less than or equal to zero, 

      // return the original rectangle 

      if (cornerRadius <= 0.0f && cornerRadius <= 0.0f)
      {
        GraphicsPath mPath = new GraphicsPath();
        mPath.AddRectangle(baseRect);
        mPath.CloseFigure();
        Matrix m = new Matrix();
        m.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
        m.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
        if (LayoutTransform != null)
        {
          ExtendedMatrix em;
          LayoutTransform.GetTransform(out em);
          m.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
        }
        m.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
        mPath.Transform(m);
        mPath.Flatten();
        return mPath;
      }

      // if the corner radius is greater than or equal to 

      // half the width, or height (whichever is shorter) 

      // then return a capsule instead of a lozenge 

      if (cornerRadius >= (Math.Min(baseRect.Width, baseRect.Height)) / 2.0)
        return GetCapsule(baseRect);

      // create the arc for the rectangle sides and declare 

      // a graphics path object for the drawing 

      float diameter = cornerRadius * 2.0F;
      SizeF sizeF = new SizeF(diameter, diameter);
      RectangleF arc = new RectangleF(baseRect.Location, sizeF);
      GraphicsPath path = new GraphicsPath();

      // top left arc 


      path.AddArc(arc, 180, 90);

      // top right arc 

      arc.X = baseRect.Right - diameter;
      path.AddArc(arc, 270, 90);

      // bottom right arc 

      arc.Y = baseRect.Bottom - diameter;
      path.AddArc(arc, 0, 90);

      // bottom left arc

      arc.X = baseRect.Left;
      path.AddArc(arc, 90, 90);

      path.CloseFigure();
      Matrix mtx = new Matrix();
      mtx.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
      mtx.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        mtx.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
      }
      mtx.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      path.Transform(mtx);

      path.Flatten();
      return path;
    }

    /// <summary>
    /// Gets the desired Capsular path.
    /// </summary>
    private GraphicsPath GetCapsule(RectangleF baseRect)
    {
      RectangleF arc;
      GraphicsPath path = new GraphicsPath();
      try
      {
        float diameter;
        if (baseRect.Width > baseRect.Height)
        {
          // return horizontal capsule 

          diameter = baseRect.Height;
          SizeF sizeF = new SizeF(diameter, diameter);
          arc = new RectangleF(baseRect.Location, sizeF);
          path.AddArc(arc, 90, 180);
          arc.X = baseRect.Right - diameter;
          path.AddArc(arc, 270, 180);
        }
        else if (baseRect.Width < baseRect.Height)
        {
          // return vertical capsule 

          diameter = baseRect.Width;
          SizeF sizeF = new SizeF(diameter, diameter);
          arc = new RectangleF(baseRect.Location, sizeF);
          path.AddArc(arc, 180, 180);
          arc.Y = baseRect.Bottom - diameter;
          path.AddArc(arc, 0, 180);
        }
        else
        {
          // return circle 

          path.AddEllipse(baseRect);
        }
      }
      catch (Exception)
      {
        path.AddEllipse(baseRect);
      }
      finally
      {
        path.CloseFigure();
      }
      Matrix mtx = new Matrix();
      mtx.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
      mtx.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        mtx.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
      }
      mtx.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      path.Transform(mtx);
      return path;
    }

    #endregion

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      if (_performLayout)
      {
        PerformLayout();
        _lastEvent = UIEvent.None;
      }
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        templateControl.BuildRenderTree();
    }

    public override void DestroyRenderTree()
    {
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        templateControl.DestroyRenderTree();
      base.DestroyRenderTree();
    }

    public virtual void BecomesVisible()
    { } 

    public virtual void BecomesHidden()
    { }
  }
}
