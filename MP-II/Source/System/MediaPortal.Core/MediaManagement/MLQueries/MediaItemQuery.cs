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

namespace MediaPortal.Core.MediaManagement.MLQueries
{
  public enum SortDirection
  {
    Ascending,
    Descending
  }

  public class SortInformation
  {
    protected MediaItemAspectMetadata.AttributeSpecification _attributeType;
    protected SortDirection _sortDirection;

    public SortInformation(MediaItemAspectMetadata.AttributeSpecification attributeType, SortDirection sortDirection)
    {
      _attributeType = attributeType;
      _sortDirection = sortDirection;
    }

    public MediaItemAspectMetadata.AttributeSpecification AttributeType
    {
      get { return _attributeType; }
      set { _attributeType = value; }
    }

    public SortDirection Direction
    {
      get { return _sortDirection; }
      set { _sortDirection = value; }
    }
  }

  /// <summary>
  /// Encapsulates a query for media items. Holds a list of selected media item aspect types and a
  /// filter criterion.
  /// </summary>
  public class MediaItemQuery
  {
    protected IFilter _filter;
    protected ICollection<Guid> _necessaryRequestedMIATypeIDs;
    protected ICollection<Guid> _optionalRequestedMIATypeIDs = null;
    protected ICollection<SortInformation> _sortInformation = null;

    public MediaItemQuery(IEnumerable<Guid> necessaryRequestedMIATypeIDs, IFilter filter)
    {
      _necessaryRequestedMIATypeIDs = new List<Guid>(necessaryRequestedMIATypeIDs);
      _filter = filter;
    }

    public ICollection<Guid> NecessaryRequestedMIATypeIDs
    {
      get { return _necessaryRequestedMIATypeIDs; }
      set { _necessaryRequestedMIATypeIDs = value; }
    }

    public ICollection<Guid> OptionalRequestedMIATypeIDs
    {
      get { return _optionalRequestedMIATypeIDs; }
      set { _optionalRequestedMIATypeIDs = value; }
    }

    public IFilter Filter
    {
      get { return _filter; }
      set { _filter = value; }
    }

    public ICollection<SortInformation> SortInformation
    {
      get { return _sortInformation; }
      set { _sortInformation = value; }
    }
  }
}
