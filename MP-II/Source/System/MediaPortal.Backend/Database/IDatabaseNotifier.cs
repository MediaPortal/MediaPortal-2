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

namespace MediaPortal.Database
{
  public interface IDatabaseNotifier
  {
    /// <summary>
    /// Register for database notifications.
    /// </summary>
    /// <param name="notification">The callback.</param>
    void Register(IDatabaseNotification notification);

    /// <summary>
    /// UnRegister for database notifications.
    /// </summary>
    /// <param name="notification">The callback.</param>
    void UnRegister(IDatabaseNotification notification);

    /// <summary>
    /// send a notify to all registered clienst
    /// </summary>
    /// <param name="database">The database.</param>
    /// <param name="notificationType">Type of the notification.</param>
    /// <param name="item">The item.</param>
    void Notify(IDatabase database, DatabaseNotificationType notificationType, IDbItem item);
  }
}
