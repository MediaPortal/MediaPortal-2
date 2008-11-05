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
using MediaPortal.Core.PluginManager;
using MediaPortal.Presentation.Players;

using MediaPortal.Media.MediaManagement;

namespace Components.Services.PlayerManager
{
  class PlayerBuilder : IPluginItemBuilder, IPlayerBuilder
  {
    #region variables
    string _type;
    string _extensions;
    IPlayerBuilder _builderInstance;
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
      return _builderInstance.GetPlayer(mediaItem, uri);
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
            return true;
          }
        }
      }

      return false;
    }
    #endregion

    #region IPluginBuilder methods

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      PlayerBuilder builder = new PlayerBuilder();

      if (itemData.Attributes.ContainsKey("Extensions"))
      {
        builder._extensions = itemData.Attributes["Extensions"];
      }

      if (itemData.Attributes.ContainsKey("Type"))
      {
        builder._type = itemData.Attributes["Type"];
      }

      builder._builderInstance = (IPlayerBuilder) plugin.InstanciatePluginObject(itemData.Attributes["ClassName"]);

      return builder;
    }

    public bool NeedsPluginActive
    {
      get { return true; }
    }

    #endregion
  }
}
