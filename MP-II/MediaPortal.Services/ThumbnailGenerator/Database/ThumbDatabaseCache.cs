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

using System.Collections.Generic;
using System.IO;
using System.Timers;

namespace MediaPortal.Services.ThumbnailGenerator.Database
{
  public class ThumbDatabaseCache
  {
    protected readonly IDictionary<DirectoryInfo, ThumbDatabase> _databases =
        new Dictionary<DirectoryInfo, ThumbDatabase>();
    protected Timer _timer;

    public ThumbDatabaseCache()
    {
      _timer = new Timer(1000);
      _timer.Elapsed += _timer_Elapsed;
      _timer.Start();
    }


    public ThumbDatabase Get(DirectoryInfo folder)
    {
      if (_databases.ContainsKey(folder))
        return _databases[folder];

      ThumbDatabase dbs = new ThumbDatabase(folder);
      _databases[folder] = dbs;
      return dbs;
    }

    private void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      lock (this)
      {
        ICollection<ThumbDatabase> releaseDbs = new List<ThumbDatabase>();
        foreach (KeyValuePair<DirectoryInfo, ThumbDatabase> dbEntry in _databases)
        {
          if (!dbEntry.Value.CanFree)
            continue;
          releaseDbs.Add(dbEntry.Value);
        }
        foreach (ThumbDatabase thumbDb in releaseDbs)
        {
          thumbDb.Close();
          _databases.Remove(thumbDb.Folder);
        }
      }
    }
  }
}
