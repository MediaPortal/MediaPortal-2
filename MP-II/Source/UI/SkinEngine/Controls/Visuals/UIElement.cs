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
using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.ScreenManagement;
using MediaPortal.SkinEngine.Xaml;
using SlimDX;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.SkinEngine.Controls.Animations;
using MediaPortal.SkinEngine.Controls.Transforms;
using MediaPortal.SkinEngine.Commands;
using MediaPortal.SkinEngine.MpfElements.Resources;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.SkinEngine.Controls.Panels;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Visuals
{

  public enum VisibilityEnum
  {
    Visible = 0,
    Hidden = 1,
    Collapsed = 2,
  }

  [Flags]
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
      return Panel.GetZIndex(x).CompareTo(Panel.GetZIndex(y));
    }

    #endregion
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

  public class UIElement : Visual, IContentEnabled
  {
    protected static IList<UIElement> EMPTY_UIELEMENT_LIST = new List<UIElement>();
    protected const string LOADED_EVENT = "UIElement.Loaded";

    #region Protected fields

    protected Property _nameProperty;
    protected Property _acutalPositionProperty;
    protected Property _marginProperty;
    protected Property _triggerProperty;
    protected Property _renderTransformProperty;
    protected Property _renderTransformOriginProperty;
    protected Property _layoutTransformProperty;
    protected Property _visibilityProperty;
    protected Property _isEnabledProperty;
    protected Property _opacityMaskProperty;
    protected Property _opacityProperty;
    protected Property _freezableProperty;
    protected Property _templateNameScopeProperty;
    protected SizeF _desiredSize;
    protected RectangleF _finalRect;
    protected ResourceDictionary _resources;
    protected bool _isLayoutInvalid = true;
    protected ExtendedMatrix _finalLayoutTransform;
    protected IExecutableCommand _loaded;
    protected bool _triggersInitialized;
    protected bool _fireLoaded = true;

    #endregion

    #region Ctor

    public UIElement()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _nameProperty = new Property(typeof(string), "");
      _acutalPositionProperty = new Property(typeof(Vector3), new Vector3(0, 0, 1));
      _marginProperty = new Property(typeof(Thickness), new Thickness(0, 0, 0, 0));
      _resources = new ResourceDictionary();
      _triggerProperty = new Property(typeof(IList<TriggerBase>), new List<TriggerBase>());
      _renderTransformProperty = new Property(typeof(Transform), null);
      _layoutTransformProperty = new Property(typeof(Transform), null);
      _renderTransformOriginProperty = new Property(typeof(Vector2), new Vector2(0, 0));
      _visibilityProperty = new Property(typeof(VisibilityEnum), VisibilityEnum.Visible);
      _isEnabledProperty = new Property(typeof(bool), true);
      _freezableProperty = new Property(typeof(bool), false);
      _opacityProperty = new Property(typeof(double), 1.0);
      _templateNameScopeProperty = new Property(typeof(INameScope), null);

      _opacityMaskProperty = new Property(typeof(Brushes.Brush), null);
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
      // FIXME Albert78: Implement Freezing
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

    #endregion

    void OnOpacityPropertyChanged(Property property, object oldValue)
    {
      FireUIEvent(UIEvent.OpacityChange, this);
    }

    void OnVisibilityPropertyChanged(Property property, object oldValue)
    {
      if (VisualParent is UIElement)
        ((UIElement) VisualParent).Invalidate();
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
    void OnLayoutPropertyChanged(Property property, object oldValue)
    {
      Invalidate();
    }

    void OnLayoutTransformChanged(IObservable observable)
    {
      Invalidate();
    }

    void OnLayoutTransformPropertyChanged(Property property, object oldValue)
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

    public Property OpacityProperty
    {
      get { return _opacityProperty; }
    }

    public double Opacity
    {
      get { return (double) _opacityProperty.GetValue(); }
      set { _opacityProperty.SetValue(value); }
    }

    public Property FreezableProperty
    {
      get { return _freezableProperty; }
    }

    public bool Freezable
    {
      get { return (bool)_freezableProperty.GetValue(); }
      set { _freezableProperty.SetValue(value); }
    }

    public Property OpacityMaskProperty
    {
      get { return _opacityMaskProperty; }
    }

    public Brushes.Brush OpacityMask
    {
      get { return (Brushes.Brush) _opacityMaskProperty.GetValue(); }
      set { _opacityMaskProperty.SetValue(value); }
    }

    public Property IsEnabledProperty
    {
      get { return _isEnabledProperty; }
    }

    public bool IsEnabled
    {
      get { return (bool) _isEnabledProperty.GetValue(); }
      set { _isEnabledProperty.SetValue(value); }
    }

    public Property VisibilityProperty
    {
      get { return _visibilityProperty; }
    }

    public VisibilityEnum Visibility
    {
      get { return (VisibilityEnum) _visibilityProperty.GetValue(); }
      set { _visibilityProperty.SetValue(value); }
    }

    public Property TriggersProperty
    {
      get { return _triggerProperty; }
    }

    public IList<TriggerBase> Triggers
    {
      get { return (IList<TriggerBase>) _triggerProperty.GetValue(); }
    }

    public Property ActualPositionProperty
    {
      get { return _acutalPositionProperty; }
    }

    public Vector3 ActualPosition
    {
      get { return (Vector3) _acutalPositionProperty.GetValue(); }
      set { _acutalPositionProperty.SetValue(value); }
    }

    public Property NameProperty
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

    public bool IsVisible
    {
      get { return Visibility == VisibilityEnum.Visible; }
      set { Visibility = value ? VisibilityEnum.Visible : VisibilityEnum.Hidden; }
    }

    public Property MarginProperty
    {
      get { return _marginProperty; }
    }

    public Thickness Margin
    {
      get { return (Thickness)_marginProperty.GetValue(); }
      set { _marginProperty.SetValue(value); }
    }

    public Property LayoutTransformProperty
    {
      get { return _layoutTransformProperty; }
    }

    public Transform LayoutTransform
    {
      get { return _layoutTransformProperty.GetValue() as Transform; }
      set { _layoutTransformProperty.SetValue(value); }
    }

    public Property RenderTransformProperty
    {
      get { return _renderTransformProperty; }
    }

    public Transform RenderTransform
    {
      get { return (Transform) _renderTransformProperty.GetValue(); }
      set { _renderTransformProperty.SetValue(value); }
    }

    public Property RenderTransformOriginProperty
    {
      get { return _renderTransformOriginProperty; }
    }

    public Vector2 RenderTransformOrigin
    {
      get { return (Vector2) _renderTransformOriginProperty.GetValue(); }
      set { _renderTransformOriginProperty.SetValue(value); }
    }

    public bool IsInvalidLayout
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

    public Property TemplateNameScopeProperty
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
      if (!float.IsNaN(size.Width))
        size.Width += (Margin.Left + Margin.Right) * SkinContext.Zoom.Width;
      if (!float.IsNaN(size.Height))
        size.Height += (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height;
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
      if (!float.IsNaN(size.Width))
        size.Width -= (Margin.Left + Margin.Right) * SkinContext.Zoom.Width;
      if (!float.IsNaN(size.Height))
        size.Height -= (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height;
    }

    /// <summary>
    /// Removes this element's margin from the specified <paramref name="rect"/>.
    /// </summary>
    /// <param name="rect">Outer element's rectangle where the margin will be removed.</param>
    public void RemoveMargin(ref RectangleF rect)
    {
      rect.X += Margin.Left * SkinContext.Zoom.Width;
      rect.Y += Margin.Top * SkinContext.Zoom.Height;

      rect.Width -= (Margin.Left + Margin.Right) * SkinContext.Zoom.Width;
      rect.Height -= (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height;
    }

    /// <summary>
    /// Returns the total desired size, i.e. the <see cref="DesiredSize"/> with the
    /// <see cref="Margin"/>.
    /// </summary>
    /// <returns><see cref="DesiredSize"/> plus <see cref="Margin"/>.</returns>
    public SizeF TotalDesiredSize()
    {
      return new SizeF(_desiredSize.Width + (Margin.Left + Margin.Right) * SkinContext.Zoom.Width,
          _desiredSize.Height + (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height);
    }

    /// <summary>
    /// Will make this element scroll the specified <paramref name="childRect"/> in a visible
    /// position inside this element's borders. If this element cannot scroll, it will delegate
    /// the call to its visual parent.
    /// </summary>
    /// <remarks>
    /// This method will be overridden by classes which can scroll their content. Such a class
    /// will take two actions here:
    /// <list type="bullet">
    /// <item>Scroll the specified <paramref name="childRect"/> to a visible region inside its borders,
    /// while undoing layout transformations which will be applied to children.</item>
    /// <item>Call this inherited method, which delegates the call to the visual parent.</item>
    /// </list>
    /// </remarks>
    public virtual void MakeVisible(RectangleF childRect)
    {
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        m.TransformRect(ref childRect);
      }
      UIElement parent = VisualParent as UIElement;
      if (parent != null)
        parent.MakeVisible(childRect);
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
    /// <see cref="Arrange"/> method might give it a smaller final region.
    /// </para>
    /// </remarks>
    /// <param name="totalSize">Total size of the element including Margins. As input, this parameter
    /// contains the size available for this child control (size constraint). As output, it must be set
    /// to the <see cref="DesiredSize"/> plus <see cref="Margin"/>.</param>
    public virtual void Measure(ref SizeF totalSize)
    {
    }

    /// <summary>
    /// Arranges the UI element and positions it in the finalrect.
    /// </summary>
    /// <param name="finalRect">The final position and size the parent computed for this child element.</param>
    public virtual void Arrange(RectangleF finalRect)
    {
      Initialize();
      InitializeTriggers();
      IsInvalidLayout = false;
    }

    /// <summary>
    /// Invalidates the layout of this UIElement.
    /// If dimensions change, it will invalidate the parent visual so the parent
    /// will re-layout itself and its children
    /// </summary>
    public virtual void Invalidate()
    {
      IsInvalidLayout = true;
    }

    /// <summary>
    /// Updates the layout.
    /// </summary>
    public virtual void UpdateLayout()
    {
      if (!IsInvalidLayout) 
        return;
    
      //Trace.WriteLine("UpdateLayout: " + Name + "  " + GetType());
      IsInvalidLayout = false;

      if (VisualParent is UIElement)
      {
        ((UIElement) VisualParent).Invalidate();
        ((UIElement) VisualParent).UpdateLayout();
      }
      else
      {
        FrameworkElement element = this as FrameworkElement;
        if (element == null)
        {
          SizeF size = new SizeF(SkinContext.SkinWidth * SkinContext.Zoom.Width, SkinContext.SkinHeight * SkinContext.Zoom.Height);
          SizeF childSize = new SizeF(size.Width, size.Height);
          Measure(ref childSize);
          Arrange(new RectangleF(0, 0, size.Width, size.Height));
        }
        else
        {
          SizeF size = new SizeF((float) element.Width * SkinContext.Zoom.Width,
              (float) element.Height * SkinContext.Zoom.Height);

          // Root element - Start counting again
          SkinContext.ResetZorder();

          if (Double.IsNaN(size.Width)) 
            size.Width = SkinContext.SkinWidth * SkinContext.Zoom.Width;
          if (Double.IsNaN(size.Height)) 
            size.Height = SkinContext.SkinHeight * SkinContext.Zoom.Height;
          ExtendedMatrix m = _finalLayoutTransform;
          if (m != null)
            SkinContext.AddLayoutTransform(m);

          SizeF childSize = new SizeF(size.Width, size.Height);
          Measure(ref childSize);
          if (m != null)
            SkinContext.RemoveLayoutTransform();

          if (m != null)
            SkinContext.AddLayoutTransform(m);
          Arrange(new RectangleF(0, 0, size.Width, size.Height));
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
    /// Will be called when a key is pressed. Derived classes may override this method
    /// to implement special key handling code.
    /// </summary>
    /// <param name="key">The key. Will be set to 'Key.None' if handled by child.</param> 
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

    #region IContentEnabled members

    public bool FindContentProperty(out IDataDescriptor dd)
    {
      return ReflectionHelper.FindMemberDescriptor(this, "Content", out dd);
    }

    #endregion

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
    }

    public virtual void Deallocate()
    {
    }

    public void SetScreen(Screen screen)
    {
      ForEachElementInTree_BreadthFirst(new SetScreenAction(screen));
    }
  }
}
