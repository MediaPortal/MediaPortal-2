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

namespace HomeEditor.Extensions
{
  /// <summary>
  /// Plugin item builder for skins to register support for the home menu Editor by registering a <see cref="HomeEditorRegistration"/>. 
  /// </summary>
  public class HomeEditorRegistrationBuilder : IPluginItemBuilder
  {
    public const string HOME_EDITOR_PROVIDER_PATH = "/HomeEditor";

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("SkinName", itemData);
      return new HomeEditorRegistration(itemData.Id, itemData.Attributes["SkinName"]);
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return true;
    }

    public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      //TODO: handle skin removal
    }
  }

  /// <summary>
  /// Holds the information about a skin that has registered support for the home menu editor.
  /// </summary>
  public class HomeEditorRegistration
  {
    public HomeEditorRegistration(string id, string skinName)
    {
      Id = new Guid(id);
      SkinName = skinName;
    }

    /// <summary>
    /// Unique ID of extension.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Name of the skin that supports the Home Menu Editor.
    /// </summary>
    public string SkinName { get; private set; }
  }
}
