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

using System;
using System.Collections.Generic;
using System.Drawing;
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
using SlimDX;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.UI.SkinEngine.Controls.Animations;
using MediaPortal.UI.SkinEngine.Controls.Transforms;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;
using Screen = MediaPortal.UI.SkinEngine.ScreenManagement.Screen;

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
      _triggerProperty = new SProperty(typeof(IList<TriggerBase>), new List<TriggerBase>());
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
    public IList<TriggerBase> Triggers
    {
      get { return (IList<TriggerBase>) _triggerProperty.GetValue(); }
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

    #endregion

    #region Resources handling

    public void SetResources(ResourceDictionary resources)
    {
      _resources = resources;
    }

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

    public virtual void FireEvent(string eventName, RoutingStrategyEnum routingStrategy)
    {
      if (routingStrategy == RoutingStrategyEnum.Tunnel)
      {
        // Tunnel strategy: All parents first, then this element
        UIElement parent = VisualParent as UIElement;
        if (parent != null)
          parent.FireEvent(eventName, routingStrategy);
      }
      UIEventDelegate dlgt = EventOccured;
      if (dlgt != null)
        dlgt(eventName);
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

    #region Input handling

    public virtual void OnMouseMove(float x, float y, ICollection<FocusCandidate> focusCandidates)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnMouseMove(x, y, focusCandidates);
      }
    }

    public virtual void OnMouseClick(MouseButtons buttons, ref bool handled)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnMouseClick(buttons, ref handled);
      }
    }

    public virtual void OnMouseWheel(int numDetents)
    {
      foreach (UIElement child in GetChildren())
      {
        if (!child.IsVisible) continue;
        child.OnMouseWheel(numDetents);
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

    public UIElement FindElementInNamescope(string name)
    {
      INameScope nameScope = FindNameScope();
      if (nameScope != null)
        return nameScope.FindName(name) as UIElement;
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
