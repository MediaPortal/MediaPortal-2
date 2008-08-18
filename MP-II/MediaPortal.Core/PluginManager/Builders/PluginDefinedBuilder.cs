#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 *  Code modified from SharpDevelop AddIn code
 *  Thanks goes to: Mike Krüger
 */

#endregion

using System;
using MediaPortal.Core;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.PluginManager.PluginSpace;
using MediaPortal.Services.PluginManager.PluginDetails;

namespace MediaPortal.Services.PluginManager.Builders
{
  /// <summary>
  /// This Builder lazy-loads another IPluginItemBuilder when it has to build an item.
  /// It is used internally to wrap Factories specified in plugins.
  /// </summary>
  internal class PluginDefinedBuilder : IPluginItemBuilder
  {
    #region Variables
    IPluginInfo _plugin;
    string _builderName;
    string _className;
    #endregion

    #region Constructors/Destructors
    internal PluginDefinedBuilder(IPluginInfo plugin, PluginProperties properties)
    {
      this._plugin = plugin;
      this._builderName = properties["name"];
      this._className = properties["class"];
    }
    #endregion

    #region Properties
    /// <summary>
    /// Returns the factory name that will be created by this factory
    /// </summary>
    public string BuilderName
    {
      get { return _builderName; }
    }
    #endregion


    #region IPluginItemBuilder Members
    public object BuildItem(IPluginRegisteredItem item)
    {
      IPluginItemBuilder builder = (IPluginItemBuilder)_plugin.CreateObject(_className);
      if (builder == null)
      {
        return null;
      }

      // replace LoadBuilder with instance of factory
      ServiceScope.Get<IPluginTree>().Builders[_builderName] = builder;

      return builder.BuildItem(item);
    }
    #endregion

    #region <Base class> Overloads
    public override string ToString()
    {
      return String.Format("[PluginDefinedBuilder: builderName = {0}, className = {0}]",
                           _builderName,
                           _className);
    }
    #endregion
  }
}
