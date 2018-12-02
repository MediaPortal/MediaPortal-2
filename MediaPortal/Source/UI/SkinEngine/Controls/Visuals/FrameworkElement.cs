#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

// Define DEBUG_LAYOUT to make MP log screen layouting information. That will slow down the layouting process significantly
// but can be used to find layouting bugs. Don't use that switch in release builds.
// Use DEBUG_MORE_LAYOUT to get more information, also for skipped method calls.
//#define DEBUG_LAYOUT
//#define DEBUG_MORE_LAYOUT

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.Fonts;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using SharpDX;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using Effect = MediaPortal.UI.SkinEngine.Controls.Visuals.Effects2D.Effect;
using Transform = MediaPortal.UI.SkinEngine.Controls.Transforms.Transform;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public enum VerticalAlignmentEnum
  {
    Top = 0,
    Center = 1,
    Bottom = 2,
    Stretch = 3,
  };

  public enum HorizontalAlignmentEnum
  {
    Left = 0,
    Center = 1,
    Right = 2,
    Stretch = 3,
  };

  public enum MoveFocusDirection
  {
    Up,
    Down,
    Left,
    Right
  }

  public class SetElementStateAction : IUIElementAction
  {
    protected ElementState _state;

    public SetElementStateAction(ElementState state)
    {
      _state = state;
    }

    public void Execute(UIElement element)
    {
      element.ElementState = _state;
    }
  }

  public enum ElementState
  {
    /// <summary>
    /// The element is used as template, style resource or during resource creation.
    /// </summary>
    Available,

    /// <summary>
    /// The element doesn't participate in the render run yet but is being prepared. This means we can take some shortcuts
    /// when setting properties.
    /// </summary>
    Preparing,

    /// <summary>
    /// The element participates in the render run. In this state, the render thread is the only thread which may access several
    /// properties of the elements in the screens (only some exceptions for properties which can be accessed by the input thread).
    /// </summary>
    Running,

    /// <summary>
    /// The element is (being) disposed.
    /// </summary>
    Disposing
  }

  public class FrameworkElement : UIElement
  {
    #region Constants

    public const string GOTFOCUS_EVENT = "FrameworkElement.GotFocus";
    public const string LOSTFOCUS_EVENT = "FrameworkElement.LostFocus";
    public const string MOUSEENTER_EVENT = "FrameworkElement.MouseEnter";
    public const string MOUSELEAVE_EVENT = "FrameworkElement.MouseLeave";
    public const string TOUCHENTER_EVENT = "FrameworkElement.TouchEnter";
    public const string TOUCHLEAVE_EVENT = "FrameworkElement.TouchLeave";

    protected const string GLOBAL_RENDER_TEXTURE_ASSET_KEY = "SkinEngine::GlobalRenderTexture";
    protected const string GLOBAL_RENDER_SURFACE_ASSET_KEY = "SkinEngine::GlobalRenderSurface";

    protected static Color4 TRANSPARENT_BLACK = new Color4(Color3.Black, 0);

    #endregion

    #region Protected fields

    protected AbstractProperty _widthProperty;
    protected AbstractProperty _heightProperty;

    protected AbstractProperty _actualWidthProperty;
    protected AbstractProperty _actualHeightProperty;
    protected AbstractProperty _minWidthProperty;
    protected AbstractProperty _minHeightProperty;
    protected AbstractProperty _maxWidthProperty;
    protected AbstractProperty _maxHeightProperty;
    protected AbstractProperty _horizontalAlignmentProperty;
    protected AbstractProperty _verticalAlignmentProperty;
    protected AbstractProperty _styleProperty;
    protected AbstractProperty _focusableProperty;
    protected AbstractProperty _hasFocusProperty;
    protected AbstractProperty _isKeyboardFocusWithinProperty;
    protected AbstractProperty _isMouseOverProperty;
    protected AbstractProperty _fontSizeProperty;
    protected AbstractProperty _fontFamilyProperty;
    protected AbstractProperty _fontWeightProperty;
    protected AbstractProperty _fontStyleProperty;

    protected AbstractProperty _contextMenuCommandProperty;

    protected PrimitiveBuffer _opacityMaskContext;
    protected PrimitiveBuffer _effectContext;
    protected static Effects.Effect _defaultEffect;

    protected bool _updateOpacityMask = false;
    protected RawRectangleF _lastOccupiedTransformedBounds = RectangleF.Empty;

    protected bool _styleSet = false;

    protected volatile SetFocusPriority _setFocusPrio = SetFocusPriority.None;

    protected Size2F? _availableSize = null;
    protected RectangleF? _outerRect = null;
    protected RectangleF? _renderedBoundingBox = null;

    protected Size2F _innerDesiredSize; // Desiredd size in local coords
    protected Size2F _desiredSize; // Desired size in parent coordinate system
    protected RawRectangleF _innerRect;
    protected volatile bool _isMeasureInvalid = true;
    protected volatile bool _isArrangeInvalid = true;

    protected Matrix3x2? _inverseFinalTransform = null;
    protected Matrix3x2? _finalTransform = null;

    protected float _lastZIndex = 0;
    private RenderTarget2DAsset _effectInput;

    #endregion

    #region Ctor & maintainance

    public FrameworkElement()
    {
      Init();
      Attach();
    }

    void Init()
    {
      // Default is not set
      _widthProperty = new SProperty(typeof(double), Double.NaN);
      _heightProperty = new SProperty(typeof(double), Double.NaN);

      // Default is not set
      _actualWidthProperty = new SProperty(typeof(double), Double.NaN);
      _actualHeightProperty = new SProperty(typeof(double), Double.NaN);

      // Min/Max width
      _minWidthProperty = new SProperty(typeof(double), 0.0);
      _minHeightProperty = new SProperty(typeof(double), 0.0);
      _maxWidthProperty = new SProperty(typeof(double), double.MaxValue);
      _maxHeightProperty = new SProperty(typeof(double), double.MaxValue);

      // Default is not set
      _styleProperty = new SProperty(typeof(Style), null);

      // Default is stretch
      _horizontalAlignmentProperty = new SProperty(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Stretch);
      _verticalAlignmentProperty = new SProperty(typeof(VerticalAlignmentEnum), VerticalAlignmentEnum.Stretch);

      // Focus properties
      _focusableProperty = new SProperty(typeof(bool), false);
      _hasFocusProperty = new SProperty(typeof(bool), false);
      _isKeyboardFocusWithinProperty = new SProperty(typeof(bool), false);

      _isMouseOverProperty = new SProperty(typeof(bool), false);

      // Context menu
      _contextMenuCommandProperty = new SProperty(typeof(IExecutableCommand), null);

      // Font properties
      _fontFamilyProperty = new SProperty(typeof(string), String.Empty);
      _fontSizeProperty = new SProperty(typeof(int), -1);
      _fontWeightProperty = new SProperty(typeof(FontWeight?), null);
      _fontStyleProperty = new SProperty(typeof(FontStyle?), null);
    }

    void Attach()
    {
      _widthProperty.Attach(OnMeasureGetsInvalid);
      _heightProperty.Attach(OnMeasureGetsInvalid);
      _actualHeightProperty.Attach(OnActualBoundsChanged);
      _actualWidthProperty.Attach(OnActualBoundsChanged);
      _styleProperty.Attach(OnStyleChanged);

      LayoutTransformProperty.Attach(OnLayoutTransformPropertyChanged);
      MarginProperty.Attach(OnMeasureGetsInvalid);
      VisibilityProperty.Attach(OnParentCompleteLayoutGetsInvalid);
      OpacityProperty.Attach(OnOpacityChanged);
      OpacityMaskProperty.Attach(OnOpacityChanged);
      ActualPositionProperty.Attach(OnActualBoundsChanged);
      IsEnabledProperty.Attach(OnEnabledChanged);

      HasFocusProperty.Attach(OnHasFocusChanged);
    }

    void Detach()
    {
      _widthProperty.Detach(OnMeasureGetsInvalid);
      _heightProperty.Detach(OnMeasureGetsInvalid);
      _actualHeightProperty.Detach(OnActualBoundsChanged);
      _actualWidthProperty.Detach(OnActualBoundsChanged);
      _styleProperty.Detach(OnStyleChanged);

      LayoutTransformProperty.Detach(OnLayoutTransformPropertyChanged);
      MarginProperty.Detach(OnMeasureGetsInvalid);
      VisibilityProperty.Detach(OnParentCompleteLayoutGetsInvalid);
      OpacityProperty.Detach(OnOpacityChanged);
      OpacityMaskProperty.Detach(OnOpacityChanged);
      ActualPositionProperty.Detach(OnActualBoundsChanged);
      IsEnabledProperty.Detach(OnEnabledChanged);

      HasFocusProperty.Detach(OnHasFocusChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      object oldLayoutTransform = LayoutTransform;
      base.DeepCopy(source, copyManager);
      FrameworkElement fe = (FrameworkElement)source;
      Width = fe.Width;
      Height = fe.Height;
      Style = copyManager.GetCopy(fe.Style);
      ActualWidth = fe.ActualWidth;
      ActualHeight = fe.ActualHeight;
      HorizontalAlignment = fe.HorizontalAlignment;
      VerticalAlignment = fe.VerticalAlignment;
      Focusable = fe.Focusable;
      FontSize = fe.FontSize;
      FontFamily = fe.FontFamily;
      FontWeight = fe.FontWeight;
      FontStyle = fe.FontStyle;
      MinWidth = fe.MinWidth;
      MinHeight = fe.MinHeight;
      MaxWidth = fe.MaxWidth;
      MaxHeight = fe.MaxHeight;
      _setFocusPrio = fe.SetFocusPrio;
      _renderedBoundingBox = null;

      ContextMenuCommand = copyManager.GetCopy(fe.ContextMenuCommand);

      // Need to manually call this because we are in a detached state
      OnLayoutTransformPropertyChanged(_layoutTransformProperty, oldLayoutTransform);

      Attach();
    }

    public override void Dispose()
    {
      if (HasFocus || SetFocus)
      {
        FrameworkElement parent = VisualParent as FrameworkElement;
        if (parent != null)
          parent.SetFocus = true;
      }
      MPF.TryCleanupAndDispose(ContextMenuCommand);
      base.Dispose();
      MPF.TryCleanupAndDispose(Style);
    }

    #endregion

    #region Event handlers

    protected override void OnUpdateElementState()
    {
      base.OnUpdateElementState();
      if (PreparingOrRunning && !_styleSet)
        UpdateStyle(null);
    }

    protected void UpdateStyle(Style oldStyle)
    {
      bool changed = false;
      if (oldStyle != null)
      {
        changed = true;
        oldStyle.Reset(this);
        MPF.TryCleanupAndDispose(oldStyle);
      }
      if (Style != null)
      {
        changed = true;
        Style.Set(this);
        _styleSet = true;
      }
      if (changed)
        InvalidateLayout(true, true);
    }

    protected virtual void OnStyleChanged(AbstractProperty property, object oldValue)
    {
      if (!PreparingOrRunning)
        return;
      UpdateStyle((Style)oldValue);
    }

    void OnActualBoundsChanged(AbstractProperty property, object oldValue)
    {
      _updateOpacityMask = true;
    }

    void OnEnabledChanged(AbstractProperty property, object oldValue)
    {
      if (!IsEnabled)
        ResetFocus();
    }

    void OnHasFocusChanged(AbstractProperty property, object oldValue)
    {
      // propagate up the Visual Tree
      var visual = VisualParent;
      while (visual != null)
      {
        var fe = visual as FrameworkElement;
        if (fe != null)
          fe.IsKeyboardFocusWithin = HasFocus;
        visual = visual.VisualParent;
      }
    }

    void OnLayoutTransformChanged(IObservable observable)
    {
      InvalidateLayout(true, true);
    }

    void OnLayoutTransformPropertyChanged(AbstractProperty property, object oldValue)
    {
      if (oldValue is Transform)
        ((Transform)oldValue).ObjectChanged -= OnLayoutTransformChanged;
      if (LayoutTransform != null)
        LayoutTransform.ObjectChanged += OnLayoutTransformChanged;
    }

    void OnOpacityChanged(AbstractProperty property, object oldValue)
    {
      _updateOpacityMask = true;
    }

    /// <summary>
    /// Called when a property has been changed which makes our arrangement invalid.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="oldValue">The old value of the property.</param>
    protected void OnArrangeGetsInvalid(AbstractProperty property, object oldValue)
    {
      InvalidateLayout(false, true);
    }

    /// <summary>
    /// Called when a property has been changed which makes our measurement invalid.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="oldValue">The old value of the property.</param>
    protected void OnMeasureGetsInvalid(AbstractProperty property, object oldValue)
    {
      InvalidateLayout(true, false);
    }

    /// <summary>
    /// Called when a property has been changed which makes our measurement and our arrangement invalid.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="oldValue">The old value of the property.</param>
    protected void OnCompleteLayoutGetsInvalid(AbstractProperty property, object oldValue)
    {
      InvalidateLayout(true, true);
    }

    /// <summary>
    /// Called when a property has been changed which makes our parent's measurement and its arrangement invalid.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="oldValue">The old value of the property.</param>
    protected void OnParentCompleteLayoutGetsInvalid(AbstractProperty property, object oldValue)
    {
      InvalidateParentLayout(true, true);
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Returns the desired size this element calculated based on the available size.
    /// This value denotes the desired size of this element including its <see cref="UIElement.Margin"/> in the parent's coordinate
    /// system, i.e. with the <see cref="UIElement.RenderTransform"/> and <see cref="UIElement.LayoutTransform"/> applied.
    /// </summary>
    public Size2F DesiredSize
    {
      get { return _desiredSize; }
    }

    public AbstractProperty WidthProperty
    {
      get { return _widthProperty; }
    }

    public double Width
    {
      get { return (double)_widthProperty.GetValue(); }
      set { _widthProperty.SetValue(value); }
    }

    public AbstractProperty HeightProperty
    {
      get { return _heightProperty; }
    }

    public double Height
    {
      get { return (double)_heightProperty.GetValue(); }
      set { _heightProperty.SetValue(value); }
    }

    public AbstractProperty ActualWidthProperty
    {
      get { return _actualWidthProperty; }
    }

    public double ActualWidth
    {
      get { return (double)_actualWidthProperty.GetValue(); }
      set { _actualWidthProperty.SetValue(value); }
    }

    public AbstractProperty ActualHeightProperty
    {
      get { return _actualHeightProperty; }
    }

    public double ActualHeight
    {
      get { return (double)_actualHeightProperty.GetValue(); }
      set { _actualHeightProperty.SetValue(value); }
    }

    public AbstractProperty MinWidthProperty
    {
      get { return _minWidthProperty; }
    }

    public double MinWidth
    {
      get { return (double)_minWidthProperty.GetValue(); }
      set { _minWidthProperty.SetValue(value); }
    }

    public AbstractProperty MinHeightProperty
    {
      get { return _minHeightProperty; }
    }

    public double MinHeight
    {
      get { return (double)_minHeightProperty.GetValue(); }
      set { _minHeightProperty.SetValue(value); }
    }

    public AbstractProperty MaxWidthProperty
    {
      get { return _maxWidthProperty; }
    }

    public double MaxWidth
    {
      get { return (double)_maxWidthProperty.GetValue(); }
      set { _maxWidthProperty.SetValue(value); }
    }

    public AbstractProperty MaxHeightProperty
    {
      get { return _maxHeightProperty; }
    }

    public double MaxHeight
    {
      get { return (double)_maxHeightProperty.GetValue(); }
      set { _maxHeightProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets this element's bounds in this element's coordinate system.
    /// This is a derived property which is calculated by the layout system.
    /// </summary>
    public RawRectangleF ActualBounds
    {
      get { return _innerRect; }
    }

    /// <summary>
    /// Gets the actual bounds in layout plus <see cref="UIElement.Margin"/> plus the space which is needed for our
    /// <see cref="UIElement.LayoutTransform"/> in the coordinate system of the parent.
    /// Render transformations aren't taken into account here.
    /// </summary>
    public RectangleF ActualTotalBounds
    {
      get { return _outerRect ?? RectangleF.Empty; }
    }

    /// <summary>
    /// Gets the box which encloses the position where this control is rendered in the world coordinate system.
    /// </summary>
    /// <remarks>
    /// This property is only set if this element was rendered at its current position. If it was not yet rendered or rearranged
    /// after the last rendering, this property is <c>null</c>.
    /// </remarks>
    public RectangleF? RenderedBoundingBox
    {
      get { return _renderedBoundingBox; }
    }

    /// <summary>
    /// Gets the best known bounding box of the render area of this element.
    /// </summary>
    /// <remarks>
    /// This property returns the <see cref="RenderedBoundingBox"/>, if present. Else, it will calculate the final transform which would
    /// be used by this element if it would have been rendered before.
    /// </remarks>
    public RawRectangleF BoundingBox
    {
      get { return _renderedBoundingBox ?? CalculateBoundingBox(_innerRect, ExtortFinalTransform()); }
    }

    public AbstractProperty HorizontalAlignmentProperty
    {
      get { return _horizontalAlignmentProperty; }
    }

    public HorizontalAlignmentEnum HorizontalAlignment
    {
      get { return (HorizontalAlignmentEnum)_horizontalAlignmentProperty.GetValue(); }
      set { _horizontalAlignmentProperty.SetValue(value); }
    }

    public AbstractProperty VerticalAlignmentProperty
    {
      get { return _verticalAlignmentProperty; }
    }

    public VerticalAlignmentEnum VerticalAlignment
    {
      get { return (VerticalAlignmentEnum)_verticalAlignmentProperty.GetValue(); }
      set { _verticalAlignmentProperty.SetValue(value); }
    }

    public AbstractProperty StyleProperty
    {
      get { return _styleProperty; }
    }

    /// <summary>
    /// Style of this element. This property must be set to a sensible value, else, this element cannot be rendered.
    /// This property can be guessed by assigning te return value of <see cref="CopyDefaultStyle"/>.
    /// </summary>
    public Style Style
    {
      get { return (Style)_styleProperty.GetValue(); }
      set { _styleProperty.SetValue(value); }
    }

    /// <summary>
    /// Helper property to make it possible in the screenfiles to set the focus to a framework element (or its first focusable child)
    /// when the screen is initialized. Use this property to set the initial focus.
    /// </summary>
    public SetFocusPriority SetFocusPrio
    {
      get { return _setFocusPrio; }
      set
      {
        _setFocusPrio = value;
        if (value > SetFocusPriority.None)
          InvalidateLayout(false, true);
      }
    }

    public bool SetFocus
    {
      get { return _setFocusPrio > SetFocusPriority.None; }
      set { _setFocusPrio = value ? SetFocusPriority.Default : SetFocusPriority.None; }
    }

    public AbstractProperty HasFocusProperty
    {
      get { return _hasFocusProperty; }
    }

    /// <summary>
    /// Returns the information whether this element currently has the focus. This element should not be set from the GUI!
    /// </summary>
    public virtual bool HasFocus
    {
      get { return (bool)_hasFocusProperty.GetValue(); }
      internal set { _hasFocusProperty.SetValue(value); }
    }

    public AbstractProperty IsKeyboardFocusWithinProperty
    {
      get { return _isKeyboardFocusWithinProperty; }
    }

    /// <summary>
    /// Gets a value indicating whether keyboard focus is anywhere within the element or its visual tree child elements.
    /// </summary>
    public virtual bool IsKeyboardFocusWithin
    {
      get { return (bool)_isKeyboardFocusWithinProperty.GetValue(); }
      internal set { _isKeyboardFocusWithinProperty.SetValue(value); }
    }

    public AbstractProperty FocusableProperty
    {
      get { return _focusableProperty; }
    }

    public bool Focusable
    {
      get { return (bool)_focusableProperty.GetValue(); }
      set { _focusableProperty.SetValue(value); }
    }

    public AbstractProperty IsMouseOverProperty
    {
      get { return _isMouseOverProperty; }
    }

    public bool IsMouseOver
    {
      get { return (bool)_isMouseOverProperty.GetValue(); }
      internal set { _isMouseOverProperty.SetValue(value); }
    }

    public AbstractProperty ContextMenuCommandProperty
    {
      get { return _contextMenuCommandProperty; }
    }

    public IExecutableCommand ContextMenuCommand
    {
      get { return (IExecutableCommand)_contextMenuCommandProperty.GetValue(); }
      internal set { _contextMenuCommandProperty.SetValue(value); }
    }

    public AbstractProperty FontFamilyProperty
    {
      get { return _fontFamilyProperty; }
    }

    // FontFamily & FontSize are defined in FrameworkElement because it should also be defined on
    // panels, and in MPF, panels inherit from FrameworkElement
    public string FontFamily
    {
      get { return (string)_fontFamilyProperty.GetValue(); }
      set { _fontFamilyProperty.SetValue(value); }
    }

    public AbstractProperty FontSizeProperty
    {
      get { return _fontSizeProperty; }
    }

    // FontFamily & FontSize are defined in FrameworkElement because it should also be defined on
    // panels, and in MPF, panels inherit from FrameworkElement
    public int FontSize
    {
      get { return (int)_fontSizeProperty.GetValue(); }
      set { _fontSizeProperty.SetValue(value); }
    }

    public AbstractProperty FontWeightProperty
    {
      get { return _fontWeightProperty; }
    }

    // FontFamily & FontSize are defined in FrameworkElement because it should also be defined on
    // panels, and in MPF, panels inherit from FrameworkElement
    public FontWeight? FontWeight
    {
      get { return (FontWeight?)_fontWeightProperty.GetValue(); }
      set { _fontWeightProperty.SetValue(value); }
    }

    public AbstractProperty FontStyleProperty
    {
      get { return _fontStyleProperty; }
    }

    // FontFamily & FontSize are defined in FrameworkElement because it should also be defined on
    // panels, and in MPF, panels inherit from FrameworkElement
    public FontStyle? FontStyle
    {
      get { return (FontStyle?)_fontStyleProperty.GetValue(); }
      set { _fontStyleProperty.SetValue(value); }
    }

    public bool IsMeasureInvalid
    {
      get { return _isMeasureInvalid; }
    }

    public bool IsArrangeInvalid
    {
      get { return _isArrangeInvalid; }
    }

    #endregion

    #region Font handling

    public string GetFontFamilyOrInherited()
    {
      string result = FontFamily;
      Visual current = this;
      while (string.IsNullOrEmpty(result) && (current = current.VisualParent) != null)
      {
        FrameworkElement currentFE = current as FrameworkElement;
        if (currentFE != null)
          result = currentFE.FontFamily;
      }
      return string.IsNullOrEmpty(result) ? FontManager.DefaultFontFamily : result;
    }

    public int GetFontSizeOrInherited()
    {
      int result = FontSize;
      Visual current = this;
      while (result == -1 && (current = current.VisualParent) != null)
      {
        FrameworkElement currentFE = current as FrameworkElement;
        if (currentFE != null)
          result = currentFE.FontSize;
      }
      return result == -1 ? FontManager.DefaultFontSize : result;
    }

    public FontWeight GetFontWeightOrInherited()
    {
      FontWeight? result = FontWeight;
      Visual current = this;
      while (!result.HasValue && (current = current.VisualParent) != null)
      {
        FrameworkElement currentFE = current as FrameworkElement;
        if (currentFE != null)
          result = currentFE.FontWeight;
      }
      return result.HasValue ? result.Value : FontManager.DefaultFontWeight;
    }

    public FontStyle GetFontStyleOrInherited()
    {
      FontStyle? result = FontStyle;
      Visual current = this;
      while (!result.HasValue && (current = current.VisualParent) != null)
      {
        FrameworkElement currentFE = current as FrameworkElement;
        if (currentFE != null)
          result = currentFE.FontStyle;
      }
      return result.HasValue ? result.Value : FontManager.DefaultFontStyle;
    }

    #endregion

    #region Keyboard handling

    internal override void OnKeyPreview(ref Key key)
    {
      base.OnKeyPreview(ref key);
      if (!HasFocus)
        return;
      if (key == Key.None) return;
      IExecutableCommand cmd = ContextMenuCommand;
      if (key == Key.ContextMenu && ContextMenuCommand != null)
      {
        if (cmd != null)
          InputManager.Instance.ExecuteCommand(cmd.Execute);
        key = Key.None;
      }
    }

    #endregion

    #region hit testing

    public override UIElement InputHitTest(Vector2 point)
    {
      if (!IsVisible)
        return null;

      if (IsInArea(point.X, point.Y))
      {
        // since we know the z-order here, lets use it, everything other is identical to UIElement implementation.
        foreach (var uiElement in GetChildren().OrderByDescending(e => (e is FrameworkElement) ? ((FrameworkElement)e)._lastZIndex : 0f))
        {
          var hitElement = uiElement.InputHitTest(point);
          if (hitElement != null)
          {
            return hitElement;
          }
        }
        if (IsInVisibleArea(point.X, point.Y))
        {
          return this;
        }
      }
      return null;
    }

    #endregion

    #region Mouse handling

    protected bool TransformMouseCoordinates(ref float x, ref float y)
    {
      Matrix3x2? ift = _inverseFinalTransform;
      if (ift.HasValue)
      {
        ift.Value.Transform(ref x, ref y);
        return true;
      }
      return false;
    }

    public bool CanHandleMouseMove()
    {
      return _inverseFinalTransform.HasValue;
    }

    internal override void OnMouseMove(float x, float y, ICollection<FocusCandidate> focusCandidates)
    {
      if (IsVisible)
      {
        if (IsInVisibleArea(x, y))
        {
          if (!IsMouseOver)
          {
            IsMouseOver = true;
            FireEvent(MOUSEENTER_EVENT, RoutingStrategyEnum.Direct);
          }
          focusCandidates.Add(new FocusCandidate(this, _lastZIndex));
        }
        else
        {
          if (IsMouseOver)
          {
            IsMouseOver = false;
            FireEvent(MOUSELEAVE_EVENT, RoutingStrategyEnum.Direct);
          }
        }
      }
      base.OnMouseMove(x, y, focusCandidates);
    }

    #endregion

    #region Focus handling

    /// <summary>
    /// Checks if this element is focusable. This is the case if the element is visible, enabled and
    /// focusable. If this is the case, this method will set the focus to this element.
    /// </summary>
    /// <param name="checkChildren">If this parameter is set to <c>true</c>, this method will also try to
    /// set the focus to any of its child elements.</param>
    public bool TrySetFocus(bool checkChildren)
    {
      if (HasFocus)
        return true;
      if (IsVisible && IsEnabled && Focusable)
      {
        Screen screen = Screen;
        if (screen == null)
          return false;
        RawRectangleF actualBounds = ActualBounds;
        BringIntoView(this, actualBounds);
        screen.UpdateFocusRect(actualBounds);
        screen.FrameworkElementGotFocus(this);
        HasFocus = true;
        return true;
      }
      if (checkChildren)
        return GetChildren().OfType<FrameworkElement>().Any(fe => fe.TrySetFocus(true));
      return false;
    }

    public void ResetFocus()
    {
      HasFocus = false;
      Screen screen = Screen;
      if (screen == null)
        return;
      screen.FrameworkElementLostFocus(this);
    }

    protected void UpdateFocus()
    {
      if (_setFocusPrio == SetFocusPriority.None)
        return;
      Screen screen = Screen;
      if (screen == null)
        return;
      screen.ScheduleSetFocus(this, _setFocusPrio);
      _setFocusPrio = SetFocusPriority.None;
    }

    /// <summary>
    /// Checks if the currently focused control is contained in this virtual keyboard control.
    /// </summary>
    public bool IsInFocusRootPath()
    {
      Screen screen = Screen;
      Visual focusPath = screen == null ? null : screen.FocusedElement;
      while (focusPath != null)
      {
        if (focusPath == this)
          // Focused control is located in our focus scope
          return true;
        focusPath = focusPath.VisualParent;
      }
      return false;
    }

    #endregion

    #region Focus & control predicition

    #region Focus movement

    protected FrameworkElement GetFocusedElementOrChild()
    {
      Screen screen = Screen;
      FrameworkElement result = screen == null ? null : screen.FocusedElement;
      if (result == null)
        foreach (UIElement child in GetChildren())
        {
          result = child as FrameworkElement;
          if (result != null)
            break;
        }
      return result;
    }

    /// <summary>
    /// Moves the focus from the currently focused element in the screen to the first child element in the given
    /// direction.
    /// </summary>
    /// <param name="direction">Direction to move the focus.</param>
    /// <returns><c>true</c>, if the focus could be moved to the desired child, else <c>false</c>.</returns>
    protected bool MoveFocus1(MoveFocusDirection direction)
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      FrameworkElement nextElement = PredictFocus(currentElement.ActualBounds, direction);
      if (nextElement == null) return false;
      return nextElement.TrySetFocus(true);
    }

    /// <summary>
    /// Moves the focus from the currently focused element in the screen to our last child in the given
    /// direction. For example if <c>direction == MoveFocusDirection.Up</c>, this method tries to focus the
    /// topmost child.
    /// </summary>
    /// <param name="direction">Direction to move the focus.</param>
    /// <returns><c>true</c>, if the focus could be moved to the desired child, else <c>false</c>.</returns>
    protected bool MoveFocusN(MoveFocusDirection direction)
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      ICollection<FrameworkElement> focusableChildren = GetFEChildren();
      if (focusableChildren.Count == 0)
        return false;
      FrameworkElement nextElement;
      while ((nextElement = FindNextFocusElement(focusableChildren, currentElement.ActualBounds, direction)) != null)
        currentElement = nextElement;
      return currentElement.TrySetFocus(true);
    }

    #endregion

    #region Focus prediction

    /// <summary>
    /// Predicts the next control located inside this element which is positioned in the specified direction
    /// <paramref name="dir"/> to the specified <paramref name="currentFocusRect"/> and
    /// which is able to get the focus.
    /// This method will search the control tree down starting with this element as root element.
    /// This method is only able to find focusable elements which are located at least one element outside the visible
    /// range (see <see cref="AddPotentialFocusableElements"/>).
    /// </summary>
    /// <param name="currentFocusRect">The borders of the currently focused control.</param>
    /// <param name="dir">Direction, the result control should be positioned relative to the
    /// currently focused control.</param>
    /// <returns>Framework element which should get the focus, or <c>null</c>, if this control
    /// tree doesn't contain an appropriate control. The returned control will be
    /// visible, focusable and enabled.</returns>
    public virtual FrameworkElement PredictFocus(RawRectangleF? currentFocusRect, MoveFocusDirection dir)
    {
      if (!IsVisible || !IsEnabled)
        return null;
      ICollection<FrameworkElement> focusableChildren = new List<FrameworkElement>();
      AddPotentialFocusableElements(currentFocusRect, focusableChildren);
      // Check child controls
      if (focusableChildren.Count == 0)
        return null;
      if (!currentFocusRect.HasValue)
        return focusableChildren.First();
      FrameworkElement result = FindNextFocusElement(focusableChildren, currentFocusRect, dir);
      if (result != null)
        return result;
      return null;
    }

    /// <summary>
    /// Searches through a collection of elements to find the best matching next focus element.
    /// </summary>
    /// <param name="potentialNextFocusElements">Collection of elements to search.</param>
    /// <param name="currentFocusRect">Bounds of the element which currently has focus.</param>
    /// <param name="dir">Direction to move the focus.</param>
    /// <returns>Next focusable element in the given <paramref name="dir"/> or <c>null</c>, if the given
    /// <paramref name="potentialNextFocusElements"/> don't contain a focusable element in the given direction.</returns>
    protected static FrameworkElement FindNextFocusElement(IEnumerable<FrameworkElement> potentialNextFocusElements,
        RawRectangleF? currentFocusRect, MoveFocusDirection dir)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      float bestCenterDistance = float.MaxValue;
      float bestTopOrLeftDifference = float.MaxValue;
      if (!currentFocusRect.HasValue)
        return null;
      foreach (FrameworkElement child in potentialNextFocusElements)
      {
        float topOrLeftDifference;
        if ((dir == MoveFocusDirection.Up && child.LocatedAbove(currentFocusRect.Value, out topOrLeftDifference)) ||
            (dir == MoveFocusDirection.Down && child.LocatedBelow(currentFocusRect.Value, out topOrLeftDifference)) ||
            (dir == MoveFocusDirection.Left && child.LocatedLeftOf(currentFocusRect.Value, out topOrLeftDifference)) ||
            (dir == MoveFocusDirection.Right && child.LocatedRightOf(currentFocusRect.Value, out topOrLeftDifference)))
        { // Calculate and compare distances of all matches
          float centerDistance = CenterDistance(child.ActualBounds, currentFocusRect.Value);
          if (centerDistance == 0)
            // If the child's center is exactly the center of the currently focused element,
            // it won't be used as next focus element
            continue;
          float distance = BorderDistance(child.ActualBounds, currentFocusRect.Value);
          if (bestMatch == null || (distance < bestDistance && topOrLeftDifference < 0)
            || (topOrLeftDifference < bestTopOrLeftDifference && (distance == bestDistance || bestTopOrLeftDifference >= 0))
            /*|| topOrLeftDifference == bestTopOrLeftDifference && centerDistance < bestCenterDistance*/)
          {
            bestMatch = child;
            bestDistance = distance;
            bestCenterDistance = centerDistance;
            bestTopOrLeftDifference = topOrLeftDifference;
          }
        }
      }
      return bestMatch;
    }

    protected static float BorderDistance(RawRectangleF r1, RawRectangleF r2)
    {
      float distX;
      float distY;
      if (r1.Left > r2.Right)
        distX = r1.Left - r2.Right;
      else if (r1.Right < r2.Left)
        distX = r2.Left - r1.Right;
      else
        distX = 0;
      if (r1.Top > r2.Bottom)
        distY = r1.Top - r2.Bottom;
      else if (r1.Bottom < r2.Top)
        distY = r2.Top - r1.Bottom;
      else
        distY = 0;
      return (float)Math.Sqrt(distX * distX + distY * distY);
    }

    protected static float CenterDistance(RawRectangleF r1, RawRectangleF r2)
    {
      float distX = Math.Abs((r1.Left + r1.Right) / 2 - (r2.Left + r2.Right) / 2);
      float distY = Math.Abs((r1.Top + r1.Bottom) / 2 - (r2.Top + r2.Bottom) / 2);
      return (float)Math.Sqrt(distX * distX + distY * distY);
    }

    /// <summary>
    /// Calculates the horizontal distance from <paramref name="r1"/> to <paramref name="r2"/>.
    /// </summary>
    /// <param name="r1"></param>
    /// <param name="r2"></param>
    /// <returns>Horizontal distance, negative if there is an overlap.</returns>
    protected static float HorizontalDistance(RawRectangleF r1, RawRectangleF r2)
    {
      if (r1.Right <= r2.Left)
        return r2.Left - r1.Right;
      if (r1.Left >= r2.Right)
        return r1.Left - r2.Right;
      if (r1.Left < r2.Left && r1.Right < r2.Right)
        return r2.Left - r1.Right;
      if (r1.Right > r2.Right && r1.Left > r2.Left)
        return r1.Left - r2.Right;
      return -r1.Width();
    }

    /// <summary>
    /// Calculates the vertical distance from <paramref name="r1"/> to <paramref name="r2"/>.
    /// </summary>
    /// <param name="r1"></param>
    /// <param name="r2"></param>
    /// <returns>Vertical distance, negative if there is an overlap.</returns>
    protected static float VerticalDistance(RawRectangleF r1, RawRectangleF r2)
    {
      if (r1.Bottom <= r2.Top)
        return r2.Top - r1.Bottom;
      if (r1.Top >= r2.Bottom)
        return r1.Top - r2.Bottom;
      if (r1.Top < r2.Top && r1.Bottom < r2.Bottom)
        return r2.Top - r1.Bottom;
      if (r1.Bottom > r2.Bottom && r1.Top > r2.Top)
        return r1.Top - r2.Bottom;
      return -r1.Height();
    }

    protected Vector2 GetCenterPosition(RectangleF rect)
    {
      return new Vector2((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
    }

    private static float CalcDirection(Vector2 start, Vector2 end)
    {
      if (IsNear(start.X, end.X) && IsNear(start.Y, end.Y))
        return float.NaN;
      double x = end.X - start.X;
      double y = end.Y - start.Y;
      double alpha = Math.Acos(x / Math.Sqrt(x * x + y * y));
      if (end.Y > start.Y) // Coordinates go from top to bottom, so y must be inverted
        alpha = -alpha;
      if (alpha < 0)
        alpha += 2 * Math.PI;
      return (float)alpha;
    }

    protected static bool AInsideB(RawRectangleF a, RawRectangleF b)
    {
      return b.Left <= a.Left && b.Right >= a.Right &&
          b.Top <= a.Top && b.Bottom >= a.Bottom;
    }

    protected bool LocatedInside(RawRectangleF otherRect)
    {
      return AInsideB(ActualBounds, otherRect);
    }

    protected bool EnclosesRect(RawRectangleF otherRect)
    {
      return AInsideB(otherRect, ActualBounds);
    }

    protected bool LocatedBelow(RawRectangleF otherRect, out float topOrLeftDifference)
    {
      RawRectangleF actualBounds = ActualBounds;
      bool isNear = IsNear(actualBounds.Top, otherRect.Bottom);
      Vector2 start = new Vector2((actualBounds.Right + actualBounds.Left) / 2, actualBounds.Top);
      Vector2 end = new Vector2((otherRect.Right + otherRect.Left) / 2, otherRect.Bottom);
      float alpha = CalcDirection(start, end);
      topOrLeftDifference = HorizontalDistance(actualBounds, otherRect); //Math.Abs(actualBounds.Left - otherRect.Left);
      return isNear || alpha > DELTA_DOUBLE && alpha < Math.PI - DELTA_DOUBLE;
    }

    protected bool LocatedAbove(RawRectangleF otherRect, out float topOrLeftDifference)
    {
      RawRectangleF actualBounds = ActualBounds;
      bool isNear = IsNear(actualBounds.Bottom, otherRect.Top);
      Vector2 start = new Vector2((actualBounds.Right + actualBounds.Left) / 2, actualBounds.Bottom);
      Vector2 end = new Vector2((otherRect.Right + otherRect.Left) / 2, otherRect.Top);
      float alpha = CalcDirection(start, end);
      topOrLeftDifference = HorizontalDistance(actualBounds, otherRect); //Math.Abs(actualBounds.Left - otherRect.Left);
      return isNear || alpha > Math.PI + DELTA_DOUBLE && alpha < 2 * Math.PI - DELTA_DOUBLE;
    }

    protected bool LocatedLeftOf(RawRectangleF otherRect, out float topOrLeftDifference)
    {
      RawRectangleF actualBounds = ActualBounds;
      bool isNear = IsNear(actualBounds.Right, otherRect.Left);
      Vector2 start = new Vector2(actualBounds.Right, (actualBounds.Top + actualBounds.Bottom) / 2);
      Vector2 end = new Vector2(otherRect.Left, (otherRect.Top + otherRect.Bottom) / 2);
      float alpha = CalcDirection(start, end);
      topOrLeftDifference = VerticalDistance(actualBounds, otherRect); //Math.Abs(actualBounds.Top - otherRect.Top);
      return isNear || alpha < Math.PI / 2 - DELTA_DOUBLE || alpha > 3 * Math.PI / 2 + DELTA_DOUBLE;
    }

    protected bool LocatedRightOf(RawRectangleF otherRect, out float topOrLeftDifference)
    {
      RawRectangleF actualBounds = ActualBounds;
      bool isNear = IsNear(actualBounds.Left, otherRect.Right);
      Vector2 start = new Vector2(actualBounds.Left, (actualBounds.Top + actualBounds.Bottom) / 2);
      Vector2 end = new Vector2(otherRect.Right, (otherRect.Top + otherRect.Bottom) / 2);
      float alpha = CalcDirection(start, end);
      topOrLeftDifference = VerticalDistance(actualBounds, otherRect); //Math.Abs(actualBounds.Top - otherRect.Top);
      return isNear || alpha > Math.PI / 2 + DELTA_DOUBLE && alpha < 3 * Math.PI / 2 - DELTA_DOUBLE;
    }

    /// <summary>
    /// Collects all focusable elements in the element tree starting with this element which are potentially located next
    /// to the given <paramref name="startingRect"/>.
    /// </summary>
    /// <remarks>
    /// This default implementation simply returns this element and all children, but sub classes might restrict the
    /// result collection.
    /// The less elements are returned, the faster the focusing engine can find an element to be focused.
    /// </remarks>
    /// <param name="startingRect">Rectangle where to start searching. If this parameter is <c>null</c> (i.e. has no value),
    /// all potential focusable elements should be returned.</param>
    /// <param name="elements">Collection to add elements which are able to get the focus.</param>
    public virtual void AddPotentialFocusableElements(RawRectangleF? startingRect, ICollection<FrameworkElement> elements)
    {
      if (!IsVisible || !IsEnabled)
        return;
      if (Focusable && !HasFocus) // If we already have the focus, we cannot be a candidate for next focused element.
        elements.Add(this);
      // General implementation: Return all visible children
      ICollection<FrameworkElement> children = GetFEChildren();
      foreach (FrameworkElement child in children)
      {
        if (!child.IsVisible || !child.IsEnabled || child.HasFocus)
          continue;
        child.AddPotentialFocusableElements(startingRect, elements);
      }
    }

    protected ICollection<FrameworkElement> GetFEChildren()
    {
      ICollection<UIElement> children = GetChildren();
      ICollection<FrameworkElement> result = new List<FrameworkElement>(children.Count);
      foreach (UIElement child in children)
      {
        FrameworkElement fe = child as FrameworkElement;
        if (fe != null)
          result.Add(fe);
      }
      return result;
    }

    #endregion

    #endregion

    #region Replacing methods for the == operator which evaluate two float.NaN values to equal

    public static bool SameValue(float val1, float val2)
    {
      return float.IsNaN(val1) && float.IsNaN(val2) || val1 == val2;
    }

    public static bool SameSize(Size2F size1, Size2F size2)
    {
      return SameValue(size1.Width, size2.Width) && SameValue(size1.Height, size2.Height);
    }

    public static bool SameSize(Size2F? size1, Size2F size2)
    {
      return size1.HasValue && SameSize(size1.Value, size2);
    }

    public static bool SameRect(RectangleF rect1, RectangleF rect2)
    {
      return SameValue(rect1.X, rect2.X) && SameValue(rect1.Y, rect2.Y) && SameValue(rect1.Width, rect2.Width) && SameValue(rect1.Height, rect2.Height);
    }

    public static bool SameRect(RawRectangleF rect1, RawRectangleF rect2)
    {
      return SameValue(rect1.Left, rect2.Left) && SameValue(rect1.Top, rect2.Top) && SameValue(rect1.Right, rect2.Right) && SameValue(rect1.Bottom, rect2.Bottom);
    }

    public static bool SameRect(RectangleF? rect1, RectangleF rect2)
    {
      return rect1.HasValue && SameRect(rect1.Value, rect2);
    }

    public static bool SameRect(RawRectangleF? rect1, RawRectangleF rect2)
    {
      return rect1.HasValue && SameRect(rect1.Value, rect2);
    }

    #endregion

    #region Layouting

    public override bool IsInArea(float x, float y)
    {
      Vector2 actualPosition = ActualPosition;
      double actualWidth = ActualWidth;
      double actualHeight = ActualHeight;
      float xTrans = x;
      float yTrans = y;
      if (!TransformMouseCoordinates(ref xTrans, ref yTrans))
        return false;
      return xTrans >= actualPosition.X && xTrans <= actualPosition.X + actualWidth && yTrans >= actualPosition.Y && yTrans <= actualPosition.Y + actualHeight;
    }

    public void InvalidateLayout(bool invalidateMeasure, bool invalidateArrange)
    {
#if DEBUG_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("InvalidateLayout {0} Name='{1}', MeasureInvalid={2}, ArrangeInvalid={3}", GetType().Name, Name, invalidateMeasure, invalidateArrange));
#endif
      // Albert: Don't use this optimization without the check for _elementState because of threading issues
      if ((_isMeasureInvalid || !invalidateMeasure) && (_isArrangeInvalid || !invalidateArrange) && _elementState != ElementState.Running)
        return;
      _isMeasureInvalid |= invalidateMeasure;
      _isArrangeInvalid |= invalidateArrange;
      if (!IsVisible)
        return;
      InvalidateParentLayout(invalidateMeasure, invalidateArrange);
    }

    /// <summary>
    /// Invalidates the layout of our visual parent.
    /// The parent will re-layout itself and its children.
    /// </summary>
    public void InvalidateParentLayout(bool invalidateMeasure, bool invalidateArrange)
    {
      FrameworkElement parent = VisualParent as FrameworkElement;
      if (parent != null)
        parent.InvalidateLayout(invalidateMeasure, invalidateArrange);
    }

    /// <summary>
    /// Given the transform to be applied to an unknown rectangle, this method finds (in axis-aligned local space)
    /// the largest rectangle that, after transform, fits within <paramref name="localBounds"/>.
    /// Largest rectangle means rectangle of the greatest area in local space (although maximal area in local space
    /// implies maximal area in transform space).
    /// </summary>
    /// <param name="transform">Transformation matrix.</param>
    /// <param name="localBounds">The bounds in local space where the returned size fits when transformed
    /// via the given <paramref name="transform"/>.</param>
    /// <returns>The dimensions, in local space, of the maximal area rectangle found.</returns>
    private static Size2F FindMaxTransformedSize(Matrix transform, Size2F localBounds)
    {
      // X (width) and Y (height) constraints for axis-aligned bounding box in dest. space
      float xConstr = localBounds.Width;
      float yConstr = localBounds.Height;

      // Avoid doing math on an empty rect
      if (IsNear(xConstr, 0) || IsNear(yConstr, 0))
        return new Size2F(0, 0);

      bool xConstrInfinite = float.IsNaN(xConstr);
      bool yConstrInfinite = float.IsNaN(yConstr);

      if (xConstrInfinite && yConstrInfinite)
        return new Size2F(float.NaN, float.NaN);

      if (xConstrInfinite) // Assume square for one-dimensional constraint 
        xConstr = yConstr;
      else if (yConstrInfinite)
        yConstr = xConstr;

      // We only deal with nonsingular matrices here. The nonsingular matrix is the one
      // that has inverse (determinant != 0).
      if (transform.Determinant() == 0)
        return new Size2F(0, 0);

      float a = transform.M11;
      float b = transform.M12;
      float c = transform.M21;
      float d = transform.M22;

      // Result width and height (in child/local space)
      float w;
      float h;

      // Because we are dealing with nonsingular transform matrices, we have (b==0 || c==0) XOR (a==0 || d==0) 
      if (IsNear(b, 0) || IsNear(c, 0))
      { // (b == 0 || c == 0) ==> a != 0 && d != 0
        float yCoverD = yConstrInfinite ? float.PositiveInfinity : Math.Abs(yConstr / d);
        float xCoverA = xConstrInfinite ? float.PositiveInfinity : Math.Abs(xConstr / a);

        if (IsNear(b, 0))
        {
          if (IsNear(c, 0))
          { // b == 0, c == 0, a != 0, d != 0

            // No constraint relation; use maximal width and height 
            h = yCoverD;
            w = xCoverA;
          }
          else
          { // b == 0, a != 0, c != 0, d != 0

            // Maximizing under line (hIntercept=xConstr/c, wIntercept=xConstr/a) 
            // BUT we still have constraint: h <= yConstr/d
            h = Math.Min(0.5f * Math.Abs(xConstr / c), yCoverD);
            w = xCoverA - ((c * h) / a);
          }
        }
        else
        { // c == 0, a != 0, b != 0, d != 0 

          // Maximizing under line (hIntercept=yConstr/d, wIntercept=yConstr/b)
          // BUT we still have constraint: w <= xConstr/a
          w = Math.Min(0.5f * Math.Abs(yConstr / b), xCoverA);
          h = yCoverD - ((b * w) / d);
        }
      }
      else if (IsNear(a, 0) || IsNear(d, 0))
      { // (a == 0 || d == 0) ==> b != 0 && c != 0 
        float yCoverB = Math.Abs(yConstr / b);
        float xCoverC = Math.Abs(xConstr / c);

        if (IsNear(a, 0))
        {
          if (IsNear(d, 0))
          { // a == 0, d == 0, b != 0, c != 0 

            // No constraint relation; use maximal width and height
            h = xCoverC;
            w = yCoverB;
          }
          else
          { // a == 0, b != 0, c != 0, d != 0

            // Maximizing under line (hIntercept=yConstr/d, wIntercept=yConstr/b)
            // BUT we still have constraint: h <= xConstr/c
            h = Math.Min(0.5f * Math.Abs(yConstr / d), xCoverC);
            w = yCoverB - ((d * h) / b);
          }
        }
        else
        { // d == 0, a != 0, b != 0, c != 0

          // Maximizing under line (hIntercept=xConstr/c, wIntercept=xConstr/a)
          // BUT we still have constraint: w <= yConstr/b
          w = Math.Min(0.5f * Math.Abs(xConstr / a), yCoverB);
          h = xCoverC - ((a * w) / c);
        }
      }
      else
      {
        float xCoverA = Math.Abs(xConstr / a); // w-intercept of x-constraint line
        float xCoverC = Math.Abs(xConstr / c); // h-intercept of x-constraint line

        float yCoverB = Math.Abs(yConstr / b); // w-intercept of y-constraint line
        float yCoverD = Math.Abs(yConstr / d); // h-intercept of y-constraint line

        // The tighest constraint governs, so we pick the lowest constraint line

        // The optimal point (w, h) for which Area = w*h is maximized occurs halfway to each intercept.
        w = Math.Min(yCoverB, xCoverA) * 0.5f;
        h = Math.Min(xCoverC, yCoverD) * 0.5f;

        if ((GreaterThanOrClose(xCoverA, yCoverB) &&
             LessThanOrClose(xCoverC, yCoverD)) ||
            (LessThanOrClose(xCoverA, yCoverB) &&
             GreaterThanOrClose(xCoverC, yCoverD)))
        {
          // Constraint lines cross; since the most restrictive constraint wins,
          // we have to maximize under two line segments, which together are discontinuous.
          // Instead, we maximize w*h under the line segment from the two smallest endpoints. 

          // Since we are not (except for in corner cases) on the original constraint lines, 
          // we are not using up all the available area in transform space.  So scale our shape up 
          // until it does in at least one dimension.

          Size2F childSizeTr = new Size2F(w, h);
          transform.TransformIncludingRectangleSize(ref childSizeTr);
          float expandFactor = Math.Min(xConstr / childSizeTr.Width, yConstr / childSizeTr.Height);
          if (!float.IsNaN(expandFactor) && !float.IsInfinity(expandFactor))
          {
            w *= expandFactor;
            h *= expandFactor;
          }
        }
      }
      return new Size2F(w, h);
    }

    /// <summary>
    /// Measures this element's size and fills the <see cref="DesiredSize"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is the first part of the two-phase measuring process. In this first phase, parent
    /// controls collect all the size requirements of their child controls.
    /// </para>
    /// <para>
    /// An input size value of <see cref="float.NaN"/> in any coordinate denotes that this child control doesn't have a size
    /// constraint in that direction. Coordinates different from <see cref="float.NaN"/> should be considered by this child
    /// control as the maximum available size in that direction. If this element still produces a bigger
    /// <see cref="DesiredSize"/>, the <see cref="Arrange(RectangleF)"/> method might give it a smaller final region.
    /// </para>
    /// </remarks>
    /// <param name="totalSize">Total size of the element including Margins. As input, this parameter
    /// contains the size available for this child control (size constraint). As output, it must be set
    /// to the <see cref="DesiredSize"/> plus <see cref="UIElement.Margin"/>.</param>
    public void Measure(ref Size2F totalSize)
    {
#if DEBUG_LAYOUT
#if DEBUG_MORE_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("Measure {0} Name='{1}', totalSize={2}", GetType().Name, Name, totalSize));
#endif
#endif
      if (!_isMeasureInvalid && SameSize(_availableSize, totalSize))
      { // Optimization: If our input data is the same and the layout isn't invalid, we don't need to measure again
        totalSize = _desiredSize;
#if DEBUG_LAYOUT
#if DEBUG_MORE_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("Measure {0} Name='{1}', cutting short, totalSize is like before and measurement is not invalid, returns desired size={2}", GetType().Name, Name, totalSize));
#endif
#endif
        return;
      }
#if DEBUG_LAYOUT
#if !DEBUG_MORE_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("Measure {0} Name='{1}', totalSize={2}", GetType().Name, Name, totalSize));
#endif
#endif
      _isMeasureInvalid = false;
      _availableSize = totalSize;
      RemoveMargin(ref totalSize, Margin);

      Matrix? layoutTransform = LayoutTransform == null ? new Matrix?() : LayoutTransform.GetTransform();
      if (layoutTransform.HasValue)
        totalSize = FindMaxTransformedSize(layoutTransform.Value, totalSize);

      if (!double.IsNaN(Width))
        totalSize.Width = (float)Width;
      if (!double.IsNaN(Height))
        totalSize.Height = (float)Height;

      totalSize = CalculateInnerDesiredSize(totalSize);

      if (!double.IsNaN(Width))
        totalSize.Width = (float)Width;
      if (!double.IsNaN(Height))
        totalSize.Height = (float)Height;

      totalSize = ClampSize(totalSize);

      _innerDesiredSize = totalSize;

      if (layoutTransform.HasValue)
        layoutTransform.Value.TransformIncludingRectangleSize(ref totalSize);

      AddMargin(ref totalSize, Margin);
      if (totalSize != _desiredSize)
        InvalidateLayout(false, true);
      _desiredSize = totalSize;
#if DEBUG_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("Measure {0} Name='{1}', returns calculated desired size={2}", GetType().Name, Name, totalSize));
#endif
    }

    /// <summary>
    /// Arranges the UI element and positions it in the given rectangle.
    /// </summary>
    /// <param name="outerRect">The final position and size the parent computed for this child element.</param>
    public void Arrange(RawRectangleF outerRect)
    {
      if (_isMeasureInvalid)
      {
#if DEBUG_LAYOUT
#if DEBUG_MORE_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("Arrange {0} Name='{1}', exiting because measurement is invalid", GetType().Name, Name));
#endif
#endif
        InvalidateLayout(true, true); // Re-schedule arrangement
        return;
      }
#if DEBUG_LAYOUT
#if DEBUG_MORE_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("Arrange {0} Name='{1}', outerRect={2}", GetType().Name, Name, outerRect));
#endif
#endif
      if (!_isArrangeInvalid && SameRect(_outerRect, outerRect))
      { // Optimization: If our input data is the same and the layout isn't invalid, we don't need to arrange again
#if DEBUG_LAYOUT
#if DEBUG_MORE_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("Arrange {0} Name='{1}', cutting short, outerRect={2} is like before and arrangement is not invalid", GetType().Name, Name, outerRect));
#endif
#endif
        return;
      }
#if DEBUG_LAYOUT
#if !DEBUG_MORE_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("Arrange {0} Name='{1}', outerRect={2}", GetType().Name, Name, outerRect));
#endif
#endif
      _isArrangeInvalid = false;

      // Those two properties have to be set by the render loop again the next time we render. We need to reset the values to null
      // because the BoundingBox property then uses a fallback value. It would be wrong to use the old _boundingBox value if we
      // re-arrange our contents.
      _renderedBoundingBox = null;
      _finalTransform = null;
      _inverseFinalTransform = null;

      _outerRect = outerRect.ToRectangleF();
      RectangleF rect = _outerRect.Value;
      RemoveMargin(ref rect, Margin);

      if (LayoutTransform != null)
      {
        Matrix layoutTransform = LayoutTransform.GetTransform().RemoveTranslation();
        if (!layoutTransform.IsIdentity)
        {
          Size2F resultInnerSize = _innerDesiredSize;
          Size2F resultOuterSize = resultInnerSize;
          layoutTransform.TransformIncludingRectangleSize(ref resultOuterSize);
          if (resultOuterSize.Width > rect.Width + DELTA_DOUBLE || resultOuterSize.Height > rect.Height + DELTA_DOUBLE)
            // Transformation of desired size doesn't fit into the available rect
            resultInnerSize = FindMaxTransformedSize(layoutTransform, rect.Size);
          rect = new RectangleF(
              rect.Location.X + (rect.Width - resultInnerSize.Width) / 2,
              rect.Location.Y + (rect.Height - resultInnerSize.Height) / 2,
              resultInnerSize.Width,
              resultInnerSize.Height);
        }
      }
      _innerRect = rect;

      InitializeTriggers();
      CheckFireLoaded(); // Has to be done after all triggers are initialized to make EventTriggers for UIElement.Loaded work properly

      ArrangeOverride();
      UpdateFocus(); // Has to be done after all children have arranged to make SetFocusPrio work properly
    }

    protected virtual void ArrangeOverride()
    {
      ActualPosition = _innerRect.Location();
      ActualWidth = _innerRect.Width();
      ActualHeight = _innerRect.Height();
    }

    protected virtual Size2F CalculateInnerDesiredSize(Size2F totalSize)
    {
      return new Size2F();
    }

    protected Size2F ClampSize(Size2F size)
    {
      if (!float.IsNaN(size.Width))
        size.Width = (float)Math.Min(Math.Max(size.Width, MinWidth), MaxWidth);
      if (!float.IsNaN(size.Height))
        size.Height = (float)Math.Min(Math.Max(size.Height, MinHeight), MaxHeight);
      return size;
    }

    /// <summary>
    /// Arranges the child horizontal and vertical in a given area. If the area is bigger than
    /// the child's desired size, the child will be arranged according to the given <paramref name="horizontalAlignment"/>
    /// and <paramref name="verticalAlignment"/>.
    /// </summary>
    /// <param name="child">The child to arrange. The child will not be changed by this method.</param>
    /// <param name="horizontalAlignment">Alignment in horizontal direction.</param>
    /// <param name="verticalAlignment">Alignment in vertical direction.</param>
    /// <param name="location">Input: The starting position of the available area. Output: The position
    /// the child should be located.</param>
    /// <param name="childSize">Input: The available area for the <paramref name="child"/>. Output:
    /// The area the child should take.</param>
    public void ArrangeChild(FrameworkElement child, HorizontalAlignmentEnum horizontalAlignment, VerticalAlignmentEnum verticalAlignment, ref Vector2 location, ref Size2F childSize)
    {
      // Be careful when changing the implementation of those arrangement methods.
      // MPF behaves a bit different from WPF: We don't clip elements at the boundaries of containers,
      // instead, we arrange them with a maximum size calculated by the container. If we would not avoid
      // that controls can become bigger than their arrange size, we would have to accomplish a means to clip
      // their render size.
      ArrangeChildHorizontal(child, horizontalAlignment, ref location, ref childSize);
      ArrangeChildVertical(child, verticalAlignment, ref location, ref childSize);
    }

    /// <summary>
    /// Arranges the child horizontal in a given area. If the area is bigger than the child's desired
    /// size, the child will be arranged according to the given <paramref name="alignment"/>.
    /// </summary>
    /// <param name="child">The child to arrange. The child will not be changed by this method.</param>
    /// <param name="alignment">Alignment in horizontal direction.</param>
    /// <param name="location">Input: The starting position of the available area. Output: The position
    /// the child should be located.</param>
    /// <param name="childSize">Input: The available area for the <paramref name="child"/>. Output:
    /// The area the child should take.</param>
    public void ArrangeChildHorizontal(FrameworkElement child, HorizontalAlignmentEnum alignment, ref Vector2 location, ref Size2F childSize)
    {
      // See comment in ArrangeChild
      Size2F desiredSize = child.DesiredSize;

      if (!double.IsNaN(desiredSize.Width) && desiredSize.Width <= childSize.Width)
      {
        // Width takes precedence over Stretch - Use Center as fallback
        if (alignment == HorizontalAlignmentEnum.Center ||
            (alignment == HorizontalAlignmentEnum.Stretch && !double.IsNaN(child.Width)))
        {
          location.X += (childSize.Width - desiredSize.Width) / 2;
          childSize.Width = desiredSize.Width;
        }
        if (alignment == HorizontalAlignmentEnum.Right)
        {
          location.X += childSize.Width - desiredSize.Width;
          childSize.Width = desiredSize.Width;
        }
        else if (alignment == HorizontalAlignmentEnum.Left)
        {
          // Leave location unchanged
          childSize.Width = desiredSize.Width;
        }
        //else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Stretch)
        // - Use all the space, nothing to do here
      }
    }

    /// <summary>
    /// Arranges the child vertical in a given area. If the area is bigger than the child's desired
    /// size, the child will be arranged according to the given <paramref name="alignment"/>.
    /// </summary>
    /// <param name="child">The child to arrange. The child will not be changed by this method.</param>
    /// <param name="alignment">Alignment in vertical direction.</param>
    /// <param name="location">Input: The starting position of the available area. Output: The position
    /// the child should be located.</param>
    /// <param name="childSize">Input: The available area for the <paramref name="child"/>. Output:
    /// The area the child should take.</param>
    public void ArrangeChildVertical(FrameworkElement child, VerticalAlignmentEnum alignment, ref Vector2 location, ref Size2F childSize)
    {
      // See comment in ArrangeChild
      Size2F desiredSize = child.DesiredSize;

      if (!double.IsNaN(desiredSize.Height) && desiredSize.Height <= childSize.Height)
      {
        // Height takes precedence over Stretch - Use Center as fallback
        if (alignment == VerticalAlignmentEnum.Center ||
            (alignment == VerticalAlignmentEnum.Stretch && !double.IsNaN(child.Height)))
        {
          location.Y += (childSize.Height - desiredSize.Height) / 2;
          childSize.Height = desiredSize.Height;
        }
        else if (alignment == VerticalAlignmentEnum.Bottom)
        {
          location.Y += childSize.Height - desiredSize.Height;
          childSize.Height = desiredSize.Height;
        }
        else if (alignment == VerticalAlignmentEnum.Top)
        {
          // Leave location unchanged
          childSize.Height = desiredSize.Height;
        }
        //else if (child.VerticalAlignment == VerticalAlignmentEnum.Stretch)
        // - Use all the space, nothing to do here
      }
    }

    public void BringIntoView()
    {
      BringIntoView(this, ActualBounds);
    }

    /// <summary>
    /// Updates tle layout of this element in the render thread.
    /// In this method, <see cref="Measure(ref Size2F)"/> and <see cref="Arrange(RectangleF)"/> are called.
    /// </summary>
    /// <remarks>
    /// This method should actually be located in the <see cref="Screen"/> class but I leave it here because all the
    /// layout debug defines are in the scope of this file.
    /// This method must be called from the render thread before the call to <see cref="Render"/>.
    /// </remarks>
    /// <param name="skinSize">The size of the skin.</param>
    public void UpdateLayoutRoot(Size2F skinSize)
    {
      Size2F size = skinSize;

#if DEBUG_LAYOUT
#if DEBUG_MORE_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayoutRoot {0} Name='{1}', measuring with screen size {2}", GetType().Name, Name, skinSize));
#endif
#endif
      do
      {
        Measure(ref size);
      } while (_isMeasureInvalid);

#if DEBUG_LAYOUT
#if DEBUG_MORE_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', arranging with screen size {2}", GetType().Name, Name, skinSize));
#endif
#endif
      // Ignore the measured size - arrange with screen size
      Arrange(SharpDXExtensions.CreateRectangleF(new Vector2(0, 0), skinSize));
    }

    /// <summary>
    /// Transforms a screen point to local element space. The <see cref="UIElement.ActualPosition"/> is also taken into account.
    /// </summary>
    /// <param name="point">Screen point</param>
    /// <returns>Returns the transformed point in element coordinates.</returns>
    public override Vector2 TransformScreenPoint(Vector2 point)
    {
      float x = point.X;
      float y = point.Y;
      if (TransformMouseCoordinates(ref x, ref y))
      {
        return base.TransformScreenPoint(new Vector2(x, y));
      }
      return base.TransformScreenPoint(point);
    }

    #endregion

    #region Style handling

    public Style CopyDefaultStyle()
    {
      Type type = GetType();
      Style result = null;
      while (result == null && type != null)
      {
        result = FindResource(type) as Style;
        type = type.BaseType;
      }
      return MpfCopyManager.DeepCopyCutLVPs(result); // Create an own copy of the style to be assigned
    }

    /// <summary>
    /// Find the default style especially in the loading phase of an element when the element tree is not yet put together.
    /// </summary>
    /// <param name="context">Current parser context.</param>
    /// <returns>Default style for this element or <c>null</c>, if no default style is defined.</returns>
    protected Style CopyDefaultStyle(IParserContext context)
    {
      Type type = GetType();
      Style result = null;
      while (result == null && type != null)
      {
        result = (ResourceDictionary.FindResourceInParserContext(type, context) ?? FindResource(type)) as Style;
        type = type.BaseType;
      }
      return MpfCopyManager.DeepCopyCutLVPs(result); // Create an own copy of the style to be assigned
    }

    #endregion

    #region UI state handling

    public override void SaveUIState(IDictionary<string, object> state, string prefix)
    {
      base.SaveUIState(state, prefix);
      if (HasFocus)
        state[prefix + "/Focused"] = true;
    }

    public override void RestoreUIState(IDictionary<string, object> state, string prefix)
    {
      base.RestoreUIState(state, prefix);
      object focused;
      bool? bFocused;
      if (state.TryGetValue(prefix + "/Focused", out focused) && (bFocused = focused as bool?).HasValue && bFocused.Value)
        SetFocusPrio = SetFocusPriority.RestoreState;
    }

    #endregion

    #region Rendering

    public virtual void RenderOverride(RenderContext localRenderContext)
    {
    }

    /// <summary>
    /// Renders the <see cref="FrameworkElement"/> to the given <paramref name="renderTarget"/>. This method works with
    /// surfaces that are created as render target, which do support multisampling.
    /// </summary>
    /// <param name="renderTarget">Render target.</param>
    /// <param name="renderContext">Render context.</param>
    public void RenderToTarget(IBitmapAsset2D renderTarget, RenderContext renderContext)
    {
      RenderToTargetInternal(renderTarget.Bitmap, renderContext);
    }

    protected void RenderToTargetInternal(Bitmap1 renderTarget, RenderContext renderContext)
    {
      // We do the following here:
      // 1. Remember old render target
      // 2. Set the rendertarget to the given Image
      // 3. Clear the surface with an alpha value of 0
      // 4. Render the control (into the surface)
      // 5. Restore the rendertarget to the backbuffer

      using (new TemporaryRenderTarget2D(renderTarget))
      {
        GraphicsDevice11.Instance.Context2D1.Clear(TRANSPARENT_BLACK);

        // Render visual to local render target (Bitmap)
        Render(renderContext);
      }
    }

    private static RectangleF CalculateBoundingBox(RawRectangleF rectangle, Matrix3x2 transformation)
    {
      Vector2 tl = rectangle.TopLeft();
      Vector2 tr = new Vector2(rectangle.Right, rectangle.Top);
      Vector2 bl = new Vector2(rectangle.Left, rectangle.Bottom);
      Vector2 br = new Vector2(rectangle.Right, rectangle.Bottom);
      transformation.Transform(ref tl);
      transformation.Transform(ref tr);
      transformation.Transform(ref bl);
      transformation.Transform(ref br);
      Vector2 rtl = new Vector2(
          Math.Min(tl.X, Math.Min(tr.X, Math.Min(bl.X, br.X))),
          Math.Min(tl.Y, Math.Min(tr.Y, Math.Min(bl.Y, br.Y))));
      Vector2 rbr = new Vector2(
          Math.Max(tl.X, Math.Max(tr.X, Math.Max(bl.X, br.X))),
          Math.Max(tl.Y, Math.Max(tr.Y, Math.Max(bl.Y, br.Y))));
      return SharpDXExtensions.CreateRectangleF(rtl, new Size2F(rbr.X - rtl.X, rbr.Y - rtl.Y));
    }

    private RenderContext ExtortRenderContext()
    {
      FrameworkElement parent = VisualParent as FrameworkElement;
      if (parent == null)
        return new RenderContext(Matrix3x2.Identity, RectangleF.Empty);
      Transform layoutTransform = LayoutTransform;
      Transform renderTransform = RenderTransform;
      Matrix? layoutTransformMatrix = layoutTransform == null ? new Matrix?() : layoutTransform.GetTransform();
      Matrix? renderTransformMatrix = renderTransform == null ? new Matrix?() : renderTransform.GetTransform();
      // To calculate our final transformation, we fake a render context starting
      return parent.ExtortRenderContext().Derive(ActualBounds, layoutTransformMatrix, renderTransformMatrix, RenderTransformOrigin, 1);
    }

    /// <summary>
    /// Our render system skips rendering of elements which are currently not visible. But in some situations, we need to calculate the bounds
    /// where this element would have been drawn if it had been rendered. This method forces the calculation of the final transformation
    /// for this element which can be applied on the <see cref="ActualBounds"/>/<see cref="_innerRect"/> to get the element's render bounds.
    /// </summary>
    /// <returns>Matrix which represents the final transformation for this element.</returns>
    private Matrix3x2 ExtortFinalTransform()
    {
      if (_finalTransform.HasValue)
        return _finalTransform.Value;
      return ExtortRenderContext().Transform;
    }

    /// <summary>
    /// Renders the current control using an Effect. Therefor first the control is rendered into a temporary bitmap, which then will be
    /// used by the Effect as input.
    /// </summary>
    /// <param name="effect">Effect to render</param>
    /// <param name="parentRenderContext">Render context</param>
    protected void RenderEffect(Effect effect, RenderContext parentRenderContext)
    {
      RectangleF bounds = new RectangleF(0, 0, SkinContext.BackBufferWidth, SkinContext.BackBufferHeight);

      // Build a key based on the control size. This allows reusing the render target for controls of same size (which happens quite often)
      string key = string.Format("EffectTarget_{0}_{1}", bounds.Width, bounds.Height);
      _effectInput = ContentManager.Instance.GetRenderTarget2D(key);
      _effectInput.AllocateRenderTarget((int)bounds.Width, (int)bounds.Height);

      if (!_effectInput.IsAllocated)
        return;

      parentRenderContext.IsEffectRender = true;
      RenderToTarget(_effectInput, parentRenderContext);
      parentRenderContext.IsEffectRender = false;

      if (!effect.IsAllocated)
        effect.Allocate();
      effect.SetParentControlBounds(_renderedBoundingBox ?? bounds);
      effect.Input = _effectInput.Bitmap;

      // Now add the original position as offset, to make bitmap appear in its right place
      // no parentRenderContext here, because output is already transformed!
      GraphicsDevice11.Instance.Context2D1.DrawImage(effect.Output, bounds.TopLeft);
    }

    public override void Render(RenderContext parentRenderContext)
    {
      if (!IsVisible)
        return;

      RawRectangleF bounds = ActualBounds;
      if (bounds.Width() <= 0 || bounds.Height() <= 0)
        return;

      // If there is a effect, call another method to render control and then the effect.
      Effect effect = Effect;
      if (effect != null && !parentRenderContext.IsEffectRender)
      {
        RenderEffect(effect, parentRenderContext);
        return;
      }

      // Adjust render contexxt
      var localRenderContext = UpdateRenderContext(parentRenderContext, bounds);

      // Begin Opacity Mask
      bool layerPushed = BeginRenderOpacityMask(localRenderContext);

      // Control content
      RenderOverride(localRenderContext);

      // End Opacity Mask
      if (layerPushed)
        EndRenderOpacityMask();

      // Calculation of absolute render size (in world coordinate system)
      parentRenderContext.IncludeTransformedContentsBounds(localRenderContext.OccupiedTransformedBounds);
      _lastZIndex = localRenderContext.ZOrder;
    }

    /// <summary>
    /// Calculates the final render context and updates the internal fields _renderedBoundingBox and _finalTransform.
    /// </summary>
    private RenderContext UpdateRenderContext(RenderContext parentRenderContext, RawRectangleF bounds)
    {
      Matrix? layoutTransformMatrix = LayoutTransform == null ? new Matrix?() : LayoutTransform.GetTransform();
      Matrix? renderTransformMatrix = RenderTransform == null ? new Matrix?() : RenderTransform.GetTransform();
      RenderContext localRenderContext = parentRenderContext.Derive(bounds, layoutTransformMatrix, renderTransformMatrix, RenderTransformOrigin, Opacity);
      Matrix3x2 finalTransform = localRenderContext.Transform;
      if (finalTransform != _finalTransform)
      {
        _finalTransform = finalTransform;
        _inverseFinalTransform = Matrix3x2.Invert(_finalTransform.Value);
        _renderedBoundingBox = CalculateBoundingBox(_innerRect, finalTransform);
      }
      return localRenderContext;
    }

    /// <summary>
    /// Begins the rendering of OpacityMask. It uses a D2D layer to clip bounds and apply an OpacityBrush.
    /// If the method returbs <c>true</c>, the <see cref="EndRenderOpacityMask"/> must be called when control was rendered.
    /// </summary>
    /// <param name="localRenderContext">RenderContext</param>
    /// <returns><c>true</c> if a layer was pushed.</returns>
    protected bool BeginRenderOpacityMask(RenderContext localRenderContext)
    {
      Brushes.Brush opacityMask = OpacityMask;
      if (opacityMask == null)
        return false;

      // If the control bounds have changed we need to update our Brush transform to make the 
      // texture coordinates match up
      if (_updateOpacityMask || !_lastOccupiedTransformedBounds.Equals(localRenderContext.OccupiedTransformedBounds))
      {
        UpdateOpacityMask(localRenderContext.OccupiedTransformedBounds, localRenderContext.ZOrder);
        _lastOccupiedTransformedBounds = localRenderContext.OccupiedTransformedBounds;
        _updateOpacityMask = false;
      }

      if (!opacityMask.RenderBrush(localRenderContext) || !opacityMask.TryAllocate())
        return false;

      IRenderBrush renderBrush = opacityMask as IRenderBrush;
      if (renderBrush != null)
      {
        if (!renderBrush.RenderContent(localRenderContext))
          return false;
      }

      LayerParameters1 layerParameters = new LayerParameters1
      {
        ContentBounds = _lastOccupiedTransformedBounds,
        LayerOptions = LayerOptions1.None,
        Opacity = 1.0f,
        OpacityBrush = opacityMask.Brush2D
      };

      GraphicsDevice11.Instance.Context2D1.PushLayer(ref layerParameters, null);
      return true;
    }

    /// <summary>
    /// Ends the rendering of OpacityMask and pops the D2D layer.
    /// </summary>
    /// <returns></returns>
    protected bool EndRenderOpacityMask()
    {
      GraphicsDevice11.Instance.Context2D1.PopLayer();
      return true;
    }

    /// <summary>
    /// Converts a <see cref="RectangleF"/> into a <see cref="Rectangle"/> with checking for proper surface coordinates in range from
    /// <see cref="new Point()"/> to <paramref name="clip"/>.
    /// </summary>
    /// <param name="rect">Source rect</param>
    /// <param name="clip">Maximum size</param>
    /// <returns>Converted rect</returns>
    protected Rectangle ToRect(RectangleF rect, Size2 clip)
    {
      int x = Math.Min(Math.Max(0, (int)Math.Floor(rect.X)), clip.Width); // Limit to 0 .. Width
      int y = Math.Min(Math.Max(0, (int)Math.Floor(rect.Y)), clip.Height); // Limit to 0 .. Height
      int width = Math.Min(Math.Max(0, (int)Math.Ceiling(rect.Width)), clip.Width - x); // Limit to 0 .. Width - x
      int height = Math.Min(Math.Max(0, (int)Math.Ceiling(rect.Height)), clip.Height - y); // Limit to 0 .. Height - y
      return new Rectangle(x, y, width, height);
    }

    protected void UpdateEffectMask(Effects.Effect effect, RectangleF bounds, float width, float height, float zPos)
    {
      Color4 col = ColorConverter.FromColor(Color.White);
      col.Alpha *= (float)Opacity;

      PositionColoredTextured[] verts = PositionColoredTextured.CreateQuad_Fan(
          bounds.Left /*- 0.5f*/, bounds.Top /*- 0.5f*/, bounds.Right /*- 0.5f*/, bounds.Bottom /*- 0.5f*/,
          bounds.Left / width, bounds.Top / height, bounds.Right / width, bounds.Bottom / height,
          zPos, col);

      effect.SetupEffect(this, ref verts, zPos, false);
      //PrimitiveBuffer.SetPrimitiveBuffer(ref _effectContext, ref verts, PrimitiveType.TriangleFan);
    }

    #endregion

    #region Opacitymask

    /// <summary>
    /// Render the current Opacity brush to a rectangle covering our given bounds. Blending is applied such that destination 
    /// RGB will remain unchanged while destination alpha will be modulated by the opacity brush alpha.
    /// </summary>
    /// <param name="renderContext">Context information</param>
    private void RenderOpacityBrush(RenderContext renderContext)
    {
      Brushes.Brush opacityMask = OpacityMask;
      if (opacityMask == null)
        return;

      // If the control bounds have changed we need to update our primitive context to make the 
      // texture coordinates match up
      if (_updateOpacityMask || _opacityMaskContext == null || !_lastOccupiedTransformedBounds.Equals(renderContext.OccupiedTransformedBounds))
      {
        UpdateOpacityMask(renderContext.OccupiedTransformedBounds, renderContext.ZOrder);
        _lastOccupiedTransformedBounds = renderContext.OccupiedTransformedBounds;
        _updateOpacityMask = false;
      }

      GraphicsDevice.EnableAlphaChannelBlending();
      GraphicsDevice.DisableAlphaTest();

      //// Now render the OpacityMask brush
      //if (opacityMask.BeginRenderBrush(_opacityMaskContext, new RenderContext(Matrix.Identity, _lastOccupiedTransformedBounds)))
      //{
      //  _opacityMaskContext.Render(0);
      //  opacityMask.EndRender();
      //}
      GraphicsDevice.DisableAlphaChannelBlending();
      GraphicsDevice.EnableAlphaTest();
    }

    void UpdateOpacityMask(RawRectangleF bounds, float zPos)
    {
      Color4 col = ColorConverter.FromColor(Color.White);
      col.Alpha *= (float)Opacity;

      //PositionColoredTextured[] verts = PositionColoredTextured.CreateQuad_Fan(
      //    bounds.Left /*- 0.5f*/, bounds.Top /*- 0.5f*/, bounds.Right /*- 0.5f*/, bounds.Bottom /*- 0.5f*/,
      //    0.0f, 0.0f, 1.0f, 1.0f,
      //    zPos, col);

      OpacityMask.SetupBrush(this, ref bounds, zPos, false);
      //PrimitiveBuffer.SetPrimitiveBuffer(ref _opacityMaskContext, ref verts, PrimitiveType.TriangleFan);
    }

    #endregion

    #region Base overrides

    public override void Deallocate()
    {
      base.Deallocate();
      Effect effect = Effect;
      if (effect != null)
        effect.Deallocate();
      PrimitiveBuffer.DisposePrimitiveBuffer(ref _effectContext);
      PrimitiveBuffer.DisposePrimitiveBuffer(ref _opacityMaskContext);
    }

    public override void FinishInitialization(IParserContext context)
    {
      base.FinishInitialization(context);
      if (Style == null)
        Style = CopyDefaultStyle(context);
    }

    #endregion
  }
}
