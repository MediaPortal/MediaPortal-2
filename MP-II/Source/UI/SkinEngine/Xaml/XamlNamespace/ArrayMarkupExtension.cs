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
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml.XamlNamespace
{
  public class ArrayMarkupExtension: IEvaluableMarkupExtension
  {

    #region Protected fields

    protected Type _type = null;
    protected IList<object> _elements = new List<object>();

    #endregion

    #region Constructor

    public ArrayMarkupExtension() { }

    #endregion

    #region Properties

    /// <summary>
    /// Returns the type of the <see cref="Elements"/> list.
    /// </summary>
    public Type @Type
    {
      get { return _type; }
      set { _type = value; }
    }

    /// <summary>
    /// Returns the elements of this array, in an object array.
    /// It is explicitly allowed to cast the returned array of type <c>object[]</c>
    /// to an array of this array's type. Let <c>a</c> be an instance of this array class,
    /// and let <c>T</c> be the type of <c>a</c>, it is allowed to:
    /// <code>
    /// T[] ta = (T[]) a.Elements;
    /// </code>
    /// </summary>
    public object[] Elements
    {
      get
      {
        object[] result = new object[_elements.Count];
        _elements.CopyTo(result, 0);
        return result; 
      }
      set
      {
        foreach (object val in value)
        {
          if (_type.IsAssignableFrom(val.GetType()))
            _elements.Add(value);
          else
            throw new XamlBindingException("A value of type '{0}' cannot be assigned to an array of type '{1}[]'",
                val.GetType(), _type);
        }
      }
    }

    #endregion

    #region IEvaluableMarkupExtension implementation

    object IEvaluableMarkupExtension.Evaluate(IParserContext context)
    {
      return _elements;
    }

    #endregion
  }
}
