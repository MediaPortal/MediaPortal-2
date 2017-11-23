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
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Utilities.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  #region List wrappers for templating

  public class LatestAddedListItem : MediaListItem
  {
  }

  public class LatestPlayedListItem : MediaListItem
  {
  }

  public class LatestPlayedChannelsListItem : LatestPlayedListItem
  {
  }

  public class FavoritesListItem : MediaListItem
  {
  }

  public class FavoriteChannelsListItem : FavoritesListItem
  {
  }

  public class CurrentProgramsListItem : MediaListItem
  {
  }

  public class CurrentSchedulesListItem : MediaListItem
  {
  }

  public class MediaListItem : ListItem
  {
    protected AbstractProperty _itemsProperty;

    public MediaListItem()
    {
      _itemsProperty = new WProperty(typeof(ItemsList), null);
    }

    public AbstractProperty ItemsProperty
    {
      get { return _itemsProperty; }
    }

    public ItemsList Items
    {
      get { return (ItemsList)_itemsProperty.GetValue(); }
      set { _itemsProperty.SetValue(value); }
    }
  }

  #endregion

  public class HomeMediaListModel
  {
    #region Protected classes

    protected interface IMediaListCreator
    {
      MediaListItem Create(MediaListModel mlm);
    }

    protected class MediaListCreator<T> : IMediaListCreator where T : MediaListItem, new()
    {
      protected string _listKey;

      public MediaListCreator(string listKey)
      {
        _listKey = listKey;
      }

      public MediaListItem Create(MediaListModel mlm)
      {
        ItemsList list = mlm.Lists[_listKey]?.AllItems;
        return list != null ? new T() { Items = list } : null;
      }
    }

    protected class MediaListGroup
    {
      public MediaListGroup(Guid actionId, params IMediaListCreator[] creators)
      {
        ActionId = actionId;
        Creators = new List<IMediaListCreator>(creators);
      }

      public Guid ActionId { get; protected set; }
      public IList<IMediaListCreator> Creators { get; protected set; }
    }

    #endregion

    #region Default action to list mappings

    protected static readonly MediaListGroup[] DEFAULT_LISTS_GROUPS = new[]
      {
      //Movies
      new MediaListGroup(new Guid("80d2e2cc-baaa-4750-807b-f37714153751"),
        new MediaListCreator<LatestAddedListItem>("LatestMovies"),
        new MediaListCreator<LatestPlayedListItem>("LastPlayMovies"),
        new MediaListCreator<FavoritesListItem>("FavoriteMovies")),
            
      //Audio
      new MediaListGroup(new Guid("30715d73-4205-417f-80aa-e82f0834171f"),
        new MediaListCreator<LatestAddedListItem>("LatestAudio"),
        new MediaListCreator<LatestPlayedListItem>("LastPlayAudio"),
        new MediaListCreator<FavoritesListItem>("FavoriteAudio")),
      
      //Episodes
      new MediaListGroup(new Guid("30f57cba-459c-4202-a587-09fff5098251"),
        new MediaListCreator<LatestAddedListItem>("LatestEpisodes"),
        new MediaListCreator<LatestPlayedListItem>("LastPlayEpisodes"),
        new MediaListCreator<FavoritesListItem>("FavoriteEpisodes")),
      
      //Recordings
      new MediaListGroup(new Guid("7f52d0a1-b7f8-46a1-a56b-1110bbfb7d51"),
        new MediaListCreator<LatestAddedListItem>("LatestRecordings"),
        new MediaListCreator<LatestPlayedListItem>("LastPlayRecordings"),
        new MediaListCreator<FavoritesListItem>("FavoriteRecordings")),
      
      //Images
      new MediaListGroup(new Guid("55556593-9fe9-436c-a3b6-a971e10c9d44"),
        new MediaListCreator<LatestAddedListItem>("LatestImages"),
        new MediaListCreator<LatestPlayedListItem>("LastPlayImages"),
        new MediaListCreator<FavoritesListItem>("FavoriteImages")),
      
      //Video
      new MediaListGroup(new Guid("a4df2df6-8d66-479a-9930-d7106525eb07"),
        new MediaListCreator<LatestAddedListItem>("LatestVideo"),
        new MediaListCreator<LatestPlayedListItem>("LastPlayVideo"),
        new MediaListCreator<FavoritesListItem>("FavoriteVideo")),
      
      //TV
      new MediaListGroup(new Guid("b4a9199f-6dd4-4bda-a077-de9c081f7703"),
        new MediaListCreator<LatestPlayedChannelsListItem>("LastPlayTV"),
        new MediaListCreator<FavoriteChannelsListItem>("FavoriteTV")),

      //TV Guide
      new MediaListGroup(new Guid("a298dfbe-9da8-4c16-a3ea-a9b354f3910c"),
        new MediaListCreator<CurrentProgramsListItem>("CurrentPrograms")),

      //Schedules
      new MediaListGroup(new Guid("87355e05-a15b-452a-85b8-98d4fc80034e"),
        new MediaListCreator<CurrentSchedulesListItem>("CurrentSchedules"))
      };

    #endregion

    public const string STR_MODEL_ID = "47700DBB-C5B5-4723-8055-F2641C0A37B4";
    public static readonly Guid MODEL_ID = new Guid(STR_MODEL_ID);

    protected const int UPDATE_DELAY_MS = 500;

    protected AbstractProperty _hasListsProperty;
    protected AbstractProperty _currentActionProperty;
    protected AbstractProperty _currentActionIdProperty;
    protected ItemsList _lists;
    protected IDictionary<Guid, MediaListGroup> _actionToListsMap;
    protected WorkflowAction _nextAction;
    protected DelayedEvent _updateEvent;

    public HomeMediaListModel()
    {
      Init();
      Attach();
    }

    protected void Init()
    {
      _hasListsProperty = new WProperty(typeof(bool), false);
      _currentActionProperty = new WProperty(typeof(WorkflowAction), null);
      _currentActionIdProperty = new WProperty(typeof(string), null);
      _lists = new ItemsList();
      _actionToListsMap = DEFAULT_LISTS_GROUPS.ToDictionary(mg => mg.ActionId);
      _updateEvent = new DelayedEvent(UPDATE_DELAY_MS);
      _updateEvent.OnEventHandler += OnUpdate;
    }

    protected void Attach()
    {
      GetHomeModel().CurrentSubItemProperty.Attach(OnCurrentHomeItemChanged);
    }

    public AbstractProperty HasListsProperty
    {
      get { return _hasListsProperty; }
    }

    public bool HasLists
    {
      get { return (bool)_hasListsProperty.GetValue(); }
      set { _hasListsProperty.SetValue(value); }
    }

    public AbstractProperty CurrentActionProperty
    {
      get { return _currentActionIdProperty; }
    }

    public WorkflowAction CurrentAction
    {
      get { return (WorkflowAction)_currentActionProperty.GetValue(); }
      set { _currentActionProperty.SetValue(value); }
    }

    public AbstractProperty CurrentActionIdProperty
    {
      get { return _currentActionIdProperty; }
    }

    public string CurrentActionId
    {
      get { return (string)_currentActionIdProperty.GetValue(); }
      set { _currentActionIdProperty.SetValue(value); }
    }

    public ItemsList Lists
    {
      get { return _lists; }
    }

    private void EnqueueUpdate(WorkflowAction action)
    {
      HasLists = false;
      _nextAction = action;
      _updateEvent.EnqueueEvent(this, EventArgs.Empty);
    }

    private void OnUpdate(object sender, EventArgs e)
    {
      UpdateMediaLists(_nextAction);
    }

    private void OnCurrentHomeItemChanged(AbstractProperty property, object oldValue)
    {
      WorkflowAction action;
      if (!TryGetAction(property.GetValue() as ListItem, out action))
        action = null;
      EnqueueUpdate(action);
    }

    protected void UpdateMediaLists(WorkflowAction action)
    {
      _lists.Clear();
      MediaListGroup group;
      if (action != null && _actionToListsMap.TryGetValue(action.ActionId, out group))
      {
        MediaListModel mlm = GetMediaListModel();
        foreach (IMediaListCreator creator in group.Creators)
        {
          MediaListItem item = creator.Create(mlm);
          if (item != null)
            _lists.Add(item);
        }
      }
      _lists.FireChange();
      HasLists = true;
      CurrentAction = action;
      CurrentActionId = action?.ActionId.ToString();
    }

    protected static HomeMenuModel GetHomeModel()
    {
      return GetModel<HomeMenuModel>(HomeMenuModel.MODEL_ID);
    }

    protected static MediaListModel GetMediaListModel()
    {
      return GetModel<MediaListModel>(MediaListModel.MEDIA_LIST_MODEL_ID);
    }

    protected static T GetModel<T>(Guid modelId)
    {
      return (T)ServiceRegistration.Get<IWorkflowManager>().
        GetModel(modelId);
    }

    protected static bool TryGetAction(ListItem item, out WorkflowAction action)
    {
      if (item != null)
      {
        action = item.AdditionalProperties[Consts.KEY_ITEM_ACTION] as WorkflowAction;
        return action != null;
      }
      action = null;
      return false;
    }
  }
}
