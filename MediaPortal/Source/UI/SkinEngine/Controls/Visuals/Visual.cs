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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public enum AlignmentX { Left, Center, Right };
  public enum AlignmentY { Top, Center, Bottom };

  public delegate void ContextChangedDlgt(object oldContext, object newContext);

  public class Visual : DependencyObject
  {
    #region Protected fields

    protected AbstractProperty _visualParentProperty;
    protected AbstractProperty _screenProperty;

    #endregion

    #region Ctor

    public Visual()
    {
      Init();
    }

    void Init()
    {
      _visualParentProperty = new SProperty(typeof(Visual), null);
      _screenProperty = new SProperty(typeof(Screen), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Visual v = (Visual) source;
      VisualParent = copyManager.GetCopy(v.VisualParent);
      Screen = v.Screen; // Visuals must not be copied to another screen so we can reuse the Screen value
    }

    #endregion

    public event ContextChangedDlgt ContextChanged;

    protected void FireContextChanged(object oldContext, object newContext)
    {
      ContextChangedDlgt dlgt = ContextChanged;
      if (dlgt != null)
        dlgt(oldContext, newContext);
    }

    /// <summary>
    /// Gets or sets the context. This is a convenience property for setting the <see cref="DependencyObject.DataContext"/>
    /// with a <see cref="BindingExtension.Source"/> value of the given <paramref name="value"/>.
    /// </summary>
    /// <value>The source value used as data context.</value>
    public object Context
    {
      get { return DataContext == null ? null : DataContext.Source; }
      set
      {
        BindingExtension bme = DataContext;
        object oldContext = bme == null ? null : bme.Source;
        if (value == null)
        {
          if (bme != null)
            bme.Dispose();
          DataContext = null;
        }
        else if (DataContext == null)
        {
          BindingExtension dc = new BindingExtension(this) {Source = value}; // Set the context value before setting the DataContext property
          DataContext = dc;
        }
        else
          DataContext.Source = value;
        FireContextChanged(oldContext, value);
      }
    }

    public AbstractProperty VisualParentProperty
    {
      get { return _visualParentProperty; }
    }

    public Visual VisualParent
    {
      get { return (Visual) _visualParentProperty.GetValue(); }
      set { _visualParentProperty.SetValue(value); }
    }

    public AbstractProperty ScreenProperty
    {
      get { return _screenProperty; }
    }

    public Screen Screen
    {
      get { return (Screen) _screenProperty.GetValue(); }
      set { _screenProperty.SetValue(value); }
    }

    /// <summary>
    /// Returns the information if the specified point is located inside the bounds of this object.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns><c>true</c>, if the specified point is inside the bounds of this object, else <c>false</c>.</returns>
    public virtual bool InsideObject(double x, double y)
    {
      return false;
    }

    public virtual void Render(RenderContext parentRenderContext) { }

    public T FindParentOfType<T>() where T : Visual
    {
      Visual current = this;
      while (current != null && !(current is T))
        current = current.VisualParent;
      return (T) current;
    }
  }
}

