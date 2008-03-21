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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using MediaPortal.Core;
using MediaPortal.Core.Properties;
using MediaPortal.Control.InputManager;
using MediaPortal.Presentation.WindowManager;
using SkinEngine.Controls.Visuals.Triggers;
using SkinEngine.Controls.Animations;
using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Bindings;

namespace SkinEngine.Controls.Visuals
{
  public enum VisibilityEnum
  {
    Visible = 0,
    Hidden = 1,
    Collapsed = 2,
  }

  public enum UIEvent : int
  {
    None = 0,
    Hidden = 1,
    Visible = 2,
    OpacityChange = 4,
    StrokeChange=8,
    FillChange=16,
  }

  public class ZOrderComparer : IComparer<UIElement>
  {
    #region IComparer<UIElement> Members

    public int Compare(UIElement x, UIElement y)
    {
      return x.ZIndex.CompareTo(y.ZIndex);
    }

    #endregion
  }

  public class UIElement : Visual, IBindingCollection
  {
    Property _nameProperty;
    Property _keyProperty;
    Property _focusableProperty;
    Property _isFocusScopeProperty;
    Property _hasFocusProperty;
    Vector3 _acutalPosition;
    Property _positionProperty;
    Property _dockProperty;
    Property _marginProperty;
    Property _triggerProperty;
    Transform _renderTransform;
    Vector2 _renderTransformOrigin;
    Property _layoutTransformProperty;
    Property _visibilityProperty;
    Property _isEnabledProperty;
    Property _rowProperty;
    Property _columnProperty;
    Property _rowSpanProperty;
    Property _columnSpanProperty;
    Property _isItemsHostProperty;
    Property _contextProperty;
    SkinEngine.Controls.Brushes.Brush _opacityMask;
    Property _opacityProperty;
    Property _freezableProperty;
    Property _zIndexProperty;
    protected SizeF _desiredSize;
    protected SizeF _originalSize;
    protected SizeF _availableSize;
    protected System.Drawing.RectangleF _finalRect;
    bool _isArrangeValid;
    ResourceDictionary _resources;
    BindingCollection _bindings;
    protected bool _isLayoutInvalid = true;
    protected ExtendedMatrix _finalLayoutTransform;
    Command _loaded;
    bool _bindingsInitialized;
    bool _triggersInitialized;
    bool _fireLoaded = true;
    bool _isVisible = true;
    double _opacityCache = 1.0;
    public bool TraceThis = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIElement"/> class.
    /// </summary>
    public UIElement()
    {
      Init();

    }
    public UIElement(UIElement el)
      : base((Visual)el)
    {
      Init();
      Name = el.Name;
      Key = el.Key;
      Focusable = el.Focusable;
      IsFocusScope = el.IsFocusScope;
      HasFocusProperty.SetValue(el.HasFocus);
      ActualPosition = el.ActualPosition;
      Position = el.Position;
      Dock = el.Dock;
      Margin = el.Margin;
      Visibility = el.Visibility;
      IsEnabled = el.IsEnabled;
      Row = el.Row;
      Column = el.Column;
      RowSpan = el.RowSpan;
      ColumnSpan = el.ColumnSpan;
      IsItemsHost = el.IsItemsHost;
      Freezable = el.Freezable;
      Opacity = el.Opacity;
      Context = el.Context;
      ZIndex = el.ZIndex;
      Loaded = el.Loaded;

      if (OpacityMask != null)
        OpacityMask = (SkinEngine.Controls.Brushes.Brush)el.OpacityMask.Clone();

      foreach (Binding binding in el._bindings)
      {
        _bindings.Add((Binding)binding.Clone());
      }

      if (el.LayoutTransform != null)
        LayoutTransform = (Transform)el.LayoutTransform.Clone();

      if (el.RenderTransform != null)
        RenderTransform = (Transform)el.RenderTransform.Clone();

      RenderTransformOrigin = el.RenderTransformOrigin;
      _resources = el.Resources;
      /*
      IDictionaryEnumerator enumer = el.Resources.GetEnumerator();
      while (enumer.MoveNext())
      {
        ICloneable clone = enumer.Value as ICloneable;
        if (clone != null)
        {
          Resources[enumer.Key] = clone.Clone();
        }
        else
        {
          Resources[enumer.Key] = enumer.Value;
          //Trace.WriteLine(String.Format("resource type:{0} is not clonable", enumer.Value));
        }
      }*/

      foreach (Trigger t in el.Triggers)
      {
        Triggers.Add((Trigger)t.Clone());
      }
    }

    void Init()
    {
      _bindings = new BindingCollection();
      _nameProperty = new Property("");
      _keyProperty = new Property("");
      _focusableProperty = new Property(false);
      _isFocusScopeProperty = new Property(true);
      _hasFocusProperty = new Property(false);
      _acutalPosition = new Vector3(0, 0, 1);
      _positionProperty = new Property(new Vector3(0, 0, 1));
      _dockProperty = new Property(Dock.Left);
      _marginProperty = new Property(new Vector4(0, 0, 0, 0));
      _resources = new ResourceDictionary();
      _triggerProperty = new Property(new TriggerCollection());
      _renderTransform = null;
      _layoutTransformProperty = new Property(null);
      _renderTransformOrigin = new Vector2(0, 0);
      _visibilityProperty = new Property(VisibilityEnum.Visible);
      _isEnabledProperty = new Property(true);
      _isItemsHostProperty = new Property(false);
      _freezableProperty = new Property(false);
      _opacityProperty = new Property(1.0);
      _contextProperty = new Property(null);

      _rowProperty = new Property(1);
      _columnProperty = new Property(1);
      _rowSpanProperty = new Property(1);
      _columnSpanProperty = new Property(1);
      _opacityMask = null;
      _zIndexProperty = new Property(-1.0);

      _positionProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _dockProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _marginProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      //_zIndexProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _visibilityProperty.Attach(new PropertyChangedHandler(OnVisibilityPropertyChanged));
      _opacityProperty.Attach(new PropertyChangedHandler(OnOpacityPropertyChanged));
    }


    /// <summary>
    /// Gets or sets the loaded event 
    /// </summary>
    /// <value>The loaded.</value>
    public Command Loaded
    {
      get
      {
        return _loaded;
      }
      set
      {
        _loaded = value;
      }
    }

    void OnOpacityPropertyChanged(Property property)
    {
      _opacityCache = (double)_opacityProperty.GetValue();
      FireUIEvent(UIEvent.OpacityChange, this);
    }

    void OnVisibilityPropertyChanged(Property property)
    {
      if (VisualParent != null)
      {
        VisualParent.Invalidate();
      }
      _isVisible = (this.Visibility == VisibilityEnum.Visible);
      if (!_isVisible)
        FireUIEvent(UIEvent.Hidden, this);
      else
        FireUIEvent(UIEvent.Visible, this);
    }

    public virtual void FireUIEvent(UIEvent eventType, UIElement source)
    {
    }

    /// <summary>
    /// Called when a property value has been changed
    /// Since all UIElement properties are layout properties
    /// we're simply calling Invalidate() here to invalidate the layout
    /// </summary>
    /// <param name="property">The property.</param>
    void OnPropertyChanged(Property property)
    {
      Invalidate();
    }

    /// <summary>
    /// Gets or sets the resources.
    /// </summary>
    /// <value>The resources.</value>
    public ResourceDictionary Resources
    {
      get
      {
        return _resources;
      }
    }

    public void SetResources(ResourceDictionary resources)
    {
      _resources = resources;
    }

    public ExtendedMatrix FinalLayoutTransform
    {
      get
      {
        return _finalLayoutTransform;
      }
    }

    /// <summary>
    /// Gets or sets the Z index property.
    /// </summary>
    /// <value>The Z index property.</value>
    public Property ZIndexProperty
    {
      get
      {
        return _zIndexProperty;
      }
      set
      {
        _zIndexProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the Z index.
    /// </summary>
    /// <value>The Z index.</value>
    public double ZIndex
    {
      get
      {
        return (double)_zIndexProperty.GetValue();
      }
      set
      {
        _zIndexProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the opacity property.
    /// </summary>
    /// <value>The opacity property.</value>
    public Property OpacityProperty
    {
      get
      {
        return _opacityProperty;
      }
      set
      {
        _opacityProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the opacity.
    /// </summary>
    /// <value>The opacity.</value>
    public double Opacity
    {
      get
      {
        return _opacityCache;// (double)_opacityProperty.GetValue();
      }
      set
      {
        _opacityProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the freezable property.
    /// </summary>
    /// <value>The freezable property.</value>
    public Property FreezableProperty
    {
      get
      {
        return _freezableProperty;
      }
      set
      {
        _freezableProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="UIElement"/> is freezable.
    /// </summary>
    /// <value><c>true</c> if freezable; otherwise, <c>false</c>.</value>
    public bool Freezable
    {
      get
      {
        return (bool)_freezableProperty.GetValue();
      }
      set
      {
        _freezableProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the context property.
    /// </summary>
    /// <value>The context property.</value>
    public Property ContextProperty
    {
      get
      {
        return _contextProperty;
      }
      set
      {
        _contextProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the context.
    /// </summary>
    /// <value>The context.</value>
    public object Context
    {
      get
      {
        return _contextProperty.GetValue();
      }
      set
      {
        _contextProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the opacity mask.
    /// </summary>
    /// <value>The opacity mask.</value>
    public SkinEngine.Controls.Brushes.Brush OpacityMask
    {
      get
      {
        return _opacityMask;
      }
      set
      {
        _opacityMask = value;
      }
    }

    /// <summary>
    /// Gets or sets the is items host property.
    /// </summary>
    /// <value>The is items host property.</value>
    public Property IsItemsHostProperty
    {
      get
      {
        return _isItemsHostProperty;
      }
      set
      {
        _isItemsHostProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this element hosts items or not
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is items host; otherwise, <c>false</c>.
    /// </value>
    public bool IsItemsHost
    {
      get
      {
        return (bool)_isItemsHostProperty.GetValue();
      }
      set
      {
        _isItemsHostProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the row property.
    /// </summary>
    /// <value>The row property.</value>
    public Property RowProperty
    {
      get
      {
        return _rowProperty;
      }
      set
      {
        _rowProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the row.
    /// </summary>
    /// <value>The row.</value>
    public int Row
    {
      get
      {
        return (int)_rowProperty.GetValue();
      }
      set
      {
        _rowProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the row property.
    /// </summary>
    /// <value>The row property.</value>
    public Property RowSpanProperty
    {
      get
      {
        return _rowSpanProperty;
      }
      set
      {
        _rowSpanProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the row.
    /// </summary>
    /// <value>The row.</value>
    public int RowSpan
    {
      get
      {
        return (int)_rowSpanProperty.GetValue();
      }
      set
      {
        _rowSpanProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the column property.
    /// </summary>
    /// <value>The column property.</value>
    public Property ColumnProperty
    {
      get
      {
        return _columnProperty;
      }
      set
      {
        _columnProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the column.
    /// </summary>
    /// <value>The column.</value>
    public int Column
    {
      get
      {
        return (int)_columnProperty.GetValue();
      }
      set
      {
        _columnProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the column span property.
    /// </summary>
    /// <value>The column span property.</value>
    public Property ColumnSpanProperty
    {
      get
      {
        return _columnSpanProperty;
      }
      set
      {
        _columnSpanProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the column span.
    /// </summary>
    /// <value>The column span.</value>
    public int ColumnSpan
    {
      get
      {
        return (int)_columnSpanProperty.GetValue();
      }
      set
      {
        _columnSpanProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the is enabled property.
    /// </summary>
    /// <value>The is enabled property.</value>
    public Property IsEnabledProperty
    {
      get
      {
        return _isEnabledProperty;
      }
      set
      {
        _isEnabledProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is enabled.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsEnabled
    {
      get
      {
        return (bool)_isEnabledProperty.GetValue();
      }
      set
      {
        _isEnabledProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the visibility property.
    /// </summary>
    /// <value>The visibility property.</value>
    public Property VisibilityProperty
    {
      get
      {
        return _visibilityProperty;
      }
      set
      {
        _visibilityProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the visibility.
    /// </summary>
    /// <value>The visibility.</value>
    public VisibilityEnum Visibility
    {
      get
      {
        return (VisibilityEnum)_visibilityProperty.GetValue();
      }
      set
      {
        //Trace.WriteLine(String.Format("set {0} :{1}", this.Name, value));
        _visibilityProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the triggers property.
    /// </summary>
    /// <value>The triggers property.</value>
    public Property TriggersProperty
    {
      get
      {
        return _triggerProperty;
      }
      set
      {
        _triggerProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the triggers.
    /// </summary>
    /// <value>The triggers.</value>
    public TriggerCollection Triggers
    {
      get
      {
        return (TriggerCollection)_triggerProperty.GetValue();
      }
    }

    /// <summary>
    /// Gets or sets the actual position.
    /// </summary>
    /// <value>The actual position.</value>
    public Vector3 ActualPosition
    {
      get
      {
        return _acutalPosition;
      }
      set
      {
        _acutalPosition = value;
      }
    }

    /// <summary>
    /// Gets or sets the name property.
    /// </summary>
    /// <value>The name property.</value>
    public Property NameProperty
    {
      get
      {
        return _nameProperty;
      }
      set
      {
        _nameProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get
      {
        return _nameProperty.GetValue() as string;
      }
      set
      {
        _nameProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the key property.
    /// </summary>
    /// <value>The key property.</value>
    public Property KeyProperty
    {
      get
      {
        return _keyProperty;
      }
      set
      {
        _keyProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>The key.</value>
    public string Key
    {
      get
      {
        return _keyProperty.GetValue() as string;
      }
      set
      {
        _keyProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the element has focus property.
    /// </summary>
    /// <value>The has focus property.</value>
    public Property HasFocusProperty
    {
      get
      {
        return _hasFocusProperty;
      }
      set
      {
        _hasFocusProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this uielement has focus.
    /// </summary>
    /// <value><c>true</c> if this uielement has focus; otherwise, <c>false</c>.</value>
    public virtual bool HasFocus
    {
      get
      {
        return (bool)_hasFocusProperty.GetValue();
      }
      set
      {
        if (value != HasFocus)
        {
          _hasFocusProperty.SetValue(value);
          if (value)
          {
            FocusManager.FocusedElement = this;
            //Trace.WriteLine(String.Format("focus:{0}", this.GetType()));
            FireEvent("OnGotFocus");
          }
          else
          {
            //Trace.WriteLine(String.Format("no focus:{0}", this.GetType()));
            FireEvent("OnLostFocus");
          }
        }
      }
    }

    /// <summary>
    /// Gets or sets the is focusable property.
    /// </summary>
    /// <value>The is focusable property.</value>
    public Property FocusableProperty
    {
      get
      {
        return _focusableProperty;
      }
      set
      {
        _focusableProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the is focusable.
    /// </summary>
    /// <value>The is focusable.</value>
    public bool Focusable
    {
      get
      {
        return (bool)_focusableProperty.GetValue();
      }
      set
      {
        _focusableProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the is focus scope property.
    /// </summary>
    /// <value>The is focus scope property.</value>
    public Property IsFocusScopeProperty
    {
      get
      {
        return _isFocusScopeProperty;
      }
      set
      {
        _isFocusScopeProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is focus scope.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is focus scope; otherwise, <c>false</c>.
    /// </value>
    public bool IsFocusScope
    {
      get
      {
        return (bool)_isFocusScopeProperty.GetValue();
      }
      set
      {
        _isFocusScopeProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the position property.
    /// </summary>
    /// <value>The position property.</value>
    public Property PositionProperty
    {
      get
      {
        return _positionProperty;
      }
      set
      {
        _positionProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the position.
    /// </summary>
    /// <value>The position.</value>
    public Vector3 Position
    {
      get
      {
        return (Vector3)_positionProperty.GetValue();
      }
      set
      {
        _positionProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the left position.
    /// </summary>
    /// <value>The left position.</value>
    public double Left
    {
      get
      {
        Vector3 v = (Vector3)_positionProperty.GetValue();
        return v.X;
      }
      set
      {
        Vector3 pos = Position;
        pos.X = (float)value;
        Position = pos;
      }
    }

    /// <summary>
    /// Gets or sets the top position.
    /// </summary>
    /// <value>The top position.</value>
    public double Top
    {
      get
      {
        Vector3 v = (Vector3)_positionProperty.GetValue();
        return v.Y;
      }
      set
      {
        Vector3 pos = Position;
        pos.Y = (float)value;
        Position = pos;
      }
    }

    /// <summary>
    /// Gets or sets the dock property.
    /// </summary>
    /// <value>The dock property.</value>
    public Property DockProperty
    {
      get
      {
        return _dockProperty;
      }
      set
      {
        _dockProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the dock.
    /// </summary>
    /// <value>The dock.</value>
    public Dock Dock
    {
      get
      {
        return (Dock)_dockProperty.GetValue();
      }
      set
      {
        _dockProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is visible.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is visible; otherwise, <c>false</c>.
    /// </value>
    public bool IsVisible
    {
      get
      {
        return _isVisible;
      }
      set
      {
        if (value)
          this.Visibility = VisibilityEnum.Visible;
        else
          this.Visibility = VisibilityEnum.Hidden;
      }
    }

    /// <summary>
    /// Gets or sets the margin property.
    /// </summary>
    /// <value>The margin property.</value>
    public Property MarginProperty
    {
      get
      {
        return _marginProperty;
      }
      set
      {
        _marginProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the margin.
    /// </summary>
    /// <value>The margin.</value>
    public Vector4 Margin
    {
      get
      {
        return (Vector4)_marginProperty.GetValue();
      }
      set
      {
        _marginProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the layout transform property.
    /// </summary>
    /// <value>The layout transform property.</value>
    public Property LayoutTransformProperty
    {
      get
      {
        return _layoutTransformProperty;
      }
      set
      {
        _layoutTransformProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the layout transform.
    /// </summary>
    /// <value>The layout transform.</value>
    public Transform LayoutTransform
    {
      get
      {
        return _layoutTransformProperty.GetValue() as Transform;
      }
      set
      {
        _layoutTransformProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the render transform.
    /// </summary>
    /// <value>The render transform.</value>
    public Transform RenderTransform
    {
      get
      {
        return _renderTransform;
      }
      set
      {
        _renderTransform = value;
      }
    }

    /// <summary>
    /// Gets or sets the render transform origin.
    /// </summary>
    /// <value>The render transform origin.</value>
    public Vector2 RenderTransformOrigin
    {
      get
      {
        return _renderTransformOrigin;
      }
      set
      {
        _renderTransformOrigin = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this UIElement has been layout
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this UIElement is arrange valid; otherwise, <c>false</c>.
    /// </value>
    public bool IsArrangeValid
    {
      get
      {
        return _isArrangeValid;
      }
      set
      {
        _isArrangeValid = value;
      }
    }

    /// <summary>
    /// Gets desired size
    /// </summary>
    /// <value>The desired size.</value>
    public SizeF DesiredSize
    {
      get
      {
        return _desiredSize;
      }
    }

    /// <summary>
    /// Gets the size for brush.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public virtual void GetSizeForBrush(out double width, out double height)
    {
      width = 0.0;
      height = 0.0;
    }

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements. </param>
    /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
    public virtual void Measure(SizeF availableSize)
    {
      _availableSize = new SizeF(availableSize.Width, availableSize.Height);
    }

    /// <summary>
    /// Arranges the UI element 
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public virtual void Arrange(System.Drawing.RectangleF finalRect)
    {
      IsArrangeValid = true;
      InitializeBindings();
      InitializeTriggers();
      _isLayoutInvalid = false;
    }

    /// <summary>
    /// Invalidates the layout of this uielement.
    /// If dimensions change, it will invalidate the parent visual so 
    /// the parent will re-layout itself and its children
    /// </summary>
    public virtual void Invalidate()
    {
      if (!IsArrangeValid) return;
      _isLayoutInvalid = true;
      if (VisualParent == null)
        _availableSize = new SizeF(0, 0);
    }

    /// <summary>
    /// Updates the layout.
    /// </summary>
    public virtual void UpdateLayout()
    {
      if (false == _isLayoutInvalid) return;
      //Trace.WriteLine("UpdateLayout :" + this.Name + "  " + this.GetType());
      _isLayoutInvalid = false;
      ExtendedMatrix m = _finalLayoutTransform;
      if (_availableSize.Width > 0 && _availableSize.Height > 0)
      {
        System.Drawing.SizeF sizeOld = new SizeF(_desiredSize.Width, _desiredSize.Height);
        System.Drawing.SizeF availsizeOld = new SizeF(_availableSize.Width, _availableSize.Height);
        if (m != null)
          SkinContext.AddLayoutTransform(m);
        Measure(_availableSize);
        if (m != null)
          SkinContext.RemoveLayoutTransform();
        _availableSize = availsizeOld;
        if (_desiredSize == sizeOld)
        {
          if (m != null)
            SkinContext.AddLayoutTransform(m);
          Arrange(_finalRect);
          if (m != null)
            SkinContext.RemoveLayoutTransform();
          return;
        }
      }
      if (VisualParent != null)
      {
        VisualParent.Invalidate();
        VisualParent.UpdateLayout();
      }
      else
      {
        FrameworkElement element = this as FrameworkElement;
        if (element == null)
        {
          SizeF size = new SizeF(SkinContext.Width * SkinContext.Zoom.Width, SkinContext.Height * SkinContext.Zoom.Height);
          Measure(size);
          Arrange(new System.Drawing.RectangleF(0, 0, (float)size.Width, (float)size.Height));
        }
        else
        {
          float w = (float)element.Width * SkinContext.Zoom.Width;
          float h = (float)element.Height * SkinContext.Zoom.Height;
          if (w == 0) w = (float)SkinContext.Width * SkinContext.Zoom.Width;
          if (h == 0) h = (float)SkinContext.Height * SkinContext.Zoom.Height;
          if (m != null)
            SkinContext.AddLayoutTransform(m);
          Measure(new SizeF(w, h));
          if (m != null)
            SkinContext.RemoveLayoutTransform();

          if (m != null)
            SkinContext.AddLayoutTransform(m);
          Arrange(new System.Drawing.RectangleF((float)element.Position.X * SkinContext.Zoom.Width, (float)element.Position.Y * SkinContext.Zoom.Height, w, h));
          if (m != null)
            SkinContext.RemoveLayoutTransform();
        }
      }
    }

    /// <summary>
    /// Finds the resource with the given keyname
    /// </summary>
    /// <param name="resourceKey">The key name.</param>
    /// <returns>resource, or null if not found.</returns>
    public object FindResource(string resourceKey)
    {
      if (Resources.Contains(resourceKey))
      {
        return Resources[resourceKey];
      }
      if (VisualParent != null)
      {
        return VisualParent.FindResource(resourceKey);
      }
      return null;
    }

    public void InitializeTriggers()
    {
      if (!_triggersInitialized)
      {
        _triggersInitialized = true;
        foreach (Trigger trigger in Triggers)
        {
          trigger.Setup(this);
        }
      }
    }

    /// <summary>
    /// Fires an event.
    /// </summary>
    /// <param name="eventName">Name of the event.</param>
    public virtual void FireEvent(string eventName)
    {
      foreach (Trigger trigger in Triggers)
      {
        EventTrigger eventTrig = trigger as EventTrigger;
        if (eventTrig != null)
        {
          if (eventTrig.RoutedEvent == eventName)
          {
            if (eventTrig.Storyboard != null)
            {
              StartStoryboard(eventTrig.Storyboard as Storyboard);
            }
          }
        }
      }
      if (eventName == "FrameworkElement.Loaded")
      {
        if (Loaded != null)
        {
          _loaded.Execute();
        }
      }
    }

    /// <summary>
    /// Starts the storyboard.
    /// </summary>
    /// <param name="board">The board.</param>
    public void StartStoryboard(Storyboard board)
    {
      Window.Animator.StartStoryboard(board, this);
    }

    /// <summary>
    /// Stops the storyboard.
    /// </summary>
    /// <param name="board">The board.</param>
    public void StopStoryboard(Storyboard board)
    {
      Window.Animator.StopStoryboard(board, this);
    }

    /// <summary>
    /// Called when the mouse moves
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public virtual void OnMouseMove(float x, float y)
    {
    }

    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public virtual void OnKeyPressed(ref Key key)
    {
    }
    public virtual bool ReplaceElementType(Type t, UIElement newElement)
    {
      return false;
    }

    /// <summary>
    /// Find the element with name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public virtual UIElement FindElement(string name)
    {
      if (Name == name)
        return this;
      return null;
    }

    /// <summary>
    /// Finds the element of type t.
    /// </summary>
    /// <param name="t">The t.</param>
    /// <returns></returns>
    public virtual UIElement FindElementType(Type t)
    {
      if (this.GetType() == t) return this;
      return null;
    }

    /// <summary>
    /// Finds the the element which is a ItemsHost
    /// </summary>
    /// <returns></returns>
    public virtual UIElement FindItemsHost()
    {
      if (IsItemsHost) return this;
      return null;
    }

    /// <summary>
    /// Finds the focused item.
    /// </summary>
    /// <returns></returns>
    public virtual UIElement FindFocusedItem()
    {
      if (HasFocus) return this;
      return null;
    }

    #region IBindingCollection Members

    public void Add(Binding binding)
    {
      _bindings.Add(binding);
    }

    public virtual void InitializeBindings()
    {
      if (!_bindingsInitialized)
      {
        _bindingsInitialized = true;
        foreach (Binding binding in _bindings)
        {
          binding.Initialize(this);
        }
      }
      if (_fireLoaded)
      {
        FireEvent("FrameworkElement.Loaded");
        _fireLoaded = false;
      }
    }

    public virtual void Reset()
    {
      _fireLoaded = true;
    }

    public virtual void Allocate()
    {
    }

    public virtual void Deallocate()
    {
    }

    public virtual void SetWindow(Window window)
    {
      Window = window;
    }
    #endregion
  }
}
