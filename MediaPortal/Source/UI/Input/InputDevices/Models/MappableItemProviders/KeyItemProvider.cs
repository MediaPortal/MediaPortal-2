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
using MediaPortal.Common.Localization;
using MediaPortal.UI.Control.InputManager;
using System.Collections.Generic;
using System.Linq;

namespace InputDevices.Models.MappableItemProviders
{
  /// <summary>
  /// Implementation of <see cref="IMappableItemProvider"/> that provides a list of <see cref="Key"/> items that can be mapped to.
  /// </summary>
  public class KeyItemProvider : IMappableItemProvider
  {
    public const string RES_KEY_TEXT = "[InputDevices.Mapping.Key]";

    public IEnumerable<MappableItem> GetMappableItems()
    {
      return Key.NAME2SPECIALKEY.Values.Where(k => k != Key.None)
        .Select(k => new MappableItem(
          $"{LocalizationHelper.Translate(RES_KEY_TEXT)} \"{k.Name}\"",
          InputAction.CreateKeyAction(k)
        ));
    }
  }
}
