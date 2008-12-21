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
using System.Collections.Generic;
using System.Timers;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Services.ThumbnailGenerator.Database
{
  public class ThumbDatabaseCache
  {
    /// <summary>
    /// Timespan in seconds after that an unused thumb db gets released.
    /// </summary>
    public const int THUMB_DB_RELEASE_TIME = 20;

    protected readonly IDictionary<string, ThumbDatabase> _databases =
        new Dictionary<string, ThumbDatabase>(WindowsFilesystemPathEqualityComparer.Instance);
    protected readonly IDictionary<string, int> _usages =
        new Dictionary<string, int>(WindowsFilesystemPathEqualityComparer.Instance);
    protected Timer _timer;

    public ThumbDatabaseCache()
    {
      _timer = new Timer(1000);
      _timer.Elapsed += _timer_Elapsed;
      _timer.Start();
    }

    /// <summary>
    /// Returns the thumbnail database for the specified folder path and increments
    /// its usage counter. Make sure to call <see cref="Release"/> after the usage
    /// of the returned database instance.
    /// </summary>
    public ThumbDatabase Acquire(string folderPath)
    {
      lock (this)
      {
        try
        {
          if (_databases.ContainsKey(folderPath))
            return _databases[folderPath];

          return _databases[folderPath] = new ThumbDatabase(folderPath);
        }
        finally
        {
          IncrementUsageCounter(folderPath);
        }
      }
    }

    /// <summary>
    /// Releases the usage of the specified thumb database. This method MUST be called when the
    /// the <see cref="Acquire"/> method was called.
    /// </summary>
    public void Release(string folderPath)
    {
      lock (this)
        DecrementUsageCounter(folderPath);
    }

    private void IncrementUsageCounter(string folderPath)
    {
      if (_usages.ContainsKey(folderPath))
        _usages[folderPath]++;
      else
        _usages[folderPath] = 1;
    }

    private void DecrementUsageCounter(string folderPath)
    {
      if (!_usages.ContainsKey(folderPath))
        throw new ArgumentException(string.Format("ThumbDatabaseCache: Thumb database for folder '{0}' isn't locked", folderPath));
      if (_usages[folderPath] == 1)
        _usages.Remove(folderPath);
      else
        _usages[folderPath]--;
    }

    private void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      lock (this)
      {
        ICollection<string> releaseDbs = new List<string>();
        foreach (KeyValuePair<string, ThumbDatabase> dbEntry in _databases)
        {
          TimeSpan ts = DateTime.Now - dbEntry.Value.LastUsed;
          if (ts.TotalSeconds < THUMB_DB_RELEASE_TIME)
            continue;
          dbEntry.Value.Close();
          releaseDbs.Add(dbEntry.Key);
        }
        foreach (string dbPath in releaseDbs)
          _databases.Remove(dbPath);
      }
    }
  }
}
