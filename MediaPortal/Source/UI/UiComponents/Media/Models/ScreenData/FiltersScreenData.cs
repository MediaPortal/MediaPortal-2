#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Views;
using MediaPortal.UiComponents.Media.FilterCriteria;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public class FiltersScreenData : AbstractScreenData
  {
    protected View _baseView;
    protected MLFilterCriterion _filterCriterion;

    public FiltersScreenData(string screen, string menuItemLabel, MLFilterCriterion filterCriterion) :
        base(screen, menuItemLabel)
    {
      _filterCriterion = filterCriterion;
    }

    public override void CreateScreenData(NavigationData navigationData)
    {
      base.CreateScreenData(navigationData);
      _baseView = navigationData.BaseView;
      StackedFiltersMLVS sfmlvs = _baseView.Specification as StackedFiltersMLVS;
      if (sfmlvs == null)
      {
        ServiceRegistration.Get<ILogger>().Error("FilterScreenData: Wrong type of media library view '{0}'", _baseView.Specification);
        return;
      }
      ICollection<AbstractScreenData> remainingScreens = new List<AbstractScreenData>(navigationData.AvailableScreens);
      remainingScreens.Remove(this);
      CreateFilterValuesList(sfmlvs, remainingScreens);
    }

    public override void ReleaseScreenData()
    {
      base.ReleaseScreenData();
      _baseView = null;
    }

    /// <summary>
    /// Updates the GUI data for a filter values selection screen which reflects the available filter values of
    /// the given view specification <paramref name="currentVS"/> for our <see cref="_filterCriterion"/>.
    /// </summary>
    /// <remarks>
    /// Updates the properties <see cref="AbstractScreenData.Items"/>, <see cref="AbstractScreenData.IsItemsEmpty"/>,
    /// <see cref="AbstractScreenData.IsItemsValid"/> and <see cref="AbstractScreenData.TooManyTimesProperty"/>.
    /// </remarks>
    /// <param name="currentVS">View specification of the view to be filtered in the current screen.</param>
    /// <param name="remainingScreens">Collection of remaining dynamic screens for the next navibation states.</param>
    protected void CreateFilterValuesList(StackedFiltersMLVS currentVS, ICollection<AbstractScreenData> remainingScreens)
    {
      ItemsList items = new ItemsList();

      try
      {
        List<FilterValue> filterValues = new List<FilterValue>(_filterCriterion.GetAvailableValues(currentVS.NecessaryMIATypeIds,
            BooleanCombinationFilter.CombineFilters(BooleanOperator.And, currentVS.Filters)));
        if (filterValues.Count > MAX_NUM_ITEMS)
        {
          // TODO: Cluster results
          IsItemsValid = true;
          IsItemsEmpty = false;
          TooManyItems = true;
          NumItemsStr = Utils.BuildNumItemsStr(filterValues.Count);
        }
        else
        {
          filterValues.Sort((f1, f2) => string.Compare(f1.Title, f2.Title));
          foreach (FilterValue filterValue in filterValues)
          {
            string filterTitle = filterValue.Title;
            StackedFiltersMLVS subVS = currentVS.CreateSubViewSpecification(filterTitle, filterValue.Filter);
            ListItem filterValueItem = new FilterItem(filterTitle)
              {
                  Command = new MethodDelegateCommand(() =>
                      {
                        WorkflowState newState = WorkflowState.CreateTransientState(filterTitle, filterTitle, false, null, false,
                            WorkflowType.Workflow);
                        NavigationData newNavigationData = new NavigationData(filterTitle, newState.StateId, subVS.BuildRootView(),
                            remainingScreens.FirstOrDefault(), remainingScreens);
                        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
                        workflowManager.NavigatePushTransient(newState, new NavigationContextConfig
                          {
                            AdditionalContextVariables = new Dictionary<string, object>
                              {
                                {MediaModel.NAVIGATION_DATA_KEY, newNavigationData}
                              }
                          });
                      })
              };
            if (filterValue.HasNumItems)
              filterValueItem.SetLabel(Consts.NUM_ITEMS_KEY, filterValue.NumItems.ToString());
            items.Add(filterValueItem);
          }
          IsItemsValid = true;
          IsItemsEmpty = items.Count == 0;
          TooManyItems = false;
          NumItemsStr = Utils.BuildNumItemsStr(items.Count);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("MediaModel: Error creating filter values list", e);
        IsItemsValid = false;
        IsItemsEmpty = true;
        TooManyItems = false;
        NumItemsStr = string.Empty;
        throw;
      }
      _items = items;
    }
  }
}