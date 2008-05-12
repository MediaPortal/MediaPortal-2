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

using MediaPortal.Presentation.Properties;

namespace Presentation.SkinEngine.Controls.Visuals
{
  using Controls;
  using MarkupExtensions;

  public enum AlignmentX { Left, Center, Right };
  public enum AlignmentY { Top, Center, Bottom };
  public enum Orientation { Vertical, Horizontal };
  public enum Dock { Left, Right, Top, Bottom, Center };

  public class Visual : DependencyObject
  {
    Property _visualParentProperty;
    Property _focusedElement;
    bool _history;
    public Window Window;

    /// <summary>
    /// Initializes a new instance of the <see cref="Visual"/> class.
    /// </summary>
    public Visual(): base()
    {
      Init();
    }

    public Visual(Visual v): base(v)
    {
      Init();
      History = v.History;
      VisualParent = v.VisualParent;
      Window = v.Window;
      // As Context is a derived property, it is not neccessary to copy it here.
      //Context = v.Context;
    }

    void Init()
    {
      _history = true;
      _visualParentProperty = new Property(typeof(UIElement), null);
      _focusedElement = new Property(typeof(UIElement), null);
    }

    /// <summary>
    /// Gets or sets the context.
    /// Hint: This property comes from frodos implementation, originally declared
    /// in class <see cref="UIElement"/>. Context is used by the templating system.
    /// We now support a more WPF-like <see cref="DependencyObject.DataContext"/>
    /// for bindings - as long as this system isn't reworked, we join those two concepts.
    /// We store the context in the <see cref="DependencyObject.DataContext"/>
    /// (See <see cref="BindingMarkupExtension"/>).
    /// TODO: Rework the templating system - maybe remove this property and create a
    /// real logical tree?
    /// </summary>
    /// <value>The context.</value>
    public object Context
    {
      get
      {
        return DataContext == null ? null : DataContext.Source;
      }
      set
      {
        if (DataContext == null && value == null)
          return;
        if (DataContext == null)
          DataContext = new BindingMarkupExtension(this);
        DataContext.Source = value;
      }
    }

    /// <summary>
    /// Gets the visual parent property.
    /// </summary>
    /// <value>The visual parent property.</value>
    public Property VisualParentProperty
    {
      get
      {
        return _visualParentProperty;
      }
    }

    /// <summary>
    /// Gets or sets the visual parent.
    /// </summary>
    /// <value>The visual parent.</value>
    public UIElement VisualParent
    {
      get
      {
        return (UIElement)_visualParentProperty.GetValue();
      }
      set
      {
        _visualParentProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets the default focused element property.
    /// </summary>
    /// <value>The visual parent property.</value>
    public Property FocusedElementProperty
    {
      get
      {
        return _focusedElement;
      }
    }

    /// <summary>
    /// Gets or sets the default focused element.
    /// </summary>
    /// <value>The focused element.</value>
    public UIElement FocusedElement
    {
      get
      {
        return _focusedElement.GetValue() as UIElement;
      }
      set
      {
        _focusedElement.SetValue(value);
      }
    }

    /// <summary>
    /// returns if the point lies inside the object.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <returns></returns>
    public virtual bool InsideObject(double x, double y)
    {
      return false;
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public virtual void DoRender()
    {
    }

    /// <summary>
    /// Renders this instance.
    /// </summary>
    public virtual void Render()
    {
      DoRender();
    }

    public bool History
    {
      get
      {
        return _history;
      }
      set
      {
        _history = value;
      }
    }

    public virtual void DoBuildRenderTree()
    {
    }
    
    public virtual void BuildRenderTree()
    {
    }
    
    public virtual void DestroyRenderTree()
    {
    }
  }
}

