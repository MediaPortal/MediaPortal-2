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

namespace MediaPortal.Core.PluginManager
{
  /// <summary>
  /// Interface with handshake methods to query the stopping process of a plugin. Implementors
  /// can provide the feature to release items they once requested by implementing the stopping
  /// request methods in an appropriate way.
  /// </summary>
  public interface IPluginItemStateTracker
  {
    /// <summary>
    /// Schedules the stopping of the plugin providing the specified <paramref name="item"/> this
    /// state tracker is watching.
    /// This method returns the information if the item can be removed from the system. Before this
    /// method is called, the plugin's state will be changed to <see cref="PluginState.EndRequest"/>.
    /// </summary>
    /// <remarks>
    /// This method is part of the first phase in the two-phase stop procedure.
    /// If the plugin state tracker returns <c>true</c> as a result of the call to its end request
    /// method, and the calls to this method also return <c>true</c> for all item users for all
    /// items of the to-be-disabled plugin, the plugin's state will change to
    /// <see cref="PluginState.Stopping"/>, then all uses of items by clients will be canceled
    /// by a call to <see cref="Stop"/>.
    /// If either this method returns <c>false</c> for one item user for one of the items of the
    /// plugin, or if the plugin state tracker returns <c>false</c> as a result of the call to its end
    /// request method, the plugin will continue to be active and the method
    /// <see cref="Continue(PluginItemMetadata)"/> will be called.
    /// After this method returned <c>true</c>, the item must not be accessed any more by the client
    /// in a way that would change the return value if this method would be called again, until
    /// one of the methods <see cref="Stop(PluginItemMetadata)"/> or
    /// <see cref="Continue(PluginItemMetadata)"/> is called.
    /// </remarks>
    /// <param name="item">The item which should be removed.</param>
    /// <returns><c>true</c>, if the specified item can be removed from the system at this time,
    /// else <c>false</c>.
    /// </returns>
    bool RequestEnd(PluginItemMetadata item);

    /// <summary>
    /// Second step of the two-phase stopping procedure. This method should stop the usage
    /// of the specified <paramref name="item"/>, which means the client isn't allowed to access the
    /// item any more. The item reference gets invalid when this method is called.
    /// </summary>
    void Stop(PluginItemMetadata item);

    /// <summary>
    /// Revokes the end request which was triggered by a former call to the
    /// <see cref="RequestEnd(PluginItemMetadata)"/> method. After this call, the <paramref name="item"/>
    /// may be accessed again as it was before the call of <see cref="RequestEnd(PluginItemMetadata)"/>
    /// method.
    /// </summary>
    void Continue(PluginItemMetadata item);
  }
}
