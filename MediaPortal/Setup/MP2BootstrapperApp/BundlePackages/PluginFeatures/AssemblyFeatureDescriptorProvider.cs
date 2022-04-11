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

namespace MP2BootstrapperApp.BundlePackages.PluginFeatures
{
  /// <summary>
  /// Implementation of <see cref="IPluginFeatureDescriptorProvider"/> that gets implementaions
  /// of <see cref="IPluginFeatureDescriptor"/> via reflection of the current assembly.
  /// </summary>
  public class AssemblyFeatureDescriptorProvider : IPluginFeatureDescriptorProvider
  {
    public IEnumerable<IPluginFeatureDescriptor> GetDescriptors()
    {
      // Get all non-abstract types that implement IPluginFeatureDescriptor
      IEnumerable<Type> featureDescriptorTypes = Assembly.GetExecutingAssembly().GetTypes()
        .Where(t => typeof(IPluginFeatureDescriptor).IsAssignableFrom(t) && !t.IsAbstract);

      List<IPluginFeatureDescriptor> featureDescriptors = new List<IPluginFeatureDescriptor>();

      foreach (Type featureDescriptorType in featureDescriptorTypes)
      {
        // Look for a paramterless constructor and instantiate the type
        ConstructorInfo constructor = featureDescriptorType.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
          throw new InvalidOperationException($"Cannot instantiate {nameof(IPluginFeatureDescriptor)} of type {featureDescriptorType}, it does not contain a parameterless public constructor");

        IPluginFeatureDescriptor pluginDescriptor = constructor.Invoke(null) as IPluginFeatureDescriptor;
        featureDescriptors.Add(pluginDescriptor);
      }

      return featureDescriptors;
    }
  }
}
