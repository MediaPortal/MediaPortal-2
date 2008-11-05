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
using MediaPortal.Core;
using MediaPortal.Media.MediaManagement;
using MediaPortal.Media.MediaManagement.Views;

namespace Components.Services.MediaManager.Views
{
  public class View : IView
  {
    #region variables

    private string _viewType;
    private string _name;
    private string _mappingTable;
    private List<IView> _subViews;
    private IQuery _query;
    private List<string> _databases;
    private readonly IRootContainer _parent;
    bool _isLastSubView;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="View"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    public View(string name)
      : this(name, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="View"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="parent">The parent.</param>
    public View(string name, IRootContainer parent)
    {
      _name = name;
      _subViews = new List<IView>();
      _databases = new List<string>();
      _parent = parent;

    }

    public bool IsLastSubView 
    {
      get
      {
        return _isLastSubView;
      }
      set
      {
        _isLastSubView = value;
      }
    }

    /// <summary>
    /// Gets or sets the view type.
    /// </summary>
    /// <value>The view type.</value>
    public string Type
    {
      get { return _viewType; }
      set { _viewType = value; }
    }

    /// <summary>
    /// Gets or sets the view name.
    /// </summary>
    /// <value>The name.</value>
    public string Title
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// Gets or sets the mapping table.
    /// </summary>
    /// <value>The mapping table.</value>
    public string MappingTable
    {
      get { return _mappingTable; }
      set { _mappingTable = value; }
    }

    /// <summary>
    /// Gets or sets the subviews.
    /// </summary>
    /// <value>The subviews.</value>
    public List<IView> SubViews
    {
      get { return _subViews; }
      set { _subViews = value; }
    }

    /// <summary>
    /// Gets or sets the query.
    /// </summary>
    /// <value>The query.</value>
    public IQuery Query
    {
      get { return _query; }
      set { _query = value; }
    }

    /// <summary>
    /// returns a list of all databases uses in this view.
    /// </summary>
    /// <value>The databases.</value>
    public List<string> Databases
    {
      get { return _databases; }
      set { _databases = value; }
    }

  }
}
