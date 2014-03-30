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

using MediaPortal.Common.PluginManager.Packages;

namespace MediaPortal.Common.PluginManager.Models
{
  /// <summary>
  /// Plugin metadata interface. Extends the <see cref="IPluginPackageInfo"/> with additional metadata
  /// properties describing various aspects of the plugin.
  /// </summary>
  public interface IPluginMetadata : IPluginPackageInfo
  {
    /// <summary>
    /// Returns metadata with information on the current install path (if installed locally) and null otherwise. 
    /// </summary>
    PluginSourceInfo SourceInfo { get; }

    /// <summary>
    /// Returns metadata required to activate a plugin.
    /// </summary>
    PluginActivationInfo ActivationInfo { get; }

    /// <summary>
    /// Returns metadata required to determine plugin compatibility (and whether dependencies are satisfied).
    /// </summary>
    PluginDependencyInfo DependencyInfo { get; }

    /// <summary>
    /// Returns social metadata (ratings, reviews) if available and null otherwise. Social metadata is fetched 
    /// from the <see cref="IPackageRepository" />.
    /// </summary>
    PluginSocialInfo SocialInfo { get; }
  }
}
