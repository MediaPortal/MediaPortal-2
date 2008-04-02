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
using System.Text;
using System.Drawing.Drawing2D;
using MediaPortal.Presentation.Properties;
using MediaPortal.Control.InputManager;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using Presentation.SkinEngine;
using Presentation.SkinEngine.DirectX;
using Presentation.SkinEngine.Rendering;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = SlimDX.Matrix;
using Presentation.SkinEngine.Controls.Visuals.Styles;
using Presentation.SkinEngine.Controls.Brushes;


namespace Presentation.SkinEngine.Controls.Visuals
{
  public class Control : FrameworkElement, IUpdateEventHandler
  {
    Property _templateProperty;
    FrameworkElement _templateControl;
    Brush _backgroundProperty;
    Brush _borderProperty;
    Property _borderThicknessProperty;
    Property _cornerRadiusProperty;
    VisualAssetContext _backgroundAsset;
    VisualAssetContext _borderAsset;
    protected bool _performLayout;
    int _verticesCountBackground;
    int _verticesCountBorder;
    PrimitiveContext _backgroundContext;
    PrimitiveContext _borderContext;
    protected UIEvent _lastEvent = UIEvent.None;
    protected bool _hidden = false;

    #region ctor
    public Control()
    {
      Init();
      Attach();
    }

    public Control(Control c): base(c)
    {
      Init();

      if (c.Template != null)
      {
        Template = (ControlTemplate)c.Template.Clone();
        FrameworkElement element = Template.LoadContent(Window) as FrameworkElement;
        if (element != null)
        {
          element.VisualParent = this;
          _templateControl = element;
          _templateControl.Context = this.Context;
        }
      }
      if (c.BorderBrush != null)
        this.BorderBrush = (Brush)c.BorderBrush.Clone();
      if (c.Background != null)
        this.Background = (Brush)c.Background.Clone();
      BorderThickness = c.BorderThickness;
      CornerRadius = c.CornerRadius;
      Attach();
    }

    public override object Clone()
    {
      return new Control(this);
    }

    void Init()
    {
      _templateProperty = new Property(null);

      _borderProperty = null;
      _backgroundProperty = null;
      _borderThicknessProperty = new Property((double)1.0);
      _cornerRadiusProperty = new Property((double)0);

    }

    void Attach()
    {
      //_borderProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      //_backgroundProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      //_borderThicknessProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _templateProperty.Attach(new PropertyChangedHandler(OnTemplateChanged));
      _cornerRadiusProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    void OnBorderBrushPropertyChanged(Property property)
    {
      _lastEvent |= UIEvent.StrokeChange;
      if (Window != null) Window.Invalidate(this);
    }

    void OnBackgroundBrushPropertyChanged(Property property)
    {
      _lastEvent |= UIEvent.FillChange;
      if (Window != null) Window.Invalidate(this);
    }

    protected override void OnStyleChanged(Property property)
    {
      if (_templateProperty == null)
        Init();
      ///@optimize: 
      Style.Set(this);
      Invalidate();
    }

    protected void OnTemplateChanged(Property property)
    {
      if (Template != null)
      {
        ///@optimize: 
        FrameworkElement element = Template.LoadContent(Window) as FrameworkElement;
        if (element != null)
        {
          element.VisualParent = this;
          _templateControl = element;
          _templateControl.Context = this.Context;
          this.Resources.Merge(Template.Resources);
          this.Triggers.Merge(Template.Triggers);
          _templateControl.SetWindow(Window);
        }
        else
        {
          _templateControl = null;
        }
      }
      else
      {
        _templateControl = null;
      }
      Invalidate();
    }

    void OnPropertyChanged(Property property)
    {
      _performLayout = true;
      if (Window != null) Window.Invalidate(this);
    }
    #endregion

    #region properties
    public FrameworkElement TemplateControl
    {
      get
      {
        return _templateControl;
      }
      set
      {
        _templateControl = value;
      }
    }

    /// <summary>
    /// Gets or sets the background brush
    /// </summary>
    /// <value>The background.</value>
    public Brush Background
    {
      get
      {
        return _backgroundProperty;
      }
      set
      {
        _backgroundProperty = value;
        if (value != null)
        {
          _backgroundProperty.ClearAttachedEvents();
          _backgroundProperty.Attach(new PropertyChangedHandler(OnBackgroundBrushPropertyChanged));
        }
      }
    }

    /// <summary>
    /// Gets or sets the Border brush
    /// </summary>
    /// <value>The Border.</value>
    public Brush BorderBrush
    {
      get
      {
        return _borderProperty;
      }
      set
      {
        _borderProperty = value;
        if (value != null)
        {
          _borderProperty.ClearAttachedEvents();
          _borderProperty.Attach(new PropertyChangedHandler(OnBorderBrushPropertyChanged));
        }
      }
    }

    public Property BorderThicknessProperty
    {
      get
      {
        return _borderThicknessProperty;
      }
      set
      {
        _borderThicknessProperty = value;
      }
    }

    public double BorderThickness
    {
      get
      {
        return (double)_borderThicknessProperty.GetValue();
      }
      set
      {
        _borderThicknessProperty.SetValue(value);
      }
    }

    public Property CornerRadiusProperty
    {
      get
      {
        return _cornerRadiusProperty;
      }
      set
      {
        _cornerRadiusProperty = value;
      }
    }

    public double CornerRadius
    {
      get
      {
        return (double)_cornerRadiusProperty.GetValue();
      }
      set
      {
        _cornerRadiusProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the control template property.
    /// </summary>
    /// <value>The control template property.</value>
    public Property TemplateProperty
    {
      get
      {
        return _templateProperty;
      }
      set
      {
        _templateProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the control template.
    /// </summary>
    /// <value>The control template.</value>
    public ControlTemplate Template
    {
      get
      {
        return _templateProperty.GetValue() as ControlTemplate;
      }
      set
      {
        _templateProperty.SetValue(value);
      }
    }
    #endregion

    #region rendering


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
        _performLayout = false;
        _lastEvent = UIEvent.None;
      }
      else if (_lastEvent != UIEvent.None)
      {
        if (!_hidden)
        {
          SetupBrush(_lastEvent);
        }
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
        if (_performLayout)
        {
          PerformLayout();
          _performLayout = false;
        }
        SkinContext.AddOpacity(this.Opacity);
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

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      RenderBorder();
      if (_templateControl != null)
      {
        SkinContext.AddOpacity(this.Opacity);
        _templateControl.Render();
        SkinContext.RemoveOpacity();
      }
    }

    #endregion

    #region measure&arrange
    public override void Measure(System.Drawing.SizeF availableSize)
    {
      //Trace.WriteLine(String.Format("Control:Measure '{0}' {1}x{2}", this.Name, availableSize.Width, availableSize.Height));

      if (!IsVisible)
      {
        _desiredSize = new SizeF(0, 0);
        _originalSize = _desiredSize;
        _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
        return;
      }
      float marginWidth = (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      float marginHeight = (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
      if (_templateControl == null)
      {
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
        SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);
        _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
        if (LayoutTransform != null)
        {
          SkinContext.RemoveLayoutTransform();
        }
        _desiredSize.Width += marginWidth;
        _desiredSize.Height += marginHeight;
        _originalSize = _desiredSize;
        return;
      }
      _desiredSize = new System.Drawing.SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);

      if (Width <= 0)
        _desiredSize.Width = (float)availableSize.Width - marginWidth;
      if (Height <= 0)
        _desiredSize.Height = (float)availableSize.Height - marginHeight;


      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      _templateControl.Measure(_desiredSize);

      if (Width <= 0)
        _desiredSize.Width = _templateControl.DesiredSize.Width;

      if (Height <= 0)
        _desiredSize.Height = _templateControl.DesiredSize.Height;

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      _desiredSize.Width += (float)marginWidth;
      _desiredSize.Height += (float)marginHeight;
      _originalSize = _desiredSize;



      // Trace.WriteLine(String.Format("Control:Measure returns '{0}' {1}x{2}", this.Name, _desiredSize.Width, _desiredSize.Height));
      _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
    }

    public override void Arrange(System.Drawing.RectangleF finalRect)
    {

      //Trace.WriteLine(String.Format("Control:arrange {0} {1},{2} {3}x{4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));

      System.Drawing.RectangleF layoutRect = new System.Drawing.RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);

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
      if (_templateControl != null)
      {
        _templateControl.Arrange(layoutRect);
        ActualPosition = _templateControl.ActualPosition;
        ActualWidth = ((FrameworkElement)_templateControl).ActualWidth;
        ActualHeight = ((FrameworkElement)_templateControl).ActualHeight;
      }

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      IsArrangeValid = true;
      InitializeBindings();
      InitializeTriggers();
      _isLayoutInvalid = false;
      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        if (Window != null) Window.Invalidate(this);
        _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      }
    }
    #endregion

    /// <summary>
    /// Fires an event.
    /// </summary>
    /// <param name="eventName">Name of the event.</param>
    public override void FireEvent(string eventName)
    {
      if (_templateControl != null)
      {
        _templateControl.FireEvent(eventName);
      }
      base.FireEvent(eventName);
    }

    #region findXXX methods
    /// <summary>
    /// Find the element with name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public override UIElement FindElement(string name)
    {
      if (_templateControl != null)
      {
        UIElement o = _templateControl.FindElement(name);
        if (o != null) return o;
      }
      return base.FindElement(name);
    }

    public override UIElement FindElementType(Type t)
    {
      if (_templateControl != null)
      {
        UIElement o = _templateControl.FindElementType(t);
        if (o != null) return o;
      }
      return base.FindElementType(t);
    }

    public override UIElement FindItemsHost()
    {
      if (_templateControl != null)
      {
        UIElement o = _templateControl.FindItemsHost();
        if (o != null) return o;
      }
      return base.FindItemsHost(); ;
    }

    /// <summary>
    /// Finds the focused item.
    /// </summary>
    /// <returns></returns>
    public override UIElement FindFocusedItem()
    {
      if (base.HasFocus) return this;
      if (_templateControl != null)
      {
        UIElement o = _templateControl.FindFocusedItem();
        if (o != null) return o;
      }
      return null;
    }
    #endregion

    #region input handling

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
      if (_templateControl != null)
        _templateControl.FireUIEvent(eventType, source);

      if (SkinContext.UseBatching)
      {
        _lastEvent |= eventType;
        if (Window != null) Window.Invalidate(this);
      }
    }

    /// <summary>
    /// Called when [mouse move].
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      if (_templateControl != null)
      {
        _templateControl.OnMouseMove(x, y);
      }
      base.OnMouseMove(x, y);
    }

    public override void OnKeyPressed(ref Key key)
    {
      base.OnKeyPressed(ref key);
      if (_templateControl != null)
        _templateControl.OnKeyPressed(ref key);
    }

    public override void Reset()
    {
      base.Reset();
      if (_templateControl != null)
        _templateControl.Reset();
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (BorderBrush != null)
        this.BorderBrush.Deallocate();
      if (Background != null)
        this.Background.Deallocate();
      if (_templateControl != null)
      {
        _templateControl.Deallocate();
      }
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
        this.BorderBrush.Allocate();
      if (Background != null)
        this.Background.Allocate();
      if (_templateControl != null)
      {
        _templateControl.Allocate();
      }
    }
    #endregion

    #region focus prediction

    /// <summary>
    /// Predicts the next FrameworkElement which is position above this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (_templateControl == null) return null;
      FrameworkElement element = ((FrameworkElement)_templateControl).PredictFocusUp(focusedFrameworkElement, ref key, strict);
      if (element != null) return element;
      return base.PredictFocusUp(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position below this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Control.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (_templateControl == null) return null;
      FrameworkElement element = ((FrameworkElement)_templateControl).PredictFocusDown(focusedFrameworkElement, ref key, strict);
      if (element != null) return element;
      return base.PredictFocusDown(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position left of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Control.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (_templateControl == null) return null;
      FrameworkElement element = ((FrameworkElement)_templateControl).PredictFocusLeft(focusedFrameworkElement, ref key, strict);
      if (element != null) return element;
      return base.PredictFocusLeft(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position right of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Control.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (_templateControl == null) return null;
      FrameworkElement element = ((FrameworkElement)_templateControl).PredictFocusRight(focusedFrameworkElement, ref key, strict);
      if (element != null) return element;
      return base.PredictFocusRight(focusedFrameworkElement, ref key, strict);
    }
    #endregion

    #region layouting
    /// <summary>
    /// Performs the layout.
    /// </summary>
    void PerformLayout()
    {
      //Trace.WriteLine("Border.PerformLayout() " + this.Name);

      double w = ActualWidth;
      double h = ActualHeight;
      float centerX, centerY;
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
      System.Drawing.RectangleF rect = new System.Drawing.RectangleF(-0.5f, -0.5f, rectSize.Width + 0.5f, rectSize.Height + 0.5f);
      rect.X += (float)ActualPosition.X;
      rect.Y += (float)ActualPosition.Y;
      PositionColored2Textured[] verts;
      GraphicsPath path;
      if (Background != null || (BorderBrush != null && BorderThickness > 0))
      {
        using (path = GetRoundedRect(rect, (float)CornerRadius))
        {
          Shape.CalcCentroid(path, out centerX, out centerY);
          if (Background != null)
          {
            if (SkinContext.UseBatching == false)
            {
              if (_backgroundAsset == null)
              {
                _backgroundAsset = new VisualAssetContext("Border._backgroundAsset:" + this.Name);
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
                _borderAsset = new VisualAssetContext("Border._borderAsset:" + this.Name);
                ContentManager.Add(_borderAsset);
              }
              _borderAsset.VertexBuffer = Shape.ConvertPathToTriangleStrip(path, (float)BorderThickness, true, out verts, _finalLayoutTransform);
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

    #region Get the desired Rounded Rectangle path.
    private GraphicsPath GetRoundedRect(RectangleF baseRect, float CornerRadius)
    {
      // if corner radius is less than or equal to zero, 

      // return the original rectangle 

      if (CornerRadius <= 0.0f && CornerRadius <= 0.0f)
      {
        GraphicsPath mPath = new GraphicsPath();
        mPath.AddRectangle(baseRect);
        mPath.CloseFigure();
        System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
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

      if (CornerRadius >= (Math.Min(baseRect.Width, baseRect.Height)) / 2.0)
        return GetCapsule(baseRect);

      // create the arc for the rectangle sides and declare 

      // a graphics path object for the drawing 

      float diameter = CornerRadius * 2.0F;
      SizeF sizeF = new SizeF(diameter, diameter);
      RectangleF arc = new RectangleF(baseRect.Location, sizeF);
      GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();

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
      System.Drawing.Drawing2D.Matrix mtx = new System.Drawing.Drawing2D.Matrix();
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
    #endregion

    #region Gets the desired Capsular path.
    private GraphicsPath GetCapsule(RectangleF baseRect)
    {
      float diameter;
      RectangleF arc;
      GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
      try
      {
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
      System.Drawing.Drawing2D.Matrix mtx = new System.Drawing.Drawing2D.Matrix();
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
    #endregion

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      if (_performLayout)
      {
        PerformLayout();
        _performLayout = false;
        _lastEvent = UIEvent.None;
      }
      if (_templateControl != null)
      {
        _templateControl.BuildRenderTree();
      }
    }

    public override void DestroyRenderTree()
    {
      if (_templateControl != null)
      {
        _templateControl.DestroyRenderTree();
      }
      base.DestroyRenderTree();
    }

    public override void SetWindow(Window window)
    {
      base.SetWindow(window);
      if (_templateControl != null)
      {
        _templateControl.SetWindow(window);
      }
    }

    public virtual void BecomesVisible()
    {
    }

    public virtual void BecomesHidden()
    {
    }
  }
}
