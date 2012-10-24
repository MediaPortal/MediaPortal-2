using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.UI.Presentation.Models;
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
      get { return (FeedBookmarkItem)_newFeedBookmark.GetValue(); }
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
      get { return (bool)_hasChanges.GetValue(); }
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
        settings.FeedsList.Clear();
        foreach (FeedBookmarkItem item in Feeds) settings.FeedsList.Add(new FeedBookmark() { Name = item.Name, Url = item.Url });
        // save
        settingsManager.Save(settings);
      }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return NEWS_SETUP_MODEL_ID; }
    }

    public bool CanEnterState(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
      // Load settings
      NewsSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NewsSettings>();
      HasChanges = false;
      NewFeedBookmark = new FeedBookmarkItem();
      Feeds.Clear();
      foreach (var feed in settings.FeedsList)
        Feeds.Add(new FeedBookmarkItem() { Name = feed.Name, Url = feed.Url });
    }

    public void ExitModelContext(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
      // TODO : if changes were made, call refresh of the feeds
    }

    public void ChangeModelContext(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext, bool push)
    {
      // Nothing to do here
    }

    public void Deactivate(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void UpdateMenuActions(UI.Presentation.Workflow.NavigationContext context, IDictionary<Guid, UI.Presentation.Workflow.WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(UI.Presentation.Workflow.NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
