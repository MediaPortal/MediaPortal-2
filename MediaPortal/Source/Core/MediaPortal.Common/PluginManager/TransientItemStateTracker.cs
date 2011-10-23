#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
  /// <summary>
  /// Default implementation of a plugin item state tracker which will always allow to reject the item.
  /// </summary>
  /// <remarks>
  /// <seealso cref="FixedItemStateTracker"/>
  /// </remarks>
  public class TransientItemStateTracker : IPluginItemStateTracker
  {
    protected string _usageDescription;

    public TransientItemStateTracker(string usageDescription)
    {
      _usageDescription = usageDescription;
    }

    public string UsageDescription
    {
      get { return _usageDescription; }
    }

    public bool RequestEnd(PluginItemRegistration itemRegistration)
    {
      return true;
    }

    public void Stop(PluginItemRegistration itemRegistration) { }

    public void Continue(PluginItemRegistration itemRegistration) { }
  }
}