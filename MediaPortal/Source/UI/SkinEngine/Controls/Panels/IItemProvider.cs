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
using MediaPortal.UI.SkinEngine.Controls.Visuals;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  /// <summary>
  /// Provides or generates items for virtual panels.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Item providers are used to provide or generate items at runtime. The typical usage is when
  /// there is a huge list of items to be displayed in a <see cref="VirtualizingStackPanel"/>, an <see cref="IItemProvider"/>
  /// is used to create item objects on-the-fly. There is no need to materialize the complete huge list.
  /// </para>
  /// <para>
  /// Item providers might be able to release parts of their materialized items again. The interface contract only
  /// states that at least the range of items, for that <see cref="Keep"/> is called, must be preserved and
  /// for each index inside that range, the same items must be returned if the same index is requested again.
  /// Items outside the "keep" range must also be stable before and after a call to <see cref="Keep"/>, i.e. if items
  /// are released asynchronously, their references must not be returned any more after the <see cref="Keep"/> call.
  /// In other words, the <see cref="Keep"/> call is the only time when the blackbox view to this item provider to the outside
  /// world is allowed to change.
  /// </para>
  /// </remarks>
  public interface IItemProvider : IDisposable
  {
    /// <summary>
    /// Returns the total number of items.
    /// </summary>
    int NumItems { get; }

    /// <summary>
    /// Instructs this item provider to keep at least the elements from the indices <paramref name="start"/> to
    /// <paramref name="end"/>. Other elements might or might not be released.
    /// </summary>
    /// <param name="start">Start index of the range to keep. May be less than <c>0</c>; in that case, 0 is assumed.</param>
    /// <param name="end">End index of the range to keep. May be greater than or equal to <see cref="NumItems"/>; in that
    /// case, <c><see cref="NumItems"/> - 1</c> is assumed.</param>
    void Keep(int start, int end);

    /// <summary>
    /// Returns the generated <paramref name="index"/>th item.
    /// </summary>
    /// <param name="index">Index of the desired item.</param>
    /// <param name="lvParent">Visual and logical parent of the new item.</param>
    /// <param name="newCreated"><c>true</c> if the item object was newly created, i.e. it needs to be initialized/measured and
    /// arranged.</param>
    /// <returns>The desired item.</returns>
    FrameworkElement GetOrCreateItem(int index, FrameworkElement lvParent, out bool newCreated);
  }
}