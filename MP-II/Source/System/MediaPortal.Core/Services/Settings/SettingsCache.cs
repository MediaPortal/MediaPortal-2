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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using MediaPortal.Core.Exceptions;

namespace MediaPortal.Core.Services.Settings
{
  public class SettingsObjectWrapper
  {
    #region Protected fields

    protected DateTime _lastUsed;
    protected object _settingsObject;

    #endregion

    #region Ctor

    public SettingsObjectWrapper(object settingsObject)
    {
      _settingsObject = settingsObject;
      Use();
    }

    #endregion

    public DateTime LastUsed
    {
      get { return _lastUsed; }
    }

    public object SettingsObject
    {
      get { return _settingsObject; }
      set
      {
        _settingsObject = value;
        Use();
      }
    }

    public void Use()
    {
      _lastUsed = DateTime.Now;
    }
  }

  /// <summary>
  /// Cache for mapping settings types to settings objects. The cache will automatically release unused
  /// settings objects after <see cref="SETTINGS_OBJ_RELEASE_TIME"/> seconds.
  /// This cache implementation is thread-safe.
  /// </summary>
  public class SettingsCache : IEnumerable<SettingsObjectWrapper>, IDisposable
  {
    /// <summary>
    /// Timespan in seconds after that an unused setting gets released.
    /// </summary>
    public const int SETTINGS_OBJ_RELEASE_TIME = 5;

    #region Protected fields

    protected readonly IDictionary<Type, SettingsObjectWrapper> _cache =
        new Dictionary<Type, SettingsObjectWrapper>();
    protected Timer _timer;
    protected object _syncObj = new object();

    #endregion

    public SettingsCache()
    {
      _timer = new Timer(1000);
      _timer.Elapsed += _timer_Elapsed;
      _timer.Start();
    }

    public void Dispose()
    {
      lock (_syncObj)
      {
        _timer.Stop();
        _timer = null;
      }
    }

    /// <summary>
    /// Gets the cached setting for the given setting type <paramref name="type"/>.
    /// </summary>
    /// <param name="type">Setting type for the setting to retrieve.</param>
    /// <returns>Settings object for the specified type or <c>null</c>, if the cache doesn't contain the
    /// setting (any more).</returns>
    public object Get(Type type)
    {
      SettingsObjectWrapper wrapper;
      lock (_syncObj)
        return _cache.TryGetValue(type, out wrapper) ? wrapper.SettingsObject : null;
    }

    /// <summary>
    /// Updates the cache with the specified <paramref name="settingsObject"/>.
    /// </summary>
    /// <param name="settingsObject">Object to update.</param>
    public void Set(object settingsObject)
    {
      if (settingsObject == null)
        return;
      Type type = settingsObject.GetType();
      lock (_syncObj)
      {
        SettingsObjectWrapper wrapper;
        if (_cache.TryGetValue(type, out wrapper))
          wrapper.SettingsObject = settingsObject;
        else
          _cache[type] = new SettingsObjectWrapper(settingsObject);
      }
    }

    /// <summary>
    /// Updates the last usage time of the setting with the specified <paramref name="type"/> to
    /// <see cref="DateTime.Now"/>. If the specified setting isn't contained in the cache (any more),
    /// this method will return without modifying the cache.
    /// </summary>
    /// <param name="type">Type of the settings object to keep in the cache.</param>
    public void Use(Type type)
    {
      SettingsObjectWrapper wrapper;
      lock (_syncObj)
        if (_cache.TryGetValue(type, out wrapper))
          wrapper.Use();
    }

    /// <summary>
    /// Removes all entries from the cache.
    /// </summary>
    public void Clear()
    {
      lock (_syncObj)
        _cache.Clear();
    }

    /// <summary>
    /// Stops the cache tidy-up temporarily, which will make the cache grow until <see cref="StopKeep"/> is
    /// called. In combination with the methods <see cref="Clear"/> and <see cref="GetEnumerator"/>, the
    /// cache can be used as a temporary object container.
    /// </summary>
    public void KeepAll()
    {
      lock (_syncObj)
        _timer.Enabled = false;
    }

    /// <summary>
    /// Re-starts the cache tidy-up.
    /// </summary>
    public void StopKeep()
    {
      lock (_syncObj)
        _timer.Enabled = true;
    }

    private void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      lock (_syncObj)
      {
        if (!_timer.Enabled)
          // Avoid threading issues (i.e. KeepAll was called after this method begun
          // but before our lock statement)
          return;
        ICollection<Type> releaseTypes = new List<Type>();
        foreach (KeyValuePair<Type, SettingsObjectWrapper> entry in _cache)
        {
          TimeSpan ts = DateTime.Now - entry.Value.LastUsed;
          if (ts.TotalSeconds < SETTINGS_OBJ_RELEASE_TIME)
            continue;
          releaseTypes.Add(entry.Key);
        }
        foreach (Type releaseType in releaseTypes)
          _cache.Remove(releaseType);
      }
    }

    /// <summary>
    /// Returns an enumerator over the cache backing objects of type <see cref="SettingsObjectWrapper"/>.
    /// The enumerator may only be requested after a call to method <see cref="KeepAll"/>, and method
    /// <see cref="StopKeep"/> must not be called before the usage of the returned enumerator has finished.
    /// If this demand is not observed, the cache can get in an undefined state.
    /// </summary>
    /// <returns>Enumerator over the backing objects of the cache.</returns>
    public IEnumerator<SettingsObjectWrapper> GetEnumerator()
    {
      lock (_syncObj)
        if (_timer.Enabled)
          throw new InvalidStateException("The enumerator on the settings cache may only be requested when the settings cache keeps its objects");
        else
          return _cache.Values.GetEnumerator();
    }

    /// <summary>
    /// <see cref="GetEnumerator"/>.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}
