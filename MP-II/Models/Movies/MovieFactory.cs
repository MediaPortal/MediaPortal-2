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
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.MetaData;
using MediaPortal.Presentation.DataObjects;

namespace Models.Movies
{
  public class MovieFactory
  {

    #region shares
    /// <summary>
    /// Loads the movies.
    /// </summary>
    /// <param name="movies">The movies.</param>
    /// <param name="mapping">The mapping.</param>
    /// <param name="sortMethod">The sort method.</param>
    /// <param name="folder">The folder.</param>
    public void LoadMovies(ref ItemsCollection movies, ref IMetaDataMappingCollection mapping, int sortMethod, string folder)
    {
      movies.Clear();
      IList<IAbstractMediaItem> items = ServiceScope.Get<IMediaManager>().GetView(folder);
      if (items == null) return;
      if (items.Count == 0) return;

      if (!String.IsNullOrEmpty(folder))
      {
        IAbstractMediaItem abstractItem = items[0];
        abstractItem = abstractItem.Parent;
        if (abstractItem != null)
        {
          if (abstractItem.Parent != null)
          {
            FolderItem parentItem = new FolderItem(abstractItem.Parent);
            Dictionary<string, object> metaData = new Dictionary<string, object>();
            metaData["title"] = "..";
            metaData["defaulticon"] = "DefaultFolderBackBig.png";
            MapMetaData(mapping, sortMethod, metaData, abstractItem.Parent, parentItem);
            movies.Add(parentItem);
          }
        }
      }
      foreach (IAbstractMediaItem abstractItem in items)
      {
        if (abstractItem.Mapping != null)
        {
          mapping = abstractItem.Mapping;
          if (sortMethod >= mapping.Mappings.Count) sortMethod = 0;
        }
        IRootContainer rootContainer = abstractItem as IRootContainer;
        if (rootContainer != null)
        {
          FolderItem rootItem = new FolderItem(rootContainer);
          Dictionary<string, object> metaData = new Dictionary<string, object>();
          metaData["defaulticon"] = "DefaultFolderBig.png";
          MapMetaData(mapping, sortMethod, metaData, rootContainer, rootItem);
          movies.Add(rootItem);
        }
        else
        {
          IMediaItem mediaItem = abstractItem as IMediaItem;
          if (mediaItem != null)
          {
            AddMediaItem(mapping, sortMethod, ref movies, mediaItem);
          }
        }
      }
    }

    /// <summary>
    /// Loads the movies.
    /// </summary>
    /// <param name="movies">The movies.</param>
    /// <param name="currentFolder">The current folder.</param>
    /// <param name="mapping">The mapping.</param>
    /// <param name="sortMethod">The sort method.</param>
    public void LoadMovies(ref ItemsCollection movies, FolderItem currentFolder, ref IMetaDataMappingCollection mapping, int sortMethod)
    {
      movies.Clear();
      if (currentFolder != null && currentFolder.MediaContainer == null)
        currentFolder = null;
      if (currentFolder == null)
      {
        IList<IRootContainer> rootContainers = ServiceScope.Get<IMediaManager>().RootContainers;
        foreach (IRootContainer rootContainer in rootContainers)
        {
          if (rootContainer.Mapping != null)
          {
            mapping = rootContainer.Mapping;
            if (sortMethod >= mapping.Mappings.Count) sortMethod = 0;
          }
          FolderItem rootItem = new FolderItem(rootContainer);
          Dictionary<string, object> metaData = new Dictionary<string, object>();
          metaData["defaulticon"] = "DefaultFolderBig.png";
          MapMetaData(mapping, sortMethod, metaData, rootContainer, rootItem);
          movies.Add(rootItem);
        }
      }
      else if (currentFolder.MediaContainer != null)
      {
        Dictionary<string, object> metaData = new Dictionary<string, object>();
        if (currentFolder.MediaContainer.Parent != null)
        {
          FolderItem parentItem = new FolderItem(currentFolder.MediaContainer.Parent);
          metaData["title"] = "..";
          metaData["defaulticon"] = "DefaultFolderBackBig.png";
          MapMetaData(mapping, sortMethod, metaData, currentFolder.MediaContainer.Parent, parentItem);
          movies.Add(parentItem);
        }
        IList<IAbstractMediaItem> subItems = currentFolder.MediaContainer.Items;
        if (subItems != null)
        {
          foreach (IAbstractMediaItem abstractItem in subItems)
          {
            if (abstractItem.Mapping != null)
            {
              mapping = abstractItem.Mapping;
              if (sortMethod >= mapping.Mappings.Count) sortMethod = 0;
            }
            IRootContainer container = abstractItem as IRootContainer;
            if (container != null)
            {
              FolderItem newItem = new FolderItem(container);
              metaData = new Dictionary<string, object>();
              metaData["defaulticon"] = "DefaultFolderBig.png";
              MapMetaData(mapping, sortMethod, metaData, abstractItem, newItem);
              movies.Add(newItem);
              continue;
            }
            IMediaItem mediaItem = abstractItem as IMediaItem;
            if (mediaItem != null)
            {
              AddMediaItem(mapping, sortMethod, ref movies, mediaItem);
            }
          }
        }
      }
      else if (currentFolder.Root != null)
      {
        Dictionary<string, object> metaData = new Dictionary<string, object>();
        metaData["title"] = "..";
        metaData["defaulticon"] = "DefaultFolderBackBig.png";
        FolderItem parentItem = new FolderItem();
        MapMetaData(mapping, sortMethod, metaData, currentFolder.Root, parentItem);
        movies.Add(parentItem);

        IList<IAbstractMediaItem> containers = currentFolder.Root.Items;
        foreach (IRootContainer container in containers)
        {
          metaData = new Dictionary<string, object>();
          metaData["defaulticon"] = "DefaultFolderBig.png";

          FolderItem newItem = new FolderItem(container);
          MapMetaData(mapping, sortMethod, metaData, container, newItem);
          movies.Add(newItem);
        }
      }
    }

    #endregion

    #region helper methods
    private static void MapMetaData(IMetaDataMappingCollection mapping, int sortMethod, IDictionary<string, object> localMetaData, IAbstractMediaItem mediaItem, ListItem newItem)
    {
      IDictionary<string, object> metadata;
      if (mediaItem == null)
      {
        metadata = localMetaData;
      }
      else
      {
        metadata = mediaItem.MetaData;
      }
      if (sortMethod >= mapping.Mappings.Count)
        sortMethod = 0;
      if (mapping != null)
      {
        foreach (IMetadataMappingItem item in mapping.Mappings[sortMethod].Items)
        {
          if (localMetaData != null && localMetaData.ContainsKey(item.MetaDataField))
          {
            string text = item.Formatter.Format(localMetaData[item.MetaDataField], item.Formatting);
            if (newItem.Contains(item.SkinLabel))
            {
              newItem.Labels.Remove(item.SkinLabel);
            }
            newItem.Add(item.SkinLabel, text);
          }
          else if (metadata.ContainsKey(item.MetaDataField))
          {
            string text = item.Formatter.Format(metadata[item.MetaDataField], item.Formatting);
            if (newItem.Contains(item.SkinLabel))
            {
              newItem.Labels.Remove(item.SkinLabel);
            }
            newItem.Add(item.SkinLabel, text);
          }
        }
      }
      if (localMetaData.ContainsKey("defaulticon"))
      {
        newItem.Add("defaulticon", localMetaData["defaulticon"].ToString());
      }
    }
    private static void AddMediaItem(IMetaDataMappingCollection mapping, int sortMethod, ref ItemsCollection movies, IMediaItem mediaItem)
    {
      Uri uri = mediaItem.ContentUri;
      if (uri != null)
      {
        string filename = uri.AbsolutePath.ToLower();
        string ext = System.IO.Path.GetExtension(filename);
        if (ext != "")
        {
          if (!IsMovie(filename))
          {
            return;
          }
        }
        else if (mediaItem.MetaData.ContainsKey("MimeType"))
        {
          string mimeType = mediaItem.MetaData["MimeType"] as string;
          if (mimeType == null) return;
          if (mimeType.Contains("video") == false) return;
        }
      }

      IDictionary<string, object> metadata = mediaItem.MetaData;

      MovieItem newItem = new MovieItem(mediaItem);
      newItem.Add("Name", mediaItem.Title);
      newItem.Add("CoverArt", "defaultVideoBig.png");
      if (mediaItem.ContentUri != null)
      {
        if (mediaItem.ContentUri.IsFile)
        {
          long size;
          DateTime creationTime;
          string textSize = FileUtil.GetFileSize(mediaItem.ContentUri.LocalPath, out  size, out  creationTime);
          newItem.DateTime = creationTime;
          newItem.Size = size;
          newItem.Add("Size", textSize);
          newItem.Add("Date", creationTime.ToString("yyyy-MM-dd hh:mm:ss"));
        }
      }
      newItem.Add("defaulticon", "defaultVideoBig.png");
      newItem.MediaItem.MetaData["defaulticon"] = "defaultVideoBig.png";

      MapMetaData(mapping, sortMethod, new Dictionary<string, object>(), mediaItem, newItem);
      movies.Add(newItem);
    }

    private static bool IsMovie(string fileName)
    {
      if (fileName.IndexOf(".mpg") >= 0)
      {
        return true;
      }
      if (fileName.IndexOf(".mpeg") >= 0)
      {
        return true;
      }
      if (fileName.IndexOf(".ts") >= 0)
      {
        return true;
      }
      if (fileName.IndexOf(".wmv") >= 0)
      {
        return true;
      }
      if (fileName.IndexOf(".avi") >= 0)
      {
        return true;
      }
      if (fileName.IndexOf(".mkv") >= 0)
      {
        return true;
      }
      if (fileName.IndexOf(".ifo") >= 0)
      {
        return true;
      }
      return false;
    }

    #endregion

  }
}
