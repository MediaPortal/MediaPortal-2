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

using System.Collections.Generic;
using MediaPortal.Core.PluginManager;
using MediaPortal.Utilities;

namespace Components.Services.ImporterManager
{
  class ImporterBuilder : IPluginItemBuilder
  {
    #region IPluginItemBuilder methods

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      string name = itemData.Id; // The importer's name is its id
      string className = itemData.Attributes["ClassName"]; // The class of the importer
      IList<string> extensions = new List<string>(); // All extensions this importer can import
      
      if (itemData.Attributes.ContainsKey("Extensions"))
        CollectionUtils.AddAll(extensions, itemData.Attributes["Extensions"].Split(new char[] {',', ' ', ';'}));

      return new LazyImporterWrapper(name, extensions, className, plugin);
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return true;
    }

    #endregion
  }
}
