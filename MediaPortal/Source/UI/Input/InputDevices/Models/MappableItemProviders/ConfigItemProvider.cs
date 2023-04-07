#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using InputDevices.Common.Mapping;
using MediaPortal.Common;
using MediaPortal.Common.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace InputDevices.Models.MappableItemProviders
{
  /// <summary>
  /// Implementation of <see cref="IMappableItemProvider"/> that provides a list of config location items that can be mapped to.
  /// </summary>
  public class ConfigItemProvider : IMappableItemProvider
  {
    public IEnumerable<MappableItem> GetMappableItems()
    {
      IConfigurationManager configurationManager = ServiceRegistration.Get<IConfigurationManager>();
      IConfigurationNode configNode = configurationManager.GetNode("/");
      return configNode.ChildNodes.Where(n => n.ConfigObj is ConfigSection)
        .Select(c => new MappableItem(
          c.ConfigObj.Text,
          InputAction.CreateConfigAction(c.Location)
        ));
    }
  }
}
