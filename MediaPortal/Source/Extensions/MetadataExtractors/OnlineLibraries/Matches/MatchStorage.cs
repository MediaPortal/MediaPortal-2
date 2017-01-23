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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;

namespace MediaPortal.Extensions.OnlineLibraries.Matches
{
  // TODO: implement lookup table and download stats in database
  /// <summary>
  /// Storage class for loading and saving <see cref="BaseFanArtMatch{T}"/> into storage, which is currently 
  /// a XML serialized file in the data folder of the application.
  /// </summary>
  /// <typeparam name="TMatch">Type of match</typeparam>
  /// <typeparam name="TId">Type of match's ID</typeparam>
  public class MatchStorage<TMatch, TId> : IDisposable where TMatch : BaseMatch
  {
    protected readonly string _matchesSettingsFile;
    protected readonly ConcurrentDictionary<String, TMatch> _storage;
    protected readonly ActionBlock<int> _saveBlock;

    public MatchStorage(string matchesSettingsFile)
    {
      _matchesSettingsFile = matchesSettingsFile;
      _storage = new ConcurrentDictionary<string, TMatch>(StringComparer.OrdinalIgnoreCase);
      _saveBlock = new ActionBlock<int>(dummy => Settings.Save(_matchesSettingsFile, _storage.Values.ToList()), new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });

      var matches = Settings.Load<List<TMatch>>(_matchesSettingsFile) ?? new List<TMatch>();
      foreach (var match in matches)
        _storage[match.ItemName] = match;
    }

    public bool TryAddMatch(TMatch match)
    {
      if (_storage.TryAdd(match.ItemName, match))
      {
        SaveMatches();
        return true;
      }
      return false;
    }

    public List<TMatch> GetMatches()
    {
      return _storage.Values.ToList();
    }

    public void SaveMatches()
    {
      // We use a dataflow block with a BoundedCapacity of 2 and a (default) MaxDegreeOfParallelism of 1;
      // That means if there is one save process ongoing, we can successfully schedule a second one. If
      // we try to add a third one while the first one is still in process and the second one is waiting
      // to be processed, no additional save process is scheduled. This is not necessary, because the
      // second one has not started, yet, and is still scheduled to be processed.
      _saveBlock.Post(0);
    }

    public void Dispose()
    {
      _saveBlock.Complete();
      _saveBlock.Completion.Wait();
    }
  }
}
