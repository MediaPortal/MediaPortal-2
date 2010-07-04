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

//#define DEBUG_LAYOUT

using System;
using System.Drawing;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Fonts;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

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

  public class FocusableElementFinder : IFinder
  {
    private static FocusableElementFinder _instance = null;

    public bool Query(UIElement current)
    {
      FrameworkElement fe = current as FrameworkElement;
      if (fe == null)
        return false;
      return fe.Focusable;
    }

    public static FocusableElementFinder Instance
    {
      get
      {
        if (_instance == null)
          _instance = new FocusableElementFinder();
        return _instance;
      }
    }
  }

  public class FrameworkElement: UIElement
  {
    public const string GOTFOCUS_EVENT = "FrameworkElement.GotFocus";
    public const string LOSTFOCUS_EVENT = "FrameworkElement.LostFocus";
    public const string MOUSEENTER_EVENT = "FrameworkElement.MouseEnter";
    public const string MOUSELEAVE_EVENT = "FrameworkElement.MouseEnter";

    #region Protected fields

    protected AbstractProperty _widthProperty;
    protected AbstractProperty _heightProperty;

    protected AbstractProperty _actualWidthProperty;
    protected AbstractProperty _actualHeightProperty;
    protected AbstractProperty _horizontalAlignmentProperty;
    protected AbstractProperty _verticalAlignmentProperty;
    protected AbstractProperty _styleProperty;
    protected AbstractProperty _focusableProperty;
    protected AbstractProperty _hasFocusProperty;
    protected AbstractProperty _isMouseOverProperty;
    protected VisualAssetContext _opacityMaskContext;
    protected AbstractProperty _fontSizeProperty;
    protected AbstractProperty _fontFamilyProperty;

    protected AbstractProperty _contextMenuCommandProperty;

    protected bool _updateOpacityMask = false;
    protected RectangleF _lastOccupiedTransformedBounds = new RectangleF();
    protected bool _setFocus = false;
    protected Matrix _inverseFinalTransform = Matrix.Identity;

    #endregion

    #region Ctor

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

      // Default is not set
      _styleProperty = new SProperty(typeof(Style), null);

      // Default is stretch
      _horizontalAlignmentProperty = new SProperty(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Stretch);
      _verticalAlignmentProperty = new SProperty(typeof(VerticalAlignmentEnum), VerticalAlignmentEnum.Stretch);

      // Focus properties
      _focusableProperty = new SProperty(typeof(bool), false);
      _hasFocusProperty = new SProperty(typeof(bool), false);

      _isMouseOverProperty = new SProperty(typeof(bool), false);

      // Context menu
      _contextMenuCommandProperty = new SProperty(typeof(IExecutableCommand), null);

      // Font properties
      _fontFamilyProperty = new SProperty(typeof(string), String.Empty);
      _fontSizeProperty = new SProperty(typeof(int), -1);
    }

    void Attach()
    {
      _widthProperty.Attach(OnLayoutPropertyChanged);
      _heightProperty.Attach(OnLayoutPropertyChanged);
      _actualHeightProperty.Attach(OnActualBoundsChanged);
      _actualWidthProperty.Attach(OnActualBoundsChanged);
      _styleProperty.Attach(OnStyleChanged);
      _hasFocusProperty.Attach(OnHasFocusPropertyChanged);
      _fontFamilyProperty.Attach(OnFontChanged);
      _fontSizeProperty.Attach(OnFontChanged);

      _opacityProperty.Attach(OnOpacityChanged);
      _opacityMaskProperty.Attach(OnOpacityChanged);
      _acutalPositionProperty.Attach(OnActualBoundsChanged);
    }

    void Detach()
    {
      _widthProperty.Detach(OnLayoutPropertyChanged);
      _heightProperty.Detach(OnLayoutPropertyChanged);
      _actualHeightProperty.Detach(OnActualBoundsChanged);
      _actualWidthProperty.Detach(OnActualBoundsChanged);
      _styleProperty.Detach(OnStyleChanged);
      _hasFocusProperty.Detach(OnHasFocusPropertyChanged);
      _fontFamilyProperty.Detach(OnFontChanged);
      _fontSizeProperty.Detach(OnFontChanged);

      _opacityProperty.Detach(OnOpacityChanged);
      _opacityMaskProperty.Detach(OnOpacityChanged);
      _acutalPositionProperty.Detach(OnActualBoundsChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      FrameworkElement fe = (FrameworkElement) source;
      Width = fe.Width;
      Height = fe.Height;
      Style = fe.Style; // No copying necessary - Styles should be immutable
      ActualWidth = fe.ActualWidth;
      ActualHeight = fe.ActualHeight;
      HorizontalAlignment = fe.HorizontalAlignment;
      VerticalAlignment = fe.VerticalAlignment;
      Focusable = fe.Focusable;
      FontSize = fe.FontSize;
      FontFamily = fe.FontFamily;
      Attach();
    }

    #endregion

    protected void UpdateFocus()
    {
      if (!_setFocus)
        return;
      _setFocus = false;
      Screen screen = Screen;
      if (screen == null)
        return;
      FrameworkElement fe = PredictFocus(null, MoveFocusDirection.Down);
      if (fe != null)
        fe.HasFocus = true;
    }

    protected virtual void OnFontChanged(AbstractProperty property, object oldValue)
    {
      InvalidateLayout();
      InvalidateParentLayout();
    }

    protected virtual void OnStyleChanged(AbstractProperty property, object oldValue)
    {
      Style.Set(this);
      InvalidateLayout();
      InvalidateParentLayout();
    }

    void OnActualBoundsChanged(AbstractProperty property, object oldValue)
    {
      _updateOpacityMask = true;
    }

    void OnHasFocusPropertyChanged(AbstractProperty property, object oldValue)
    {
      Screen screen = Screen;
      if (screen == null)
      {
        // HasFocus has been set before the screen was initialized - simply remember the focus request in attribute _setFocus and reset HasFocus to false
        _setFocus = HasFocus;
        HasFocus = false;
        return;
      }
      if (HasFocus)
      {
        MakeVisible(this, ActualBounds);
        if (!screen.FrameworkElementGotFocus(this))
        {
          HasFocus = false;
          _setFocus = true;
          return;
        }
      }
      else
        screen.FrameworkElementLostFocus(this);
    }

    /// <summary>
    /// Called when a property value has been changed
    /// Since all UIElement properties are layout properties
    /// we're simply calling InvalidateLayout() here to invalidate the layout
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="oldValue">The old value of the property.</param>
    void OnLayoutPropertyChanged(AbstractProperty property, object oldValue)
    {
      InvalidateLayout();
    }

    void OnOpacityChanged(AbstractProperty property, object oldValue)
    {
      _updateOpacityMask = true;
    }

    #region Public properties

    public AbstractProperty WidthProperty
    {
      get { return _widthProperty; }
    }

    public double Width
    {
      get { return (double) _widthProperty.GetValue(); }
      set { _widthProperty.SetValue(value); }
    }

    public AbstractProperty HeightProperty
    {
      get { return _heightProperty; }
    }

    public double Height
    {
      get { return (double) _heightProperty.GetValue(); }
      set { _heightProperty.SetValue(value); }
    }

    public AbstractProperty ActualWidthProperty
    {
      get { return _actualWidthProperty; }
    }

    public double ActualWidth
    {
      get { return (double) _actualWidthProperty.GetValue(); }
      set { _actualWidthProperty.SetValue(value); }
    }

    public AbstractProperty ActualHeightProperty
    {
      get { return _actualHeightProperty; }
    }

    public double ActualHeight
    {
      get { return (double) _actualHeightProperty.GetValue(); }
      set { _actualHeightProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets this element's bounds in this element's coordinate system.
    /// This is a derived property which is calculated by the layout system.
    /// </summary>
    public RectangleF ActualBounds
    {
      get { return _innerRect; }
    }

    /// <summary>
    /// Gets the actual bounds plus <see cref="UIElement.Margin"/> plus the space which is needed for our
    /// <see cref="UIElement.LayoutTransform"/>.
    /// </summary>
    public RectangleF ActualTotalBounds
    {
      get { return _outerRect ?? new RectangleF(); }
    }

    public AbstractProperty HorizontalAlignmentProperty
    {
      get { return _horizontalAlignmentProperty; }
    }

    public HorizontalAlignmentEnum HorizontalAlignment
    {
      get { return (HorizontalAlignmentEnum) _horizontalAlignmentProperty.GetValue(); }
      set { _horizontalAlignmentProperty.SetValue(value); }
    }

    public AbstractProperty VerticalAlignmentProperty
    {
      get { return _verticalAlignmentProperty; }
    }

    public VerticalAlignmentEnum VerticalAlignment
    {
      get { return (VerticalAlignmentEnum) _verticalAlignmentProperty.GetValue(); }
      set { _verticalAlignmentProperty.SetValue(value);  }
    }

    public AbstractProperty StyleProperty
    {
      get { return _styleProperty; }
    }

    public Style Style
    {
      get { return (Style) _styleProperty.GetValue(); }
      set { _styleProperty.SetValue(value); }
    }

    /// <summary>
    /// Helper property to make it possible in the screenfiles to set the focus to a framework element (or its first focusable child)
    /// before the screen was initialized. Use this property to set the initial focus.
    /// </summary>
    public bool SetFocus
    {
      get { return _setFocus; }
      set { _setFocus = value; }
    }

    public AbstractProperty HasFocusProperty
    {
      get { return _hasFocusProperty; }
    }

    public virtual bool HasFocus
    {
      get { return (bool) _hasFocusProperty.GetValue(); }
      internal set { _hasFocusProperty.SetValue(value); }
    }

    public AbstractProperty FocusableProperty
    {
      get { return _focusableProperty; }
    }

    public bool Focusable
    {
      get { return (bool) _focusableProperty.GetValue(); }
      set { _focusableProperty.SetValue(value); }
    }

    public AbstractProperty IsMouseOverProperty
    {
      get { return _isMouseOverProperty; }
    }

    public bool IsMouseOver
    {
      get { return (bool) _isMouseOverProperty.GetValue(); }
      internal set { _isMouseOverProperty.SetValue(value); }
    }

    public AbstractProperty ContextMenuCommandProperty
    {
      get { return _contextMenuCommandProperty; }
    }

    public IExecutableCommand ContextMenuCommand
    {
      get { return (IExecutableCommand) _contextMenuCommandProperty.GetValue(); }
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
      get { return (string) _fontFamilyProperty.GetValue(); }
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
      get { return (int) _fontSizeProperty.GetValue(); }
      set { _fontSizeProperty.SetValue(value); }
    }

    #endregion

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

    public override void OnKeyPreview(ref Key key)
    {
      base.OnKeyPreview(ref key);
      if (!HasFocus)
        return;
      if (key == Key.None) return;
      if (key == Key.ContextMenu && ContextMenuCommand != null)
      {
        ContextMenuCommand.Execute();
        key = Key.None;
      }
    }

    /// <summary>
    /// Checks if this element is focusable. This is the case if the element is visible, enabled and
    /// focusable. If this is the case, this method will set the focus to this element.
    /// </summary>
    public bool TrySetFocus(bool checkChildren)
    {
      if (IsVisible && IsEnabled && Focusable)
      {
        HasFocus = true;
        return true;
      }
      if (checkChildren)
      {
        foreach (UIElement child in GetChildren())
        {
          FrameworkElement fe = child as FrameworkElement;
          if (fe == null || !fe.IsVisible || !fe.IsEnabled)
            continue;
          if (fe.TrySetFocus(true))
            return true;
        }
      }
      return false;
    }

    #region Replacing methods for the == operator which evaluate two float.NaN values to equal

    public static bool SameValue(float val1, float val2)
    {
      return float.IsNaN(val1) && float.IsNaN(val2) || val1 == val2;
    }

    public static bool SameSize(SizeF size1, SizeF size2)
    {
      return SameValue(size1.Width, size2.Width) && SameValue(size1.Height, size2.Height);
    }

    public static bool SameSize(SizeF? size1, SizeF size2)
    {
      return size1.HasValue && SameSize(size1.Value, size2);
    }

    public static bool SameRect(RectangleF rect1, RectangleF rect2)
    {
      return SameValue(rect1.X, rect2.X) && SameValue(rect1.Y, rect2.Y) && SameValue(rect1.Width, rect2.Width) && SameValue(rect1.Height, rect2.Height);
    }

    public static bool SameRect(RectangleF? rect1, RectangleF rect2)
    {
      return rect1.HasValue && SameRect(rect1.Value, rect2);
    }

    #endregion

    #region Measure & Arrange

    /// <summary>
    /// Updates the layout, i.e. calls <see cref="Measure(ref SizeF)"/> and <see cref="Arrange(RectangleF)"/>.
    /// Must be done from the render thread.
    /// </summary>
    public void UpdateLayout()
    {
      // When measure or arrange is directly or indirectly called from the following code, we need the measure/arrange to be
      // forced and not be optimized when the available size/outer rect are the same.
      // We could introduce new variables _isMeasureInvalid and _isArrangementInvalid, set it to true here and
      // check them in the Measure/Arrange methods, but it is easier to just clear our available size/outer rect cache which
      // also causes the Measure/Arrange to take place:
      _availableSize = null;
      _outerRect = null;

#if DEBUG_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}'", GetType().Name, Name));
#endif

      UIElement parent = VisualParent as UIElement;
      if (parent == null)
      {
        SizeF size = new SizeF(Screen.SkinWidth, Screen.SkinHeight);

#if DEBUG_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', no visual parent so measure with screen size {2}", GetType().Name, Name, size));
#endif
        Measure(ref size);

#if DEBUG_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', no visual parent so we arrange with screen size {2}", GetType().Name, Name, size));
#endif
        // Ignore the measured size - arrange with screen size
        Arrange(new RectangleF(0, 0, Screen.SkinWidth, Screen.SkinHeight));
      }
      else
      { // We have a visual parent, i.e parent != null
        if (!_availableSize.HasValue || !_outerRect.HasValue)
        {
#if DEBUG_LAYOUT
          System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', no available size or no outer rect, updating layout at parent {2}", GetType().Name, Name, parent));
#endif
          // We weren't Measured nor Arranged before - need to update parent
          parent.InvalidateLayout();
          return;
        }

        SizeF availableSize = new SizeF(_availableSize.Value.Width, _availableSize.Value.Height);
        SizeF formerDesiredSize = _desiredSize;

#if DEBUG_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', measuring with former available size {2}", GetType().Name, Name, availableSize));
#endif
        Measure(ref availableSize);

        if (_desiredSize != formerDesiredSize)
        {
#if DEBUG_LAYOUT
          System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', measuring returned different desired size, updating parent (former: {2}, now: {3})", GetType().Name, Name, formerDesiredSize, _desiredSize));
#endif
          // Our size has changed - we need to update our parent
          parent.InvalidateLayout();
          return;
        }
        // Our size is the same as before - just arrange
        RectangleF outerRect = new RectangleF(_outerRect.Value.Location, _outerRect.Value.Size);
#if DEBUG_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', measuring returned same desired size, arranging with old outer rect {2}", GetType().Name, Name, outerRect));
#endif
        Arrange(outerRect);
      }
    }

    /// <summary>
    /// Given the transform currently applied to child, this method finds (in axis-aligned local space)
    /// the largest rectangle that, after transform, fits within transformSpaceBounds.
    /// Largest rectangle means rectangle of the greatest area in local space (although maximal area in local space
    /// implies maximal area in transform space).
    /// </summary>
    /// <param name="transform">Transformation matrix.</param>
    /// <param name="localBounds">The bounds in local space where the returned size fits when transformed
    /// via the given <paramref name="transform"/>.</param>
    /// <returns>The dimensions, in local space, of the maximal area rectangle found.</returns>
    private static SizeF FindMaxTransformedSize(Matrix transform, SizeF localBounds)
    {
      // X (width) and Y (height) constraints for axis-aligned bounding box in dest. space
      float xConstr = localBounds.Width;
      float yConstr = localBounds.Height;

      // Avoid doing math on an empty rect
      if (IsNear(xConstr, 0) || IsNear(yConstr, 0)) 
        return new SizeF(0, 0); 

      bool xConstrInfinite = float.IsNaN(xConstr); 
      bool yConstrInfinite = float.IsNaN(yConstr);

      if (xConstrInfinite && yConstrInfinite)
        return new SizeF(float.NaN, float.NaN);

      if(xConstrInfinite) // Assume square for one-dimensional constraint 
        xConstr = yConstr; 
      else if (yConstrInfinite)
        yConstr = xConstr; 

      // We only deal with nonsingular matrices here. The nonsingular matrix is the one
      // that has inverse (determinant != 0).
      if(transform.Determinant() == 0) 
        return new SizeF(0, 0);

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
        float yCoverD = yConstrInfinite ? float.PositiveInfinity : Math.Abs(yConstr/d); 
        float xCoverA = xConstrInfinite ? float.PositiveInfinity : Math.Abs(xConstr/a);

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
            h = Math.Min(0.5f*Math.Abs(xConstr/c), yCoverD); 
            w = xCoverA - ((c * h) / a);
          } 
        }
        else
        { // c == 0, a != 0, b != 0, d != 0 

          // Maximizing under line (hIntercept=yConstr/d, wIntercept=yConstr/b)
          // BUT we still have constraint: w <= xConstr/a
          w = Math.Min( 0.5f*Math.Abs(yConstr/b), xCoverA);
          h = yCoverD - ((b * w) / d);
        }
      }
      else if (IsNear(a, 0) || IsNear(d, 0))
      { // (a == 0 || d == 0) ==> b != 0 && c != 0 
        float yCoverB = Math.Abs(yConstr/b);
        float xCoverC = Math.Abs(xConstr/c); 

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
            h = Math.Min(0.5f*Math.Abs(yConstr/d), xCoverC);
            w = yCoverB - ((d * h) / b);
          }
        }
        else
        { // d == 0, a != 0, b != 0, c != 0

          // Maximizing under line (hIntercept=xConstr/c, wIntercept=xConstr/a)
          // BUT we still have constraint: w <= yConstr/b
          w = Math.Min(0.5f*Math.Abs(xConstr/a), yCoverB);
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

          SizeF childSizeTr = new SizeF(w, h);
          transform.TransformSize(ref childSizeTr);
          float expandFactor = Math.Min(xConstr / childSizeTr.Width, yConstr / childSizeTr.Height); 
          if (!float.IsNaN(expandFactor) && !float.IsInfinity(expandFactor)) 
          {
            w *= expandFactor;
            h *= expandFactor; 
          }
        }
      }
      return new SizeF(w, h);
    }

    public sealed override void Measure(ref SizeF totalSize)
    {
#if DEBUG_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("Measure {0} Name='{1}', totalSize={2}", GetType().Name, Name, totalSize));
#endif
      if (SameSize(_availableSize, totalSize))
      { // Optimization: If our input data is the same and the layout isn't invalid, we don't need to measure again
        totalSize = _desiredSize;
#if DEBUG_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("Measure {0} Name='{1}', cutting short, totalSize is like before, returns desired size={2}", GetType().Name, Name, totalSize));
#endif
        return;
      }
      _availableSize = new SizeF(totalSize);
      RemoveMargin(ref totalSize, Margin);
      
      Matrix? layoutTransform = LayoutTransform == null ? new Matrix?() : LayoutTransform.GetTransform();
      if (layoutTransform.HasValue)
        totalSize = FindMaxTransformedSize(layoutTransform.Value, totalSize);

      if (!double.IsNaN(Width))
        totalSize.Width = (float) Width;
      if (!double.IsNaN(Height))
        totalSize.Height = (float) Height;

      totalSize = CalculateDesiredSize(new SizeF(totalSize));

      if (!double.IsNaN(Width))
        totalSize.Width = (float) Width;
      if (!double.IsNaN(Height))
        totalSize.Height = (float) Height;

      if (layoutTransform.HasValue)
        layoutTransform.Value.TransformSize(ref totalSize);

      AddMargin(ref totalSize, Margin);
      _desiredSize = totalSize;
#if DEBUG_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("Measure {0} Name='{1}', returns calculated desired size={2}", GetType().Name, Name, totalSize));
#endif
    }

    public sealed override void Arrange(RectangleF outerRect)
    {
#if DEBUG_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("Arrange {0} Name='{1}', outerRect={2}", GetType().Name, Name, outerRect));
#endif
      if (SameRect(_outerRect, outerRect))
      { // Optimization: If our input data is the same and the layout isn't invalid, we don't need to arrange again
#if DEBUG_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("Arrange {0} Name='{1}', cutting short, outerRect={2} is like before", GetType().Name, Name, outerRect));
#endif
        return;
      }
      _outerRect = new RectangleF(outerRect.Location, outerRect.Size);
      RectangleF rect = new RectangleF(outerRect.Location, outerRect.Size);
      RemoveMargin(ref rect, Margin);

      if (LayoutTransform != null)
      {
        Matrix layoutTransform = LayoutTransform.GetTransform().RemoveTranslation();
        if (!layoutTransform.IsIdentity)
        {
          SizeF transformedSize = FindMaxTransformedSize(layoutTransform, rect.Size);

          rect = new RectangleF(
              rect.Location.X + (rect.Width - transformedSize.Width)/2,
              rect.Location.Y + (rect.Height - transformedSize.Height)/2,
              transformedSize.Width,
              transformedSize.Height);
        }
      }
      _innerRect = rect;
      ArrangeOverride();

      Initialize();
      InitializeTriggers();
    }

    protected virtual void ArrangeOverride()
    {
      ActualPosition = _innerRect.Location;
      ActualWidth = _innerRect.Width;
      ActualHeight = _innerRect.Height;
      UpdateFocus();
    }

    protected virtual SizeF CalculateDesiredSize(SizeF totalSize)
    {
      return new SizeF();
    }

    /// <summary>
    /// Arranges the child horizontal and vertical in a given area. If the area is bigger than
    /// the child's desired size, the child will be arranged according to its
    /// <see cref="HorizontalAlignment"/> and <see cref="VerticalAlignment"/>.
    /// </summary>
    /// <param name="child">The child to arrange. The child will not be changed by this method.</param>
    /// <param name="location">Input: The starting position of the available area. Output: The position
    /// the child should be located.</param>
    /// <param name="childSize">Input: The available area for the <paramref name="child"/>. Output:
    /// The area the child should take.</param>
    public void ArrangeChild(FrameworkElement child, ref PointF location, ref SizeF childSize)
    {
      SizeF desiredSize = child.DesiredSize;

      if (!double.IsNaN(desiredSize.Width))
      {
        if (desiredSize.Width < childSize.Width)
        {
          // Width takes precedence over Stretch - Use Center as fallback
          if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center ||
              (child.HorizontalAlignment == HorizontalAlignmentEnum.Stretch && !double.IsNaN(child.Width)))
          {
            location.X += (childSize.Width - desiredSize.Width)/2;
            childSize.Width = desiredSize.Width;
          }
          else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
          {
            location.X += childSize.Width - desiredSize.Width;
            childSize.Width = desiredSize.Width;
          }
          else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Left)
          {
            // Leave location unchanged
            childSize.Width = desiredSize.Width;
          }
          //else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Stretch)
          // Do nothing
        }
      }

      if (!double.IsNaN(desiredSize.Height))
      {
        if (desiredSize.Height < childSize.Height)
        {
          // Height takes precedence over Stretch - Use Center as fallback
          if (child.VerticalAlignment == VerticalAlignmentEnum.Center ||
              (child.VerticalAlignment == VerticalAlignmentEnum.Stretch && !double.IsNaN(child.Height)))
          {
            location.Y += (childSize.Height - desiredSize.Height)/2;
            childSize.Height = desiredSize.Height;
          }
          else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
          {
            location.Y += childSize.Height - desiredSize.Height;
            childSize.Height = desiredSize.Height;
          }
          else if (child.VerticalAlignment == VerticalAlignmentEnum.Top)
          {
            // Leave location unchanged
            childSize.Height = desiredSize.Height;
          }
          //else if (child.VerticalAlignment == VerticalAlignmentEnum.Stretch)
          // Do nothing
        }
      }
    }

    /// <summary>
    /// Arranges the child horizontal in a given area. If the area is bigger than the child's desired
    /// size, the child will be arranged according to its <see cref="HorizontalAlignment"/>.
    /// </summary>
    /// <param name="child">The child to arrange. The child will not be changed by this method.</param>
    /// <param name="location">Input: The starting position of the available area. Output: The position
    /// the child should be located.</param>
    /// <param name="childSize">Input: The available area for the <paramref name="child"/>. Output:
    /// The area the child should take.</param>
    public void ArrangeChildHorizontal(FrameworkElement child, ref PointF location, ref SizeF childSize)
    {
      SizeF desiredSize = child.DesiredSize;

      if (!double.IsNaN(desiredSize.Width) && desiredSize.Width < childSize.Width)
      {
        // Width takes precedence over Stretch - Use Center as fallback
        if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center ||
            (child.HorizontalAlignment == HorizontalAlignmentEnum.Stretch && !double.IsNaN(child.Width)))
        {
          location.X += (childSize.Width - desiredSize.Width) / 2;
          childSize.Width = desiredSize.Width;
        }
        else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
        {
          location.X += childSize.Width - desiredSize.Width;
          childSize.Width = desiredSize.Width;
        }
        else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Left)
        {
          // Leave location unchanged
          childSize.Width = desiredSize.Width;
        }
        //else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Stretch)
        // Do nothing
      }
    }

    /// <summary>
    /// Arranges the child vertical in a given area. If the area is bigger than the child's desired
    /// size, the child will be arranged according to its <see cref="VerticalAlignment"/>.
    /// </summary>
    /// <param name="child">The child to arrange. The child will not be changed by this method.</param>
    /// <param name="location">Input: The starting position of the available area. Output: The position
    /// the child should be located.</param>
    /// <param name="childSize">Input: The available area for the <paramref name="child"/>. Output:
    /// The area the child should take.</param>
    public void ArrangeChildVertical(FrameworkElement child, ref PointF location, ref SizeF childSize)
    {
      SizeF desiredSize = child.DesiredSize;

      if (!double.IsNaN(desiredSize.Width) && desiredSize.Height < childSize.Height)
      {
        // Height takes precedence over Stretch - Use Center as fallback
        if (child.VerticalAlignment == VerticalAlignmentEnum.Center ||
            (child.VerticalAlignment == VerticalAlignmentEnum.Stretch && !double.IsNaN(child.Height)))
        {
          location.Y += (childSize.Height - desiredSize.Height) / 2;
          childSize.Height = desiredSize.Height;
        }
        else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
        {
          location.Y += childSize.Height - desiredSize.Height;
          childSize.Height = desiredSize.Height;
        }
        else if (child.VerticalAlignment == VerticalAlignmentEnum.Top)
        {
          // Leave location unchanged
          childSize.Height = desiredSize.Height;
        }
        //else if (child.VerticalAlignment == VerticalAlignmentEnum.Stretch)
        // Do nothing
      }
    }

    #endregion

    protected void TransformMouseCoordinates(ref float x, ref float y)
    {
      _inverseFinalTransform.Transform(ref x, ref y);
    }

    public override void OnMouseMove(float x, float y)
    {
      float xTrans = x;
      float yTrans = y;
      TransformMouseCoordinates(ref xTrans, ref yTrans);
      if (ActualBounds.Contains(xTrans, yTrans))
      {
        if (!IsMouseOver)
        {
          IsMouseOver = true;
          FireEvent(MOUSEENTER_EVENT);
        }
        if (!HasFocus && IsInVisibleArea(xTrans, yTrans))
          TrySetFocus(false);
      }
      else
      {
        if (IsMouseOver)
        {
          IsMouseOver = false;
          FireEvent(MOUSELEAVE_EVENT);
        }
        if (HasFocus)
          HasFocus = false;
      }
      base.OnMouseMove(x, y);
    }

    public override bool IsInVisibleArea(float x, float y)
    {
      UIElement parent = VisualParent as UIElement;
      return parent != null ? parent.IsChildVisibleAt(this, x, y) : IsInArea(x, y);
    }

    public override bool IsInArea(float x, float y)
    {
      return x >= ActualPosition.X && x <= ActualPosition.X + ActualWidth &&
          y >= ActualPosition.Y && y <= ActualPosition.Y + ActualHeight;
    }

    #region Focus & control predicition

    #region Focus movement

    protected FrameworkElement GetFocusedElementOrChild()
    {
      FrameworkElement result = Screen == null ? null : Screen.FocusedElement;
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
      FrameworkElement nextElement;
      while ((nextElement = PredictFocus(currentElement.ActualBounds, direction)) != null)
        currentElement = nextElement;
      return currentElement.TrySetFocus(true);
    }

    #endregion

    #region Focus prediction

    private float CalcCenterDirection(RectangleF otherRect)
    {
      PointF thisCenter = GetCenterPosition(ActualBounds);
      PointF thatCenter = GetCenterPosition(otherRect);
      if (IsNear(thisCenter.X, thatCenter.X) && IsNear(thisCenter.Y, thatCenter.Y))
        return float.NaN;
      double x = thatCenter.X - thisCenter.X;
      double y = thatCenter.Y - thisCenter.Y;
      double alpha = Math.Acos(x/Math.Sqrt(x*x+y*y));
      if (thatCenter.Y > thisCenter.Y) // Coordinates go from top to bottom, so y must be inverted
        alpha = -alpha;
      if (alpha < 0)
        alpha += 2*Math.PI;
      return (float) alpha;
    }

    protected bool IsLocatedAbove(RectangleF otherRect)
    {
      float alpha = CalcCenterDirection(otherRect);
      return alpha >= Math.PI/4 && alpha <= 3*Math.PI/4;
    }

    protected bool IsLocatedBelow(RectangleF otherRect)
    {
      float alpha = CalcCenterDirection(otherRect);
      return alpha >= 5*Math.PI/4 && alpha <= 7*Math.PI/4;
    }

    protected bool IsLocatedRightOf(RectangleF otherRect)
    {
      float alpha = CalcCenterDirection(otherRect);
      return alpha <= Math.PI/4 || alpha >= 7*Math.PI/4;
    }

    protected bool IsLocatedLeftOf(RectangleF otherRect)
    {
      float alpha = CalcCenterDirection(otherRect);
      return alpha >= 3*Math.PI/4 && alpha <= 5*Math.PI/4;
    }

    /// <summary>
    /// Predicts the next control which is positioned in the specified direction
    /// <paramref name="dir"/> to the specified <paramref name="currentFocusRect"/> and
    /// which is able to get the focus.
    /// This method will search the control tree down starting with this element as root element.
    /// </summary>
    /// <param name="currentFocusRect">The borders of the currently focused control.</param>
    /// <param name="dir">Direction, the result control should be positioned relative to the
    /// currently focused control.</param>
    /// <returns>Framework element which should get the focus, or <c>null</c>, if this control
    /// tree doesn't contain an appropriate control. The returned control will be
    /// visible, focusable and enabled.</returns>
    public virtual FrameworkElement PredictFocus(RectangleF? currentFocusRect, MoveFocusDirection dir)
    {
      if (!IsVisible)
        return null;
      // Check if this control is a possible return value
      if (IsEnabled && Focusable)
        if (!currentFocusRect.HasValue ||
            (dir == MoveFocusDirection.Up && IsLocatedBelow(currentFocusRect.Value)) ||
            (dir == MoveFocusDirection.Down && IsLocatedAbove(currentFocusRect.Value)) ||
            (dir == MoveFocusDirection.Left && IsLocatedRightOf(currentFocusRect.Value)) ||
            (dir == MoveFocusDirection.Right && IsLocatedLeftOf(currentFocusRect.Value)))
          return this;
      // Check child controls
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      float bestCenterDistance = float.MaxValue;
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible || !(child is FrameworkElement)) continue;
        FrameworkElement fe = (FrameworkElement) child;

        FrameworkElement match = fe.PredictFocus(currentFocusRect, dir);
        if (match != null)
        {
          if (!currentFocusRect.HasValue)
            // If we don't have a comparison rect, simply return first match.
            return match;
          // Calculate and compare distances of all matches
          float centerDistance = CenterDistance(match.ActualBounds, currentFocusRect.Value);
          if (centerDistance == 0)
            // If the control's center is exactly the center of the currently focused element,
            // it won't be used as next focus element
            continue;
          float distance = BorderDistance(match.ActualBounds, currentFocusRect.Value);
          if (bestMatch == null || distance < bestDistance ||
              distance == bestDistance && centerDistance < bestCenterDistance)
          {
            bestMatch = match;
            bestDistance = distance;
            bestCenterDistance = centerDistance;
          }
        }
      }
      return bestMatch;
    }

    protected static float BorderDistance(RectangleF r1, RectangleF r2)
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
      return (float) Math.Sqrt(distX * distX + distY * distY);
    }

    protected static float CenterDistance(RectangleF r1, RectangleF r2)
    {
      float distX = Math.Abs((r1.Left + r1.Right) / 2 - (r2.Left + r2.Right) / 2);
      float distY = Math.Abs((r1.Top + r1.Bottom) / 2 - (r2.Top + r2.Bottom) / 2);
      return (float) Math.Sqrt(distX * distX + distY * distY);
    }

    protected PointF GetCenterPosition(RectangleF rect)
    {
      return new PointF((rect.Left + rect.Right)/2, (rect.Top + rect.Bottom)/2);
    }

    #endregion

    #endregion

    public virtual void DoRender(RenderContext localRenderContext)
    {
    }

    public void RenderToTexture(Texture texture, RenderContext renderContext)
    {
      // We do the following here:
      // 1. Set the rendertarget to the given texture
      // 2. Clear the texture with an alpha value of 0
      // 3. Render the control (into the texture)
      // 4. Restore the rendertarget to the backbuffer

      // Get the current backbuffer
      using (Surface backBuffer = GraphicsDevice.Device.GetRenderTarget(0))
        // Get the surface of our render texture
        using (Surface renderTextureSurface = texture.GetSurfaceLevel(0))
        {
          // Change the rendertarget to the render texture
          GraphicsDevice.Device.SetRenderTarget(0, renderTextureSurface);

          // Fill the background of the texture with an alpha value of 0
          GraphicsDevice.Device.Clear(ClearFlags.Target, Color.FromArgb(0, Color.Black), 1.0f, 0);

          // Render the control into the given texture
          DoRender(renderContext);

          // Restore the backbuffer
          GraphicsDevice.Device.SetRenderTarget(0, backBuffer);
        }
    }

    public override void Render(RenderContext parentRenderContext)
    {
      if (!IsVisible)
        return;

      RectangleF bounds = ActualBounds;
      if (bounds.Width <= 0 || bounds.Height <= 0)
        return;

      Matrix? layoutTransformMatrix = LayoutTransform == null ? new Matrix?() : LayoutTransform.GetTransform();
      Matrix? renderTransformMatrix = RenderTransform == null ? new Matrix?() : RenderTransform.GetTransform();

      RenderContext localRenderContext = parentRenderContext.Derive(bounds, layoutTransformMatrix,
          renderTransformMatrix, RenderTransformOrigin, Opacity);
      _inverseFinalTransform = Matrix.Invert(localRenderContext.MouseTransform);

      if (OpacityMask == null)
        // Simply render without opacity mask
        DoRender(localRenderContext);
      else
      { // Control has an opacity mask
        Size textureSize = new Size(SkinContext.SkinResources.SkinWidth, SkinContext.SkinResources.SkinHeight);
        if (_updateOpacityMask || !_opacityMaskContext.IsAllocated)
          PrepareOpacityMaskContext(textureSize);

        // FIXME: Fix this. If the window doesn't have the size of the skin, drawing through an opacity
        // mask doesn't stretch the output
        RenderContext tempRenderContext = new RenderContext(Matrix.Identity, localRenderContext.Transform, bounds);
        RenderToTexture(_opacityMaskContext.Texture, tempRenderContext);

        if (tempRenderContext.OccupiedTransformedBounds != _lastOccupiedTransformedBounds)
          // We must check this each render pass because the control might have changed its bounds due to a render transform
          _updateOpacityMask = true;
        _lastOccupiedTransformedBounds = tempRenderContext.OccupiedTransformedBounds;

        if (_updateOpacityMask)
        {
          UpdateOpacityMask(tempRenderContext.OccupiedTransformedBounds, textureSize, localRenderContext.ZOrder);
          _updateOpacityMask = false;
        }

        // Now render the opacitytexture with the OpacityMask brush

        OpacityMask.BeginRenderOpacityBrush(_opacityMaskContext.Texture, localRenderContext);
        GraphicsDevice.Device.VertexFormat = _opacityMaskContext.VertexFormat;
        GraphicsDevice.Device.SetStreamSource(0, _opacityMaskContext.VertexBuffer, 0, _opacityMaskContext.StrideSize);
        GraphicsDevice.Device.DrawPrimitives(_opacityMaskContext.PrimitiveType, 0, 2);
        OpacityMask.EndRender();

        _opacityMaskContext.LastTimeUsed = SkinContext.FrameRenderingStartTime.AddDays(1);
      }
      // Calculation of absolute render size (in world coordinate system)
      parentRenderContext.IncludeTransformedContentsBounds(localRenderContext.OccupiedTransformedBounds);
    }

    #region Opacitymask

    void PrepareOpacityMaskContext(Size textureSize)
    {
      if (_opacityMaskContext != null)
      {
        _opacityMaskContext.Free(false);
        _opacityMaskContext = null;
      }
      if (OpacityMask == null)
        return;
      _opacityMaskContext = new VisualAssetContext("FrameworkElement.OpacityMaskContext:" + Name, Screen.Name,
          new Texture(GraphicsDevice.Device, textureSize.Width, textureSize.Height, 1,
              Usage.RenderTarget, Format.A8R8G8B8, Pool.Default));
      ContentManager.Add(_opacityMaskContext);
    }

    void UpdateOpacityMask(RectangleF bounds, SizeF textureSize, float zPos)
    {
      if (OpacityMask == null)
        return;

      PositionColored2Textured[] verts = new PositionColored2Textured[6];

      Color4 col = ColorConverter.FromColor(Color.White);
      col.Alpha *= (float) Opacity;
      int color = col.ToArgb();
      
      float left = bounds.Left - 0.5f;
      float right = bounds.Right + 0.5f;
      float top = bounds.Top - 0.5f;
      float bottom = bounds.Bottom + 0.5f;
      float uLeft = bounds.Left / textureSize.Width;
      float uRight = bounds.Right / textureSize.Width;
      float vTop = bounds.Top / textureSize.Height;
      float vBottom = bounds.Bottom / textureSize.Height;

      // Upper left
      verts[0].X = left;
      verts[0].Y = top;
      verts[0].Color = color;
      verts[0].Tu1 = uLeft;
      verts[0].Tv1 = vTop;
      verts[0].Z = zPos;

      // Bottom left
      verts[1].X = left;
      verts[1].Y = bottom;
      verts[1].Color = color;
      verts[1].Tu1 = uLeft;
      verts[1].Tv1 = vBottom;
      verts[1].Z = zPos;

      // Bottom right
      verts[2].X = right;
      verts[2].Y = bottom;
      verts[2].Color = color;
      verts[2].Tu1 = uRight;
      verts[2].Tv1 = vBottom;
      verts[2].Z = zPos;

      // Upper left
      verts[3].X = left;
      verts[3].Y = top;
      verts[3].Color = color;
      verts[3].Tu1 = uLeft;
      verts[3].Tv1 = vTop;
      verts[3].Z = zPos;

      // Bottom right
      verts[4].X = right;
      verts[4].Y = bottom;
      verts[4].Color = color;
      verts[4].Tu1 = uRight;
      verts[4].Tv1 = vBottom;
      verts[4].Z = zPos;

      // Upper right
      verts[5].X = right;
      verts[5].Y = top;
      verts[5].Color = color;
      verts[5].Tu1 = uRight;
      verts[5].Tv1 = vTop;
      verts[5].Z = zPos;

      _opacityMaskContext.SetVerts(verts, PrimitiveType.TriangleList);
      OpacityMask.SetupBrush(this, ref verts, zPos, false);
    }

    #endregion

    public override void Allocate()
    {
      base.Allocate();
      if (_opacityMaskContext != null)
        ContentManager.Add(_opacityMaskContext);
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (_opacityMaskContext != null)
      {
        _opacityMaskContext.Free(true);
        ContentManager.Remove(_opacityMaskContext);
        _opacityMaskContext = null;
      }
    }
  }
}
