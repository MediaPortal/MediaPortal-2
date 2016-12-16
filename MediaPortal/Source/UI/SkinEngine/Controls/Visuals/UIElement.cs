#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Effects;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities;
using SharpDX;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.UI.SkinEngine.Controls.Animations;
using MediaPortal.UI.SkinEngine.Controls.Transforms;
using MediaPortal.UI.SkinEngine.MpfElements.Input;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;
using Screen = MediaPortal.UI.SkinEngine.ScreenManagement.Screen;
using KeyEventArgs = MediaPortal.UI.SkinEngine.MpfElements.Input.KeyEventArgs;
using KeyEventHandler = MediaPortal.UI.SkinEngine.MpfElements.Input.KeyEventHandler;
using MouseEventArgs = MediaPortal.UI.SkinEngine.MpfElements.Input.MouseEventArgs;
using MouseEventHandler = MediaPortal.UI.SkinEngine.MpfElements.Input.MouseEventHandler;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  #region Additional enums, delegates and classes

  public enum VisibilityEnum
  {
    Visible = 0,
    Hidden = 1,
    Collapsed = 2,
  }

  public enum RoutingStrategyEnum
  {
    /// <summary>
    /// Event handlers on the event source are invoked. The routed event then routes to successive parent elements until reaching the element tree root.
    /// </summary>
    Bubble,

    /// <summary>
    /// Only the source element itself is given the opportunity to invoke handlers in response.
    /// </summary>
    Direct,

    /// <summary>
    /// Initially, event handlers at the element tree root are invoked. The routed event then travels a route through successive child elements
    /// along the route, towards the node element that is the routed event source (the element that raised the routed event).
    /// </summary>
    Tunnel,

    /// <summary>
    /// Event handlers of the complete visual tree starting at the element itself are invoked.
    /// </summary>
    VisualTree
  }

  /// <summary>
  /// Delegate interface which decides if an UI element fulfills a special condition.
  /// </summary>
  public interface IMatcher
  {
    /// <summary>
    /// Query method which decides if the specified <paramref name="current"/>
    /// element fulfills the condition exposed by this class.
    /// </summary>
    /// <returns><c>true</c> if the specified <paramref name="current"/> element
    /// fulfills the condition exposed by this class, else <c>false</c>.</returns>
    bool Match(UIElement current);
  }

  /// <summary>
  /// Finder implementation which returns an element if multiple child finders accept it.
  /// </summary>
  public class MultiMatcher : IMatcher
  {
    protected IMatcher[] _matchers;

    public MultiMatcher(IMatcher[] matchers)
    {
      _matchers = matchers;
    }

    public bool Match(UIElement current)
    {
      return _matchers.All(matcher => matcher.Match(current));
    }
  }

  /// <summary>
  /// Matcher implementation which returns an element if it is visible.
  /// </summary>
  public class VisibleElementMatcher : IMatcher
  {
    private static VisibleElementMatcher _instance = null;

    public bool Match(UIElement current)
    {
      return current.IsVisible;
    }

    public static VisibleElementMatcher Instance
    {
      get { return _instance ?? (_instance = new VisibleElementMatcher()); }
    }
  }

  /// <summary>
  /// Matcher implementation which looks for elements of the specified type.
  /// </summary>
  public class TypeMatcher : IMatcher
  {
    protected Type _type;

    public TypeMatcher(Type type)
    {
      _type = type;
    }

    public bool Match(UIElement current)
    {
      return _type == current.GetType();
    }
  }

  /// <summary>
  /// Matcher implementation which looks for elements of a the given type or
  /// of a type derived from the given type.
  /// </summary>
  public class SubTypeMatcher : IMatcher
  {
    protected Type _type;

    public SubTypeMatcher(Type type)
    {
      _type = type;
    }

    public bool Match(UIElement current)
    {
      return _type.IsAssignableFrom(current.GetType());
    }
  }

  /// <summary>
  /// Matcher implementation which looks for elements of a specified name.
  /// </summary>
  public class NameMatcher : IMatcher
  {
    protected string _name;

    public NameMatcher(string name)
    {
      _name = name;
    }

    public bool Match(UIElement current)
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

  public struct FocusCandidate
  {
    public FrameworkElement Candidate;
    public float ZIndex;

    public FocusCandidate(FrameworkElement candidate, float zIndex)
    {
      Candidate = candidate;
      ZIndex = zIndex;
    }
  }

  public delegate void UIEventDelegate(string eventName);

  #endregion

  public abstract class UIElement : Visual, IContentEnabled, IBindingContainer
  {
    #region Constants

    protected const string LOADED_EVENT = "UIElement.Loaded";
    protected const string VISIBILITY_CHANGED_EVENT = "UIElement.VisibilityChanged";

    public const double DELTA_DOUBLE = 0.01;

    #endregion

    #region Protected fields

    protected AbstractProperty _nameProperty;
    protected AbstractProperty _actualPositionProperty;
    protected AbstractProperty _marginProperty;
    protected AbstractProperty _triggerProperty;
    protected AbstractProperty _renderTransformProperty;
    protected AbstractProperty _renderTransformOriginProperty;
    protected AbstractProperty _effectProperty;
    protected AbstractProperty _layoutTransformProperty;
    protected AbstractProperty _visibilityProperty;
    protected AbstractProperty _isEnabledProperty;
    protected AbstractProperty _opacityMaskProperty;
    protected AbstractProperty _opacityProperty;
    protected AbstractProperty _freezableProperty;
    protected AbstractProperty _templateNameScopeProperty;
    protected ResourceDictionary _resources;
    protected ElementState _elementState = ElementState.Available;
    protected bool _triggersInitialized = false;
    protected bool _fireLoaded = true;
    protected bool _allocated = false;
    protected readonly object _renderLock = new object(); // Can be used to synchronize several accesses between render thread and other threads

    #endregion

    #region Ctor & maintainance

    //MP2-522 this static constructor ensures that all static fields (notably RoutedEvent registrations) are initialized before an instance of this class is created
    static UIElement()
    {
      RegisterEvents(typeof(UIElement));
    }

    protected UIElement()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _nameProperty = new SProperty(typeof(string), string.Empty);
      _actualPositionProperty = new SProperty(typeof(PointF), new PointF(0, 0));
      _marginProperty = new SProperty(typeof(Thickness), new Thickness(0, 0, 0, 0));
      _resources = new ResourceDictionary();
      _triggerProperty = new SProperty(typeof(TriggerCollection), new TriggerCollection());
      _renderTransformProperty = new SProperty(typeof(Transform), null);
      _layoutTransformProperty = new SProperty(typeof(Transform), null);
      _renderTransformOriginProperty = new SProperty(typeof(Vector2), new Vector2(0, 0));
      _effectProperty = new SProperty(typeof(Effect), null);
      _visibilityProperty = new SProperty(typeof(VisibilityEnum), VisibilityEnum.Visible);
      _isEnabledProperty = new SProperty(typeof(bool), true);
      _freezableProperty = new SProperty(typeof(bool), false);
      _opacityProperty = new SProperty(typeof(double), 1.0);
      _templateNameScopeProperty = new SProperty(typeof(INameScope), null);

      _opacityMaskProperty = new SProperty(typeof(Brushes.Brush), null);
    }

    void Attach()
    {
      _visibilityProperty.Attach(OnVisibilityChanged);
    }

    void Detach()
    {
      _visibilityProperty.Detach(OnVisibilityChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      UIElement el = (UIElement) source;
      // We do not copy the focus flag, only one element can have focus
      //HasFocus = el.HasFocus;
      string copyName = el.Name;
      ActualPosition = el.ActualPosition;
      Margin = new Thickness(el.Margin);
      Visibility = el.Visibility;
      IsEnabled = el.IsEnabled;
      // TODO Albert78: Implement Freezing
      Freezable = el.Freezable;
      Opacity = el.Opacity;
      OpacityMask = copyManager.GetCopy(el.OpacityMask);
      LayoutTransform = copyManager.GetCopy(el.LayoutTransform);
      RenderTransform = copyManager.GetCopy(el.RenderTransform);
      RenderTransformOrigin = copyManager.GetCopy(el.RenderTransformOrigin);
      Effect = copyManager.GetCopy(el.Effect);
      TemplateNameScope = copyManager.GetCopy(el.TemplateNameScope);
      _resources = copyManager.GetCopy(el._resources);

      foreach (TriggerBase t in el.Triggers)
        Triggers.Add(copyManager.GetCopy(t));
      _triggersInitialized = false;

      // copy routed events
      CopyRoutedEvents(source as UIElement, copyManager);

      copyManager.CopyCompleted += (cm =>
        {
          // When copying, the namescopes of our parent objects might not have been initialized yet. This can be the case
          // when the TemplateNamescope property or a LogicalParent property wasn't copied yet, for example.
          // That's why we cannot simply copy the Name property in the DeepCopy method.
          Name = copyName;
          copyName = null;
        });
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      foreach (UIElement child in GetChildren())
        child.StopAndDispose();
      foreach (TriggerBase triggerBase in Triggers)
        triggerBase.Dispose();
     
      // clear the routed event handler dictionary to be sure to not keep any other object alive
      // if the handler infos contains disposable command stencil, they'll get disposed too
      foreach (var eventHandlerInfos in _eventHandlerDictionary.Values)
      {
        foreach (var eventHandlerInfo in eventHandlerInfos)
        {
          var disposable = eventHandlerInfo.CommandStencilHandler as IDisposable;
          if (disposable != null) disposable.Dispose();
        }
      }
      _eventHandlerDictionary.Clear();

      MPF.TryCleanupAndDispose(RenderTransform);
      MPF.TryCleanupAndDispose(LayoutTransform);
      MPF.TryCleanupAndDispose(Effect);
      MPF.TryCleanupAndDispose(TemplateNameScope);
      MPF.TryCleanupAndDispose(OpacityMask);
      MPF.TryCleanupAndDispose(_resources);
    }

    public void CleanupAndDispose()
    {
      SetElementState(ElementState.Disposing);
      Deallocate();
      ResetScreen();
      StopAndDispose();
    }

    protected internal void StopAndDispose()
    {
      Screen screen = Screen;
      if (screen != null)
        screen.Animator.StopAll(this);

      // uninitialize triggers, so they detach from the event source, ...
      UninitializeTriggers();

      Dispose(); // First dispose bindings before we can reset our VisualParent
      VisualParent = null;
    }

    #endregion

    #region Event handlers

    void OnVisibilityChanged(AbstractProperty prop, object oldVal)
    {
      FireEvent(VISIBILITY_CHANGED_EVENT, RoutingStrategyEnum.VisualTree);
    }

    #endregion

    #region Public properties & events

    /// <summary>
    /// Event handler called for all events defined by their event string like <see cref="LOADED_EVENT"/> or <see cref="VISIBILITY_CHANGED_EVENT"/>.
    /// </summary>
    public event UIEventDelegate EventOccured;

    public ResourceDictionary Resources
    {
      get { return _resources; }
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
      get { return (bool) _freezableProperty.GetValue(); }
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

    public bool IsAllocated
    {
      get { return _allocated; }
    }

    public AbstractProperty TriggersProperty
    {
      get { return _triggerProperty; }
    }

    /// <summary>
    /// Gets or sets the list of triggers of this UI element.
    /// </summary>
    /// <remarks>
    /// Before triggers are modified, <see cref="UninitializeTriggers"/> must be called to make the old triggers be reset and the new triggers
    /// be initialized correctly.
    /// </remarks>
    public TriggerCollection Triggers
    {
      get { return (TriggerCollection)_triggerProperty.GetValue(); }
    }

    public AbstractProperty ActualPositionProperty
    {
      get { return _actualPositionProperty; }
    }

    public PointF ActualPosition
    {
      get { return (PointF) _actualPositionProperty.GetValue(); }
      set { _actualPositionProperty.SetValue(value); }
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
            ns.RegisterName(Name, this);
          }
          catch (ArgumentException)
          {
            ServiceRegistration.Get<ILogger>().Warn("Name '" + Name + "' was registered twice in namescope '" + ns + "'");
          }
      }
    }

    public AbstractProperty MarginProperty
    {
      get { return _marginProperty; }
    }

    public Thickness Margin
    {
      get { return (Thickness) _marginProperty.GetValue(); }
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
    
    public AbstractProperty EffectProperty
    {
      get { return _effectProperty; }
    }

    public Effect Effect
    {
      get { return (Effect) _effectProperty.GetValue(); }
      set { _effectProperty.SetValue(value); }
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

    /// <summary>
    /// The element state reflects the state flow from prepared over running to disposing.
    /// </summary>
    /// <remarks>
    /// The element state is used to determine the threading model for UI elements. In the <see cref="Visuals.ElementState.Available"/>
    /// state, the UI element is not yet rendered and thus it is safe to change values of the rendering system directly.
    /// In state <see cref="Visuals.ElementState.Running"/>, the UI element is being rendered and thus all values affecting the
    /// rendering system must be set via the render thread (see <see cref="UIElement.SetValueInRenderThread"/>).
    /// In state <see cref="Visuals.ElementState.Disposing"/>, the element is about to be disposed, thus no more change triggers and
    /// other time-consuming tasks need to be executed.
    /// </remarks>
    public ElementState ElementState
    {
      get { return _elementState; }
      internal set
      {
        if (_elementState == value)
          return;
        _elementState = value;
        OnUpdateElementState();
      }
    }

    protected virtual void OnUpdateElementState()
    {
      if (PreparingOrRunning)
        ActivateBindings();
      if (_elementState == ElementState.Disposing)
        DisposeBindings();
    }

    internal bool PreparingOrRunning
    {
      get { return _elementState == ElementState.Preparing || _elementState == ElementState.Running; }
    }

    #endregion

    #region Layouting

    /// <summary>
    /// Adds the given <paramref name="margin"/> to the specified <param name="size"/> parameter.
    /// </summary>
    /// <remarks>
    /// <see cref="float.NaN"/> values will be preserved, i.e. if a <paramref name="size"/> coordinate
    /// is <see cref="float.NaN"/>, it won't be changed.
    /// </remarks>
    /// <param name="size">Size parameter where the margin will be added.</param>
    /// <param name="margin">Margin to be added.</param>
    public static void AddMargin(ref SizeF size, Thickness margin)
    {
      if (!float.IsNaN(size.Width))
        size.Width += margin.Left + margin.Right;
      if (!float.IsNaN(size.Height))
        size.Height += margin.Top + margin.Bottom;
    }

    /// <summary>
    /// Adds the given <paramref name="margin"/> to the specified <paramref name="rect"/>.
    /// </summary>
    /// <param name="rect">Inner element's rectangle where the margin will be added.</param>
    /// <param name="margin">Margin to be added.</param>
    public static void AddMargin(ref RectangleF rect, Thickness margin)
    {
      rect.X -= margin.Left;
      rect.Y -= margin.Top;

      rect.Width += margin.Left + margin.Right;
      rect.Height += margin.Top + margin.Bottom;
    }

    /// <summary>
    /// Removes the given <paramref name="margin"/> from the specified <param name="size"/> parameter.
    /// </summary>
    /// <remarks>
    /// <see cref="float.NaN"/> values will be preserved, i.e. if a <paramref name="size"/> coordinate
    /// is <see cref="float.NaN"/>, it won't be changed.
    /// </remarks>
    /// <param name="size">Size parameter where the margin will be removed.</param>
    /// <param name="margin">Margin to be removed.</param>
    public static void RemoveMargin(ref SizeF size, Thickness margin)
    {
      if (!float.IsNaN(size.Width))
        size.Width -= margin.Left + margin.Right;
      if (!float.IsNaN(size.Height))
        size.Height -= margin.Top + margin.Bottom;
    }

    /// <summary>
    /// Removes the given <paramref name="margin"/> from the specified <paramref name="rect"/>.
    /// </summary>
    /// <param name="rect">Outer element's rectangle where the margin will be removed.</param>
    /// <param name="margin">Margin to be removed.</param>
    public static void RemoveMargin(ref RectangleF rect, Thickness margin)
    {
      rect.X += margin.Left;
      rect.Y += margin.Top;

      rect.Width -= margin.Left + margin.Right;
      rect.Height -= margin.Top + margin.Bottom;
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
    /// while undoing layout transformations which will be applied to children. At the same time,
    /// the <paramref name="elementBounds"/> should be updated to the new bounds after scrolling.</item>
    /// <item>Call this inherited method (in <see cref="UIElement"/>), which delegates the call to
    /// the visual parent.</item>
    /// </list>
    /// The call to the visual parent should use the same <paramref name="element"/> but an updated
    /// <paramref name="elementBounds"/> rectangle. The <paramref name="elementBounds"/> parameter
    /// should reflect the updated element's bounds after the scrolling update has taken place in the
    /// next layout cycle.
    /// </remarks>
    /// <param name="element">The original element which should be made visible.</param>
    /// <param name="elementBounds">The element's bounds which will be active after the scrolling
    /// update.</param>
    public virtual void BringIntoView(UIElement element, RectangleF elementBounds)
    {
      UIElement parent = VisualParent as UIElement;
      if (parent != null)
        parent.BringIntoView(element, elementBounds);
    }

    /// <summary>
    /// Returns the information if the specified (absolute) coordinates are located in this element's range and
    /// this control is visible at the given coords.
    /// </summary>
    /// <param name="x">Absolute X-coordinate.</param>
    /// <param name="y">Absolute Y-coordinate.</param>
    /// <returns><c>true</c> if the specified coordinates lay in this element's visible range.</returns>
    public virtual bool IsInVisibleArea(float x, float y)
    {
      UIElement parent = VisualParent as UIElement;
      return IsInArea(x, y) && (parent == null || parent.IsChildRenderedAt(this, x, y));
    }

    /// <summary>
    /// Returns the information if the specified (absolute) coordinates are located in this element's range.
    /// </summary>
    /// <param name="x">Absolute X-coordinate.</param>
    /// <param name="y">Absolute Y-coordinate.</param>
    /// <returns><c>true</c> if the specified coordinates lay in this element's range.</returns>
    public abstract bool IsInArea(float x, float y);

    /// <summary>
    /// Returns the information if the given child is rendered at the given position.
    /// Panels will check their children in the given render order.
    /// Controls, which clip their render output at their boundaries or which hide individual children
    /// will check if the given child is shown.
    /// Other controls, wich don't clip their children's render output, will simply return the return value
    /// of their parent's <see cref="IsChildRenderedAt"/> method with <c>this</c> as argument.
    /// </summary>
    /// <param name="child">Child to check.</param>
    /// <param name="x">Absolute X coordinate to check.</param>
    /// <param name="y">Absolute Y coordinate to check.</param>
    /// <returns><c>true</c> if the given child of this element is rendered.</returns>
    public virtual bool IsChildRenderedAt(UIElement child, float x, float y)
    {
      UIElement parent = VisualParent as UIElement;
      return parent == null ? true : parent.IsChildRenderedAt(this, x, y);
    }

    /// <summary>
    /// Checks if this element and all visual parents are visible and thus this element might be rendered.
    /// </summary>
    /// <returns><c>true</c>, if the element is visible, else <c>false</c>.</returns>
    public bool CheckVisibility()
    {
      if (!IsVisible)
        return false;
      UIElement visualParent = VisualParent as UIElement;
      if (visualParent == null)
        // Root element
        return true;
      return visualParent.CheckVisibility();
    }

    public static bool InVisualPath(UIElement possibleParent, UIElement child)
    {
      Visual current = child;
      while (current != null)
        if (ReferenceEquals(possibleParent, current))
          return true;
        else
          current = current.VisualParent;
      return false;
    }

    public static bool IsNear(double x, double y)
    {
      return Math.Abs(x - y) < DELTA_DOUBLE;
    }

    public static bool GreaterThanOrClose(double x, double y)
    {
      return x > y || IsNear(x, y);
    }

    public static bool LessThanOrClose(double x, double y)
    {
      return x < y || IsNear(x, y);
    }

    /// <summary>
    /// Transforms a screen point to local element space. The <see cref="UIElement.ActualPosition"/> is also taken into account.
    /// </summary>
    /// <param name="point">Screen point</param>
    /// <returns>Returns the transformed point in element coordinates.</returns>
    public virtual PointF TransformScreenPoint(PointF point)
    {
      // overridden in FrameworkElement to apply transformation
      var actualPosition = ActualPosition;
      return new PointF(point.X - actualPosition.X, point.Y - actualPosition.Y);
    }

    #endregion

    #region Resources handling

    public void SetResources(ResourceDictionary resources)
    {
      _resources = resources;
    }

    bool _searchingResource = false;

    /// <summary>
    /// Finds the resource with the given resource key.
    /// </summary>
    /// <param name="resourceKey">The resource key.</param>
    /// <returns>Resource with the specified key, or <c>null</c> if not found.</returns>
    public object FindResource(object resourceKey)
    {
      if (_searchingResource)
        // Avoid recursive calls
        return null;
      _searchingResource = true;
      try
      {
        object result;
        if (Resources.TryGetValue(resourceKey, out result))
          return result;
        UIElement logicalParent = LogicalParent as UIElement;
        return logicalParent != null ? logicalParent.FindResource(resourceKey) : SkinContext.SkinResources.FindStyleResource(resourceKey);
      }
      finally
      {
        _searchingResource = false;
      }
    }

    #endregion

    #region UI element state handling

    public void SetScreen(Screen screen)
    {
      if (screen != null)
        ForEachElementInTree_BreadthFirst(new SetScreenAction(screen));
    }

    public void ResetScreen()
    {
      ForEachElementInTree_BreadthFirst(new SetScreenAction(null));
    }

    /// <summary>
    /// Sets the element state. The <see cref="Screen"/> must have been assigned before the element state is set to
    /// <see cref="Visuals.ElementState.Running"/>.
    /// </summary>
    /// <param name="state"></param>
    public void SetElementState(ElementState state)
    {
      ForEachElementInTree_BreadthFirst(new SetElementStateAction(state));
    }

    #endregion

    #region Bindings handling

    void IBindingContainer.AddBindings(IEnumerable<IBinding> bindings)
    {
      foreach (IBinding binding in bindings)
        AddDeferredBinding(binding);
      if (PreparingOrRunning)
        ActivateBindings();
    }

    public override void SetBindingValue(IDataDescriptor dd, object value)
    {
      SetValueInRenderThread(dd, value);
    }

    #endregion

    #region Storyboards

    public void StartStoryboard(Storyboard board, HandoffBehavior handoffBehavior)
    {
      Screen screen = Screen;
      if (screen == null)
        return;
      screen.Animator.StartStoryboard(board, this, handoffBehavior);
    }

    public void StopStoryboard(Storyboard board)
    {
      Screen screen = Screen;
      if (screen == null)
        return;
      screen.Animator.StopStoryboard(board, this);
    }

    #endregion

    #region Thread synchronization

    public void SetValueInRenderThread(IDataDescriptor dataDescriptor, object value)
    {
      if (_elementState == ElementState.Disposing)
        return;
      Screen screen = Screen;
      if (screen == null || _elementState == ElementState.Available || _elementState == ElementState.Preparing ||
          Thread.CurrentThread == SkinContext.RenderThread)
        dataDescriptor.Value = value;
      else
        screen.Animator.SetValue(dataDescriptor, value);
    }

    /// <summary>
    /// Gets either the value of the given <paramref name="dataDescriptor"/> or, if there is a value to be set in the
    /// render thread, that pending value.
    /// </summary>
    /// <param name="dataDescriptor">Data descriptor whose value should be returned.</param>
    /// <param name="value">Pending value or current value.</param>
    /// <returns><c>true</c>, if the returned value is pending to be set, else <c>false</c>.</returns>
    public bool GetPendingOrCurrentValue(IDataDescriptor dataDescriptor, out object value)
    {
      Screen screen = Screen;
      Animator animator = screen == null ? null : screen.Animator;
      try
      {
        if (animator != null)
        {
          Monitor.Enter(animator.SyncObject);
          if (animator.TryGetPendingValue(dataDescriptor, out value))
            return true;
        }
        value = dataDescriptor.Value;
      }
      finally
      {
        if (animator != null)
          Monitor.Exit(animator.SyncObject);
      }
      return false;
    }

    /// <summary>
    /// Convenience method for calling <see cref="GetPendingOrCurrentValue(IDataDescriptor,out object)"/> if it is not
    /// interesting whether the value was still pending or whether the current value was returned.
    /// </summary>
    /// <param name="dataDescriptor">Data descriptor whose value should be returned.</param>
    /// <returns>Pending value or current value.</returns>
    public object GetPendingOrCurrentValue(IDataDescriptor dataDescriptor)
    {
      object value;
      GetPendingOrCurrentValue(dataDescriptor, out value);
      return value;
    }

    #endregion

    #region Events & triggers

    public void FireEvent(string eventName, RoutingStrategyEnum routingStrategy)
    {
      if (routingStrategy == RoutingStrategyEnum.Tunnel)
      {
        // Tunnel strategy: All parents first, then this element
        UIElement parent = VisualParent as UIElement;
        if (parent != null)
          parent.FireEvent(eventName, routingStrategy);
      }
      DoFireEvent(eventName);
      switch (routingStrategy)
      {
        case RoutingStrategyEnum.Bubble:
          // Bubble strategy: First this element, then all parents
          UIElement parent = VisualParent as UIElement;
          if (parent != null)
            parent.FireEvent(eventName, routingStrategy);
          break;
        case RoutingStrategyEnum.VisualTree:
          // VisualTree strategy: First this element, then all children
          foreach (UIElement child in GetChildren())
            child.FireEvent(eventName, routingStrategy);
          break;
      }
    }

    protected virtual void DoFireEvent(string eventName)
    {
      UIEventDelegate dlgt = EventOccured;
      if (dlgt != null)
        dlgt(eventName);
    }

    public void CheckFireLoaded()
    {
      if (!_fireLoaded)
        return;
      FireEvent(LOADED_EVENT, RoutingStrategyEnum.VisualTree);
      _fireLoaded = false;
    }

    public void UninitializeTriggers()
    {
      if (!_triggersInitialized)
        return;
      foreach (TriggerBase trigger in Triggers)
        trigger.Reset();
      _triggersInitialized = false;
    }

    public void InitializeTriggers()
    {
      if (_triggersInitialized)
        return;
      foreach (TriggerBase trigger in Triggers)
        trigger.Setup(this);
      _triggersInitialized = true;
    }

    #endregion

    #region routed events

    internal static void RegisterEvents(Type type)
    {
      EventManager.RegisterClassHandler(type, PreviewMouseDownEvent, new MouseButtonEventHandler(OnPreviewMouseDownThunk), true);
      EventManager.RegisterClassHandler(type, MouseDownEvent, new MouseButtonEventHandler(OnMouseDownThunk), true);
      EventManager.RegisterClassHandler(type, PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnPreviewMouseLeftButtonDownThunk), false);
      EventManager.RegisterClassHandler(type, MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnMouseLeftButtonDownThunk), false);
      EventManager.RegisterClassHandler(type, PreviewMouseRightButtonDownEvent, new MouseButtonEventHandler(OnPreviewMouseRightButtonDownThunk), false);
      EventManager.RegisterClassHandler(type, MouseRightButtonDownEvent, new MouseButtonEventHandler(OnMouseRightButtonDownThunk), false);

      EventManager.RegisterClassHandler(type, PreviewMouseUpEvent, new MouseButtonEventHandler(OnPreviewMouseUpThunk), true);
      EventManager.RegisterClassHandler(type, MouseUpEvent, new MouseButtonEventHandler(OnMouseUpThunk), true);
      EventManager.RegisterClassHandler(type, PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnPreviewMouseLeftButtonUpThunk), false);
      EventManager.RegisterClassHandler(type, MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnMouseLeftButtonUpThunk), false);
      EventManager.RegisterClassHandler(type, PreviewMouseRightButtonUpEvent, new MouseButtonEventHandler(OnPreviewMouseRightButtonUpThunk), false);
      EventManager.RegisterClassHandler(type, MouseRightButtonUpEvent, new MouseButtonEventHandler(OnMouseRightButtonUpThunk), false);

      EventManager.RegisterClassHandler(type, PreviewMouseWheelEvent, new MouseWheelEventHandler(OnPreviewMouseWheelThunk), false);
      EventManager.RegisterClassHandler(type, MouseWheelEvent, new MouseWheelEventHandler(OnMouseWheelThunk), false);

      EventManager.RegisterClassHandler(type, PreviewMouseClickEvent, new MouseButtonEventHandler(OnPreviewMouseClickThunk), false);
      EventManager.RegisterClassHandler(type, MouseClickEvent, new MouseButtonEventHandler(OnMouseClickThunk), false);

      EventManager.RegisterClassHandler(type, PreviewMouseMoveEvent, new MouseEventHandler(OnPreviewMouseMoveThunk), false);
      EventManager.RegisterClassHandler(type, MouseMoveEvent, new MouseEventHandler(OnMouseMoveThunk), false);

      EventManager.RegisterClassHandler(type, PreviewKeyPressEvent, new KeyEventHandler(OnPreviewKeyPressThunk), false);
      EventManager.RegisterClassHandler(type, KeyPressEvent, new KeyEventHandler(OnKeyPressThunk), false);
    }


    private static void OnPreviewMouseDownThunk(object sender, MouseButtonEventArgs e)
    {
      if (!e.Handled)
      {
        var uiElement = sender as UIElement;
        if (uiElement != null)
        {
          uiElement.OnPreviewMouseDown(e);
        }
      }
      switch (e.ChangedButton)
      {
        case MouseButton.Left:
          ReRaiseEventAs(sender as UIElement, e, PreviewMouseLeftButtonDownEvent);
          break;
        case MouseButton.Right:
          ReRaiseEventAs(sender as UIElement, e, PreviewMouseRightButtonDownEvent);
          break;
      }
    }

    /// <summary>
    /// Invoked when unhandled PreviewMouseDown event reaches this element. This method is called before the MouseDown event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnPreviewMouseDown(MouseButtonEventArgs e)
    { }

    public static readonly RoutedEvent PreviewMouseDownEvent = EventManager.RegisterRoutedEvent(
      "PreviewMouseDown", RoutingStrategy.Tunnel, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler PreviewMouseDown
    {
      add { AddHandler(PreviewMouseDownEvent, value); }
      remove { RemoveHandler(PreviewMouseDownEvent, value); }
    }


    private static void OnMouseDownThunk(object sender, MouseButtonEventArgs e)
    {
      if (!e.Handled)
      {
        var uiElement = sender as UIElement;
        if (uiElement != null)
        {
          uiElement.OnMouseDown(e);
        }
      }
      switch (e.ChangedButton)
      {
        case MouseButton.Left:
          ReRaiseEventAs(sender as UIElement, e, MouseLeftButtonDownEvent);
          break;
        case MouseButton.Right:
          ReRaiseEventAs(sender as UIElement, e, MouseRightButtonDownEvent);
          break;
      }
    }

    /// <summary>
    /// Invoked when unhandled MouseDown event reaches this element. This method is called before the MouseDown event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnMouseDown(MouseButtonEventArgs e)
    { }

    public static readonly RoutedEvent MouseDownEvent = EventManager.RegisterRoutedEvent(
      "MouseDown", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler MouseDown
    {
      add { AddHandler(MouseDownEvent, value); }
      remove { RemoveHandler(MouseDownEvent, value); }
    }


    private static void OnPreviewMouseLeftButtonDownThunk(object sender, MouseButtonEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnPreviewMouseLeftButtonDown(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled PreviewMouseLeftButtonDown event reaches this element. This method is called before the MouseLeftButtonDown event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    { }

    // since PreviewMouseLeftButtonDownEvent is raised by the PreviewMouseDownEvent, which is already tunneled, we use RoutingStrategy.Direct here!
    public static readonly RoutedEvent PreviewMouseLeftButtonDownEvent = EventManager.RegisterRoutedEvent(
      "PreviewMouseLeftButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler PreviewMouseLeftButtonDown
    {
      add { AddHandler(PreviewMouseLeftButtonDownEvent, value); }
      remove { RemoveHandler(PreviewMouseLeftButtonDownEvent, value); }
    }


    private static void OnMouseLeftButtonDownThunk(object sender, MouseButtonEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnMouseLeftButtonDown(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled MouseLeftButtonDown event reaches this element. This method is called before the MouseLeftButtonDown event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    { }

    // since MouseLeftButtonDownEvent is raised by the MouseDownEvent, which is already tunneled, we use RoutingStrategy.Direct here!
    public static readonly RoutedEvent MouseLeftButtonDownEvent = EventManager.RegisterRoutedEvent(
      "MouseLeftButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler MouseLeftButtonDown
    {
      add { AddHandler(MouseLeftButtonDownEvent, value); }
      remove { RemoveHandler(MouseLeftButtonDownEvent, value); }
    }
    

    private static void OnPreviewMouseRightButtonDownThunk(object sender, MouseButtonEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnPreviewMouseRightButtonDown(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled PreviewMouseRightButtonDown event reaches this element. This method is called before the MouseRightButtonDown event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
    { }

    // since PreviewMouseRightButtonDownEvent is raised by the PreviewMouseDownEvent, which is already tunneled, we use RoutingStrategy.Direct here!
    public static readonly RoutedEvent PreviewMouseRightButtonDownEvent = EventManager.RegisterRoutedEvent(
      "PreviewMouseRightButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler PreviewMouseRightButtonDown
    {
      add { AddHandler(PreviewMouseRightButtonDownEvent, value); }
      remove { RemoveHandler(PreviewMouseRightButtonDownEvent, value); }
    }


    private static void OnMouseRightButtonDownThunk(object sender, MouseButtonEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnMouseRightButtonDown(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled MouseRightButtonDown event reaches this element. This method is called before the MouseRightButtonDown event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnMouseRightButtonDown(MouseButtonEventArgs e)
    { }

    // since MouseRightButtonDownEvent is raised by the MouseDownEvent, which is already tunneled, we use RoutingStrategy.Direct here!
    public static readonly RoutedEvent MouseRightButtonDownEvent = EventManager.RegisterRoutedEvent(
      "MouseRightButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler MouseRightButtonDown
    {
      add { AddHandler(MouseRightButtonDownEvent, value); }
      remove { RemoveHandler(MouseRightButtonDownEvent, value); }
    }
    

    private static void OnPreviewMouseUpThunk(object sender, MouseButtonEventArgs e)
    {
      if (!e.Handled)
      {
        var uiElement = sender as UIElement;
        if (uiElement != null)
        {
          uiElement.OnPreviewMouseDown(e);
        }
      }
      switch (e.ChangedButton)
      {
        case MouseButton.Left:
          ReRaiseEventAs(sender as UIElement, e, PreviewMouseLeftButtonUpEvent);
          break;
        case MouseButton.Right:
          ReRaiseEventAs(sender as UIElement, e, PreviewMouseRightButtonUpEvent);
          break;
      }
    }

    /// <summary>
    /// Invoked when unhandled PreviewMouseUp event reaches this element. This method is called before the MouseUp event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnPreviewMouseUp(MouseButtonEventArgs e)
    { }

    public static readonly RoutedEvent PreviewMouseUpEvent = EventManager.RegisterRoutedEvent(
      "PreviewMouseUp", RoutingStrategy.Tunnel, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler PreviewMouseUp
    {
      add { AddHandler(PreviewMouseUpEvent, value); }
      remove { RemoveHandler(PreviewMouseUpEvent, value); }
    }


    private static void OnMouseUpThunk(object sender, MouseButtonEventArgs e)
    {
      if (!e.Handled)
      {
        var uiElement = sender as UIElement;
        if (uiElement != null)
        {
          uiElement.OnMouseUp(e);
        }
      }
      switch (e.ChangedButton)
      {
        case MouseButton.Left:
          ReRaiseEventAs(sender as UIElement, e, MouseLeftButtonUpEvent);
          break;
        case MouseButton.Right:
          ReRaiseEventAs(sender as UIElement, e, MouseRightButtonUpEvent);
          break;
      }
    }

    /// <summary>
    /// Invoked when unhandled MouseUp event reaches this element. This method is called before the MouseUp event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnMouseUp(MouseButtonEventArgs e)
    { }

    public static readonly RoutedEvent MouseUpEvent = EventManager.RegisterRoutedEvent(
      "MouseUp", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler MouseUp
    {
      add { AddHandler(MouseUpEvent, value); }
      remove { RemoveHandler(MouseUpEvent, value); }
    }


    private static void OnPreviewMouseLeftButtonUpThunk(object sender, MouseButtonEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnPreviewMouseLeftButtonUp(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled PreviewMouseLeftButtonUp event reaches this element. This method is called before the MouseLeftButtonUp event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    { }

    // since PreviewMouseLeftButtonUpEvent is raised by the PreviewMouseUpEvent, which is already tunneled, we use RoutingStrategy.Direct here!
    public static readonly RoutedEvent PreviewMouseLeftButtonUpEvent = EventManager.RegisterRoutedEvent(
      "PreviewMouseLeftButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler PreviewMouseLeftButtonUp
    {
      add { AddHandler(PreviewMouseLeftButtonUpEvent, value); }
      remove { RemoveHandler(PreviewMouseLeftButtonUpEvent, value); }
    }


    private static void OnMouseLeftButtonUpThunk(object sender, MouseButtonEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnMouseLeftButtonUp(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled MouseLeftButtonUp event reaches this element. This method is called before the MouseLeftButtonUp event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    { }

    // since MouseLeftButtonUpEvent is raised by the MouseUpEvent, which is already tunneled, we use RoutingStrategy.Direct here!
    public static readonly RoutedEvent MouseLeftButtonUpEvent = EventManager.RegisterRoutedEvent(
      "MouseLeftButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler MouseLeftButtonUp
    {
      add { AddHandler(MouseLeftButtonUpEvent, value); }
      remove { RemoveHandler(MouseLeftButtonUpEvent, value); }
    }


    private static void OnPreviewMouseRightButtonUpThunk(object sender, MouseButtonEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnPreviewMouseRightButtonUp(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled PreviewMouseRightButtonUp event reaches this element. This method is called before the MouseRightButtonUp event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
    { }

    // since PreviewMouseRightButtonUpEvent is raised by the PreviewMouseUpEvent, which is already tunneled, we use RoutingStrategy.Direct here!
    public static readonly RoutedEvent PreviewMouseRightButtonUpEvent = EventManager.RegisterRoutedEvent(
      "PreviewMouseRightButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler PreviewMouseRightButtonUp
    {
      add { AddHandler(PreviewMouseRightButtonUpEvent, value); }
      remove { RemoveHandler(PreviewMouseRightButtonUpEvent, value); }
    }


    private static void OnMouseRightButtonUpThunk(object sender, MouseButtonEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnMouseRightButtonUp(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled MouseRightButtonUp event reaches this element. This method is called before the MouseRightButtonUp event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnMouseRightButtonUp(MouseButtonEventArgs e)
    { }

    // since MouseRightButtonUpEvent is raised by the MouseUpEvent, which is already tunneled, we use RoutingStrategy.Direct here!
    public static readonly RoutedEvent MouseRightButtonUpEvent = EventManager.RegisterRoutedEvent(
      "MouseRightButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler MouseRightButtonUp
    {
      add { AddHandler(MouseRightButtonUpEvent, value); }
      remove { RemoveHandler(MouseRightButtonUpEvent, value); }
    }


    private static void OnPreviewMouseWheelThunk(object sender, MouseWheelEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnPreviewMouseWheel(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled PreviewMouseWheel event reaches this element. This method is called before the PreviewMouseWheel event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnPreviewMouseWheel(MouseWheelEventArgs e)
    { }

    public static readonly RoutedEvent PreviewMouseWheelEvent = EventManager.RegisterRoutedEvent(
      "PreviewMouseWheel", RoutingStrategy.Tunnel, typeof(MouseWheelEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseWheelEventHandler PreviewMouseWheel
    {
      add { AddHandler(PreviewMouseWheelEvent, value); }
      remove { RemoveHandler(PreviewMouseWheelEvent, value); }
    }


    private static void OnMouseWheelThunk(object sender, MouseWheelEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnMouseWheel(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled MouseWheel event reaches this element. This method is called before the MouseWheel event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnMouseWheel(MouseWheelEventArgs e)
    { }

    public static readonly RoutedEvent MouseWheelEvent = EventManager.RegisterRoutedEvent(
      "MouseWheel", RoutingStrategy.Bubble, typeof(MouseWheelEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseWheelEventHandler MouseWheel
    {
      add { AddHandler(MouseWheelEvent, value); }
      remove { RemoveHandler(MouseWheelEvent, value); }
    }


    private static void OnPreviewMouseClickThunk(object sender, MouseButtonEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnPreviewMouseClick(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled PreviewMouseClick event reaches this element. This method is called before the PreviewMouseLeftClick event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnPreviewMouseClick(MouseButtonEventArgs e)
    { }

    public static readonly RoutedEvent PreviewMouseClickEvent = EventManager.RegisterRoutedEvent(
      "PreviewMouseClick", RoutingStrategy.Tunnel, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler PreviewMouseClick
    {
      add { AddHandler(PreviewMouseClickEvent, value); }
      remove { RemoveHandler(PreviewMouseClickEvent, value); }
    }


    private static void OnMouseClickThunk(object sender, MouseButtonEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnMouseClick(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled MouseClick event reaches this element. This method is called before the MouseClick event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnMouseClick(MouseButtonEventArgs e)
    { }

    public static readonly RoutedEvent MouseClickEvent = EventManager.RegisterRoutedEvent(
      "MouseClick", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseButtonEventHandler MouseClick
    {
      add { AddHandler(MouseClickEvent, value); }
      remove { RemoveHandler(MouseClickEvent, value); }
    }


    private static void OnPreviewMouseMoveThunk(object sender, MouseEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnPreviewMouseMove(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled PreviewMouseMove event reaches this element. This method is called before the PreviewMouseMove event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnPreviewMouseMove(MouseEventArgs e)
    { }

    public static readonly RoutedEvent PreviewMouseMoveEvent = EventManager.RegisterRoutedEvent(
      "PreviewMouseMove", RoutingStrategy.Tunnel, typeof(MouseEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseEventHandler PreviewMouseMove
    {
      add { AddHandler(PreviewMouseMoveEvent, value); }
      remove { RemoveHandler(PreviewMouseMoveEvent, value); }
    }


    private static void OnMouseMoveThunk(object sender, MouseEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnMouseMove(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled MouseMove event reaches this element. This method is called before the MouseMove event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnMouseMove(MouseEventArgs e)
    { }

    public static readonly RoutedEvent MouseMoveEvent = EventManager.RegisterRoutedEvent(
      "MouseMove", RoutingStrategy.Bubble, typeof(MouseEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event MouseEventHandler MouseMove
    {
      add { AddHandler(MouseMoveEvent, value); }
      remove { RemoveHandler(MouseMoveEvent, value); }
    }


    private static void OnPreviewKeyPressThunk(object sender, KeyEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnPreviewKeyPress(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled PreviewKeyPress event reaches this element. This method is called before the PreviewKeyPress event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnPreviewKeyPress(KeyEventArgs e)
    { }

    public static readonly RoutedEvent PreviewKeyPressEvent = EventManager.RegisterRoutedEvent(
      "PreviewKeyPress", RoutingStrategy.Tunnel, typeof(KeyEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event KeyEventHandler PreviewKeyPress
    {
      add { AddHandler(PreviewKeyPressEvent, value); }
      remove { RemoveHandler(PreviewKeyPressEvent, value); }
    }


    private static void OnKeyPressThunk(object sender, KeyEventArgs e)
    {
      var uiElement = sender as UIElement;
      if (uiElement != null)
      {
        uiElement.OnKeyPress(e);
      }
    }

    /// <summary>
    /// Invoked when unhandled KeyPress event reaches this element. This method is called before the KeyPress event is fired.
    /// </summary>
    /// <param name="e">The event arguments for the event.</param>
    /// <remarks>This base implementation is empty.</remarks>
    protected virtual void OnKeyPress(KeyEventArgs e)
    { }

    public static readonly RoutedEvent KeyPressEvent = EventManager.RegisterRoutedEvent(
      "KeyPress", RoutingStrategy.Bubble, typeof(KeyPressEventHandler), typeof(UIElement));

    // Provide CLR accessors for the event 
    public event KeyPressEventHandler KeyPress
    {
      add { AddHandler(KeyPressEvent, value); }
      remove { RemoveHandler(KeyPressEvent, value); }
    }

    #endregion

    #region routed event handling

    private readonly Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>> _eventHandlerDictionary = new Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>>();

    /// <summary>
    /// Adds an <see cref="RoutedEvent"/> handler to this element.
    /// </summary>
    /// <param name="routedEvent">Routed event identifier.</param>
    /// <param name="handler">Handler for the event.</param>
    public void AddHandler(RoutedEvent routedEvent, Delegate handler)
    {
      AddHandler(routedEvent, handler, false);
    }

    /// <summary>
    /// Adds an <see cref="RoutedEvent"/> handler to this element, using a command stencil as handler.
    /// </summary>
    /// <param name="routedEvent">Routed event identifier.</param>
    /// <param name="handler">Handler for the event.</param>
    public void AddHandler(RoutedEvent routedEvent, ICommandStencil handler)
    {
      AddHandler(routedEvent, handler, false);
    }

    /// <summary>
    /// Adds an <see cref="RoutedEvent"/> handler to this element.
    /// </summary>
    /// <param name="routedEvent">Routed event identifier.</param>
    /// <param name="handler">Handler for the event.</param>
    /// <param name="handledEventsToo"><c>true</c> if the handler should be invoked for events that has been marked as handled; <c>false</c> for the default behavior.</param>
    public void AddHandler(RoutedEvent routedEvent, Delegate handler, bool handledEventsToo)
    {
      List<RoutedEventHandlerInfo> handlerList;
      if (!_eventHandlerDictionary.TryGetValue(routedEvent, out handlerList))
      {
        handlerList = new List<RoutedEventHandlerInfo>(1);
        _eventHandlerDictionary.Add(routedEvent, handlerList);
      }
      var handlerInfo = new RoutedEventHandlerInfo(handler, handledEventsToo);
      handlerList.Add(handlerInfo);
    }

    /// <summary>
    /// Adds an <see cref="RoutedEvent"/> handler to this element, using a command stencil as handler.
    /// </summary>
    /// <param name="routedEvent">Routed event identifier.</param>
    /// <param name="handler">Handler for the event.</param>
    /// <param name="handledEventsToo"><c>true</c> if the handler should be invoked for events that has been marked as handled; <c>false</c> for the default behavior.</param>
    public void AddHandler(RoutedEvent routedEvent, ICommandStencil handler, bool handledEventsToo)
    {
      List<RoutedEventHandlerInfo> handlerList;
      if (!_eventHandlerDictionary.TryGetValue(routedEvent, out handlerList))
      {
        handlerList = new List<RoutedEventHandlerInfo>(1);
        _eventHandlerDictionary.Add(routedEvent, handlerList);
      }
      var handlerInfo = new RoutedEventHandlerInfo(handler, handledEventsToo);
      handlerList.Add(handlerInfo);
    }

    /// <summary>
    /// Removes an <see cref="RoutedEvent"/> handler from this element.
    /// </summary>
    /// <param name="routedEvent">Routed event identifier.</param>
    /// <param name="handler">Handler of the event.</param>
    public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
    {
      List<RoutedEventHandlerInfo> handlerList;
      if (_eventHandlerDictionary.TryGetValue(routedEvent, out handlerList))
      {
        for (var n = 0; n < handlerList.Count; ++n)
        {
          if (handlerList[n].Handler == handler)
          {
            handlerList.RemoveAt(n);
            break;
          }
        }
      }
    }

    /// <summary>
    /// Removes an <see cref="RoutedEvent"/> handler from this element, using a command stencil as handler.
    /// </summary>
    /// <param name="routedEvent">Routed event identifier.</param>
    /// <param name="handler">Handler of the event.</param>
    public void RemoveHandler(RoutedEvent routedEvent, ICommandStencil handler)
    {
      List<RoutedEventHandlerInfo> handlerList;
      if (_eventHandlerDictionary.TryGetValue(routedEvent, out handlerList))
      {
        for (var n = 0; n < handlerList.Count; ++n)
        {
          if (handlerList[n].CommandStencilHandler == handler)
          {
            handlerList.RemoveAt(n);
            break;
          }
        }
      }
    }

    public void RaiseEvent(RoutedEventArgs args)
    {
      if (args == null) throw new ArgumentNullException("args");
      RaiseEventImpl(this, args);
    }

    protected static void ReRaiseEventAs(UIElement sender, RoutedEventArgs args, RoutedEvent newEvent)
    {
      if(sender == null)
        return;
      if (args == null) throw new ArgumentNullException("args");
      if (newEvent == null) throw new ArgumentNullException("newEvent");

      var oldEvent = args.RoutedEvent;
      try
      {
        args.RoutedEvent = newEvent;
        RaiseEventImpl(sender, args);
      }
      finally
      {
        args.RoutedEvent = oldEvent;
      }
    }

    private static void RaiseEventImpl(UIElement sender, RoutedEventArgs args)
    {
      args.Source = sender;
      Visual visual;
      switch (args.RoutedEvent.RoutingStrategy)
      {
        case RoutingStrategy.Direct:
          InvokeEventHandlers(sender, args);
          break;

        case RoutingStrategy.Bubble:
          visual = sender;
          while (visual != null)
          {
            var uiElement = visual as UIElement;
            if (uiElement != null) 
              InvokeEventHandlers(uiElement, args);
            visual = visual.VisualParent;
          }
          break;

        case RoutingStrategy.Tunnel:
          var stack = new List<UIElement>();
          visual = sender;
          while (visual != null)
          {
            var uiElement = visual as UIElement;
            if (uiElement != null)
              stack.Add(uiElement);
            visual = visual.VisualParent;
          }
          for (int n = stack.Count - 1; n >= 0; --n)
          {
            InvokeEventHandlers(stack[n], args);
          }
          break;
      }
    }

    private static void InvokeEventHandlers(UIElement source, RoutedEventArgs args)
    {
      args.Source = source;
      foreach (var handler in GlobalEventManager.GetTypedClassEventHandlers(source.GetType(), args.RoutedEvent))
      {
        handler.InvokeHandler(source, args);
      }
      List<RoutedEventHandlerInfo> handlers;
      if (source._eventHandlerDictionary.TryGetValue(args.RoutedEvent, out handlers))
      {
        foreach (var handler in handlers)
        {
          handler.InvokeHandler(source, args);
        }
      }
    }

    private void CopyRoutedEvents(UIElement source, ICopyManager copyManager)
    {
      foreach (var handlerTouple in source._eventHandlerDictionary)
      {
        foreach (var handlerInfo in handlerTouple.Value)
        {
          if (handlerInfo.CommandStencilHandler != null)
          {
            AddHandler(handlerTouple.Key, 
              copyManager.GetCopy(handlerInfo.CommandStencilHandler), 
              handlerInfo.HandledEventsToo);
          }
          else
          {
            AddHandler(handlerTouple.Key, handlerInfo.Handler, handlerInfo.HandledEventsToo);
          }
        }
      }
    }

    #endregion

    #region hit testing

    // WPF signature: public IInputElement InputHitTest(Point point)
    /// <summary>
    /// Returns the hit element.
    /// </summary>
    /// <param name="point">Point to check.</param>
    /// <returns><see cref="UIElement"/> that was hit or <c>null</c>.</returns>
    public virtual UIElement InputHitTest(PointF point)
    {
      if (!IsVisible)
        return null;

      if (IsInArea(point.X, point.Y))
      {
        foreach (var uiElement in GetChildren().Reverse())
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

    #region Input handling

    /// <summary>
    /// Internal mouse move handler for focus and IsMouseOver handling
    /// </summary>
    /// <param name="x">Mouse x position</param>
    /// <param name="y">Mouse y position</param>
    /// <param name="focusCandidates">List with focus candidates. Add <c>this</c> if it is a focus candidate.</param>
    /// <remarks>
    /// For normal mouse move handling use On(Preview)MouseMove(object, MouseEventArgs) or (Preview)MouseMove events
    /// </remarks>
    internal virtual void OnMouseMove(float x, float y, ICollection<FocusCandidate> focusCandidates)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnMouseMove(x, y, focusCandidates);
      }
    }

    /// <summary>
    /// Internal mouse move handler for backward compatibility
    /// </summary>
    /// <param name="buttons">Mouse button</param>
    /// <param name="handled"><c>true</c> if handled; else <c>false</c>; Set to <c>true</c> if mouse click was handled.</param>
    /// <remarks>
    /// For normal mouse click handling use On(Preview)MouseClick(object, MouseEventArgs) or (Preview)MouseClick events
    /// </remarks>
    internal virtual void OnMouseClick(MouseButtons buttons, ref bool handled)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnMouseClick(buttons, ref handled);
      }
    }

    public virtual void OnTouchDown(TouchDownEvent touchEventArgs)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnTouchDown(touchEventArgs);
      }
    }

    public virtual void OnTouchUp(TouchUpEvent touchEventArgs)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnTouchUp(touchEventArgs);
      }
    }

    public virtual void OnTouchMove(TouchMoveEvent touchEventArgs)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnTouchMove(touchEventArgs);
      }
    }

    public virtual void OnTouchEnter(TouchEvent touchEventArgs)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnTouchEnter(touchEventArgs);
      }
    }

    public virtual void OnTouchLeave(TouchEvent touchEventArgs)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnTouchLeave(touchEventArgs);
      }
    }

    /// <summary>
    /// Will be called when a key is pressed before the registered shortcuts are checked.
    /// Derived classes may override this method to implement special priority key handling code.
    /// </summary>
    /// <param name="key">The key. Should be set to 'Key.None' if handled by child.</param> 
    /// <remarks>For internal use. By default use <see cref="OnPreviewKeyPress(KeyEventArgs)"/>,</remarks>
    internal virtual void OnKeyPreview(ref Key key)
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
    /// <remarks>For internal use. By default use <see cref="OnKeyPress(KeyEventArgs)"/>,</remarks>
    internal virtual void OnKeyPressed(ref Key key)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnKeyPressed(ref key);
        if (key == Key.None) return;
      }
    }

    /// <summary>
    /// Capture mouse events for this element
    /// </summary>
    /// <returns>Returns <c>true</c> if the mouse was captured successfully.</returns>
    /// <remarks>
    /// If you want to capture the mouse for the whole subtree use <see cref="Screen.CaptureMouse(UIElement, CaptureMode)"/> 
    /// with capture mode set to <see cref="CaptureMode.SubTree"/>.
    /// </remarks>
    public bool CaptureMouse()
    {
      return Screen.CaptureMouse(this);
    }

    /// <summary>
    /// Releases the mouse capture, if it currently belongs to this element.
    /// </summary>
    /// <remarks>
    /// If the mouse capture does not belong to this element, nothing happens. 
    /// If you want to release the capture for any element se <see cref="Screen.CaptureMouse(UIElement, CaptureMode)"/> 
    /// with element set to <c>null</c> and/or capture mode set to <see cref="CaptureMode.None"/>.
    /// </remarks>
    public void ReleaseMouseCapture()
    {
      if (ReferenceEquals(Screen.MouseCaptured, this))
      {
        Screen.CaptureMouse(null);
      }
    }

    #endregion

    #region Children management

    public override INameScope FindNameScope()
    {
      if (this is INameScope)
        return this as INameScope;
      if (TemplateNameScope != null)
        return TemplateNameScope;
      return LogicalParent == null ? Screen : LogicalParent.FindNameScope();
    }

    /// <summary>
    /// Adds all direct children in the visual tree to the specified <paramref name="childrenOut"/> collection.
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
    /// condition specified by <paramref name="matcher"/>. This method has to be
    /// overridden in all descendants exposing visual children, for every child
    /// this method has to be called.
    /// This method does a depth-first search.
    /// </summary>
    /// <param name="matcher">Callback interface which decides whether an element
    /// is the element searched for.</param>
    /// <returns><see cref="UIElement"/> for which the specified
    /// <paramref name="matcher"/> delegate returned <c>true</c>.</returns>
    public UIElement FindElement(IMatcher matcher)
    {
      return FindElement_BreadthFirst(matcher);
    }

    public ICollection<UIElement> FindElements(IMatcher matcher)
    {
      ICollection<UIElement> result = new List<UIElement>();
      if (matcher.Match(this))
        result.Add(this);
      ICollection<UIElement> children = GetChildren();
      foreach (UIElement child in children)
        CollectionUtils.AddAll(result, child.FindElements(matcher));
      return result;
    }

    public UIElement FindElement_DepthFirst(IMatcher matcher)
    {
      Stack<UIElement> searchStack = new Stack<UIElement>();
      IList<UIElement> elementList = new List<UIElement>();
      searchStack.Push(this);
      while (searchStack.Count > 0)
      {
        UIElement current = searchStack.Pop();
        if (matcher.Match(current))
          return current;
        elementList.Clear();
        current.AddChildren(elementList);
        foreach (UIElement child in elementList)
          searchStack.Push(child);
      }
      return null;
    }

    public UIElement FindElement_BreadthFirst(IMatcher matcher)
    {
      LinkedList<UIElement> searchList = new LinkedList<UIElement>(new UIElement[] { this });
      LinkedListNode<UIElement> current;
      while ((current = searchList.First) != null)
      {
        if (matcher.Match(current.Value))
          return current.Value;
        searchList.RemoveFirst();
        current.Value.AddChildren(searchList);
      }
      return null;
    }

    public DependencyObject FindElementInNamescope(string name)
    {
      INameScope nameScope = FindNameScope();
      if (nameScope != null)
        return nameScope.FindName(name) as DependencyObject;
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

    #endregion

    #region UI state handling

    /// <summary>
    /// Saves the state of this <see cref="UIElement"/> and all its child elements in the given <paramref name="state"/> memento.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This function is used to store a visual state of parts of the screen, for example the scroll position of containers and
    /// the current focus. Descendants which have sensible data to store and restore can override this method.
    /// </para>
    /// <para>
    /// Note that between the calls to <see cref="SaveUIState"/> and <see cref="RestoreUIState"/>, the screen might have been rebuilt and/or
    /// changed its UI elements so elements must be able to restore their state from a memento which potentially doesn't fit any more to 100%.
    /// So the keys in the <paramref name="state"/> dictionary should be choosen in a way that they still can be used in changed elements,
    /// i.e. try to prevent simple int indices as keys, for example.
    /// </para>
    /// </remarks>
    /// <param name="state">State memento which can be used to save the UI state.</param>
    /// <param name="prefix">Key prefix to be used in the given <paramref name="state"/> memento dictionary.
    /// To call <see cref="SaveUIState"/> for child elements, a <c>"/"</c> plus a descriptive string for the child element should be added,
    /// like <c>"/children"</c>.</param>
    public virtual void SaveUIState(IDictionary<string, object> state, string prefix)
    {
      SaveChildrenState(state, prefix);
    }

    /// <summary>
    /// Restores the state of this <see cref="UIElement"/> and all its child elements from the given <paramref name="state"/> memento.
    /// </summary>
    /// <remarks>
    /// <see cref="SaveUIState"/>.
    /// <para>
    /// Note that this memento might have been stored by the screen in a different state with different controls present so the memento
    /// might not fit any more to the element's state. Descendants must try to restore as much of their state as they can and behave fail-safe.
    /// </para>
    /// </remarks>
    /// <param name="state">State memento which was filled by <see cref="SaveUIState"/>.</param>
    /// <param name="prefix">Key prefix which was used in the given <paramref name="state"/> memento dictionary.</param>
    public virtual void RestoreUIState(IDictionary<string, object> state, string prefix)
    {
      RestoreChildrenState(state, prefix);
    }

    /// <summary>
    /// Saves the UI state of all children in the given <paramref name="state"/> memento under the given <paramref name="prefix"/>
    /// (see <see cref="SaveUIState"/>).
    /// </summary>
    /// <remarks>
    /// This method is a generic implementation which simply saves the states of all children returned by <see cref="GetChildren"/>.
    /// Sub classes can override this behaviour by a better implementation.
    /// </remarks>
    /// <param name="state">State memento which can be used to save the children's UI state.</param>
    /// <param name="prefix">Key prefix to be used in the given <paramref name="state"/> memento dictionary.
    /// To call <see cref="SaveUIState"/> for child elements, a <c>"/"</c> plus a descriptive string for the child element should be added,
    /// like <c>"/children"</c>.</param>
    protected virtual void SaveChildrenState(IDictionary<string, object> state, string prefix)
    {
      int i = 0;
      foreach (UIElement child in GetChildren())
        child.SaveUIState(state, prefix + "/Child_" + (i++));
    }

    /// <summary>
    /// Counterpart to <see cref="SaveChildrenState"/>.
    /// </summary>
    /// <param name="state">State memento which was filled by <see cref="SaveChildrenState"/>.</param>
    /// <param name="prefix">Key prefix which was used in the given <paramref name="state"/> memento dictionary.</param>
    public virtual void RestoreChildrenState(IDictionary<string, object> state, string prefix)
    {
      int i = 0;
      foreach (UIElement child in GetChildren())
        child.RestoreUIState(state, prefix + "/Child_" + (i++));
    }

    #endregion

    #region IContentEnabled members

    public bool FindContentProperty(out IDataDescriptor dd)
    {
      return ReflectionHelper.FindMemberDescriptor(this, "Content", out dd);
    }

    #endregion

    #region Base overrides

    public virtual void Allocate()
    {
      _allocated = true;
      foreach (FrameworkElement child in GetChildren())
        child.Allocate();
    }

    public virtual void Deallocate()
    {
      _allocated = false;
      foreach (FrameworkElement child in GetChildren())
        child.Deallocate();
    }

    public override string ToString()
    {
      string name = Name;
      return GetType().Name + (string.IsNullOrEmpty(name) ? string.Empty : (", Name: '" + name + "'")) + ", ElementState: " + _elementState;
    }

    #endregion
  }
}
