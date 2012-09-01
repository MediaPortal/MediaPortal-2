#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.Plugins.SlimTv.Interfaces.Extensions
{
  /// <summary>
  /// Plugin item builder for <c>SlimTvProgramExtension</c> plugin items.
  /// </summary>
  public class SlimTvExtensionBuilder : IPluginItemBuilder
  {
    public const string SLIMTVEXTENSIONPATH = "/SlimTv/Extensions";

    #region IPluginItemBuilder Member

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("ClassName", itemData);
      BuilderHelper.CheckParameter("Caption", itemData);
      return new SlimTvProgramExtension(plugin.GetPluginType(itemData.Attributes["ClassName"]), itemData.Attributes["Caption"], itemData.Id);
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
  /// <see cref="SlimTvProgramExtension"/> holds extension metadata.
  /// </summary>
  public class SlimTvProgramExtension
  {
    /// <summary>
    /// Gets the registered type.
    /// </summary>
    public Type ExtensionClass { get; private set; }
    /// <summary>
    /// Gets the caption of the entry, should be defined as localized string (<example>[SlimTv.MyProgramExtension]</example>).
    /// </summary>
    public string Caption { get; private set; }

    /// <summary>
    /// Unique ID of extension.
    /// </summary>
    public Guid Id { get; private set; }

    public SlimTvProgramExtension(Type type, string mimetype, string id)
    {
      ExtensionClass = type;
      Caption = mimetype;
      Id = new Guid(id);
    }
  }
}
