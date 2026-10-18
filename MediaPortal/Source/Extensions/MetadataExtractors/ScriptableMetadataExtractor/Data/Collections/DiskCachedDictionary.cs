#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using Newtonsoft.Json;
using System.IO;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Collections
{
  public class DiskCachedDictionary<TKey, TValue> : CachedDictionary<TKey, int>
  {
    // This used to be a BinaryFormatter. .NET removed it (SYSLIB0011 is an error since .NET 8)
    // because it was never portable and is a deserialization attack surface - exactly the two
    // things this port is about. The payload here is a disposable on-disk cache, so JSON is
    // enough and needs no new dependency. Cache files written by the old BinaryFormatter build
    // are unreadable and get rewritten on the next scrape; nothing else is lost.
    //
    // Deliberately no TypeNameHandling: it would reintroduce "instantiate whatever the file
    // says", which is the vulnerability BinaryFormatter had.
    private bool _initialized = false;
    private string _cacheLocation = null;

    public void Init(string id = null)
    {
      if (_initialized) return;
      _initialized = true;

      if (string.IsNullOrEmpty(id))
        _cacheLocation = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\ScriptableScraperProvider\Cache\");
      else
        _cacheLocation = ServiceRegistration.Get<IPathManager>().GetPath($@"<DATA>\ScriptableScraperProvider\Cache\{id}\");
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
      File.WriteAllText(_cacheLocation + lookup, JsonConvert.SerializeObject(value));

      return lookup;
    }

    private TValue Deserialize(int lookup)
    {
      Init();

      return JsonConvert.DeserializeObject<TValue>(File.ReadAllText(_cacheLocation + lookup));
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
