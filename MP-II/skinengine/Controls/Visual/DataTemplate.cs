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
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core.InputManager;

using SkinEngine;


namespace SkinEngine.Controls.Visuals
{
  public class DataTemplate : FrameworkElement, IList
  {
    Property _visualTree;
    public DataTemplate()
    {
      Init();
    }

    public DataTemplate(DataTemplate template)
      : base(template)
    {
      Init();
      if (template.VisualTree != null)
        VisualTree = template.VisualTree;
    }

    public override object Clone()
    {
      return new DataTemplate(this);
    }

    void Init()
    {
      _visualTree = new Property(null);
    }

    /// <summary>
    /// Gets or sets the visual tree property.
    /// </summary>
    /// <value>The visual tree property.</value>
    public Property VisualTreeProperty
    {
      get
      {
        return _visualTree;
      }
      set
      {
        _visualTree = value;
      }
    }

    /// <summary>
    /// Gets or sets the visual tree.
    /// </summary>
    /// <value>The visual tree.</value>
    public FrameworkElement VisualTree
    {
      get
      {
        return _visualTree.GetValue() as FrameworkElement;
      }
      set
      {
        _visualTree.SetValue(value);
      }
    }
    /// <summary>
    /// Find the element with name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public override UIElement FindElement(string name)
    {
      if (VisualTree != null)
      {
        UIElement element = VisualTree.FindElement(name);
        if (element != null) return element;
      }
      return base.FindElement(name);
    }

    /// <summary>
    /// Finds the element of type t.
    /// </summary>
    /// <param name="t">The t.</param>
    /// <returns></returns>
    public override UIElement FindElementType(Type t)
    {
      if (VisualTree != null)
      {
        UIElement element = VisualTree.FindElementType(t);
        if (element != null) return element;
      }
      return base.FindElementType(t);
    }

    /// <summary>
    /// Finds the the element which is a ItemsHost
    /// </summary>
    /// <returns></returns>
    public override UIElement FindItemsHost()
    {
      if (VisualTree != null)
      {
        UIElement element = VisualTree.FindItemsHost();
        if (element != null) return element;
      }
      return base.FindItemsHost();
    }

    /// <summary>
    /// Finds the focused item.
    /// </summary>
    /// <returns></returns>
    public override UIElement FindFocusedItem()
    {
      if (HasFocus) return this;
      if (VisualTree != null)
      {
        UIElement element = VisualTree.FindFocusedItem();
        return element;
      }
      return null;
    }

    /// <summary>
    /// Gets or sets the type of the data. (not used in our xaml engine)
    /// </summary>
    /// <value>The type of the data.</value>
    public string DataType
    {
      get
      {
        return "";
      }
      set
      {
      }
    }

    #region IList Members

    public int Add(object value)
    {
      VisualTree = (FrameworkElement)value;
      return 1;
    }

    public void Clear()
    {
    }

    public bool Contains(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int IndexOf(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Insert(int index, object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public bool IsFixedSize
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsReadOnly
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public void Remove(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void RemoveAt(int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public object this[int index]
    {
      get
      {
        throw new Exception("The method or operation is not implemented.");
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    #region ICollection Members

    public void CopyTo(Array array, int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int Count
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsSynchronized
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public object SyncRoot
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    #endregion

    #region IEnumerable Members

    public IEnumerator GetEnumerator()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion
  }
}
