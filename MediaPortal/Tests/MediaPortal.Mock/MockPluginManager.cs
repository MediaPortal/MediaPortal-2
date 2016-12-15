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
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Attributes;
using MediaPortal.Common.PluginManager;

namespace MediaPortal.Mock
{
  public class MockPluginManager : IPluginManager
  {
    public PluginManagerState State
    {
      get { throw new NotImplementedException(); }
    }

    public IDictionary<string, CoreAPIAttribute> CoreComponents
    {
      get { throw new NotImplementedException(); }
    }

    public IDictionary<Guid, PluginRuntime> AvailablePlugins
    {
      get { throw new NotImplementedException(); }
    }

    public bool MaintenanceMode
    {
      get { throw new NotImplementedException(); }
    }

    public void Initialize()
    {
      throw new NotImplementedException();
    }

    public void Startup(bool maintenanceMode)
    {
      throw new NotImplementedException();
    }

    public void Shutdown()
    {
      throw new NotImplementedException();
    }

    public PluginRuntime AddPlugin(IPluginMetadata pluginMetadata)
    {
      throw new NotImplementedException();
    }

    public bool TryStartPlugin(Guid pluginId, bool activate)
    {
      throw new NotImplementedException();
    }

    public bool TryStopPlugin(Guid pluginId)
    {
      throw new NotImplementedException();
    }

    public void RegisterSystemPluginItemBuilder(string builderName, IPluginItemBuilder builderInstance)
    {
      throw new NotImplementedException();
    }

    public ICollection<Guid> FindConflicts(IPluginMetadata plugin)
    {
      throw new NotImplementedException();
    }

    public ICollection<Guid> FindMissingDependencies(IPluginMetadata plugin)
    {
      throw new NotImplementedException();
    }

    public PluginItemMetadata GetPluginItemMetadata(string location, string id)
    {
      throw new NotImplementedException();
    }

    public ICollection<PluginItemMetadata> GetAllPluginItemMetadata(string location)
    {
      throw new NotImplementedException();
    }

    public ICollection<string> GetAvailableChildLocations(string location)
    {
      throw new NotImplementedException();
    }

    public T RequestPluginItem<T>(string location, string id, IPluginItemStateTracker stateTracker) where T : class
    {
      throw new NotImplementedException();
    }

    public object RequestPluginItem(string location, string id, Type type, IPluginItemStateTracker stateTracker)
    {
      throw new NotImplementedException();
    }

    public ICollection<T> RequestAllPluginItems<T>(string location, IPluginItemStateTracker stateTracker) where T : class
    {
      return new List<T>();
    }

    public ICollection RequestAllPluginItems(string location, Type type, IPluginItemStateTracker stateTracker)
    {
      throw new NotImplementedException();
    }

    public void RevokePluginItem(string location, string id, IPluginItemStateTracker stateTracker)
    {
      throw new NotImplementedException();
    }

    public void RevokeAllPluginItems(string location, IPluginItemStateTracker stateTracker)
    {
      throw new NotImplementedException();
    }

    public void AddItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener)
    {
    }

    public void RemoveItemRegistrationChangeListener(string location, IItemRegistrationChangeListener listener)
    {
    }
  }
}