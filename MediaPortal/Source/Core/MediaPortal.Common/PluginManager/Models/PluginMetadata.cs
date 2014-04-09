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

namespace MediaPortal.Common.PluginManager.Models
{
  /// <summary>
  /// Plugin metadata class responsible for storing all known information on a plugin. Although the
  /// classes used are not technically immutable or thread-safe, they are intended to be treated as 
  /// such - users may generally assume that the information does not change while the program is
  /// running, except for <see cref="SocialInfo"/> which is fetched lazily (by explicit request).
  /// </summary>
  public class PluginMetadata
  {
    #region Plugin Details
    /// <summary>
    /// Returns the plugin's name.
    /// </summary>
	  public string Name { get; internal set; }

    /// <summary>
    /// Returns the plugin's unique id.
    /// </summary>
	  public Guid PluginId { get; internal set; }

    /// <summary>
    /// Returns the plugin's copyright statement.
    /// </summary>
	  public string Copyright { get; internal set; }

    /// <summary>
    /// Returns the plugin's author.
    /// </summary>
	  public string Author { get; internal set; }

    /// <summary>
    /// Returns a short description of the plugins function.
    /// </summary>
	  public string Description { get; internal set; }

    /// <summary>
    /// Returns the plugin's version.
    /// </summary>
	  public string PluginVersion { get; internal set; }

    /// <summary>
    /// Returns the release date of this version of the plugin.
    /// </summary>
	  public DateTime ReleaseDate { get; internal set; }
    #endregion

    #region Additional Metadata (Source, Activation, Dependency, Social)
    /// <summary>
    /// Returns metadata with information on the current install path (if installed locally) and null otherwise. 
    /// </summary>
    public PluginSourceInfo SourceInfo { get; internal set; }

    /// <summary>
    /// Returns metadata required to activate a plugin.
    /// </summary>
    public PluginActivationInfo ActivationInfo { get; internal set; }

    /// <summary>
    /// Returns metadata required to determine plugin compatibility (and whether dependencies are satisfied).
    /// </summary>
    public PluginDependencyInfo DependencyInfo { get; internal set; }

    /// <summary>
    /// Returns social metadata (ratings, reviews) if available and null otherwise. Social metadata not
    /// available by default and must be requested explicitly.
    /// </summary>
	  public PluginSocialInfo SocialInfo { get; internal set; }
    #endregion

    #region ToString
    public override string ToString()
    {
      return LogInfo;
    }
    #endregion

    #region Internal Logging Helpers
    /// <summary>
    /// Returns a string with the plugins name, version, author and id.
    /// </summary>
    internal string LogInfo
    {
      get { return string.Format( "'{0}' (version: {1}; authors: {2}; id '{3}')", Name, PluginVersion, Author, PluginId ); }
    }

    /// <summary>
    /// Returns a string with the plugins name and id.
    /// </summary>
    internal string LogId
    {
      get { return string.Format( "'{0}' (id '{1}')", Name, PluginId ); }
    }

    /// <summary>
    /// Returns a string with the plugins name.
    /// </summary>
    internal string LogName
    {
      get { return string.Format( "'{0}'", Name ); }
    }
    #endregion
  }
}
