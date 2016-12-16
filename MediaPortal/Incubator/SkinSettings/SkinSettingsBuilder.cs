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

using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.PluginManager.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinSettings
{
  public class SkinSettingsBuilder : IPluginItemBuilder
  {
    public const string SKIN_SETTINGS_PROVIDER_PATH = "/SkinSettings";

    #region IPluginItemBuilder Member

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("ClassName", itemData);
      BuilderHelper.CheckParameter("Name", itemData);
      return new SkinSettingsRegistration(plugin.GetPluginType(itemData.Attributes["ClassName"]), itemData.Id, itemData.Attributes["Name"]);
    }

    public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      // Noting to do
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return false;
    }

    #endregion
  }

  public class SkinSettingsRegistration
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
    /// Name of settings when referenced in SkinSettingsModel
    /// </summary>
    public string Name { get; private set; }

    public SkinSettingsRegistration(Type type, string providerId, string name)
    {
      ProviderClass = type;
      Id = new Guid(providerId);
      Name = name;
    }
  }
}
