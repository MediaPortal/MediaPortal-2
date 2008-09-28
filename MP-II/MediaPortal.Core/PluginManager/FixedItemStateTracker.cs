#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

namespace MediaPortal.Core.PluginManager
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
  /// </remarks>
  public class FixedItemStateTracker : IPluginItemStateTracker
  {
    public bool RequestEnd(PluginItemMetadata item)
    {
      return false;
    }

    public void Stop(PluginItemMetadata item) { }

    public void Continue(PluginItemMetadata item) { }
  }
}