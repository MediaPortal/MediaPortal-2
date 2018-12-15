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
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.UiComponents.Media.FilterTrees
{
  /// <summary>
  /// Represents a hierarchical tree of filters for use in stacked media views.
  /// <para>
  /// Implementations of this class can apply custom adding and building of filters based on a <see cref="FilterTreePath"/>, for example
  /// the <see cref="RelationshipFilterTree"/> can build a complex filter for multiple roles connected by a <see cref="RelationshipFilter"/>.   
  /// </para>
  /// </summary>
  public interface IFilterTree
  {
    /// <summary>
    /// Adds a filter to this node.
    /// </summary>
    /// <param name="filter">The filter to add, the filter will be combined with any existing filters.</param>
    void AddFilter(IFilter filter);

    /// <summary>
    /// Adds a filter to the node specified in <paramref name="path"/>.
    /// </summary>
    /// <param name="filter">The filter to add. The filter will be combined with any existing filters for the node.</param>
    /// <param name="path">The absolute path to the node from this node.</param>
    void AddFilter(IFilter filter, FilterTreePath path);

    /// <summary>
    /// Adds a linked id to the node specified in <paramref name="path"/>.
    /// The linked id will override any existing filters for the node. 
    /// </summary>
    /// <param name="linkedId">The media item id to link to the node.</param>
    /// <param name="path">The absolute path to the node from this node.</param>
    void AddLinkedId(Guid linkedId, FilterTreePath path);

    /// <summary>
    /// Builds a filter starting at this node including subfilters for child nodes.
    /// </summary>
    /// <returns></returns>
    IFilter BuildFilter();

    /// <summary>
    /// Builds a filter starting at the node specified in <paramref name="path"/>
    /// including subfilters for parent and child nodes.
    /// </summary>
    /// <param name="path">The absolute path to the node from this node.</param>
    /// <returns></returns>
    IFilter BuildFilter(FilterTreePath path);

    /// <summary>
    /// Creates a complete copy of this <see cref="IFilterTree"/> including
    /// all child nodes and filters.
    /// </summary>
    /// <returns></returns>
    IFilterTree DeepCopy();
  }
}