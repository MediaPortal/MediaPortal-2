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

using MediaPortal.UI.Presentation.Models;
using MediaPortal.Common;
using MediaPortal.Common.General;

namespace MediaPortal.UiComponents.News.Models
{
  public class CurrentNewsModel : BaseTimerControlledModel
  {
    /// <summary>
    /// Update interval for the current news item.
    /// </summary>
    const long NEWSITEM_UPDATE_INTERVAL = 10 * 1000;

    protected readonly AbstractProperty _currentNewsItemProperty = new WProperty(typeof(NewsItem), new NewsItem());

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

    public CurrentNewsModel()
      : base(true, 100)
    {
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

          // Decrease interval once we have the first item
          ChangeInterval(NEWSITEM_UPDATE_INTERVAL);
        }
      } 
    }
  }
}
