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

using System.Threading;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Views;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public abstract class SearchScreenData : ItemsScreenData
  {
    #region Protected fields

    protected AbstractProperty _simpleSearchTextProperty;
    protected Timer _searchTimer;
    protected StackedFiltersMLVS _baseViewSpecification = null;

    #endregion

    protected SearchScreenData(string screen, string menuItemLabel, PlayableItemCreatorDelegate playableItemCreator) :
        base(screen, menuItemLabel, playableItemCreator)
    {
      _searchTimer = new Timer(OnSearchTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
    }

    public override void  CreateScreenData(NavigationData navigationData)
    {
      base.CreateScreenData(navigationData);
      InitializeSearch(navigationData.BaseViewSpecification);
    }

    public override void ReleaseScreenData()
    {
      base.ReleaseScreenData();
      StopSearch();
    }

    /// <summary>
    /// Gets the contents of the text edit field containing the current search text for the simple search.
    /// </summary>
    public string SimpleSearchText
    {
      get { return (string) _simpleSearchTextProperty.GetValue(); }
      internal set { _simpleSearchTextProperty.SetValue(value); }
    }

    public AbstractProperty SimpleSearchTextProperty
    {
      get { return _simpleSearchTextProperty; }
    }

    void OnSimpleSearchTextChanged(AbstractProperty prop, object oldValue)
    {
      _searchTimer.Change(Consts.SEARCH_TEXT_TYPE_TIMESPAN, Consts.INFINITE_TIMESPAN);
    }

    void OnSearchTimerElapsed(object sender)
    {
      if (_searchTimer == null)
        // Already disposed
        return;
      if (string.IsNullOrEmpty(SimpleSearchText))
        return;
      View view = new SimpleTextSearchViewSpecification(Consts.SIMPLE_SEARCH_VIEW_NAME_RESOURCE, SimpleSearchText,
          BooleanCombinationFilter.CombineFilters(BooleanOperator.And, _baseViewSpecification.Filters),
          _baseViewSpecification.NecessaryMIATypeIds, _baseViewSpecification.OptionalMIATypeIds, true, true).BuildView();
      ReloadMediaItems(view, false);
    }

    protected void InitializeSearch(ViewSpecification baseViewSpecification)
    {
      _baseViewSpecification = baseViewSpecification as StackedFiltersMLVS;
      if (_baseViewSpecification == null)
        return;
      if (_simpleSearchTextProperty == null)
      {
        _simpleSearchTextProperty = new WProperty(typeof(string), string.Empty);
        _simpleSearchTextProperty.Attach(OnSimpleSearchTextChanged);
      }
      SimpleSearchText = string.Empty;
      IsItemsValid = true;
      IsItemsEmpty = false;
      TooManyItems = false;
      NumItemsStr = "-";
      _items = new ItemsList();
      _searchTimer = new Timer(OnSearchTimerElapsed, null, Consts.SEARCH_TEXT_TYPE_TIMESPAN, Consts.INFINITE_TIMESPAN);
    }

    protected void StopSearch()
    {
      Timer timer = _searchTimer;
      _searchTimer = null;
      timer.Dispose();
      _simpleSearchTextProperty = null;
    }
  }
}