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
using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using SlimDX;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.UI.SkinEngine.Controls.Animations;
using MediaPortal.UI.SkinEngine.Controls.Transforms;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{

  public enum VisibilityEnum
  {
    Visible = 0,
    Hidden = 1,
    Collapsed = 2,
  }

  [Flags]
  public enum UIEvent
  {
    None = 0,
    Hidden = 1,
    Visible = 2,
    OpacityChange = 4,
    StrokeChange = 8,
    FillChange = 16,
  }

  /// <summary>
  /// Delegate interface which decides if an element fulfills a special condition.
  /// </summary>
  public interface IFinder
  {
    /// <summary>
    /// Query method which decides if the specified <paramref name="current"/>
    /// element fulfills the condition exposed by this class.
    /// </summary>
    /// <returns><c>true</c> if the specified <paramref name="current"/> element
    /// fulfills the condition exposed by this class, else <c>false</c>.</returns>
    bool Query(UIElement current);
  }

  /// <summary>
  /// Finder implementation which returns an element if multiple child finders accept it.
  /// </summary>
  public class MultiFinder : IFinder
  {
    protected IFinder[] _finders;

    public MultiFinder(IFinder[] finders)
    {
      _finders = finders;
    }

    public bool Query(UIElement current)
    {
      foreach (IFinder finder in _finders)
        if (!finder.Query(current))
          return false;
      return true;
    }
  }

  /// <summary>
  /// Finder implementation which returns an element if it is visible.
  /// </summary>
  public class VisibleElementFinder : IFinder
  {
    private static VisibleElementFinder _instance = null;

    public bool Query(UIElement current)
    {
      return current.IsVisible;
    }

    public static VisibleElementFinder Instance
    {
      get
      {
        if (_instance == null)
          _instance = new VisibleElementFinder();
        return _instance;
      }
    }
  }

  /// <summary>
  /// Finder implementation which looks for elements of the specified type.
  /// </summary>
  public class TypeFinder : IFinder
  {
    protected Type _type;

    public TypeFinder(Type type)
    {
      _type = type;
    }

    public bool Query(UIElement current)
    {
      return _type == current.GetType();
    }
  }

  /// <summary>
  /// Finder implementation which looks for elements of a the given type or
  /// of a type derived from the given type.
  /// </summary>
  public class SubTypeFinder : IFinder
  {
    protected Type _type;

    public SubTypeFinder(Type type)
    {
      _type = type;
    }

    public bool Query(UIElement current)
    {
      return _type.IsAssignableFrom(current.GetType());
    }
  }

  /// <summary>
  /// Finder implementation which looks for elements of a specified name.
  /// </summary>
  public class NameFinder : IFinder
  {
    protected string _name;

    public NameFinder(string name)
    {
      _name = name;
    }

    public bool Query(UIElement current)
    {
      return _name == current.Name;
    }
  }

  /// <summary>
  /// Delegate interface which takes an action on an <see cref="UIElement"/>.
  /// </summary>
  public interface IUIElementAction
  {
    /// <summary>
    /// Executes this action on the specified <paramref name="element"/>.
    /// </summary>
    /// <param name="element">The element to execute this action.</param>
    void Execute(UIElement element);
  }

  /// <summary>
  /// UI element action which sets the specified screen to ui elements.
  /// </summary>
  public class SetScreenAction : IUIElementAction
  {
    protected Screen _screen;

    public SetScreenAction(Screen screen)
    {
      _screen = screen;
    }

    public void Execute(UIElement element)
    {
      element.Screen = _screen;
    }
  }

  public delegate void UIEventDelegate(string eventName);

  public abstract class UIElement : Visual, IContentEnabled
  {
    protected static IList<UIElement> EMPTY_UIELEMENT_LIST = new List<UIElement>();
    protected const string LOADED_EVENT = "UIElement.Loaded";

    public const double DELTA_DOUBLE = 0.01;

    #region Protected fields

    protected AbstractProperty _nameProperty;
    protected AbstractProperty _acutalPositionProperty;
    protected AbstractProperty _marginProperty;
    protected AbstractProperty _triggerProperty;
    protected AbstractProperty _renderTransformProperty;
    protected AbstractProperty _renderTransformOriginProperty;
    protected AbstractProperty _layoutTransformProperty;
    protected AbstractProperty _visibilityProperty;
    protected AbstractProperty _isEnabledProperty;
    protected AbstractProperty _opacityMaskProperty;
    protected AbstractProperty _opacityProperty;
    protected AbstractProperty _freezableProperty;
    protected AbstractProperty _templateNameScopeProperty;
    protected SizeF? _availableSize;
    protected SizeF _desiredSize;
    protected RectangleF? _outerRect;
    protected RectangleF _finalRect;
    protected ResourceDictionary _resources;
    protected volatile bool _isLayoutInvalid = true;
    protected ExtendedMatrix _finalLayoutTransform;
    protected IExecutableCommand _loaded;
    protected bool _triggersInitialized;
    protected bool _fireLoaded = true;

    #endregion

    #region Ctor

    protected UIElement()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _nameProperty = new SProperty(typeof(string), string.Empty);
      _acutalPositionProperty = new SProperty(typeof(Vector3), new Vector3(0, 0, 1));
      _marginProperty = new SProperty(typeof(Thickness), new Thickness(0, 0, 0, 0));
      _resources = new ResourceDictionary();
      _triggerProperty = new SProperty(typeof(IList<TriggerBase>), new List<TriggerBase>());
      _renderTransformProperty = new SProperty(typeof(Transform), null);
      _layoutTransformProperty = new SProperty(typeof(Transform), null);
      _renderTransformOriginProperty = new SProperty(typeof(Vector2), new Vector2(0, 0));
      _visibilityProperty = new SProperty(typeof(VisibilityEnum), VisibilityEnum.Visible);
      _isEnabledProperty = new SProperty(typeof(bool), true);
      _freezableProperty = new SProperty(typeof(bool), false);
      _opacityProperty = new SProperty(typeof(double), 1.0);
      _templateNameScopeProperty = new SProperty(typeof(INameScope), null);

      _opacityMaskProperty = new SProperty(typeof(Brushes.Brush), null);
    }

    void Attach()
    {
      _marginProperty.Attach(OnLayoutPropertyChanged);
      _visibilityProperty.Attach(OnVisibilityPropertyChanged);
      _opacityProperty.Attach(OnOpacityPropertyChanged);
      _layoutTransformProperty.Attach(OnLayoutTransformPropertyChanged);
    }

    void Detach()
    {
      _marginProperty.Detach(OnLayoutPropertyChanged);
      _visibilityProperty.Detach(OnVisibilityPropertyChanged);
      _opacityProperty.Detach(OnOpacityPropertyChanged);
      _layoutTransformProperty.Detach(OnLayoutTransformPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      UIElement el = (UIElement) source;
      Name = copyManager.GetCopy(el.Name);
      // We do not copy the focus flag, only one element can have focus
      //HasFocus = copyManager.GetCopy(el.HasFocus);
      ActualPosition = copyManager.GetCopy(el.ActualPosition);
      Margin = copyManager.GetCopy(el.Margin);
      Visibility = copyManager.GetCopy(el.Visibility);
      IsEnabled = copyManager.GetCopy(el.IsEnabled);
      // TODO Albert78: Implement Freezing
      Freezable = copyManager.GetCopy(el.Freezable);
      Opacity = copyManager.GetCopy(el.Opacity);
      Loaded = copyManager.GetCopy(el.Loaded);
      OpacityMask = copyManager.GetCopy(el.OpacityMask);
      object oldLayoutTransform = LayoutTransform;
      LayoutTransform = copyManager.GetCopy(el.LayoutTransform);
      RenderTransform = copyManager.GetCopy(el.RenderTransform);
      RenderTransformOrigin = copyManager.GetCopy(el.RenderTransformOrigin);
      TemplateNameScope = copyManager.GetCopy(el.TemplateNameScope);
      // Simply reuse the Resources
      SetResources(el._resources);

      // Need to manually call this because we are in a detached state
      OnLayoutTransformPropertyChanged(_layoutTransformProperty, oldLayoutTransform);

      foreach (TriggerBase t in el.Triggers)
        Triggers.Add(copyManager.GetCopy(t));
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      foreach (UIElement child in GetChildren())
        child.Dispose();
    }

    #endregion

    void OnOpacityPropertyChanged(AbstractProperty property, object oldValue)
    {
      FireUIEvent(UIEvent.OpacityChange, this);
    }

    void OnVisibilityPropertyChanged(AbstractProperty property, object oldValue)
    {
      InvalidateParent();
      if (IsVisible)
        FireUIEvent(UIEvent.Visible, this);
      else
        FireUIEvent(UIEvent.Hidden, this);
    }

    /// <summary>
    /// Called when a property value has been changed which makes the current layout invalid.
    /// This method will call Invalidate() to invalidate the layout.
    /// </summary>
    /// <param name="property">The property which was changed.</param>
    /// <param name="oldValue">The old value of the property.</param>
    void OnLayoutPropertyChanged(AbstractProperty property, object oldValue)
    {
      Invalidate();
    }

    void OnLayoutTransformChanged(IObservable observable)
    {
      Invalidate();
    }

    void OnLayoutTransformPropertyChanged(AbstractProperty property, object oldValue)
    {
      if (oldValue is Transform)
        ((Transform) oldValue).ObjectChanged -= OnLayoutTransformChanged;
      if (LayoutTransform != null)
        LayoutTransform.ObjectChanged += OnLayoutTransformChanged;
    }

    public void SetResources(ResourceDictionary resources)
    {
      _resources = resources;
    }

    #region Public properties

    /// <summary>
    /// Event handler called for all events defined by their event string
    /// like <see cref="LOADED_EVENT"/>.
    /// </summary>
    public event UIEventDelegate EventOccured;

    public IExecutableCommand Loaded
    {
      get { return _loaded; }
      set { _loaded = value; }
    }

    public ResourceDictionary Resources
    {
      get { return _resources; }
    }

    public ExtendedMatrix FinalLayoutTransform
    {
      get { return _finalLayoutTransform; }
    }

    public AbstractProperty OpacityProperty
    {
      get { return _opacityProperty; }
    }

    public double Opacity
    {
      get { return (double) _opacityProperty.GetValue(); }
      set { _opacityProperty.SetValue(value); }
    }

    public AbstractProperty FreezableProperty
    {
      get { return _freezableProperty; }
    }

    public bool Freezable
    {
      get { return (bool)_freezableProperty.GetValue(); }
      set { _freezableProperty.SetValue(value); }
    }

    public AbstractProperty OpacityMaskProperty
    {
      get { return _opacityMaskProperty; }
    }

    public Brushes.Brush OpacityMask
    {
      get { return (Brushes.Brush) _opacityMaskProperty.GetValue(); }
      set { _opacityMaskProperty.SetValue(value); }
    }

    public AbstractProperty IsEnabledProperty
    {
      get { return _isEnabledProperty; }
    }

    public bool IsEnabled
    {
      get { return (bool) _isEnabledProperty.GetValue(); }
      set { _isEnabledProperty.SetValue(value); }
    }

    public AbstractProperty VisibilityProperty
    {
      get { return _visibilityProperty; }
    }

    public VisibilityEnum Visibility
    {
      get { return (VisibilityEnum) _visibilityProperty.GetValue(); }
      set { _visibilityProperty.SetValue(value); }
    }

    public bool IsVisible
    {
      get { return Visibility == VisibilityEnum.Visible; }
      set { Visibility = value ? VisibilityEnum.Visible : VisibilityEnum.Hidden; }
    }

    public AbstractProperty TriggersProperty
    {
      get { return _triggerProperty; }
    }

    public IList<TriggerBase> Triggers
    {
      get { return (IList<TriggerBase>) _triggerProperty.GetValue(); }
    }

    public AbstractProperty ActualPositionProperty
    {
      get { return _acutalPositionProperty; }
    }

    public Vector3 ActualPosition
    {
      get { return (Vector3) _acutalPositionProperty.GetValue(); }
      set { _acutalPositionProperty.SetValue(value); }
    }

    public AbstractProperty NameProperty
    {
      get { return _nameProperty; }
    }

    public string Name
    {
      get { return _nameProperty.GetValue() as string; }
      set
      {
        INameScope ns = FindNameScope();
        if (ns != null)
          ns.UnregisterName(Name);
        _nameProperty.SetValue(value);
        if (ns != null)
          try
          {
            if (ns.FindName(Name) == this)
              return; // Avoid exception when registered multiple times
            ns.RegisterName(Name, this);
          }
          catch
          {
            throw new ArgumentException("Name '"+Name+"' was registered twice in namescope '"+ns+"'");
          }
      }
    }

    public AbstractProperty MarginProperty
    {
      get { return _marginProperty; }
    }

    public Thickness Margin
    {
      get { return (Thickness)_marginProperty.GetValue(); }
      set { _marginProperty.SetValue(value); }
    }

    public AbstractProperty LayoutTransformProperty
    {
      get { return _layoutTransformProperty; }
    }

    public Transform LayoutTransform
    {
      get { return _layoutTransformProperty.GetValue() as Transform; }
      set { _layoutTransformProperty.SetValue(value); }
    }

    public AbstractProperty RenderTransformProperty
    {
      get { return _renderTransformProperty; }
    }

    public Transform RenderTransform
    {
      get { return (Transform) _renderTransformProperty.GetValue(); }
      set { _renderTransformProperty.SetValue(value); }
    }

    public AbstractProperty RenderTransformOriginProperty
    {
      get { return _renderTransformOriginProperty; }
    }

    public Vector2 RenderTransformOrigin
    {
      get { return (Vector2) _renderTransformOriginProperty.GetValue(); }
      set { _renderTransformOriginProperty.SetValue(value); }
    }

    public bool IsLayoutInvalid
    {
      get { return _isLayoutInvalid; }
      set { _isLayoutInvalid = value;}
    }

    /// <summary>
    /// Returns the desired size this element calculated based on the available size.
    /// This value denotes the desired size of this element without <see cref="Margin"/>.
    /// </summary>
    public SizeF DesiredSize
    {
      get { return _desiredSize; }
    }

    public bool IsTemplateControlRoot
    {
      get { return TemplateNameScope != null; }
    }

    public AbstractProperty TemplateNameScopeProperty
    {
      get { return _templateNameScopeProperty; }
    }

    public INameScope TemplateNameScope
    {
      get { return (INameScope) _templateNameScopeProperty.GetValue(); }
      set { _templateNameScopeProperty.SetValue(value); }
    }

    #endregion

    #region Layouting

    /// <summary>
    /// Adds this element's margin to the specified <param name="size"/> parameter.
    /// </summary>
    /// <remarks>
    /// <see cref="float.NaN"/> values will be preserved, i.e. if a <paramref name="size"/> coordinate
    /// is <see cref="float.NaN"/>, it won't be changed.
    /// </remarks>
    /// <param name="size">Size parameter where the margin will be added.</param>
    public void AddMargin(ref SizeF size)
    {
      AddMargin(ref size, Margin);
    }

    /// <summary>
    /// Adds this element's margin to the specified <paramref name="rect"/>.
    /// </summary>
    /// <param name="rect">Inner element's rectangle where the margin will be added.</param>
    public void AddMargin(ref RectangleF rect)
    {
      AddMargin(ref rect, Margin);
    }

    /// <summary>
    /// Removes this element's margin from the specified <param name="size"/> parameter.
    /// </summary>
    /// <remarks>
    /// <see cref="float.NaN"/> values will be preserved, i.e. if a <paramref name="size"/> coordinate
    /// is <see cref="float.NaN"/>, it won't be changed.
    /// </remarks>
    /// <param name="size">Size parameter where the margin will be removed.</param>
    public void RemoveMargin(ref SizeF size)
    {
      RemoveMargin(ref size, Margin);
    }

    /// <summary>
    /// Removes this element's margin from the specified <paramref name="rect"/>.
    /// </summary>
    /// <param name="rect">Outer element's rectangle where the margin will be removed.</param>
    public void RemoveMargin(ref RectangleF rect)
    {
      RemoveMargin(ref rect, Margin);
    }

    public static void AddMargin(ref SizeF size, Thickness margin)
    {
      if (!float.IsNaN(size.Width))
        size.Width += (margin.Left + margin.Right) * SkinContext.Zoom.Width;
      if (!float.IsNaN(size.Height))
        size.Height += (margin.Top + margin.Bottom) * SkinContext.Zoom.Height;
    }

    public static void AddMargin(ref RectangleF rect, Thickness margin)
    {
      rect.X -= margin.Left * SkinContext.Zoom.Width;
      rect.Y -= margin.Top * SkinContext.Zoom.Height;

      rect.Width += (margin.Left + margin.Right) * SkinContext.Zoom.Width;
      rect.Height += (margin.Top + margin.Bottom) * SkinContext.Zoom.Height;
    }

    public static void RemoveMargin(ref SizeF size, Thickness margin)
    {
      if (!float.IsNaN(size.Width))
        size.Width -= (margin.Left + margin.Right) * SkinContext.Zoom.Width;
      if (!float.IsNaN(size.Height))
        size.Height -= (margin.Top + margin.Bottom) * SkinContext.Zoom.Height;
    }

    public static void RemoveMargin(ref RectangleF rect, Thickness margin)
    {
      rect.X += margin.Left * SkinContext.Zoom.Width;
      rect.Y += margin.Top * SkinContext.Zoom.Height;

      rect.Width -= (margin.Left + margin.Right) * SkinContext.Zoom.Width;
      rect.Height -= (margin.Top + margin.Bottom) * SkinContext.Zoom.Height;
    }

    /// <summary>
    /// Will make this element scroll the specified <paramref name="element"/> in a visible
    /// position inside this element's borders. The call should also be delegated to the parent element
    /// with the original element and its updated bounds as parameter; this will make all parents also
    /// scroll their visible range.
    /// </summary>
    /// <remarks>
    /// This method will be overridden by classes which can scroll their content. Such a class
    /// will take two actions here:
    /// <list type="bullet">
    /// <item>Scroll the specified <paramref name="element"/> to a visible region inside its borders,
    /// while undoing layout transformations which will be applied to children.</item>
    /// <item>Call this inherited method, which delegates the call to the visual parent.</item>
    /// </list>
    /// The call to the visual parent should use the same <paramref name="element"/> but an updated
    /// <paramref name="elementBounds"/> rectangle.
    /// </remarks>
    /// <param name="element">The original element which should be made visible.</param>
    /// <param name="elementBounds">The element's bounds after the scrolling update has taken place in the
    /// next layout cycle.</param>
    public virtual void MakeVisible(UIElement element, RectangleF elementBounds)
    {
      UIElement parent = VisualParent as UIElement;
      if (parent != null)
        parent.MakeVisible(element, elementBounds);
    }

    /// <summary>
    /// Returns the information if the specified (absolute) coordinates lay in this element's visible range.
    /// </summary>
    /// <param name="x">Absolute X-coordinate.</param>
    /// <param name="y">Absolute Y-coordinate.</param>
    /// <returns><c>true</c> if the specified coordinates lay in this element's visible range.</returns>
    public virtual bool IsInVisibleArea(float x, float y)
    {
      return false;
    }

    /// <summary>
    /// Returns the information if the specified (absolute) coordinates lay in this element's range.
    /// </summary>
    /// <param name="x">Absolute X-coordinate.</param>
    /// <param name="y">Absolute Y-coordinate.</param>
    /// <returns><c>true</c> if the specified coordinates lay in this element's range.</returns>
    public virtual bool IsInArea(float x, float y)
    {
      return false;
    }

    /// <summary>
    /// Returns the information if the specified (absolute) coordinates lay in the specified child's visible range.
    /// </summary>
    /// <param name="child">The child to check.</param>
    /// <param name="x">Absolute X-coordinate.</param>
    /// <param name="y">Absolute Y-coordinate.</param>
    /// <returns><c>true</c> if the specified coordinates lay in the specified child's visible range.</returns>
    public virtual bool IsChildVisibleAt(UIElement child, float x, float y)
    {
      return child.IsInArea(x, y) && IsInVisibleArea(x, y);
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

    /// <summary>
    /// Measures this element's size and fills the <see cref="DesiredSize"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is the first part of the two-phase measuring process. In this first phase, parent
    /// controls collect all the size requirements of their child controls.
    /// </para>
    /// <para>
    /// An input size value of <see cref="float.NaN"/> denotes that this child control doesn't have a size
    /// constraint in this direction. All other size values need to be considered by this child control as
    /// the maximum available size. If this element still produces a bigger <see cref="DesiredSize"/>, the
    /// <see cref="Arrange(RectangleF,bool)"/> method might give it a smaller final region.
    /// </para>
    /// </remarks>
    /// <param name="totalSize">Total size of the element including Margins. As input, this parameter
    /// contains the size available for this child control (size constraint). As output, it must be set
    /// to the <see cref="DesiredSize"/> plus <see cref="Margin"/>.</param>
    /// <param name="force">If set to <c>true</c>, the measurement will be executed even if the given
    /// <paramref name="totalSize"/> doesn't differ from the total size given in the last call.</param>
    public void Measure(ref SizeF totalSize, bool force)
    {
#if DEBUG_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("Measure {0} Name='{1}', totalSize={2}", GetType().Name, Name, totalSize));
#endif
      if (SameSize(_availableSize, totalSize) && !force)
      { // Optimization: If our input data is the same and the layout isn't invalid, we don't need to measure again
        totalSize = _desiredSize;
#if DEBUG_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("Measure {0} Name='{1}', cutting short, totalSize is like before, returns desired size={2}", GetType().Name, Name, totalSize));
#endif
        return;
      }
      _availableSize = new SizeF(totalSize);
      RemoveMargin(ref totalSize);
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      MeasureOverride(ref totalSize);
      SkinContext.FinalLayoutTransform.TransformSize(ref totalSize);
      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();
      AddMargin(ref totalSize);
      _desiredSize = totalSize;
#if DEBUG_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("Measure {0} Name='{1}', returns calculated desired size={2}", GetType().Name, Name, totalSize));
#endif
    }

    public void Measure(ref SizeF totalSize)
    {
      Measure(ref totalSize, false);
    }

    protected virtual void MeasureOverride(ref SizeF totalSize)
    {
    }

    /// <summary>
    /// Arranges the UI element and positions it in the finalrect.
    /// </summary>
    /// <param name="outerRect">The final position and size the parent computed for this child element.</param>
    /// <param name="force">If set to <c>true</c>, the arrangement is done even if the given <paramref name="outerRect"/>
    /// doesn't differ from the outer rect given in the last call.</param>
    public void Arrange(RectangleF outerRect, bool force)
    {
#if DEBUG_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("Arrange {0} Name='{1}', outerRect={2}", GetType().Name, Name, outerRect));
#endif
      if (SameRect(_outerRect, outerRect) && !force)
      { // Optimization: If our input data is the same and the layout isn't invalid, we don't need to
        // arrange again
#if DEBUG_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("Arrange {0} Name='{1}', cutting short, outerRect={2} is like before", GetType().Name, Name, outerRect));
#endif
        return;
      }
      _outerRect = new RectangleF(outerRect.Location, outerRect.Size);
      RectangleF rect = new RectangleF(outerRect.Location, outerRect.Size);
      RemoveMargin(ref rect);

      // TODO: Check if we need this if statement
      if (!rect.IsEmpty)
        _finalRect = new RectangleF(rect.Location, rect.Size);

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      ArrangeOverride(rect);
      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();

      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      Initialize();
      InitializeTriggers();
    }

    public void Arrange(RectangleF outerRect)
    {
      Arrange(outerRect, false);
    }

    protected virtual void ArrangeOverride(RectangleF finalRect)
    {
      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
    }

    /// <summary>
    /// Invalidates the layout of this UIElement.
    /// If dimensions change, it will invalidate the parent visual so the parent
    /// will re-layout itself and its children.
    /// </summary>
    public virtual void Invalidate()
    {
      _isLayoutInvalid = true;
    }

    /// <summary>
    /// Invalidates the layout of our visual parent.
    /// The parent will re-layout itself and its children.
    /// </summary>
    public void InvalidateParent()
    {
      UIElement parent = VisualParent as UIElement;
      if (parent != null)
        parent.Invalidate();
    }

    /// <summary>
    /// Updates the layout, i.e. calls <see cref="Measure(ref SizeF,bool)"/> and <see cref="Arrange(RectangleF,bool)"/>,
    /// if <see cref="_isLayoutInvalid"/> is set.
    /// </summary>
    public void UpdateLayout()
    {
      if (!_isLayoutInvalid) 
        return;
    
#if DEBUG_LAYOUT
      System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}'", GetType().Name, Name));
#endif
      //Trace.WriteLine("UpdateLayout: " + Name + "  " + GetType());
      _isLayoutInvalid = false;

      UIElement parent = VisualParent as UIElement;
      if (parent == null)
      {
        SizeF screenSize = new SizeF(SkinContext.SkinWidth * SkinContext.Zoom.Width, SkinContext.SkinHeight * SkinContext.Zoom.Height);
        SizeF size = new SizeF(screenSize.Width, screenSize.Height);

#if DEBUG_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', no visual parent so measure with screen size {2}", GetType().Name, Name, size));
#endif
        Measure(ref size, true);

        // Root element - restart counting
        SkinContext.ResetZorder();

#if DEBUG_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', no visual parent so arrange with screen size {2}", GetType().Name, Name, size));
#endif
        // Ignore the measured size - arrange with screen size
        Arrange(new RectangleF(0, 0, screenSize.Width, screenSize.Height), true);
      }
      else
      {
        if (!_availableSize.HasValue || !_outerRect.HasValue)
        {
#if DEBUG_LAYOUT
          System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', no available size or no outer rect, updating layout at parent {2}", GetType().Name, Name, parent));
#endif
          parent.Invalidate();
          parent.UpdateLayout();
          return;
        }

        SizeF availableSize = new SizeF(_availableSize.Value.Width, _availableSize.Value.Height);
        SizeF formerDesiredSize = _desiredSize;

        ExtendedMatrix m = _finalLayoutTransform;
        if (m != null)
          SkinContext.AddLayoutTransform(m);

#if DEBUG_LAYOUT
        System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', measuring with former available size {2}", GetType().Name, Name, availableSize));
#endif
        Measure(ref availableSize, true);
        if (m != null)
          SkinContext.RemoveLayoutTransform();

        if (_desiredSize != formerDesiredSize)
        {
#if DEBUG_LAYOUT
          System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', measuring returned different desired size, updating parent (former: {2}, now: {3})", GetType().Name, Name, formerDesiredSize, _desiredSize));
#endif
          _isLayoutInvalid = true; // At least, we need to do arrangement again
          // Our size has changed - we need to update our parent
          parent.Invalidate();
          parent.UpdateLayout();
          return;
        }
        else
        { // Our size is the same as before - just arrange
          if (m != null)
            SkinContext.AddLayoutTransform(m);

          SkinContext.SetZOrder(ActualPosition.Z);

          RectangleF outerRect = new RectangleF(_outerRect.Value.Location, _outerRect.Value.Size);
#if DEBUG_LAYOUT
          System.Diagnostics.Trace.WriteLine(string.Format("UpdateLayout {0} Name='{1}', measuring returned same desired size, arranging with old outer rect {2}", GetType().Name, Name, outerRect));
#endif
          Arrange(outerRect, true);
          if (m != null)
            SkinContext.RemoveLayoutTransform();
        }
      }
    }

    #endregion

    /// <summary>
    /// Finds the resource with the given resource key.
    /// </summary>
    /// <param name="resourceKey">The resource key.</param>
    /// <returns>Resource with the specified key, or <c>null</c> if not found.</returns>
    public object FindResource(object resourceKey)
    {
      if (Resources.ContainsKey(resourceKey))
        return Resources[resourceKey];
      if (LogicalParent is UIElement)
        return ((UIElement) LogicalParent).FindResource(resourceKey);
      return SkinContext.SkinResources.FindStyleResource(resourceKey);
    }

    public void InitializeTriggers()
    {
      if (!_triggersInitialized)
      {
        _triggersInitialized = true;
        foreach (TriggerBase trigger in Triggers)
          trigger.Setup(this);
      }
    }

    public void StartStoryboard(Storyboard board, HandoffBehavior handoffBehavior)
    {
      if (Screen == null)
        return;
      Screen.Animator.StartStoryboard(board, this, handoffBehavior);
    }

    public void StopStoryboard(Storyboard board)
    {
      if (Screen == null)
        return;
      Screen.Animator.StopStoryboard(board, this);
    }

    public void SetValueInRenderThread(IDataDescriptor dataDescriptor, object value)
    {
      if (Screen != null)
        Screen.Animator.SetValue(dataDescriptor, value);
      else
        dataDescriptor.Value = value;
    }

    public bool TryGetPendingValue(IDataDescriptor dataDescriptor, out object value)
    {
      if (Screen != null)
        return Screen.Animator.TryGetPendingValue(dataDescriptor, out value);
      value = null;
      return false;
    }

    public virtual void FireUIEvent(UIEvent eventType, UIElement source)
    { }

    public virtual void FireEvent(string eventName)
    {
      foreach (TriggerBase trigger in Triggers)
      {
        EventTrigger eventTrig = trigger as EventTrigger;
        if (eventTrig != null)
          if (eventTrig.RoutedEvent == eventName)
            foreach (TriggerAction ta in eventTrig.Actions)
              ta.Execute(this);
      }
      if (eventName == LOADED_EVENT)
      {
        if (_loaded != null)
          _loaded.Execute();
      }
      foreach (UIElement child in GetChildren())
        child.FireEvent(eventName);
      if (EventOccured != null)
        EventOccured(eventName);
    }

    public virtual void OnMouseMove(float x, float y)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnMouseMove(x, y);
      }
    }

    /// <summary>
    /// Will be called when a key is pressed before the registered shortcuts are checked.
    /// Derived classes may override this method to implement special priority key handling code.
    /// </summary>
    /// <param name="key">The key. Should be set to 'Key.None' if handled by child.</param> 
    public virtual void OnKeyPreview(ref Key key)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnKeyPreview(ref key);
        if (key == Key.None) return;
      }
    }

    /// <summary>
    /// Will be called when a key is pressed. Derived classes may override this method
    /// to implement special key handling code.
    /// </summary>
    /// <param name="key">The key. Should be set to 'Key.None' if handled by child.</param> 
    public virtual void OnKeyPressed(ref Key key)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnKeyPressed(ref key);
        if (key == Key.None) return;
      }
    }

    public override INameScope FindNameScope()
    {
      if (this is INameScope)
        return this as INameScope;
      else if (TemplateNameScope != null)
        return TemplateNameScope;
      else
        return LogicalParent == null ? Screen : LogicalParent.FindNameScope();
    }

    /// <summary>
    /// Adds all children in the visual tree to the specified <paramref name="childrenOut"/> collection.
    /// </summary>
    /// <param name="childrenOut">Collection to add children to.</param>
    public virtual void AddChildren(ICollection<UIElement> childrenOut) { }

    /// <summary>
    /// Convenience method for <see cref="AddChildren"/>.
    /// </summary>
    /// <returns>Collection of child elements.</returns>
    public ICollection<UIElement> GetChildren()
    {
      ICollection<UIElement> result = new List<UIElement>();
      AddChildren(result);
      return result;
    }

    /// <summary>
    /// Steps the structure down (in direction to the child elements) along
    /// the visual tree to find an <see cref="UIElement"/> which fulfills the
    /// condition specified by <paramref name="finder"/>. This method has to be
    /// overridden in all descendants exposing visual children, for every child
    /// this method has to be called.
    /// This method does a depth-first search.
    /// </summary>
    /// <param name="finder">Callback interface which decides whether an element
    /// is the element searched for.</param>
    /// <returns><see cref="UIElement"/> for which the specified
    /// <paramref name="finder"/> delegate returned <c>true</c>.</returns>
    public UIElement FindElement(IFinder finder)
    {
      return FindElement_BreadthFirst(finder);
    }

    public UIElement FindElement_DepthFirst(IFinder finder)
    {
      Stack<UIElement> searchStack = new Stack<UIElement>();
      IList<UIElement> elementList = new List<UIElement>();
      searchStack.Push(this);
      while (searchStack.Count > 0)
      {
        UIElement current = searchStack.Pop();
        if (finder.Query(current))
          return current;
        elementList.Clear();
        current.AddChildren(elementList);
        foreach (UIElement child in elementList)
          searchStack.Push(child);
      }
      return null;
    }

    public UIElement FindElement_BreadthFirst(IFinder finder)
    {
      LinkedList<UIElement> searchList = new LinkedList<UIElement>(new UIElement[] {this});
      LinkedListNode<UIElement> current;
      while ((current = searchList.First) != null)
      {
        if (finder.Query(current.Value))
          return current.Value;
        searchList.RemoveFirst();
        current.Value.AddChildren(searchList);
      }
      return null;
    }

    public UIElement FindElementInNamescope(string name)
    {
      INameScope nameScope = FindNameScope();
      if (nameScope != null)
        return nameScope.FindName(name) as UIElement;
      else
        return null;
    }

    public void ForEachElementInTree_BreadthFirst(IUIElementAction action)
    {
      LinkedList<UIElement> searchList = new LinkedList<UIElement>(new UIElement[] { this });
      LinkedListNode<UIElement> current;
      while ((current = searchList.First) != null)
      {
        action.Execute(current.Value);
        searchList.RemoveFirst();
        current.Value.AddChildren(searchList);
      }
    }

    public void ForEachElementInTree_DepthFirst(IUIElementAction action)
    {
      Stack<UIElement> searchStack = new Stack<UIElement>();
      IList<UIElement> elementList = new List<UIElement>();
      searchStack.Push(this);
      while (searchStack.Count > 0)
      {
        UIElement current = searchStack.Pop();
        action.Equals(current);
        elementList.Clear();
        current.AddChildren(elementList);
        foreach (UIElement child in elementList)
          searchStack.Push(child);
      }
    }

    public virtual void Initialize()
    {
      if (_fireLoaded)
      {
        FireEvent(LOADED_EVENT);
        _fireLoaded = false;
      }
    }

    public virtual void Allocate()
    {
      foreach (FrameworkElement child in GetChildren())
        child.Allocate();
    }

    public virtual void Deallocate()
    {
      foreach (FrameworkElement child in GetChildren())
        child.Deallocate();
    }

    public override void SetBindingValue(IDataDescriptor dd, object value)
    {
      SetValueInRenderThread(dd, value);
    }

    public void SetScreen(Screen screen)
    {
      ForEachElementInTree_BreadthFirst(new SetScreenAction(screen));
    }

    public static bool InVisualPath(UIElement check, UIElement child)
    {
      Visual current = child;
      while (current != null)
        if (ReferenceEquals(check, current))
          return true;
        else
          current = current.VisualParent;
      return false;
    }

    public static bool IsNear(double x, double y)
    {
      return Math.Abs(x - y) < DELTA_DOUBLE;
    }

    #region IContentEnabled members

    public bool FindContentProperty(out IDataDescriptor dd)
    {
      return ReflectionHelper.FindMemberDescriptor(this, "Content", out dd);
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      string name = Name;
      return GetType().Name + (string.IsNullOrEmpty(name) ? string.Empty : (", Name: '" + name + "'"));
    }

    #endregion
  }
}
