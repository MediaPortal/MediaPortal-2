#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.FilterCriteria;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models
{
  public class MediaUserFilterModel
  {
    #region Consts

    public const string MF_MODEL_ID_STR = "72188C42-212E-449C-B30B-9DDDB915BEF8";
    public static Guid MF_MODEL_ID = new Guid(MF_MODEL_ID_STR);

    #endregion

    protected readonly ItemsList _filterItemsList = new ItemsList();
    protected readonly AbstractProperty _filterAvailableProperty = new WProperty(typeof(bool), false);
    protected readonly AbstractProperty _saveAsNameProperty = new WProperty(typeof(string), string.Empty);
    protected List<FilterValue> _filters;

    protected NavigationData GetCurrentNavigationData()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false);
    }

    protected void UpdateFiltersList()
    {
      NavigationData navigationData = GetCurrentNavigationData();
      var filterSpec = navigationData.BaseViewSpecification as Views.MediaLibraryQueryViewSpecification;
      FilterAvailable = filterSpec != null;

      var userFilterHandler = new UserFilterHandler();
      if (!userFilterHandler.GetSavedUserFilters(out _filters))
        return;

      UpdateFilterItemsList();
    }

    private void UpdateFilterItemsList()
    {
      _filterItemsList.Clear();
      foreach (var filterValue in _filters)
      {
        var filterCopy = filterValue;
        ListItem screenItem = new ListItem(Consts.KEY_NAME, filterValue.Title)
        {
          Selected = true,
          AdditionalProperties = { { "Filter", filterCopy } }
        };
        _filterItemsList.Add(screenItem);
      }
      _filterItemsList.FireChange();
    }

    public void SaveFilters()
    {
      var userFilterHandler = new UserFilterHandler();
      _filters = _filterItemsList.Where(item => item.Selected).Select(item => item.AdditionalProperties["Filter"]).Cast<FilterValue>().ToList();
      userFilterHandler.SaveUserFilters(_filters);
      UpdateFilterItemsList();
    }

    public void AddFilter()
    {
      NavigationData navigationData = GetCurrentNavigationData();
      var filterSpec = navigationData.BaseViewSpecification as Views.MediaLibraryQueryViewSpecification;
      if (filterSpec == null)
        return;

      FilterValue newFilter = new FilterValue(SaveAsName, filterSpec.Filter, null, null);
      _filters.Add(newFilter);
      UpdateFilterItemsList();
    }

    #region Members to be accessed from the GUI

    public AbstractProperty FilterAvailableProperty
    {
      get { return _filterAvailableProperty; }
    }

    public bool FilterAvailable
    {
      get { return (bool)_filterAvailableProperty.GetValue(); }
      set { _filterAvailableProperty.SetValue(value); }
    }

    public AbstractProperty SaveAsNameProperty
    {
      get { return _saveAsNameProperty; }
    }

    public string SaveAsName
    {
      get { return (string)_saveAsNameProperty.GetValue(); }
      set { _saveAsNameProperty.SetValue(value); }
    }

    public ItemsList FilterItemsList
    {
      get
      {
        UpdateFiltersList();
        return _filterItemsList;
      }
    }

    #endregion
  }
}
