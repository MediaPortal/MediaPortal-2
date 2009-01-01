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

using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls;
using MediaPortal.SkinEngine.MarkupExtensions;
using MediaPortal.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public enum AlignmentX { Left, Center, Right };
  public enum AlignmentY { Top, Center, Bottom };
  public enum Orientation { Vertical, Horizontal };
  public enum Dock { Left, Right, Top, Bottom, Center };

  public class Visual : DependencyObject
  {
    #region Private fields

    Property _visualParentProperty;
    Property _focusedElement;

    #endregion

    public Screen Screen;

    #region Ctor

    public Visual()
    {
      Init();
    }

    void Init()
    {
      _visualParentProperty = new Property(typeof(Visual), null);
      _focusedElement = new Property(typeof(FrameworkElement), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Visual v = (Visual) source;
      VisualParent = copyManager.GetCopy(v.VisualParent);
      Screen = copyManager.GetCopy(v.Screen);
    }

    #endregion

    /// <summary>
    /// Gets or sets the context.
    /// Hint: This property comes from frodos implementation, originally declared
    /// in class <see cref="UIElement"/>. Context is used by the templating system.
    /// We now support a more WPF-like <see cref="DependencyObject.DataContext"/>
    /// for bindings - as long as this system isn't reworked, we join those two concepts.
    /// We store the context in the <see cref="DependencyObject.DataContext"/>
    /// (See <see cref="BindingMarkupExtension"/>).
    /// TODO: Rework the templating system - remove this property and create a logical tree
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
        if (value == null)
        {
          if (DataContext != null)
            DataContext.Dispose();
          DataContext = null;
        }
        else if (DataContext == null)
        {
          BindingMarkupExtension dc = new BindingMarkupExtension(this);
          dc.Source = value; // Set the context value before setting the DataContext property
          DataContext = dc;
        }
        else
          DataContext.Source = value;
      }
    }

    public Property VisualParentProperty
    {
      get { return _visualParentProperty; }
    }

    public Visual VisualParent
    {
      get { return (Visual) _visualParentProperty.GetValue(); }
      set { _visualParentProperty.SetValue(value); }
    }

    public Property FocusedElementProperty
    {
      get { return _focusedElement; }
    }

    /// <summary>
    /// Gets or sets the default focused element.
    /// </summary>
    /// <value>The focused element.</value>
    public FrameworkElement FocusedElement
    {
      get { return _focusedElement.GetValue() as FrameworkElement; }
      set { _focusedElement.SetValue(value); }
    }

    /// <summary>
    /// Returns the information if the specified point is located inside this object.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns></returns>
    public virtual bool InsideObject(double x, double y)
    {
      return false;
    }

    public virtual void DoRender()
    { }

    public virtual void Render()
    {
      DoRender();
    }

    public virtual void DoBuildRenderTree()
    { }
    
    public virtual void BuildRenderTree()
    { }
    
    public virtual void DestroyRenderTree()
    { }
  }
}

