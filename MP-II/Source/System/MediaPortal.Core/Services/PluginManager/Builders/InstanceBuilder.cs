#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using MediaPortal.Core.PluginManager;

namespace MediaPortal.Core.Services.PluginManager.Builders
{
  /// <summary>
  /// Builds an item of type "Instance". The "Instance" item type provides an instance of a
  /// specified class which will be loaded from the plugin's assemblies.
  /// </summary>
  /// <remarks>
  /// The item registration has to provide the parameter "ClassName" which holds the fully
  /// qualified name of the class to instantiate:
  /// <example>
  /// &lt;Instance ClassName="Foo"/&gt;
  /// </example>
  /// </remarks>
  public class InstanceBuilder : IPluginItemBuilder
  {
    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("ClassName", itemData);
      return plugin.InstanciatePluginObject(itemData.Attributes["ClassName"]);
    }

    public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      if (item == null)
        return;
      plugin.RevokePluginObject(item.GetType().FullName);
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return true;
    }
  }
}
