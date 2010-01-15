#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.MediaManagement.MLQueries;

namespace MediaPortal.UI.Views
{
  /// <summary>
  /// Media library view specification which can attend a dynamic filtering path through the media library contents.
  /// </summary>
  /// <remarks>
  /// <para>
  /// When presenting a search- and navigation-interface through the media library contents (music, movies and pictures),
  /// the system will typically provide a collection of possible filter criteria (for example: By artist, by album, ...).
  /// When a filter criterion is choosen by the user, the system will show possible values (for example artist names),
  /// the user can choose from. When a value is choosen, the system will provide the filtered contents in a new view
  /// which is stacked on the view before. All filter conditions from the parent view will then also be valid for the
  /// sub view, plus the new filter which was choosen by the user.
  /// </para>
  /// </remarks>
  public class StackedFiltersMLVS : MediaLibraryViewSpecification
  {
    protected ICollection<IFilter> _filters;

    public StackedFiltersMLVS(string viewDisplayName, ICollection<IFilter> filters,
        IEnumerable<Guid> necessaryMIATypeIDs, IEnumerable<Guid> optionalMIATypeIDs, bool onlyOnline) :
        base(viewDisplayName, new MediaItemQuery(necessaryMIATypeIDs, optionalMIATypeIDs,
            BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filters)), onlyOnline)
    {
      _filters = filters;
    }

    public ICollection<IFilter> Filters
    {
      get { return _filters; }
    }

    public StackedFiltersMLVS CreateSubViewSpecification(string viewDisplayName, IFilter filter)
    {
      IList<IFilter> baseFilters = _filters == null ? new List<IFilter>(1) : new List<IFilter>(_filters);
      baseFilters.Add(filter);
      return new StackedFiltersMLVS(viewDisplayName, baseFilters, NecessaryMIATypeIds, OptionalMIATypeIds, OnlyOnline);
    }

    public static StackedFiltersMLVS CreateRootViewSpecification(string viewDisplayName,
        ICollection<Guid> necessaryRequestedMIATypeIDs, ICollection<Guid> optionalRequestedMIATypeIDs,
        IFilter baseFilter, bool onlyOnline)
    {
      IFilter[] filters = baseFilter == null ? null : new IFilter[] {baseFilter};
      return new StackedFiltersMLVS(viewDisplayName, filters,
          necessaryRequestedMIATypeIDs, optionalRequestedMIATypeIDs, onlyOnline);
    }

  }
}
