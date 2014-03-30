using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MediaPortal.Attributes;
using MediaPortal.Common.General;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.Common.PluginManager.Validation
{
  public class Validator
  {
    private readonly ConcurrentDictionary<Guid, PluginMetadata> _models;
    private readonly ConcurrentHashSet<Guid> _disabledPlugins;
    private readonly IDictionary<string, CoreAPIAttribute> _coreComponents;

    public Validator( ConcurrentDictionary<Guid, PluginMetadata> models, ConcurrentHashSet<Guid> disabledPlugins, IDictionary<string, CoreAPIAttribute> coreComponents )
    {
      _models = models;
      _disabledPlugins = disabledPlugins;
      _coreComponents = coreComponents;
    }

    public ValidationResult Validate( PluginMetadata plugin )
    {
      var conflictValidator = new ConflictValidator( _models, _disabledPlugins );
      var dependencyValidator = new DependencyPresenceValidator( _models );
      var compatibilityValidator = new CompatibilityValidator( _models, _coreComponents );

      var result = new ValidationResult
      {
        MissingDependencies = dependencyValidator.Validate( plugin ),
        ConflictsWith = conflictValidator.Validate( plugin ),
        IncompatibleWith = compatibilityValidator.Validate( plugin )
      };
      return result;
    }
  }
}
