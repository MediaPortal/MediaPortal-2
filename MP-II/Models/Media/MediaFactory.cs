#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Media.MediaManager;
using MediaPortal.Presentation.DataObjects;

namespace Models.Media
{
  public class MediaFactory
  {

    #region shares
    public static IRootContainer GetItemForFolder(string folder)
    {
      IList<IAbstractMediaItem> itemsInView = ServiceScope.Get<IMediaManager>().GetView(folder);
      if (itemsInView == null)
        return null;
      foreach (IAbstractMediaItem item in itemsInView)
      {
        IRootContainer container = item as IRootContainer;
        if (container != null)
        {
          return container;
        }
        IMediaItem mediaItem = item as IMediaItem;
        if (mediaItem != null)
        {
          return mediaItem.Parent;
        }
      }
      return null;
    }

    public static void LoadItems(ItemsCollection items, IRootContainer parentItem)
    {
      if (parentItem == null)
        ServiceScope.Get<ILogger>().Info("Get root");
      else
        ServiceScope.Get<ILogger>().Info("Get {0}", parentItem.FullPath);
      IList<IAbstractMediaItem> itemsInView = ServiceScope.Get<IMediaManager>().GetView(parentItem);
      if (itemsInView == null)
        return;
      items.Clear();
      if (parentItem != null)
      {
        ContainerItem containerItem = new ContainerItem(parentItem.Parent);
        containerItem.Add("Name", "..");
        containerItem.Add("Size", "");
        containerItem.Add("Date", "");
        containerItem.Add("defaulticon", "DefaultFolderBig.png");
        containerItem.Add("CoverArt", "");
        items.Add(containerItem);
      }
      foreach (IAbstractMediaItem item in itemsInView)
      {
        IRootContainer container = item as IRootContainer;
        if (container != null)
        {
          ContainerItem containerItem = new ContainerItem(container);
          containerItem.Add("Name", container.Title);
          containerItem.Add("Size", "");
          containerItem.Add("Date", "");
          containerItem.Add("defaulticon", "DefaultFolderBig.png");

          if (item.MetaData != null && item.MetaData.ContainsKey("CoverArt"))
          {
            containerItem.Add("CoverArt", item.MetaData["CoverArt"].ToString());
          }
          else
          {
            containerItem.Add("CoverArt", "");
          }

          if (containerItem.MediaContainer != null)
          {
            containerItem.MediaContainer.Parent = parentItem;
          }
          items.Add(containerItem);
          continue;
        }
        IMediaItem mediaItem = item as IMediaItem;
        if (mediaItem != null)
        {
          AddItem(items, mediaItem);
          continue;
        }
      }
      /*
            foreach (ListItem listItem in items)
            {
              ContainerItem container = listItem as ContainerItem;
              if (container != null)
              {
                if (container.MediaContainer == null)
                  ServiceScope.Get<ILogger>().Info("  + [root]  -  [root]");
                else
                  if (container.MediaContainer.Parent != null)
                    ServiceScope.Get<ILogger>().Info("  + {0}  -  {1}", container.MediaContainer.Title, container.MediaContainer.Parent.FullPath);
                  else
                    ServiceScope.Get<ILogger>().Info("  + {0}  -  [root]", container.MediaContainer.Title);
              }
              MediaItem mitem = listItem as MediaItem;
              if (mitem != null)
              {
                if (mitem.Item.Parent != null)
                  ServiceScope.Get<ILogger>().Info("  - {0}  -  {1}", mitem.Item.Title, mitem.Item.Parent.FullPath);
                else
                  ServiceScope.Get<ILogger>().Info("  - {0}  -  ", mitem.Item.Title);
              }
            }
      */
    }


    #endregion

    #region helper methods

    private static void AddItem(ICollection<ListItem> songs, IMediaItem mediaItem)
    {
      IDictionary<string, object> metadata = mediaItem.MetaData;
      MediaItem newItem = new MediaItem(mediaItem);
      newItem.Add("Name", mediaItem.Title);
      IEnumerator<KeyValuePair<string, object>> enumer = metadata.GetEnumerator();
      while (enumer.MoveNext())
      {
        if (enumer.Current.Value != null)
        {
          if (enumer.Current.Key == "Size")
          {
            newItem.Add(enumer.Current.Key, FileUtil.GetSize((long)enumer.Current.Value));
          }
          else if (enumer.Current.Value.GetType() == typeof(DateTime))
          {
            newItem.Add(enumer.Current.Key, ((DateTime)(enumer.Current.Value)).ToString("yyyy-MM-dd hh:mm:ss"));
          }
          else
          {
            newItem.Add(enumer.Current.Key, enumer.Current.Value.ToString());
          }
        }
      }

      newItem.Add("defaulticon", "defaultVideoBig.png");

      songs.Add(newItem);
    }

    #endregion

  }
}
