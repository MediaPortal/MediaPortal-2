#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.Common.PluginManager
{
  public delegate bool ItemStateTrackerRequestEndDlgt(PluginItemRegistration itemRegistration);
  public delegate void ItemStateTrackerStopDlgt(PluginItemRegistration itemRegistration);
  public delegate void ItemStateTrackerContinueDlgt(PluginItemRegistration itemRegistration);

  /// <summary>
  /// Default implementation of a plugin item state tracker which supports callback delegates for the <see cref="RequestEnd"/>,
  /// <see cref="Stop"/> and <see cref="Continue"/> methods.
  /// </summary>
  /// <remarks>
  /// <seealso cref="FixedItemStateTracker"/>
  /// </remarks>
  public class DefaultItemStateTracker : IPluginItemStateTracker
  {
    protected string _usageDescription;
    public ItemStateTrackerRequestEndDlgt _endRequested = null;
    public ItemStateTrackerStopDlgt _stopped = null;
    public ItemStateTrackerContinueDlgt _continued = null;

    public DefaultItemStateTracker(string usageDescription)
    {
      _usageDescription = usageDescription;
    }

    public ItemStateTrackerRequestEndDlgt EndRequested
    {
      get { return _endRequested; }
      set { _endRequested = value; }
    }

    public ItemStateTrackerStopDlgt Stopped
    {
      get { return _stopped; }
      set { _stopped = value; }
    }

    public ItemStateTrackerContinueDlgt Continued
    {
      get { return _continued; }
      set { _continued = value; }
    }

    #region IPluginItemStateTracker implementation

    public string UsageDescription
    {
      get { return _usageDescription; }
    }

    bool IPluginItemStateTracker.RequestEnd(PluginItemRegistration itemRegistration)
    {
      ItemStateTrackerRequestEndDlgt dlgt = EndRequested;
      return dlgt == null || dlgt(itemRegistration);
    }

    void IPluginItemStateTracker.Stop(PluginItemRegistration itemRegistration)
    {
      ItemStateTrackerStopDlgt dlgt = Stopped;
      if (dlgt != null)
        dlgt(itemRegistration);
    }

    void IPluginItemStateTracker.Continue(PluginItemRegistration itemRegistration)
    {
      ItemStateTrackerContinueDlgt dlgt = Continued;
      if (dlgt != null)
        dlgt(itemRegistration);
    }

    #endregion
  }
}