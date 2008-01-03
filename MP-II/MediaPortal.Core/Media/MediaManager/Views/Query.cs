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
using System.Collections.Generic;
using MediaPortal.Core.MediaManager.Views;

namespace MediaPortal.Core.MediaManager.Views
{
  public class Query : IQuery
  {
    private List<IExpression> _subQueries;
    private Operator _operator;
    private object _value;
    private string _key;
    private SortOrder _sortOrder = SortOrder.None;
    private int _limit = -1;
    List<string> _sortFields = new List<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Query"/> class.
    /// </summary>
    /// <param name="metaData">The meta data.</param>
    /// <param name="op">The op.</param>
    /// <param name="obj">The obj.</param>
    public Query(string key, Operator op, object obj)
    {
      _key = key;
      _operator = op;
      _value = obj;
      _subQueries = new List<IExpression>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Query"/> class.
    /// </summary>
    /// <param name="metaData">The meta data.</param>
    /// <param name="op">The op.</param>
    public Query(string key, Operator op)
    {
      _key = key;
      _operator = op;
      _subQueries = new List<IExpression>();
    }

    public Query()
    {
      _key = "";
      _operator = Operator.None;
      _subQueries = new List<IExpression>();
    }

    public Query(string key, IExpression compount)
    {
      _key = key;
      _operator = Operator.None;
      _subQueries = new List<IExpression>();
      _subQueries.Add(compount);
    }

    /// <summary>
    /// Gets or sets the sub queries.
    /// </summary>
    /// <value>The sub views.</value>
    public List<IExpression> SubQueries
    {
      get { return _subQueries; }
      set { _subQueries = value; }
    }

    /// <summary>
    /// Gets or sets the meta data name.
    /// </summary>
    /// <value>The meta data name.</value>
    public string Key
    {
      get { return _key; }
      set { _key = value; }
    }

    /// <summary>
    /// Gets or sets the sort.
    /// </summary>
    /// <value>The sort.</value>
    public SortOrder Sort
    {
      get { return _sortOrder; }
      set { _sortOrder = value; }
    }

    public List<string> SortFields
    {
      get { return _sortFields; }
      set { _sortFields = value; }
    }

    /// <summary>
    /// Gets or sets the limit.
    /// </summary>
    /// <value>The limit.</value>
    public int Limit
    {
      get { return _limit; }
      set { _limit = value; }
    }

    /// <summary>
    /// Gets or sets the operator.
    /// </summary>
    /// <value>The operator.</value>
    public Operator Operator
    {
      get { return _operator; }
      set { _operator = value; }
    }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <value>The value.</value>
    public object Value
    {
      get { return _value; }
      set { _value = value; }
    }


    /// <summary>
    /// Gets from statement.
    /// </summary>
    /// <value>From statement.</value>
    public List<string> FromStatement
    {
      get
      {
        List<string> fieldNames = new List<string>();
        if (Operator == Operator.Distinct)
        {
          if (!fieldNames.Contains(Key))
          {
            fieldNames.Add(Key);
          }
        }

        foreach (IExpression e in SubQueries)
        {
          if (e.Left != null)
          {
            foreach (string f in e.Left.FromStatement)
            {
              if (!fieldNames.Contains(f))
              {
                fieldNames.Add(f);
              }
            }
          }
          if (e.Right != null)
          {
            foreach (string f in e.Right.FromStatement)
            {
              if (!fieldNames.Contains(f))
              {
                fieldNames.Add(f);
              }
            }
          }
        }
        return fieldNames;
      }
    }

    public string WhereStatement
    {
      get
      {
        if (Operator != Operator.None && Operator != Operator.Distinct)
        {
          if (Key.Length != 0 && _value != null)
          {
            //if we use a query in which a multi-attribute field is used
            //then we should run the query with select * from table where multiattributefield  like '%[value]%
            string attrValue = Value.ToString().Replace("'", "''");
            if (_operator == Operator.Like)
            {
              return String.Format("({0} {1} '%{2}%')", Key, _operator.ToString(), attrValue);
            }
            else
            {
              return String.Format("({0} {1} '{2}')", Key, _operator.ToString(), attrValue);
            }
          }
        }
        string line = "";
        foreach (IExpression e in SubQueries)
        {
          string left = "";
          string right = "";
          if (e.Left != null)
          {
            left = e.Left.WhereStatement;
          }
          if (e.Right != null)
          {
            right = e.Right.WhereStatement;
          }
          if (left.Length > 0 && right.Length > 0)
          {
            line += String.Format("({0} {1} {2}) ", left, e.Operator.ToString(), right);
          }
          else if (right.Length > 0)
          {
            if (line.Length == 0)
            {
              line += String.Format("{0} ", right);
            }
            else
            {
              line += String.Format("and {0} ", right);
            }
          }
          else if (left.Length > 0)
          {
            if (line.Length == 0)
            {
              line += String.Format("{0} ", left);
            }
            else
            {
              line += String.Format("and {0} ", left);
            }
          }
        }
        return line;
      }
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
        if (Key != null && Key.Length > 0)
        {
          fieldNames.Add(Key);
        }

        foreach (IExpression e in SubQueries)
        {
          List<string> subFields = e.FieldNames;
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