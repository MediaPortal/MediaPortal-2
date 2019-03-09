#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.PluginManager.Builders;
using System;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  public class MediaListProviderBuilder : IPluginItemBuilder
  {
    public const string MEDIA_LIST_PROVIDER_PATH = "/Media/MediaListProviders";

    #region IPluginItemBuilder Member

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("ClassName", itemData);
      return new MediaListProviderRegistration(plugin.GetPluginType(itemData.Attributes["ClassName"]), itemData.Attributes["Key"], itemData.Id);
    }

    public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      // Noting to do
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return true;
    }

    #endregion
  }

  /// <summary>
  /// <see cref="MediaListProviderRegistration"/> holds extension media lists.
  /// </summary>
  public class MediaListProviderRegistration
  {
    /// <summary>
    /// Gets the registered type.
    /// </summary>
    public Type ProviderClass { get; private set; }

    /// <summary>
    /// Unique ID of extension.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Key for the media list. Using the same key will replace the default lists.
    /// </summary>
    public string Key { get; private set; }

    public MediaListProviderRegistration(Type type, string key, string providerId)
    {
      Key = key;
      ProviderClass = type;
      Id = new Guid(providerId);
    }
  }
}
