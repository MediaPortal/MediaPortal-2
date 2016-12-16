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
using System.Linq;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.News.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.Common.General;

namespace MediaPortal.UiComponents.News.Models
{
  public class NewsSetupModel : IWorkflowModel
  {
    #region Consts

    public const string NEWS_SETUP_MODEL_ID_STR = "138253FF-FF43-4732-AA29-F69C8B288342";
    public readonly static Guid NEWS_SETUP_MODEL_ID = new Guid(NEWS_SETUP_MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected readonly AbstractProperty _hasChanges = new WProperty(typeof(bool), false);
    protected readonly ItemsList _feeds = new ItemsList();
    protected readonly AbstractProperty _newFeedBookmark = new WProperty(typeof(FeedBookmarkItem), new FeedBookmarkItem());

    #endregion

    #region Public properties - Bindable Data

    /// <summary>
    /// Exposes the list of configured feeds to the skin.
    /// </summary>
    public ItemsList Feeds
    {
      get { return _feeds; }
    }

    public AbstractProperty NewFeedBookmarkProperty
    {
      get { return _newFeedBookmark; }
    }

    /// <summary>
    /// Eposes a FeedBookmark to the skin to be used for the Add dialog.
    /// </summary>
    public FeedBookmarkItem NewFeedBookmark
    {
      get { return (FeedBookmarkItem) _newFeedBookmark.GetValue(); }
      set { _newFeedBookmark.SetValue(value); }
    }

    public AbstractProperty HasChangesProperty
    {
      get { return _hasChanges; }
    }

    /// <summary>
    /// Exposes a boolean indicating if there were changes that need saving.
    /// </summary>
    public bool HasChanges
    {
      get { return (bool) _hasChanges.GetValue(); }
      set { _hasChanges.SetValue(value); }
    }

    #endregion

    #region Public methods - Commands

    public void AddNewFeed()
    {
      Feeds.Add(NewFeedBookmark);
      NewFeedBookmark = new FeedBookmarkItem();
      HasChanges = true;
      Feeds.FireChange();
    }

    public void DeleteSelectedFeeds()
    {
      var feedsToDelete = Feeds.Where(f => f.Selected).ToList();
      if (feedsToDelete.Count > 0)
      {
        feedsToDelete.ForEach(d => Feeds.Remove(d));
        HasChanges = true;
        Feeds.FireChange();
      }
    }

    public void Select(ListItem item)
    {
      item.Selected = !item.Selected;
    }

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      if (HasChanges)
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        NewsSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NewsSettings>();
        // Apply new feeds list
        lock (settings.FeedsList)
        {
          settings.FeedsList.Clear();
          foreach (FeedBookmarkItem item in Feeds) settings.FeedsList.Add(new FeedBookmark { Name = item.Name, Url = item.Url });
        }
        // Save
        settingsManager.Save(settings);
      }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return NEWS_SETUP_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Load settings
      NewsSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NewsSettings>();
      HasChanges = false;
      NewFeedBookmark = new FeedBookmarkItem();
      Feeds.Clear();
      lock (settings.FeedsList)
      {
        if (settings.FeedsList.Count == 0)
        {
          foreach (var feed in NewsSettings.GetDefaultRegionalFeeds())
            Feeds.Add(new FeedBookmarkItem { Name = feed.Name, Url = feed.Url });
        }
        else
        {
          foreach (var feed in settings.FeedsList)
            Feeds.Add(new FeedBookmarkItem { Name = feed.Name, Url = feed.Url });
        }
      }
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // If changes were made, call refresh of the feeds
      if (HasChanges)
      {
        INewsCollector newsCollector = ServiceRegistration.Get<INewsCollector>(false);
        if (newsCollector != null)
        {
          newsCollector.RefreshNow();
        }
        HasChanges = false;
      }
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Nothing to do here
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
  }
}
