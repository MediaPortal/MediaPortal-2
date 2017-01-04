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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View implementation which presents a list of of all registered systems in the media library, one sub view for each system.
  /// </summary>
  public class AllSystemsViewSpecification : ViewSpecification
  {
    #region Protected fields

    protected readonly IEnumerable<string> _restrictedMediaCategories;

    #endregion
    
    #region Ctor

    public AllSystemsViewSpecification(string viewDisplayName, IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds, IEnumerable<string> restrictedMediaCategories = null) :
      base(viewDisplayName, necessaryMIATypeIds, optionalMIATypeIds)
    {
      _restrictedMediaCategories = restrictedMediaCategories;
    }

    #endregion

    #region Base overrides

    public override bool CanBeBuilt
    {
      get
      {
        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        return scm.IsHomeServerConnected;
      }
    }

    public override IViewChangeNotificator CreateChangeNotificator()
    {
      // We could also try to be notified when new clients are attached. Currently, that event is only reflected in the client manager's state variable
      // AttachedClients but isn't routed to a system message in the client yet.
      return new ServerConnectionChangeNotificator();
    }

    protected string BuildExtendedDisplayName(string systemId, string systemName)
    {
      return string.Format("{1} ({0})", systemId, systemName);
    }

    protected string BuildDisplayName(string systemId, string systemName)
    {
      return systemName;
    }

    /// <summary>
    /// Takes the given <paramref name="rawSystemsIds2Names"/> and builds a sorted dictionary which contains system names in simple form
    /// ("[System name]") or maybe in extended form ("[System name] ([System id])") for those systems where the names are present multiple times.
    /// </summary>
    /// <param name="rawSystemsIds2Names">Dictionary which contains the raw system data mapping of (system ids to system names) to build the
    /// result dictionary from.</param>
    /// <returns>Mapping of unique system names to their ids.</returns>
    protected IDictionary<string, string> BuildSystemsDictNames2Ids(ICollection<KeyValuePair<string, string>> rawSystemsIds2Names)
    {
      ILocalization localization = ServiceRegistration.Get<ILocalization>();
      IDictionary<string, string> systemNames2Ids = new SortedDictionary<string, string>(StringComparer.Create(localization.CurrentCulture, false));
      
      // Prepare list of double names to simplify the decision below for which system we need to add the system id
      ICollection<string> rawNames = new HashSet<string>();
      ICollection<string> doubleNames = new HashSet<string>();
      foreach (KeyValuePair<string, string> rawSystemsId2Name in rawSystemsIds2Names)
      {
        string name = rawSystemsId2Name.Value ?? string.Empty;
        if (rawNames.Contains(name))
          doubleNames.Add(name);
        else
          rawNames.Add(name);
      }
      foreach (KeyValuePair<string, string> kvp in rawSystemsIds2Names)
      {
        string systemId = kvp.Key;
        string systemName = kvp.Value ?? string.Empty;
        systemNames2Ids.Add(doubleNames.Contains(systemName) ? BuildExtendedDisplayName(systemId, systemName) : BuildDisplayName(systemId, systemName), systemId);
      }

      return systemNames2Ids;
    }

    protected internal override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      mediaItems = new List<MediaItem>();
      subViewSpecifications = new List<ViewSpecification>();
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = scm.ContentDirectory;
      IServerController sc = scm.ServerController;
      if (cd == null || sc == null)
        return;
      ICollection<KeyValuePair<string, string>> systems = new List<KeyValuePair<string, string>>(
          sc.GetAttachedClients().Select(client => new KeyValuePair<string, string>(client.SystemId, client.LastClientName)))
        {
            new KeyValuePair<string, string>(scm.HomeServerSystemId, scm.LastHomeServerName) // Add the server too
        };
      foreach (KeyValuePair<string, string> kvp in BuildSystemsDictNames2Ids(systems))
      {
        var clientShares = cd.GetShares(kvp.Value, SharesFilter.All);
        if (clientShares.Count == 0)
          continue;
      
        // Check if we want to filter only for given MediaCategories
        if (_restrictedMediaCategories != null && !clientShares.Any(share => share.MediaCategories.Intersect(_restrictedMediaCategories).Any()))
          continue;
        subViewSpecifications.Add(new SystemSharesViewSpecification(kvp.Value, kvp.Key, _necessaryMIATypeIds, _optionalMIATypeIds, _restrictedMediaCategories));
      }
    }

    #endregion
  }
}
