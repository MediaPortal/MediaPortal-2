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
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UiComponents.Media.FilterTrees
{
  /// <summary>
  /// Implementation of <see cref="IFilterTree"/> that connects filters for different roles by using
  /// <see cref="RelationshipFilter"/>s and <see cref="FilteredRelationshipFilter"/>s.
  /// </summary>
  public class RelationshipFilterTree : IFilterTree
  {
    //Parent node if this is a child node
    protected RelationshipFilterTree _parent;
    //Child nodes by role
    protected IDictionary<Guid, RelationshipFilterTree> _children;
    //This node's role
    protected Guid _role;
    //Linked ids for this node's role, will override any filters
    protected ICollection<Guid> _linkedIds;
    //Combination of all filters applied to this node's role
    protected IFilter _filter;

    /// <summary>
    /// Constructs the root node of a filter tree with the role specified in <paramref name="role"/>.
    /// </summary>
    /// <param name="role">The role of this node.</param>
    public RelationshipFilterTree(Guid role)
    {
      _children = new Dictionary<Guid, RelationshipFilterTree>();
      _linkedIds = new HashSet<Guid>();
      _role = role;
    }

    /// <summary>
    /// Constructs a node in an existing filter tree with the role specified in <paramref name="role"/>
    /// and parent specified in <paramref name="parent"/>.
    /// </summary>
    /// <param name="role">The role of this node.</param>
    /// <param name="parent">The parent of this node.</param>
    protected RelationshipFilterTree(Guid role, RelationshipFilterTree parent)
      : this(role)
    {
      _parent = parent;
    }

    /// <summary>
    /// Builds a filter starting at this node including subfilters for child nodes.
    /// </summary>
    /// <returns></returns>
    public IFilter BuildFilter()
    {
      return BuildChildFilters(null);
    }

    /// <summary>
    /// Builds a filter starting at the node specified in <paramref name="path"/>
    /// including subfilters for parent and child nodes.
    /// </summary>
    /// <param name="path">The absolute path to the node from this node.</param>
    /// <returns></returns>
    public IFilter BuildFilter(FilterTreePath path)
    {
      //We won't add missing nodes to avoid polluting the tree.
      //The filter will still be created correctly because the 'missing' nodes
      //have a reference to the tree via their parent field and can therefore walk up correctly.
      RelationshipFilterTree node = FindNodeForPath(path, false);
      return node.BuildChildAndParentFilters(null);
    }

    /// <summary>
    /// Adds a filter to this node.
    /// </summary>
    /// <param name="filter">The filter to add, the filter will be combined with any existing filters.</param>
    public void AddFilter(IFilter filter)
    {
      AddFilter(filter, null);
    }

    /// <summary>
    /// Adds a filter to the node specified in <paramref name="path"/>.
    /// </summary>
    /// <param name="filter">The filter to add. The filter will be combined with any existing filters for the node.</param>
    /// <param name="path">The absolute path to the node from this node.</param>
    public void AddFilter(IFilter filter, FilterTreePath path)
    {
      RelationshipFilterTree node = FindNodeForPath(path, true);
      node.CombineFilter(filter);
    }

    /// <summary>
    /// Adds a linked id for the role specified in <paramref name="path"/>.
    /// The linked id will override any existing filters for the node. 
    /// </summary>
    /// <param name="linkedId">The media item id to link to the node.</param>
    /// <param name="path">The absolute path to the node from this node.</param>
    public void AddLinkedId(Guid linkedId, FilterTreePath path)
    {
      RelationshipFilterTree node = FindNodeForPath(path, true);
      node.LinkedIds.Add(linkedId);
    }

    /// <summary>
    /// Creates a deep copy of the tree including all child nodes and their properties.
    /// </summary>
    /// <returns></returns>
    public IFilterTree DeepCopy()
    {
      return DeepCopy(null);
    }

    /// <summary>
    /// The role of this node.
    /// </summary>
    public Guid Role
    {
      get { return _role; }
    }

    /// <summary>
    /// The parent node of this node.
    /// </summary>
    protected RelationshipFilterTree Parent
    {
      get { return _parent; }
    }

    /// <summary>
    /// The media item ids linked to this node.
    /// </summary>
    protected ICollection<Guid> LinkedIds
    {
      get { return _linkedIds; }
      set { _linkedIds = value; }
    }

    /// <summary>
    /// Creates a deep copy of this node and all child nodes and their properties.
    /// </summary>
    /// <param name="parent">The new parent node of the copied child nodes.</param>
    /// <returns></returns>
    protected RelationshipFilterTree DeepCopy(RelationshipFilterTree parent)
    {
      RelationshipFilterTree copy = new RelationshipFilterTree(_role, parent);
      copy._linkedIds = new HashSet<Guid>(_linkedIds);
      copy._filter = _filter;
      foreach (var child in _children)
        copy._children[child.Key] = child.Value.DeepCopy(copy);
      return copy;
    }

    /// <summary>
    /// Combines the <paramref name="filter"/> with any existing filters.
    /// </summary>
    /// <param name="filter">The filter to combine.</param>
    protected void CombineFilter(IFilter filter)
    {
      _filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, _filter, filter);
    }

    /// <summary>
    /// Finds the node for the role specified in <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The absolute path to the node from this node.</param>
    /// <param name="addMissingNodesToTree">Whether to add any missing nodes to the tree.</param>
    /// <returns></returns>
    protected RelationshipFilterTree FindNodeForPath(FilterTreePath path, bool addMissingNodesToTree)
    {
      RelationshipFilterTree node = this;
      if (path != null)
        foreach (FilterTreePathSegment segment in path.Segments)
          node = node.FindChild(segment.Role, addMissingNodesToTree);
      return node;
    }

    /// <summary>
    /// Finds the child node of this node with the role specified in <paramref name="role"/>.
    /// </summary>
    /// <param name="role">The role of the child node to find.</param>
    /// <param name="addMissingNodesToTree">Whether to add any missing nodes to the tree.</param>
    /// <returns></returns>
    protected RelationshipFilterTree FindChild(Guid role, bool addMissingNodesToTree)
    {
      RelationshipFilterTree node;
      if (!_children.TryGetValue(role, out node))
      {
        node = new RelationshipFilterTree(role, this);
        if (addMissingNodesToTree)
          _children[role] = node;
      }
      return node;
    }

    /// <summary>
    /// Combines the <paramref name="currentFilter"/> with a <see cref="RelationshipFilter"/> for each media item id
    /// in <paramref name="linkedIds"/> with the apecified <paramref name="role"/> and <paramref name="linkedRole"/>. 
    /// </summary>
    /// <param name="currentFilter">The filter to combine.</param>
    /// <param name="role">The role to use for the <see cref="RelationshipFilter"/>.</param>
    /// <param name="linkedRole">The linked role to use for the <see cref="RelationshipFilter"/>.</param>
    /// <param name="linkedIds">The linked ids to use for the <see cref="RelationshipFilter"/>.</param>
    /// <returns></returns>
    protected static IFilter CombineWithRelationship(IFilter currentFilter, Guid role, Guid linkedRole, IEnumerable<Guid> linkedIds)
    {
      IFilter relationships = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, linkedIds.Select(id => new RelationshipFilter(role, linkedRole, id)));
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.And, currentFilter, relationships);
    }

    /// <summary>
    /// Combines the <paramref name="currentFilter"/> with a <see cref="FilteredRelationshipFilter"/> with
    /// the specified <paramref name="role"/>, <paramref name="linkedRole"/> and <paramref name="subFilter"/>. 
    /// </summary>
    /// <param name="currentFilter">The filter to combine.</param>
    /// <param name="role">The role to use for the <see cref="RelationshipFilter"/>.</param>
    /// <param name="linkedRole">The linked role to use for the <see cref="RelationshipFilter"/>.</param>
    /// <param name="subFilter">The filter to use for the <see cref="RelationshipFilter"/>.</param>
    /// <param name="ignoreNullSubFilter">Whether to not combine the filters if <paramref name="subFilter"/> is <c>null</c>.</param>
    /// <returns></returns>
    protected static IFilter CombineWithFilteredRelationship(IFilter currentFilter, Guid role, Guid linkedRole, IFilter subFilter, bool ignoreNullSubFilter)
    {
      if (subFilter == null && ignoreNullSubFilter)
        return currentFilter;
      IFilter filteredRelationship = new FilteredRelationshipFilter(role, linkedRole, subFilter);
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.And, currentFilter, filteredRelationship);
    }

    /// <summary>
    /// Walks the filter tree from this node to all child nodes and constructs a filter connected by
    /// the child nodes' roles. Optionally excluding any nodes with the role specified in <paramref name="excludeRole"/>.
    /// </summary>
    /// <param name="excludeRole">If not null, specifies the role of any child nodes to exclude when walking the tree.</param>
    /// <returns></returns>
    protected IFilter BuildChildFilters(Guid? excludeRole)
    {
      IFilter baseFilter = _filter;
      //Walk the tree for all children
      foreach (RelationshipFilterTree child in _children.Values)
      {
        if (excludeRole.HasValue && child.Role == excludeRole.Value)
          continue;
        if (child.LinkedIds.Count > 0)
          //We only support combining filters with AND so if the child has any linked ids, and therefore will only
          //ever return those media items, we can optimise and ignore all child filters and relationships
          baseFilter = CombineWithRelationship(baseFilter, _role, child.Role, child.LinkedIds);
        else
          //No linked ids, get child filters and relationships.
          baseFilter = CombineWithFilteredRelationship(baseFilter, _role, child.Role, child.BuildFilter(), true);
      }
      return baseFilter;
    }

    /// <summary>
    /// Walks the filter tree from this node to all child nodes and up to all parent nodes and their children and
    /// constructs a filter connected by the nodes' roles. Optionally excluding any nodes with the role specified in <paramref name="excludeRole"/>.
    /// </summary>
    /// <param name="excludeRole">If not null, specifies the role of any child nodes to exclude when walking the tree.</param>
    /// <returns></returns>
    protected IFilter BuildChildAndParentFilters(Guid? excludeRole)
    {
      if (_linkedIds.Count > 0)
        //We only support combining filters with AND so if we have any linked ids, and therefore will only
        //ever return those media items, we can optimise and ignore all filters and relationships
        return new MediaItemIdFilter(_linkedIds);

      //Get all child filters and relationships
      IFilter baseFilter = BuildChildFilters(excludeRole);
      if (_parent != null)
      {
        if (_parent.LinkedIds.Count > 0)
          //We only support combining filters with AND so if parent has any linked ids, and therefore will only
          //ever return those media items, we can optimise and ignore all parent filters and relationships
          baseFilter = CombineWithRelationship(baseFilter, _role, _parent.Role, _parent.LinkedIds);
        else
          //No linked ids, get parent filters and relationships.
          //Set ignoreSubfilter to false as we need to ensure that the filter only returns items that descend from the root role
          //and therefore need to include a relationship 'chain' to the root node.
          baseFilter = CombineWithFilteredRelationship(baseFilter, _role, _parent.Role, _parent.BuildChildAndParentFilters(_role), false);
      }
      return baseFilter;
    }
  }
}