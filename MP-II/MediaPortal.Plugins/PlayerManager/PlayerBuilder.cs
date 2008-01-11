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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Players;
using MediaPortal.Core.MediaManager;

namespace MediaPortal.Plugins.PlayerManager
{
  class PlayerBuilder : IPluginBuilder, IPlayerBuilder
  {
    #region variables
    string _type;
    string _extensions;
    INodeItem _item;
    IPlayerBuilder _playerInstance; // should this be IPlayer?
    #endregion

    #region Properties
    public string Extensions
    {
      get { return _extensions; }
    }
    #endregion

    #region Public methods
    public IPlayer GetPlayer(IMediaItem mediaItem, Uri uri)
    {
      if (_playerInstance == null)
      {
        try
        {
          // _playerInstance = (IPlayer)_item.CreateObject(_item["class"]);
          _playerInstance = (IPlayerBuilder)_item.CreateObject(_item["class"]);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Error(e.ToString() + "Can't create player : " + _item.Id);
          return null;
        }
      }
      // return _playerInstance; // should a new instance be created each time?
      return _playerInstance.GetPlayer(mediaItem, uri);
    }
    #endregion

    #region IPlayerBuilder methods
    public bool CanPlay(IMediaItem mediaItem, Uri uri)
    {
      string ext = System.IO.Path.GetExtension(uri.AbsolutePath);

      // First Check the file extension
      if (_extensions.IndexOf(ext) > -1)
        return true;

      // First check the Mime Type
      if (mediaItem.MetaData.ContainsKey("MimeType"))
      {
        string mimeType = mediaItem.MetaData["MimeType"] as string;
        if (mimeType != null)
        {
          if (mimeType.Contains(_type))
          {
            //if (_extensions.IndexOf(ext) > -1)
            return true;
          }
        }
      }

      return false;
    }
    #endregion

    #region IPluginBuilder methods
    public object BuildItem(object caller, INodeItem item, ArrayList subItems)
    {
      PlayerBuilder builder = new PlayerBuilder();
      builder._item = item;

      if (item.Contains("extensions"))
      {
        builder._extensions = item["extensions"];
      }

      if (item.Contains("type"))
      {
        builder._type = item["type"];
      }

      return builder;
    }
    #endregion
  }
}
