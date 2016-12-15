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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Common.Localization;

namespace MediaPortal.UiComponents.News.Models
{
  public class NewsModel : IWorkflowModel
  {
    #region Consts

    public const string NEWS_MODEL_ID_STR = "D5B308C1-4585-4051-AB78-E10D17C3CC2D";
    public readonly static Guid NEWS_MODEL_ID = new Guid(NEWS_MODEL_ID_STR);

    public const string WORKFLOWSTATEID_NEWSFEEDS_STR = "7A8AB062-07E9-4727-B9C5-05A65CDD6F12";
    public readonly static Guid WORKFLOWSTATEID_NEWSFEEDS = new Guid(WORKFLOWSTATEID_NEWSFEEDS_STR);

    public const string WORKFLOWSTATEID_NEWSITEMS_STR = "380B17A6-010E-4BB2-B79C-965CC3F8EFDD";
    public readonly static Guid WORKFLOWSTATEID_NEWSITEMS = new Guid(WORKFLOWSTATEID_NEWSITEMS_STR);

    public const string WORKFLOWSTATEID_NEWSITEMDETAIL_STR = "1116DED3-49F2-41FF-B234-D004AB1AB1B2";
    public readonly static Guid WORKFLOWSTATEID_NEWSITEMDETAIL = new Guid(WORKFLOWSTATEID_NEWSITEMDETAIL_STR);

    #endregion

    #region Protected fields

    protected readonly AbstractProperty _isUpdating = new WProperty(typeof(bool), false);
    protected readonly ItemsList _feeds = new ItemsList();
    protected NewsFeed _selectedFeed;
    protected NewsItem _selectedItem;

    #endregion

    #region Public properties - Bindable Data

    /// <summary>
    /// Exposes the loaded feeds to the skin.
    /// </summary>
    public ItemsList Feeds
    {
      get { return _feeds; }
    }

    /// <summary>
    /// Exposes the currently selected news feed to the skin.
    /// </summary>
    public NewsFeed SelectedFeed
    {
      get { return _selectedFeed; }
    }

    /// <summary>
    /// Exposes the currently selected news item to the skin.
    /// </summary>
    public NewsItem SelectedItem
    {
      get { return _selectedItem; }
    }

    /// <summary>
    /// Exposes info about a currently running background data refresh to the skin.
    /// </summary>
    public bool IsUpdating
    {
      get { return (bool)_isUpdating.GetValue(); }
      set { _isUpdating.SetValue(value); }
    }
    public AbstractProperty IsUpdatingProperty { get { return _isUpdating; } }

    #endregion

    #region Public methods - Commands

    public void Select(ListItem item)
    {
      if (item == null)
        return;

      var feed = item as NewsFeed;
      if (feed != null)
      {
        _selectedFeed = feed;

        ServiceRegistration.Get<IWorkflowManager>().NavigatePush(WORKFLOWSTATEID_NEWSITEMS, new NavigationContextConfig
        {
          NavigationContextDisplayLabel = feed.Title
        });
      }
      else
      {
        var feedItem = item as NewsItem;
        if (feedItem != null)
        {
          _selectedItem = feedItem;
          ServiceRegistration.Get<IWorkflowManager>().NavigatePush(WORKFLOWSTATEID_NEWSITEMDETAIL, new NavigationContextConfig
          {
            NavigationContextDisplayLabel = feedItem.PublishDate.ToString("g", ServiceRegistration.Get<ILocalization>().CurrentCulture)
          });
        }
      }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return NEWS_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      GetFeeds();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      StopListenToDataRefresh();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      if (!push)
      {
        if (oldContext.WorkflowState.StateId == WORKFLOWSTATEID_NEWSITEMS)
        {
          _selectedFeed = null;
        }
        else if (oldContext.WorkflowState.StateId == WORKFLOWSTATEID_NEWSITEMDETAIL)
        {
          _selectedItem = null;
        }
      }
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion

    #region Private members

    void GetFeeds()
    {
      INewsCollector newsCollector = ServiceRegistration.Get<INewsCollector>(false);
      if (newsCollector != null)
      {
        SetNewFeeds(newsCollector, false);
        StartListenToDataRefresh(newsCollector);
      }
    }

    private void SetNewFeeds(INewsCollector newsCollector, bool fireChanged)
    {
      var allFeeds = newsCollector.GetAllFeeds();
      _feeds.Clear();
      allFeeds.ForEach(f => _feeds.Add(f));
      if (fireChanged)
        _feeds.FireChange();
    }

    void StartListenToDataRefresh(INewsCollector newsCollector)
    {
      newsCollector.RefeshStarted += NewsDataRefeshStarted;
      newsCollector.RefeshFinished += NewsDataRefeshFinished;
      IsUpdating = newsCollector.IsRefeshing;
    }

    void StopListenToDataRefresh()
    {
      INewsCollector newsCollector = ServiceRegistration.Get<INewsCollector>(false);
      if (newsCollector != null)
      {
        newsCollector.RefeshStarted -= NewsDataRefeshStarted;
        newsCollector.RefeshFinished -= NewsDataRefeshFinished;
      }
    }

    void NewsDataRefeshStarted()
    {
      IsUpdating = true;
    }

    void NewsDataRefeshFinished(INewsCollector newsCollector)
    {
      IsUpdating = false;
      SetNewFeeds(newsCollector, true);
    }

    #endregion
  }
}
