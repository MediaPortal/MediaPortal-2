#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Security.Cryptography.X509Certificates;
using MediaPortal.UI.SkinEngine.Controls.Visuals;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  /// <summary>
  /// Provides or generates group heder items for virtualized panels
  /// <remarks>
  /// <see cref="IItemProvider"/> for more general information.
  /// </remarks>
  /// </summary>
  public interface IGroupedItemProvider : IItemProvider
  {
    /// <summary>
    /// Returns the group header element for the <paramref name="itemIndex"/>th item
    /// </summary>
    /// <param name="itemIndex">Index of the item to get the header for.</param>
    /// <param name="isFirstVisibleItem"><c>true</c> if this is the 1st visible item in the panel.
    /// If this item is not the 1st one on the group, then the header for the 1st item in the group is returned.</param>
    /// <param name="lvParent">Visual and logical parent of the new item.</param>
    /// <param name="newCreated"><c>true</c> if the item object was newly created, i.e. it needs to be initialized/measured and
    /// arranged.</param>
    /// <returns>
    /// Returns the group header element or <c>null</c> if no heder is needed for this item
    /// </returns>
    FrameworkElement GetOrCreateGroupHeader(int itemIndex, bool isFirstVisibleItem, FrameworkElement lvParent, out bool newCreated);

    /// <summary>
    /// Gets if grouping is active
    /// </summary>
    bool IsGroupingActive { get; }

    /// <summary>
    /// Gets the number of groups
    /// </summary>
    int GroupCount { get; }

    /// <summary>
    /// Gets the group info
    /// </summary>
    /// <param name="groupIndex">Inedex of group</param>
    /// <returns></returns>
    GroupInfo GetGroupInfo(int groupIndex);

    /// <summary>
    /// Gets the group index from an item index
    /// </summary>
    /// <param name="itemIndex">Index of item</param>
    /// <returns></returns>
    int GetGroupIndex(int itemIndex);

    /// <summary>
    /// Gets the group info from an item index
    /// </summary>
    /// <param name="itemIndex">Index of item</param>
    /// <returns></returns>
    GroupInfo GetGroupInfoFromItem(int itemIndex);
  }

  /// <summary>
  /// Contains item info for grouped items
  /// </summary>
  public struct GroupInfo
  {
    public GroupInfo(int firstItem, int lastItem)
    {
      FirstItem = firstItem;
      LastItem = lastItem;
    }
    /// <summary>
    /// Index of first item in group
    /// </summary>
    public int FirstItem;

    /// <summary>
    /// Index of last item in group
    /// </summary>
    public int LastItem;

    /// <summary>
    /// Gets the number of items in the group
    /// </summary>
    public int ItemCount {  get { return LastItem - FirstItem + 1; } }
  }
}