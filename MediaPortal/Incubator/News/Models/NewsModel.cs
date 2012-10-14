using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

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

        int firstWordEnd = feed.Title.IndexOf(' ');
        string newStateLabel = firstWordEnd > 0 ? feed.Title.Substring(0, firstWordEnd) : feed.Title;
        ServiceRegistration.Get<IWorkflowManager>().NavigatePush(WORKFLOWSTATEID_NEWSITEMS, new NavigationContextConfig
        {
          NavigationContextDisplayLabel = newStateLabel
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
            NavigationContextDisplayLabel = feedItem.PublishDate.ToString("g")
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

    public bool CanEnterState(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
      GetFeeds();
    }

    public void ExitModelContext(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
      StopListenToDataRefresh();
    }

    public void ChangeModelContext(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext, bool push)
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
      newsCollector.Refeshed += NewsDataRefeshed;
    }

    void StopListenToDataRefresh()
    {
      INewsCollector newsCollector = ServiceRegistration.Get<INewsCollector>(false);
      if (newsCollector != null)
      {
        newsCollector.Refeshed -= NewsDataRefeshed;
      }
    }

    void NewsDataRefeshed(INewsCollector newsCollector)
    {
      SetNewFeeds(newsCollector, true);
    }

    #endregion
  }
}
