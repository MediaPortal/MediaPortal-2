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
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.PluginManager.Builders;

namespace MediaPortal.Common.Services.ThumbnailGenerator
{
  /// <summary>
  /// Plugin item builder for <c>ThumbnailProviderBuilder</c> plugin items.
  /// </summary>
  public class ThumbnailProviderBuilder : IPluginItemBuilder
  {
    public const string THUMBNAIL_PROVIDER_PATH = "/Thumbnails/Providers";

    #region IPluginItemBuilder Member

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("ClassName", itemData);
      BuilderHelper.CheckParameter("ProviderName", itemData);
      BuilderHelper.CheckParameter("Priority", itemData);
      return new ThumbnailProviderRegistration(plugin.GetPluginType(itemData.Attributes["ClassName"]), itemData.Id, itemData.Attributes["ProviderName"], itemData.Attributes["Priority"]);
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
  /// <see cref="ThumbnailProviderRegistration"/> holds extension metadata.
  /// </summary>
  public class ThumbnailProviderRegistration
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
    /// Gets the priority of the provider. Lower numbers are preferred in execution order.
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Name of the thumbnail provider.
    /// </summary>
    public string ProviderName { get; private set; }

    public ThumbnailProviderRegistration(Type type, string providerId, string providerName, string priority)
    {
      ProviderClass = type;
      ProviderName = providerName;
      Priority = int.Parse(priority);
      Id = new Guid(providerId);
    }
  }
}

