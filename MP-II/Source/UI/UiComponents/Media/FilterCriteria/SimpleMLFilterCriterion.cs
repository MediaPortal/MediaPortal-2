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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.UI.ServerCommunication;

namespace UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Filter criterion which creates a filter by a simple attribute value.
  /// </summary>
  public class SimpleMLFilterCriterion : MLFilterCriterion
  {
    protected MediaItemAspectMetadata.AttributeSpecification _attributeType;

    public SimpleMLFilterCriterion(MediaItemAspectMetadata.AttributeSpecification attributeType)
    {
      _attributeType = attributeType;
    }

    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter filter)
    {
      IContentDirectory cd = ServiceScope.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return new List<FilterValue>();
      HomogenousCollection distinctValues = cd.GetDistinctAssociatedValues(_attributeType, necessaryMIATypeIds, filter);
      ICollection<FilterValue> result = new List<FilterValue>(distinctValues.Count);
      foreach (object value in distinctValues)
      {
        if (value == null || string.Empty == value as string)
          result.Add(new FilterValue(VALUE_EMPTY_TITLE, new EmptyFilter(_attributeType), this));
        else
          result.Add(new FilterValue(value.ToString(),
              new RelationalFilter(_attributeType, RelationalOperator.EQ, value), this));
      }
      return result;
    }

    public override IFilter CreateFilter(FilterValue filterValue)
    {
      return (IFilter) filterValue.Value;
    }

    #endregion
  }
}
