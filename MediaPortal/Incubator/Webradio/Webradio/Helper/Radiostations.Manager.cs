#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Webradio.Settings;

namespace Webradio.Helper
{
  public partial class Radiostations
  {
    private static Radiostations _instance;
    private static IDictionary<string, RadioStation> _instanceDictionary;

    internal static readonly object LOCK = new object();

    public static Radiostations Instance
    {
      get { lock (LOCK) return _instance = (_instance ?? Read(StreamListFile)); }
    }

    public static IDictionary<string, RadioStation> InstanceDictionary
    {
      get
      {
        lock (LOCK)
        {
          if (_instanceDictionary == null)
          {
            _instanceDictionary = new ConcurrentDictionary<string, RadioStation>();
            if (Instance.Stations != null)
            {
              foreach (var myStream in Instance.Stations)
              {
                // This also overwrites duplicate entries, the last one wins.
                _instanceDictionary[myStream.Id] = myStream;
              }
            }
          }
          return _instanceDictionary;
        }
      }
    }

    public static void Reset()
    {
      lock (LOCK)
      {
        _instance = null;
        _instanceDictionary = null;
      }
    }

    public static List<RadioStation> Filtered(Filter filter, List<RadioStation> streams)
    {
      return streams.Where(stream => filter.Countrys.Contains(stream.Country)).Where(stream => stream.Genres.Any(genre => filter.Genres.Contains(genre))).ToList();
    }

    public static RadioStation ById(string id)
    {
      return Instance.Stations.FirstOrDefault(s => s.Id == id);
    }

    #region private functions

    private static Radiostations Read(string file)
    {
      Radiostations rs = new Radiostations();

      try
      {
        rs = Json.Deserialize<Radiostations>(File.ReadAllText(file));
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Webradio: Error Import Radiostations '{0}'", ex);
      }

      if (rs.Stations == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("Webradio: No Stations found in '{0}'", file);
      }

      return rs;
    }

    private static bool Contains(List<string> l, string s)
    {
      return l.Count == 0 || l.Contains(s);
    }

    #endregion
  }
}
