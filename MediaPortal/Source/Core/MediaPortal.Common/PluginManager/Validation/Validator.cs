#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Attributes;
using MediaPortal.Common.General;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.Common.PluginManager.Validation
{
  /// <summary>
  /// Validation helper class used to execute all known validators with a singe method call,
  /// which collects information on all validation errors and returns them as a
  /// <see cref="ValidationResult"/>.
  /// </summary>
  internal class Validator
  {
    #region Fields

    private readonly ConcurrentDictionary<Guid, PluginMetadata> _models;
    private readonly ConcurrentHashSet<Guid> _disabledPlugins;
    private readonly IDictionary<string, CoreAPIAttribute> _coreComponents;

    #endregion

    #region Ctor

    public Validator(ConcurrentDictionary<Guid, PluginMetadata> models, ConcurrentHashSet<Guid> disabledPlugins, IDictionary<string, CoreAPIAttribute> coreComponents)
    {
      _models = models;
      _disabledPlugins = disabledPlugins;
      _coreComponents = coreComponents;
    }

    #endregion

    #region Validate

    /// <summary>
    /// Executes all known validators for a single plugin and returns information with
    /// all errors found.
    /// </summary>
    /// <param name="plugin">The plugin to validate</param>
    /// <returns>
    /// A custom <see cref="ValidationResult"/> with all errors found by the
    /// individual validators.
    /// </returns>
    public ValidationResult Validate(PluginMetadata plugin)
    {
      var conflictValidator = new ConflictValidator(_models, _disabledPlugins);
      var dependencyValidator = new DependencyPresenceValidator(_models);
      var compatibilityValidator = new CompatibilityValidator(_models, _coreComponents);

      var result = new ValidationResult
      {
        MissingDependencies = dependencyValidator.Validate(plugin),
        ConflictsWith = conflictValidator.Validate(plugin),
        IncompatibleWith = compatibilityValidator.Validate(plugin)
      };
      return result;
    }

    #endregion
  }
}