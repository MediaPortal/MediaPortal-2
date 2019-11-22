#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Collections
{
  public class DiskCachedDictionary<TKey, TValue> : CachedDictionary<TKey, int>
  {
    private BinaryFormatter _serializer = new BinaryFormatter();
    private bool _initialized = false;
    private string _cacheLocation = null;

    public void Init()
    {
      if (_initialized) return;
      _initialized = true;

      _cacheLocation = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\ScriptableScraperProvider\Cache\");
      Directory.CreateDirectory(_cacheLocation);
    }

    public void DeInit()
    {
      if (!_initialized) return;

      Directory.Delete(_cacheLocation, true);
      _initialized = false;
    }

    ~DiskCachedDictionary()
    {
      DeInit();
    }

    private int Serialize(TKey key, TValue value)
    {
      Init();

      int lookup = key.GetHashCode();
      FileStream stream = File.Create(_cacheLocation + lookup);

      _serializer.Serialize(stream, value);
      stream.Close();

      return lookup;
    }

    private TValue Deserialize(int lookup)
    {
      Init();

      FileStream stream = File.OpenRead(_cacheLocation + lookup);
      return (TValue)_serializer.Deserialize(stream);
    }

    public void Add(TKey key, TValue value)
    {
      int lookup = Serialize(key, value);
      base.Add(key, lookup);
    }

    public override bool Remove(TKey key)
    {
      File.Delete(_cacheLocation + key.GetHashCode());
      return base.Remove(key);
    }

    public new TValue this[TKey key]
    {
      get
      {
        int lookup = base[key];
        return Deserialize(lookup);
      }
      set
      {
        int lookup = Serialize(key, value);
        base[key] = lookup;
      }
    }

    public override void Clear()
    {
      DeInit();
      base.Clear();
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
      int lookup;

      bool success = base.TryGetValue(key, out lookup);
      if (success)
        value = Deserialize(lookup);
      else
        value = default(TValue);

      return success;
    }
  }
}
