#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using MediaPortal.Core.Properties;
using MediaPortal.Core.InputManager;
using SkinEngine.Controls.Visuals.Triggers;
using SkinEngine.Controls.Animations;

namespace SkinEngine.Controls.Visuals
{
  public class UIElement : Visual
  {
    Property _nameProperty;
    Property _keyProperty;
    Property _isFocusableProperty;
    Property _hasFocusProperty;
    Property _visibleProperty;
    Property _acutalPositionProperty;
    Property _positionProperty;
    Property _dockProperty;
    Property _marginProperty;
    Property _triggerProperty;
    protected Size _desiredSize;
    protected Size _availableSize;
    bool _isArrangeValid;
    ResourceDictionary _resources;
    List<Timeline> _runningAnimations;

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
      IsFocusable = el.IsFocusable;
      HasFocus = el.HasFocus;
      IsVisible = el.IsVisible;
      ActualPosition = el.ActualPosition;
      Position = el.Position;
      Dock = el.Dock;
      Margin = el.Margin;
      _resources = el.Resources;
      foreach (Trigger t in el.Triggers)
      {
        Triggers.Add((Trigger)t.Clone());
      }
    }
    void Init()
    {
      _runningAnimations = new List<Timeline>();
      _nameProperty = new Property("");
      _keyProperty = new Property("");
      _isFocusableProperty = new Property(false);
      _hasFocusProperty = new Property(false);
      _visibleProperty = new Property((bool)true);
      _acutalPositionProperty = new Property(new Vector3(0, 0, 1));
      _positionProperty = new Property(new Vector3(0, 0, 1));
      _dockProperty = new Property(Dock.Top);
      _marginProperty = new Property(new Vector4(0, 0, 0, 0));
      _resources = new ResourceDictionary();
      _triggerProperty = new Property(new TriggerCollection());

      _visibleProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _positionProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _dockProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _marginProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
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
    /// Gets or sets the actual position property.
    /// </summary>
    /// <value>The actual position property.</value>
    public Property ActualPositionProperty
    {
      get
      {
        return _acutalPositionProperty;
      }
      set
      {
        _acutalPositionProperty = value;
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
        return (Vector3)_acutalPositionProperty.GetValue();
      }
      set
      {
        _acutalPositionProperty.SetValue(value);
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
    public bool HasFocus
    {
      get
      {
        return (bool)_hasFocusProperty.GetValue();
      }
      set
      {
        _hasFocusProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the is focusable property.
    /// </summary>
    /// <value>The is focusable property.</value>
    public Property IsFocusableProperty
    {
      get
      {
        return _isFocusableProperty;
      }
      set
      {
        _isFocusableProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the is focusable.
    /// </summary>
    /// <value>The is focusable.</value>
    public bool IsFocusable
    {
      get
      {
        return (bool)_isFocusableProperty.GetValue();
      }
      set
      {
        _isFocusableProperty.SetValue(value);
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
    /// Gets or sets the is visible property.
    /// </summary>
    /// <value>The is visible property.</value>
    public Property IsVisibleProperty
    {
      get
      {
        return _visibleProperty;
      }
      set
      {
        _visibleProperty = value;
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
        return (bool)_visibleProperty.GetValue();
      }
      set
      {
        _visibleProperty.SetValue(value);
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
    public Size DesiredSize
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
    public virtual void Measure(Size availableSize)
    {
      _availableSize = availableSize;
    }

    /// <summary>
    /// Arranges the UI element 
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public virtual void Arrange(Rectangle finalRect)
    {
      IsArrangeValid = true;
    }

    /// <summary>
    /// Invalidates the layout of this uielement.
    /// If dimensions change, it will invalidate the parent visual so 
    /// the parent will re-layout itself and its children
    /// </summary>
    public virtual void Invalidate()
    {
      if (!IsArrangeValid) return;
      if (_availableSize.Width > 0 && _availableSize.Height > 0)
      {
        System.Drawing.Size sizeOld = _desiredSize;
        System.Drawing.Size availsizeOld = _availableSize;
        Measure(_availableSize);
        _availableSize = availsizeOld;
        if (_desiredSize == sizeOld)
        {
          Arrange(new Rectangle((int)ActualPosition.X, (int)ActualPosition.Y, _desiredSize.Width, _desiredSize.Height));
          return;
        }
      }
      if (VisualParent != null)
      {
        VisualParent.Invalidate();
      }
      else
      {
        FrameworkElement element = this as FrameworkElement;
        if (element == null)
        {
          Measure(new Size((int)SkinContext.Width, (int)SkinContext.Height));
          Arrange(new Rectangle(0, 0, (int)SkinContext.Width, (int)SkinContext.Height));
        }
        else
        {
          int w = (int)element.Width;
          int h = (int)element.Height;
          if (w == 0) w = (int)SkinContext.Width;
          if (h == 0) h = (int)SkinContext.Height;
          Measure(new Size(w, h));
          Arrange(new Rectangle((int)element.Position.X, (int)element.Position.Y, w, h));
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

    /// <summary>
    /// Fires an event.
    /// </summary>
    /// <param name="eventName">Name of the event.</param>
    public virtual void FireEvent(string eventName)
    {
      foreach (EventTrigger trigger in Triggers)
      {
        if (trigger.RoutedEvent == eventName)
        {
          if (trigger.Storyboard != null)
          {
            lock (_runningAnimations)
            {
              if (!_runningAnimations.Contains(trigger.Storyboard))
              {
                _runningAnimations.Add(trigger.Storyboard);
                trigger.Storyboard.Start(SkinContext.TimePassed);
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Animates any timelines for this uielement.
    /// </summary>
    public virtual void Animate()
    {
      if (_runningAnimations.Count == 0) return;
      List<Timeline> stoppedAnimations = new List<Timeline>();
      lock (_runningAnimations)
      {
        foreach (Timeline line in _runningAnimations)
        {
          line.Animate(SkinContext.TimePassed);
          if (line.IsStopped)
            stoppedAnimations.Add(line);
        }
        foreach (Timeline line in stoppedAnimations)
        {
          line.Stop();
          _runningAnimations.Remove(line);
        }
      }
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
  }
}
