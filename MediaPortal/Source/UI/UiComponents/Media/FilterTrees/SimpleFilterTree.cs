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

using MediaPortal.Common.MediaManagement.MLQueries;
using System;

namespace MediaPortal.UiComponents.Media.FilterTrees
{
  /// <summary>
  /// Implementation of <see cref="IFilterTree"/> that only maintains a single filter and does not support connecting filters by relationship.
  /// This implementation can be used in media views that do not support relationships.
  /// </summary>
  public class SimpleFilterTree : IFilterTree
  {
    protected IFilter _filter;

    public void AddFilter(IFilter filter)
    {
      CombineFilter(filter);
    }

    public void AddFilter(IFilter filter, FilterTreePath path)
    {
      if (path != null && path.Segments.Count > 0)
        throw new ArgumentException(string.Format("{0} must be null or empty when adding a filter to a {1}", nameof(path), GetType().Name), nameof(path));
      AddFilter(filter);
    }

    public void AddLinkedId(Guid linkedId, FilterTreePath path)
    {
      throw new InvalidOperationException(string.Format("{0} does not support adding a linked id", GetType().Name));
    }

    public IFilter BuildFilter()
    {
      return _filter;
    }

    public IFilter BuildFilter(FilterTreePath path)
    {
      if(path != null && path.Segments.Count > 0)
        throw new ArgumentException(string.Format("{0} must be null or empty when building a filter from a {1}", nameof(path), GetType().Name), nameof(path));
      return BuildFilter();
    }

    public IFilterTree DeepCopy()
    {
      IFilterTree tree = new SimpleFilterTree();
      tree.AddFilter(_filter);
      return tree; 
    }

    protected void CombineFilter(IFilter filter)
    {
      if (_filter == null)
        _filter = filter;
      else
        _filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, _filter, filter);
    }
  }
}
