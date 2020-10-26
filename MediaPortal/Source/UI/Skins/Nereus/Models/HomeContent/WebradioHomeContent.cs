#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class WebradioHomeContent : AbstractHomeContent
  {
    public WebradioHomeContent()
    {
      
    }
    protected override void PopulateBackingList()
    {
      var alm = GetWebradioListModel();

      _backingList.Add(new LastPlayedWebradioList(alm.Lists["LastPlayedWebradio"].AllItems));
      _backingList.Add(new TopPlayedWebradioList(alm.Lists["TopPlayedWebradio"].AllItems));
    }
  }

  public class LastPlayedWebradioList : ItemsListWrapper
  {
    public LastPlayedWebradioList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.LatestPlayed]")
    { }
  }

  public class TopPlayedWebradioList : ItemsListWrapper
  {
    public TopPlayedWebradioList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Favorites]")
    { }
  }
}
