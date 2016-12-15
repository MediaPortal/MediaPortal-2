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
using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.Views;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Utilities.Events;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public abstract class AbstractSearchScreenData : AbstractItemsScreenData
  {
    #region Protected fields

    protected AbstractProperty _simpleSearchTextProperty;
    protected DelayedEvent _delayedEvent;
    protected MediaLibraryQueryViewSpecification _baseViewSpecification = null;

    #endregion

    protected AbstractSearchScreenData(string screen, string menuItemLabel, PlayableItemCreatorDelegate playableItemCreator) :
      base(screen, menuItemLabel, null, playableItemCreator, false)
    {
      _delayedEvent = new DelayedEvent(Consts.TS_SEARCH_TEXT_TYPE.TotalMilliseconds);
      _delayedEvent.OnEventHandler += OnSearchTimerElapsed;
    }

    public override string MoreThanMaxItemsHint
    {
      get { return LocalizationHelper.Translate(Consts.RES_MORE_THAN_MAX_ITEMS_SEARCH_RESULT_HINT, Consts.MAX_NUM_ITEMS_VISIBLE); }
    }

    public override string ListBeingBuiltHint
    {
      get { return Consts.RES_SEARCH_RESULT_BEING_BUILT_HINT; }
    }

    public override void CreateScreenData(NavigationData navigationData)
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
      get { return (string)_simpleSearchTextProperty.GetValue(); }
      internal set { _simpleSearchTextProperty.SetValue(value); }
    }

    public AbstractProperty SimpleSearchTextProperty
    {
      get { return _simpleSearchTextProperty; }
    }

    public override void Reload()
    {
      DoSearch();
    }

    protected override IEnumerable<MediaItem> GetAllMediaItemsOverride()
    {
      return BuildAllItemsView().MediaItems;
    }

    void OnSimpleSearchTextChanged(AbstractProperty prop, object oldValue)
    {
      _delayedEvent.EnqueueEvent(this, EventArgs.Empty);
    }

    void OnSearchTimerElapsed(object sender, EventArgs eventArgs)
    {
      DoSearch();
    }

    protected void DoSearch()
    {
      if (string.IsNullOrEmpty(SimpleSearchText))
        return;
      View view = BuildAllItemsView();
      ReloadMediaItems(view, false);
    }

    protected virtual string GetSearchTerm()
    {
      // TODO: also support "starts with"
      return string.Format("%{0}%", SimpleSearchText);
    }

    protected virtual IFilter BuildTextSearchFilter()
    {
      var filter = new LikeFilter(MediaAspect.ATTR_TITLE, GetSearchTerm(), null);
      return filter;
    }

    protected View BuildAllItemsView()
    {
      return new SimpleTextSearchViewSpecification(Consts.RES_SIMPLE_SEARCH_VIEW_NAME, BuildTextSearchFilter(),
        _baseViewSpecification.NecessaryMIATypeIds, _baseViewSpecification.OptionalMIATypeIds,
          true, true).BuildView();
    }

    protected void InitializeSearch(ViewSpecification baseViewSpecification)
    {
      _baseViewSpecification = baseViewSpecification as MediaLibraryQueryViewSpecification;
      if (_baseViewSpecification == null)
        return;
      if (_simpleSearchTextProperty == null)
      {
        _simpleSearchTextProperty = new WProperty(typeof(string), string.Empty);
        _simpleSearchTextProperty.Attach(OnSimpleSearchTextChanged);
      }
      SimpleSearchText = string.Empty;

      // Initialize data manually which would have been initialized by AbstractItemsScreenData.UpdateMediaItems else
      IsItemsValid = true;
      IsItemsEmpty = false;
      TooManyItems = false;
      NumItemsStr = "-";
      NumItems = 0;
      lock (_syncObj)
        _view = null;
      _items = new ItemsList();
    }

    protected void StopSearch()
    {
      _simpleSearchTextProperty = null;
    }
  }
}
