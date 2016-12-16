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

using System;
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Settings;

namespace MediaPortal.UI.ServerCommunication.Settings
{
  public class ServerConnectionSettings
  {
    protected string _homeServerSystemId = null;
    protected string _lastHomeServerName;
    protected SystemName _lastHomeServerSystem;
    protected Dictionary<Guid, RelocationMode> _cachedSharesUpdates = new Dictionary<Guid, RelocationMode>();

    /// <summary>
    /// UUID of our home server. The server connection manager will always try to connect to a home server
    /// of this UUID.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string HomeServerSystemId
    {
      get { return _homeServerSystemId; }
      set { _homeServerSystemId = value; }
    }

    /// <summary>
    /// Cached display name of the last connected home server.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string LastHomeServerName
    {
      get { return _lastHomeServerName; }
      set { _lastHomeServerName = value; }
    }

    /// <summary>
    /// Computer name of the last connected home server.
    /// </summary>
    [Setting(SettingScope.Global)]
    public SystemName LastHomeServerSystem
    {
      get { return _lastHomeServerSystem; }
      set { _lastHomeServerSystem = value; }
    }

    /// <summary>
    /// Contains a collection of update commands of local shares, which have been received while the server was not connected.
    /// The update commands will be executed the next time when the server is connected.
    /// </summary>
    /// <remarks>
    /// Settings serialization will be done by property <see cref="CachedSharesUpdates_Values"/> because XML serializer cannot
    /// serialize <see cref="Dictionary{TKey,TValue}"/>.
    /// </remarks>
    public Dictionary<Guid, RelocationMode> CachedSharesUpdates
    {
      get { return _cachedSharesUpdates; }
      set { _cachedSharesUpdates = value; }
    }

    /// <summary>
    /// Workaround property to enable automatic serialization because the <see cref="Dictionary{TKey,TValue}"/> of property
    /// <see cref="CachedSharesUpdates"/> cannot be serialized.
    /// </summary>
    [Setting(SettingScope.Global)]
    public DictionaryEntry[] CachedSharesUpdates_Values
    {
      get
      {
        DictionaryEntry[] entries = new DictionaryEntry[_cachedSharesUpdates.Count];
        int count = 0;
        foreach (KeyValuePair<Guid, RelocationMode> entry in _cachedSharesUpdates)
          entries[count++] = new DictionaryEntry(entry.Key, entry.Value.ToString());
        return entries;
      }
      set
      {
        _cachedSharesUpdates = new Dictionary<Guid, RelocationMode>(value.Length);
        foreach (DictionaryEntry entry in value)
          _cachedSharesUpdates.Add((Guid) entry.Key, (RelocationMode) Enum.Parse(typeof(RelocationMode), (string) entry.Value));
      }
    }
  }
}
