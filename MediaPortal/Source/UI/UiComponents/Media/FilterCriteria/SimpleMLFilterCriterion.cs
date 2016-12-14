#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Linq;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Filter criterion which creates a filter by a simple attribute value.
  /// </summary>
  public class SimpleMLFilterCriterion : MLFilterCriterion
  {
    protected MediaItemAspectMetadata.AttributeSpecification _keyAttributeType = null;
    protected MediaItemAspectMetadata.AttributeSpecification _valueAttributeType = null;
    protected IEnumerable<Guid> _necessaryMIATypeIds = null;

    public SimpleMLFilterCriterion(MediaItemAspectMetadata.AttributeSpecification attributeType)
    {
      _valueAttributeType = attributeType;
    }

    public SimpleMLFilterCriterion(MediaItemAspectMetadata.AttributeSpecification attributeType, IEnumerable<Guid> necessaryMIATypeIds)
    {
      _valueAttributeType = attributeType;
      _necessaryMIATypeIds = necessaryMIATypeIds;
    }

    public SimpleMLFilterCriterion(MediaItemAspectMetadata.AttributeSpecification keyAttributeType, MediaItemAspectMetadata.AttributeSpecification valueAttributeType, IEnumerable<Guid> necessaryMIATypeIds)
    {
      _keyAttributeType = keyAttributeType;
      _valueAttributeType = valueAttributeType;
      _necessaryMIATypeIds = necessaryMIATypeIds;
    }

    #region Base overrides

    public override ICollection<FilterValue> GetAvailableValues(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");

      if (_necessaryMIATypeIds != null)
        necessaryMIATypeIds = _necessaryMIATypeIds;
      HomogenousMap valueGroups = null;
      HomogenousMap valueKeys = null;
      if (_keyAttributeType != null)
      {
        Tuple<HomogenousMap, HomogenousMap> values = cd.GetKeyValueGroups(_keyAttributeType, _valueAttributeType, selectAttributeFilter, ProjectionFunction.None, necessaryMIATypeIds, filter, true, 
          ShowVirtualSetting.ShowVirtualMedia(necessaryMIATypeIds));
        valueGroups = values.Item1;
        valueKeys = values.Item2;
      }
      else
      {
        valueGroups = cd.GetValueGroups(_valueAttributeType, selectAttributeFilter, ProjectionFunction.None, necessaryMIATypeIds, filter, true, 
          ShowVirtualSetting.ShowVirtualMedia(necessaryMIATypeIds));
      }
      IList<FilterValue> result = new List<FilterValue>(valueGroups.Count);
      int numEmptyEntries = 0;
      foreach (KeyValuePair<object, object> group in valueGroups)
      {
        if (_keyAttributeType != null)
        {
          string name = GetDisplayName(group.Key);
          if (name == string.Empty)
            numEmptyEntries += (int)group.Value;
          else
          {
            IFilter queryFilter = new RelationalFilter(_valueAttributeType, RelationalOperator.EQ, group.Key);
            if (filter != null)
              queryFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, queryFilter, filter);
            result.Add(new FilterValue(valueKeys[group.Key], name, new FilteredRelationshipFilter(Guid.Empty, queryFilter), null, (int)group.Value, this));
          }
        }
        else
        {
          string name = GetDisplayName(group.Key);
          if (name == string.Empty)
            numEmptyEntries += (int)group.Value;
          else
          {
            IFilter queryFilter = new RelationalFilter(_valueAttributeType, RelationalOperator.EQ, group.Key);
            if (filter != null)
              queryFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, queryFilter, filter);
            result.Add(new FilterValue(name, new FilteredRelationshipFilter(Guid.Empty, queryFilter), null, (int)group.Value, this));
          }
        }
      }
      if (numEmptyEntries > 0)
      {
        IFilter queryFilter = new EmptyFilter(_valueAttributeType);
        if (filter != null)
          queryFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, queryFilter, filter);
        result.Insert(0, new FilterValue(Consts.RES_VALUE_EMPTY_TITLE, new FilteredRelationshipFilter(Guid.Empty, queryFilter), null, numEmptyEntries, this));
      }
      return result;
    }

    protected virtual string GetDisplayName (object groupKey)
    {
      return string.Format("{0}", groupKey).Trim();
    }

    public override ICollection<FilterValue> GroupValues(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");

      if (_necessaryMIATypeIds != null)
        necessaryMIATypeIds = _necessaryMIATypeIds.ToList();
      IList<MLQueryResultGroup> valueGroups = cd.GroupValueGroups(_valueAttributeType, selectAttributeFilter, ProjectionFunction.None,
          necessaryMIATypeIds, filter, true, GroupingFunction.FirstCharacter, ShowVirtualSetting.ShowVirtualMedia(necessaryMIATypeIds));
      IList<FilterValue> result = new List<FilterValue>(valueGroups.Count);
      int numEmptyEntries = 0;
      foreach (MLQueryResultGroup group in valueGroups)
      {
        string name = string.Format("{0}", group.GroupKey);
        name = name.Trim();
        if (name == string.Empty)
          numEmptyEntries += group.NumItemsInGroup;
        else
          result.Add(new FilterValue(name, null, group.AdditionalFilter, group.NumItemsInGroup, this));
      }
      if (numEmptyEntries > 0)
        result.Insert(0, new FilterValue(Consts.RES_VALUE_EMPTY_TITLE, new EmptyFilter(_valueAttributeType), null, numEmptyEntries, this));
      return result;
    }

    #endregion
  }
}
