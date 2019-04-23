#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.UI.Presentation.Models;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using System.Linq;

namespace MediaPortal.UiComponents.News.Models
{
  public class CurrentNewsModel : BaseTimerControlledModel
  {
    /// <summary>
    /// Update interval for the current news item.
    /// </summary>
    const long NEWSITEM_UPDATE_INTERVAL = 10 * 1000;
    const int NEWSITEM_TOP_COUNT = 10;

    protected readonly AbstractProperty _currentNewsItemProperty = new WProperty(typeof(NewsItem), new NewsItem());
    protected ItemsList _currentNewsItems = new ItemsList();
    protected ItemsList _currentTopNewsItems = new ItemsList();
    protected bool _inited = false;

    /// <summary>
    /// Exposes the current news item to the skin.
    /// </summary>
    public AbstractProperty CurrentNewsItemProperty
    {
      get { return _currentNewsItemProperty; }
    }

    /// <summary>
    /// Current news item
    /// </summary>
    public NewsItem CurrentNewsItem
    {
      get { return (NewsItem)_currentNewsItemProperty.GetValue(); }
      set { _currentNewsItemProperty.SetValue(value); }
    }

    /// <summary>
    /// Current news items ordered by date
    /// </summary>
    public ItemsList CurrentNewsItems
    {
      get { return _currentNewsItems; }
    }

    /// <summary>
    /// Current top news items ordered by date
    /// </summary>
    public ItemsList CurrentTopNewsItems
    {
      get { return _currentTopNewsItems; }
    }

    public CurrentNewsModel()
      : base(true, 100)
    {
      _inited = false;
    }

    protected override void Update()
    {
      INewsCollector newsCollector = ServiceRegistration.Get<INewsCollector>(false);
      if (newsCollector != null)
      {
        NewsItem newNewsItem = newsCollector.GetRandomNewsItem();
        if (newNewsItem != null)
        {
          newNewsItem.CopyTo(CurrentNewsItem);

          _currentNewsItems.Clear();
          _currentTopNewsItems.Clear();
          var items = newsCollector.GetAllNewsItems();
          if (items?.Count > 0)
          {
            foreach (var newsItem in items.OrderByDescending(i => i.PublishDate))
            {
              _currentNewsItems.Add(newsItem);
              if (_currentTopNewsItems.Count < NEWSITEM_TOP_COUNT)
                _currentTopNewsItems.Add(newsItem);
            }
          }
          _currentNewsItems.FireChange();
          _currentTopNewsItems.FireChange();

          if (!_inited)
          {
            // Decrease interval once we have the first item
            ChangeInterval(NEWSITEM_UPDATE_INTERVAL);
          }
          _inited = true;
        }
      } 
    }
  }
}
