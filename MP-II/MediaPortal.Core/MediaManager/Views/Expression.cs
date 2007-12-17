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

using System.Collections.Generic;
using MediaPortal.Core.MediaManager.Views;

namespace MediaPortal.Core.MediaManager.Views
{
  public class Expression : IExpression
  {
    private IQuery _queryLeft;
    private IQuery _queryRight;
    private Operator _operator;

    public Expression(IQuery query)
    {
      _queryLeft = query;
    }

    public Expression(IQuery left, Operator op, IQuery right)
    {
      _queryLeft = left;
      _queryRight = right;
      _operator = op;
    }

    public IQuery Left
    {
      get { return _queryLeft; }
      set { _queryLeft = value; }
    }

    public IQuery Right
    {
      get { return _queryRight; }
      set { _queryRight = value; }
    }

    public Operator Operator
    {
      get { return _operator; }
      set { _operator = value; }
    }

    /// <summary>
    /// Gets the field names used in this query.
    /// </summary>
    /// <value>The field names.</value>
    public List<string> FieldNames
    {
      get
      {
        List<string> fieldNames = new List<string>();
        if (Left != null)
        {
          List<string> subFields = Left.FieldNames;
          foreach (string name in subFields)
          {
            fieldNames.Add(name);
          }
        }
        if (Right != null)
        {
          List<string> subFields = Right.FieldNames;
          foreach (string name in subFields)
          {
            fieldNames.Add(name);
          }
        }
        return fieldNames;
      }
    }
  }
}