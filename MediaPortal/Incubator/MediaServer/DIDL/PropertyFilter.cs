#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.Plugins.MediaServer.DIDL
{
  public class PropertyFilter
  {
    public bool FilterNothing { get; private set; }
    public IList<string> AllowedList { get; private set; }
    public string ElementBase { get; private set; }

    public PropertyFilter(string filter)
    {
      AllowedList = new List<string>();
      if (filter == "*" || filter == string.Empty)
      {
        FilterNothing = true;
      }
      else
      {
        var items = filter.Split(',');
        foreach (var item in items)
        {
          AllowedList.Add(item.Trim());
        }
      }
    }

    public bool IsAllowed(string item)
    {
      item = ElementBase + item;
      return FilterNothing || AllowedList.Contains(item);
    }

    public PropertyFilter CloneWithElementBase(string namespaceBase, string elementBase)
    {
      if (namespaceBase != string.Empty) namespaceBase += ":";
      var filter = new PropertyFilter(string.Empty)
                     {
                       ElementBase = namespaceBase + elementBase,
                       AllowedList = AllowedList,
                       FilterNothing = FilterNothing
                     };
      return filter;
    }
  }
}