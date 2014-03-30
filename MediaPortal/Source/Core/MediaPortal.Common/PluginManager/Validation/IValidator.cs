using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MediaPortal.Attributes;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.Common.PluginManager.Validation
{
  public interface IValidator
  {
    HashSet<Guid> Validate( PluginMetadata plugin );
  }
}
