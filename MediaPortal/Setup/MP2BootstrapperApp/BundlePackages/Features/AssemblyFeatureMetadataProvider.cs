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

namespace MP2BootstrapperApp.BundlePackages.Features
{
  /// <summary>
  /// Implementation of <see cref="IFeatureMetadataProvider"/> that gets implementaions
  /// of <see cref="IFeatureMetadata"/> via reflection of the current assembly.
  /// </summary>
  public class AssemblyFeatureMetadataProvider : IFeatureMetadataProvider
  {
    public IEnumerable<IFeatureMetadata> GetMetadata()
    {
      // Get all non-abstract types that implement IFeatureMetadata
      IEnumerable<Type> metadataTypes = Assembly.GetExecutingAssembly().GetTypes()
        .Where(t => typeof(IFeatureMetadata).IsAssignableFrom(t) && !t.IsAbstract);

      List<IFeatureMetadata> featureMetadata = new List<IFeatureMetadata>();

      foreach (Type metadataType in metadataTypes)
      {
        // Look for a paramterless constructor and instantiate the type
        ConstructorInfo constructor = metadataType.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
          throw new InvalidOperationException($"Cannot instantiate {nameof(IFeatureMetadata)} of type {metadataType}, it does not contain a parameterless public constructor");

        IFeatureMetadata metadata = constructor.Invoke(null) as IFeatureMetadata;
        featureMetadata.Add(metadata);
      }

      return featureMetadata;
    }
  }
}
