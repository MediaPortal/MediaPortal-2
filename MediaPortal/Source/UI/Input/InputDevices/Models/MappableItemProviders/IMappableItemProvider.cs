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
using System.Collections.Generic;

namespace InputDevices.Models.MappableItemProviders
{
  /// <summary>
  /// Interface for a class that can provide an enumeration of mappable actions to be displayed in the GUI.
  /// </summary>
  public interface IMappableItemProvider
  {
    /// <summary>
    /// Gets an enumeration of <see cref="MappableItem"/> that will be displayed in the GUI.
    /// Implementations should ensure that this method can be called multiple times without side-effects
    /// to allow a single instance to be created and reused each time the list needs to be displayed.
    /// </summary>
    /// <returns></returns>
    IEnumerable<MappableItem> GetMappableItems();
  }

  /// <summary>
  /// Class that represents a single mappable action that will be displayed in a list in the GUI. 
  /// </summary>
  public class MappableItem
  {
    public MappableItem(string displayName, InputAction mappableAction)
      : this(LocalizationHelper.CreateResourceString(displayName), mappableAction)
    { }

    public MappableItem(IResourceString displayName, InputAction mappableAction)
    {
      DisplayName = displayName;
      MappableAction = mappableAction;
    }

    /// <summary>
    /// The name of the action that will be displayed.
    /// </summary>
    public IResourceString DisplayName { get; }

    /// <summary>
    /// The action that will be mapped.
    /// </summary>
    public InputAction MappableAction { get; }
  }
}
