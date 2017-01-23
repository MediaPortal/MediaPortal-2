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

namespace MediaPortal.Common.PluginManager
{
  /// <summary>
  /// Default implementation of a plugin item state tracker which will prevent to reject the item
  /// and so prevents the item's plugin from being disabled.
  /// </summary>
  /// <remarks>
  /// Instances of this class can be used if a requested item cannot be removed from the running system
  /// any more.
  /// If possible, every item user should try to be able to cancel its item usage. This helps the plugin
  /// system to be able to remove plugins which have been loaded at runtime.
  /// <seealso cref="DefaultItemStateTracker"/>
  /// </remarks>
  public class FixedItemStateTracker : DefaultItemStateTracker
  {
    public FixedItemStateTracker(string usageDescription) : base(usageDescription)
    {
      EndRequested += itemRegistration => false;
    }
  }
}