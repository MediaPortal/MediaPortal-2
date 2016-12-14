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
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Services.Players.Builders
{
  public class PlayerBuilder : IPluginItemBuilder
  {
    // At the moment, we simply use an InstanceBuilder here. In the future, we could provide the option to
    // give a set of supported file extensions in the player builder item registration, which can be evaluated before
    // requesting the player builder -> lazy load the player builders on request
    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("ClassName", itemData);
      int priority = 0;
      string prioString;
      if (itemData.Attributes.TryGetValue("Priority", out prioString))
        int.TryParse(prioString, out priority);

      return new PlayerBuilderWrapper(itemData.Id, plugin.InstantiatePluginObject(itemData.Attributes["ClassName"]) as IPlayerBuilder, priority);
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

  public class PlayerBuilderWrapper
  {
    /// <summary>
    /// Gets the player builder.
    /// </summary>
    public IPlayerBuilder PlayerBuilder { get; private set; }

    /// <summary>
    /// Unique ID of extension.
    /// </summary>
    public String Id { get; private set; }

    /// <summary>
    /// Priority of builder, higher values are executed first.
    /// </summary>
    public int Priority { get; private set; }

    public PlayerBuilderWrapper(String id, IPlayerBuilder builder, int priority)
    {
      Id = id;
      PlayerBuilder = builder;
      Priority = priority;
    }
  }
}
