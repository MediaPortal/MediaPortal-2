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
using System.Timers;

namespace MediaPortal.Services.ThumbnailGenerator.Database
{
  public class ThumbDatabaseCache
  {
    private Dictionary<string, ThumbDatabase> _databases;
    private static ThumbDatabaseCache _instance;
    private Timer _timer;

    public static ThumbDatabaseCache Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = new ThumbDatabaseCache();
        }
        return _instance;
      }
    }

    public ThumbDatabaseCache()
    {
      _databases = new Dictionary<string, ThumbDatabase>();
      _timer = new Timer(1000);
      _timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
      _timer.Start();
    }


    public ThumbDatabase Get(string name)
    {
      if (_databases.ContainsKey(name))
      {
        return _databases[name];
      }

      ThumbDatabase dbs = new ThumbDatabase();
      dbs.Open(name);
      _databases[name] = dbs;
      return dbs;
    }

    private void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      lock (this)
      {
        bool disposing;
        do
        {
          disposing = false;
          Dictionary<string, ThumbDatabase>.Enumerator enumer = _databases.GetEnumerator();
          while (enumer.MoveNext())
          {
            if (enumer.Current.Value.CanFree)
            {
              enumer.Current.Value.Close();
              _databases.Remove(enumer.Current.Key);
              disposing = true;
              break;
            }
          }
        } while (disposing);
      }
    }
  }
}
