#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UiComponents.Media.FilterTrees
{
  /// <summary>
  /// Represents an absolute path to a node in an <see cref="IFilterTree"/>.
  /// </summary>
  public class FilterTreePath
  {
    protected IList<FilterTreePathSegment> _segments;

    /// <summary>
    /// Combines multiple <see cref="FilterTreePath"/>s into a single path. 
    /// </summary>
    /// <param name="parts">The <see cref="FilterTreePath"/>s to combine</param>
    /// <returns></returns>
    public static FilterTreePath Combine(params FilterTreePath[] parts)
    {
      IEnumerable<FilterTreePath> validPaths = parts.Where(p => p != null && p.Segments.Count > 0);
      FilterTreePath combinedPath = null;
      foreach (FilterTreePath part in validPaths)
      {
        if (combinedPath == null)
          combinedPath = new FilterTreePath();
        foreach (FilterTreePathSegment segment in part.Segments)
          combinedPath.Segments.Add(segment);
      }
      return combinedPath;
    }

    /// <summary>
    /// Creates an empty <see cref="FilterTreePath"/> which represents the root node of an <see cref="IFilterTree"/>. 
    /// </summary>
    public FilterTreePath()
    {
      _segments = new List<FilterTreePathSegment>();
    }

    /// <summary>
    /// Creates a <see cref="FilterTreePath"/> with a collection of<see cref="FilterTreePathSegment"/>s with the roles
    /// specified in <paramref name="pathRoles"/>.
    /// </summary>
    /// <param name="pathRoles">The roles of each <see cref="FilterTreePathSegment"/>.</param>
    public FilterTreePath(IEnumerable<Guid> pathRoles)
      : this()
    {
      if (pathRoles != null)
        foreach (Guid role in pathRoles)
          _segments.Add(new FilterTreePathSegment(role));
    }

    /// <summary>
    /// Creates a <see cref="FilterTreePath"/> with <see cref="FilterTreePathSegment"/>s with the roles
    /// specified in <paramref name="pathRoles"/>.
    /// </summary>
    /// <param name="pathRoles">The roles of each <see cref="FilterTreePathSegment"/>.</param>
    public FilterTreePath(params Guid[] pathRoles)
      : this((IEnumerable<Guid>)pathRoles)
    {
    }

    /// <summary>
    /// Collection of <see cref="FilterTreePathSegment"/>s for this <see cref="FilterTreePath"/>.
    /// </summary>
    public IList<FilterTreePathSegment> Segments
    {
      get { return _segments; }
    }
  }

  /// <summary>
  /// Represents an individual connection between a parent node and it's child node with the
  /// specified role.
  /// </summary>
  public class FilterTreePathSegment
  {
    protected Guid _role;

    /// <summary>
    /// Creates a <see cref="FilterTreePathSegment"/> with the specified role.
    /// </summary>
    /// <param name="role">The role of the child node.</param>
    public FilterTreePathSegment(Guid role)
    {
      _role = role;
    }

    /// <summary>
    /// The role of the child node.
    /// </summary>
    public Guid Role
    {
      get { return _role; }
    }
  }
}
