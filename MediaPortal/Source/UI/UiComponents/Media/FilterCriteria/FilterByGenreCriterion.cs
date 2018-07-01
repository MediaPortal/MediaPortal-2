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

using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using MediaPortal.UiComponents.Media.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Services.GenreConverter;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  public class FilterByGenreCriterion : SimpleMLFilterCriterion
  {
    protected string _genreCategory;

    public FilterByGenreCriterion(IEnumerable<Guid> necessaryMiaTypeIds, string genreCategory)
      : base(GenreAspect.ATTR_ID, GenreAspect.ATTR_GENRE, necessaryMiaTypeIds)
    {
      _genreCategory = genreCategory;
    }

    #region Base overrides

    public override async Task<ICollection<FilterValue>> GetAvailableValuesAsync(IEnumerable<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        throw new NotConnectedException("The MediaLibrary is not connected");

      bool showVirtual = VirtualMediaHelper.ShowVirtualMedia(necessaryMIATypeIds);
      ViewSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ViewSettings>();
      IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
      Dictionary<int, FilterValue> genredFilters = new Dictionary<int, FilterValue>();
      List<FilterValue> ungenredFilters = new List<FilterValue>();

      if (_necessaryMIATypeIds != null)
        necessaryMIATypeIds = _necessaryMIATypeIds;
      HomogenousMap valueGroups = null;
      HomogenousMap valueKeys = null;
      if (_keyAttributeType != null)
      {
        Tuple<HomogenousMap, HomogenousMap> values = await cd.GetKeyValueGroupsAsync(_keyAttributeType, _valueAttributeType, selectAttributeFilter, ProjectionFunction.None, necessaryMIATypeIds, filter, true, showVirtual);
        valueGroups = values.Item1;
        valueKeys = values.Item2;
      }
      else
      {
        valueGroups = await cd.GetValueGroupsAsync(_valueAttributeType, selectAttributeFilter, ProjectionFunction.None, necessaryMIATypeIds, filter, true, showVirtual);
      }
      IList<FilterValue> result = new List<FilterValue>(valueGroups.Count);
      int numEmptyEntries = 0;
      foreach (KeyValuePair<object, object> group in valueGroups)
      {
        string name = GetDisplayName(group.Key);
        if (name == string.Empty)
        {
          numEmptyEntries += (int)group.Value;
        }
        else if (!string.IsNullOrEmpty(_genreCategory) && settings.UseLocalizedGenres)
        {
          int? genreId = valueKeys[group.Key] as int?;
          if (!genreId.HasValue)
          {
            ungenredFilters.Add(new FilterValue(valueKeys[group.Key], name, new RelationalFilter(_valueAttributeType, RelationalOperator.EQ, group.Key), null, (int)group.Value, this));
          }
          else if (!genredFilters.ContainsKey(genreId.Value))
          {
            if (converter.GetGenreName(genreId.Value, _genreCategory, null, out string genreName))
              name = genreName;
            genredFilters.Add(genreId.Value, new FilterValue(genreId.Value, name, new RelationalFilter(_valueAttributeType, RelationalOperator.EQ, group.Key), null, (int)group.Value, this));
          }
          else
          {
            genredFilters[genreId.Value] = new FilterValue(genreId.Value, genredFilters[genreId.Value].Title, 
              BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, genredFilters[genreId.Value].Filter, new RelationalFilter(_valueAttributeType, RelationalOperator.EQ, group.Key)), null, 
              genredFilters[genreId.Value].NumItems.Value + (int)group.Value, this);
          }
        }
        else
        {
          result.Add(new FilterValue(valueKeys[group.Key], name, new RelationalFilter(_valueAttributeType, RelationalOperator.EQ, group.Key), null, (int)group.Value, this));
        }
      }

      foreach (var gf in genredFilters.Values)
        result.Add(gf);

      foreach (var ugf in ungenredFilters)
        result.Add(ugf);

      if (numEmptyEntries > 0)
        result.Insert(0, new FilterValue(Consts.RES_VALUE_EMPTY_TITLE, new EmptyFilter(_valueAttributeType), null, numEmptyEntries, this));

      return result;
    }

    public override Task<ICollection<FilterValue>> GroupValuesAsync(ICollection<Guid> necessaryMIATypeIds, IFilter selectAttributeFilter, IFilter filter)
    {
      return Task.FromResult((ICollection<FilterValue>)null);
    }

    #endregion
  }
}
