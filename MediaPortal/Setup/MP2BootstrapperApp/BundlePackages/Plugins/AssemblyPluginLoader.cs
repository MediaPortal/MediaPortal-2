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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MP2BootstrapperApp.BundlePackages.Plugins
{
  /// <summary>
  /// Implementation of <see cref="IPluginLoader"/> that loads <see cref="IPluginDescriptor"/>s from the current assembly.
  /// </summary>
  public class AssemblyPluginLoader : IPluginLoader
  {
    public IEnumerable<IPluginDescriptor> GetPluginDescriptors()
    {
      // Get all non-abstract types that implement IPluginDescriptor
      IEnumerable<Type> pluginDescriptorTypes = Assembly.GetExecutingAssembly().GetTypes()
        .Where(t => typeof(IPluginDescriptor).IsAssignableFrom(t) && !t.IsAbstract);

      List<IPluginDescriptor> pluginDescriptors = new List<IPluginDescriptor>();

      foreach (Type pluginDescriptorType in pluginDescriptorTypes)
      {
        // Look for a paramterless constructor and instantiate the type
        ConstructorInfo constructor = pluginDescriptorType.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
          throw new InvalidOperationException($"Cannot instantiate {nameof(IPluginDescriptor)} of type {pluginDescriptorType}, it does not contain a parameterless public constructor");

        IPluginDescriptor pluginDescriptor = constructor.Invoke(null) as IPluginDescriptor;
        pluginDescriptors.Add(pluginDescriptor);
      }

      return pluginDescriptors;
    }
  }
}
