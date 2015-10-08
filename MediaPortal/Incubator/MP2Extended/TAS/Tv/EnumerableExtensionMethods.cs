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