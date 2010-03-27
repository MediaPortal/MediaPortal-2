#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.PluginManager;

namespace MediaPortal.Core.Services.PluginManager
{
  /// <summary>
  /// Plugin builder registration class. Instances of this class are registered at the plugin manager to
  /// build plugin item instances.
  /// </summary>
  public class PluginBuilderRegistration
  {
    #region Protected fields

    protected string _builderName;
    protected string _builderClassName;
    protected PluginRuntime _pluginRuntime;

    protected IPluginItemBuilder _builderInstance = null;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new plugin builder registration data structure.
    /// </summary>
    /// <param name="builderName">The name of the builder to be registered.</param>
    /// <param name="builderClassName">The class name of the builder to be registered.</param>
    /// <param name="parentPlugin">The plugin which registered this builder.</param>
    public PluginBuilderRegistration(string builderName, string builderClassName, PluginRuntime parentPlugin)
    {
      _builderName = builderName;
      _builderClassName = builderClassName;
      _pluginRuntime = parentPlugin;
    }

    #endregion

    /// <summary>
    /// Returns the plugin builder's name.
    /// </summary>
    public string BuilderName
    {
      get { return _builderName; }
    }

    /// <summary>
    /// Returns the plugin builder's class name.
    /// </summary>
    public string BuilderClassName
    {
      get { return _builderClassName; }
    }

    /// <summary>
    /// Gets the information if this builder already instantiated.
    /// </summary>
    public bool IsInstantiated
    {
      get { return _builderInstance != null; }
    }

    /// <summary>
    /// Gets the builder's instance, if it was already created. Else, the value will be <c>null</c>.
    /// </summary>
    public IPluginItemBuilder Builder
    {
      get { return _builderInstance; }
      set { _builderInstance = value; }
    }

    /// <summary>
    /// Gets the runtime class of the plugin which registered this builder.
    /// </summary>
    internal PluginRuntime PluginRuntime
    {
      get { return _pluginRuntime; }
    }
  }
}
