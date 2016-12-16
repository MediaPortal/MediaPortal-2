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

namespace MediaPortal.Extensions.UserServices.FanArtService.Interfaces.Providers
{
  /// <summary>
  /// Plugin item builder for <c>FanartProviderBuilder</c> plugin items.
  /// </summary>
  public class FanartProviderBuilder : IPluginItemBuilder
  {
    public const string FANART_PROVIDER_PATH = "/Fanart/Providers";

    #region IPluginItemBuilder Member

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("ClassName", itemData);
      BuilderHelper.CheckParameter("MediaTypes", itemData);
      return new FanartProviderRegistration(plugin.GetPluginType(itemData.Attributes["ClassName"]), itemData.Attributes["MediaTypes"], itemData.Id);
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
  /// <see cref="FanartProviderRegistration"/> holds extension metadata.
  /// </summary>
  public class FanartProviderRegistration
  {
    /// <summary>
    /// Gets the registered type.
    /// </summary>
    public Type ProviderClass { get; private set; }
    
    /// <summary>
    /// Gets the the comma separated list of supported media types.
    /// </summary>
    public string MediaTypes { get; private set; }

    /// <summary>
    /// Unique ID of extension.
    /// </summary>
    public Guid Id { get; private set; }

    public FanartProviderRegistration(Type type, string mediaTypes, string providerId)
    {
      ProviderClass = type;
      MediaTypes = mediaTypes;
      Id = new Guid(providerId);
    }
  }
}
