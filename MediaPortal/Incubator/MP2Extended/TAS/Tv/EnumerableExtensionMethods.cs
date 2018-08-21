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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.TAS.Tv
{
  internal static class IEnumerableExtensionMethods
  {
    public static IEnumerable<T> SortChannelList<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebChannelBasic
    {
      switch (sortInput)
      {
        case WebSortField.Title:
          return list.OrderBy(x => x.Title, orderInput);
        case WebSortField.NaturalTitle:
          return list.OrderByNatural(x => x.Title, orderInput);
        case WebSortField.User:
        default:
          // There are two ways to order channels in MediaPortal:
          // - The SortOrder property of a channel (SortOrder field in channel table)
          // - The order in which the channels are in a group (SortOrder field in GroupMap table). This isn't exposed as a property
          //   somehwere, we just get the items in this order from TvBusinessLayer and have to deal with it. 
          // While using the first makes more sense from a programmers POV, the user expects the second one, so let's use that
          // one here, which means that we don't sort. 

          if (orderInput.HasValue && orderInput.Value == WebSortOrder.Desc)
          {
            return list.Reverse();
          }
          return list;
      }
    }

    public static IEnumerable<T> SortGroupList<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebChannelGroup
    {
      switch (sortInput)
      {
        case WebSortField.Title:
          return list.OrderBy(x => x.GroupName, orderInput);
        case WebSortField.NaturalTitle:
          return list.OrderByNatural(x => x.GroupName, orderInput);
        case WebSortField.User:
        default:
          return list.OrderBy(x => x.SortOrder, orderInput);
      }
    }

    public static IEnumerable<T> SortScheduleList<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebScheduleBasic
    {
      switch (sortInput)
      {
        case WebSortField.Channel:
          return list.OrderBy(x => x.ChannelId, orderInput);
        case WebSortField.StartTime:
          return list.OrderBy(x => x.StartTime, orderInput);
        case WebSortField.NaturalTitle:
          return list.OrderByNatural(x => x.Title, orderInput);
        case WebSortField.Title:
        default:
          return list.OrderBy(x => x.Title, orderInput);
      }
    }

    public static IEnumerable<T> SortScheduledRecordingList<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebScheduledRecording
    {
      switch (sortInput)
      {
        case WebSortField.Channel:
          return list.OrderBy(x => x.ChannelId, orderInput);
        case WebSortField.StartTime:
          return list.OrderBy(x => x.StartTime, orderInput);
        case WebSortField.NaturalTitle:
          return list.OrderByNatural(x => x.Title, orderInput);
        case WebSortField.Title:
        default:
          return list.OrderBy(x => x.Title, orderInput);
      }
    }

    public static IEnumerable<T> SortRecordingList<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebRecordingBasic
    {
      switch (sortInput)
      {
        case WebSortField.Channel:
          return list.OrderBy(x => x.ChannelId, orderInput);
        case WebSortField.StartTime:
        case WebSortField.DateAdded:
          return list.OrderBy(x => x.StartTime, orderInput);
        case WebSortField.NaturalTitle:
          return list.OrderByNatural(x => x.Title, orderInput);
        case WebSortField.Title:
        default:
          return list.OrderBy(x => x.Title, orderInput);
      }
    }
  }
}
