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
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.Common.General;

namespace MediaPortal.UiComponents.News.Models
{
  public class NewsFeed : ListItem
  {
    public NewsFeed()
    {
      Items = new ItemsList();
    }

    public ItemsList Items { get; protected set; }

    protected readonly AbstractProperty _title = new WProperty(typeof(string), "No Title");

    public AbstractProperty TitleProperty
    {
      get { return _title; }
    }

    public string Title
    {
      get { return (string) _title.GetValue(); }
      set { _title.SetValue(value); }
    }

    protected readonly AbstractProperty _description = new WProperty(typeof(string), "");

    public AbstractProperty DescriptionProperty
    {
      get { return _description; }
    }

    public string Description
    {
      get { return (string) _description.GetValue(); }
      set { _description.SetValue(value); }
    }

    protected readonly AbstractProperty _lastUpdated = new WProperty(typeof(DateTime), DateTime.MinValue);

    public AbstractProperty LastUpdatedProperty
    {
      get { return _lastUpdated; }
    }

    public DateTime LastUpdated
    {
      get { return (DateTime) _lastUpdated.GetValue(); }
      set { _lastUpdated.SetValue(value); }
    }

    protected readonly AbstractProperty _icon = new WProperty(typeof(string), string.Empty);

    public AbstractProperty IconProperty
    {
      get { return _icon; }
    }

    public string Icon
    {
      get { return (string) _icon.GetValue(); }
      set { _icon.SetValue(value); }
    }
  }
}
