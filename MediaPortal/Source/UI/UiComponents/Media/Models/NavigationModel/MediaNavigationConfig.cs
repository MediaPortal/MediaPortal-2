#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.FilterTrees;
using System;

namespace MediaPortal.UiComponents.Media.Models.NavigationModel
{
  /// <summary>
  /// Configuration to be used to override default media navigation initialization.
  /// </summary>
  public class MediaNavigationConfig
  {
    /// <summary>
    /// The screen to use as the navigation root. This screen will be used to
    /// load the screen hierarchy and will be removed from the list of available screens.
    /// </summary>
    public Type RootScreenType { get; set; }

    /// <summary>
    /// The default screen to show if there is no saved screen hierarchy. 
    /// </summary>
    public Type DefaultScreenType { get; set; }

    /// <summary>
    /// Media item id to use to apply a MediaItemIdFilter to the root media view.
    /// </summary>
    public Guid? LinkedId { get; set; }

    /// <summary>
    /// Filter to apply to the root media view.
    /// </summary>
    public IFilter Filter { get; set; }

    /// <summary>
    /// Relationship of the linked id/filter to the base media items.
    /// </summary>
    public FilterTreePath FilterPath { get; set; }
  }
}
