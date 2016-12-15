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
using System.Collections;

namespace MediaPortal.UI.SkinEngine.MpfElements
{
  /// <summary>
  /// Provides data for selection changed events.
  /// </summary>
  public class SelectionChangedEventArgs : RoutedEventArgs
  {
    public SelectionChangedEventArgs(RoutedEvent id, IList removedItems, IList addedItems) :
      base(id)
    {
      if (removedItems == null)
        throw new ArgumentNullException("removedItems");
      if (addedItems == null)
        throw new ArgumentNullException("addedItems");

      RemovedItems = new object[removedItems.Count];
      removedItems.CopyTo((Array) RemovedItems, 0);

      AddedItems = new object[addedItems.Count];
      addedItems.CopyTo((Array) AddedItems, 0);
    }

    #region public properties

    /// <summary>
    /// Gets an list with all removed (unselected) items.
    /// </summary>
    public IList RemovedItems { get; private set; }

    /// <summary>
    /// Gets an list with all added (selected) items.
    /// </summary>
    public IList AddedItems { get; private set; }

    /// <summary>
    /// Gets the 1st added item or <c>null</c> if no item was added.
    /// </summary>
    public object FirstAddedItem
    {
      get { return AddedItems != null && AddedItems.Count > 0 ? AddedItems[0] : null; }
    }

    #endregion

    #region base overrides

    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
    {
      var handler = genericHandler as SelectionChangedEventHandler;
      if (handler != null)
      {
        handler(genericTarget, this);
      }
      else
      {
        base.InvokeEventHandler(genericHandler, genericTarget);
      }
    }

    #endregion
  }

  /// <summary>
  /// Represents the method that will handle selection changed events.
  /// </summary>
  /// <param name="sender">Sender of the event</param>
  /// <param name="e">Event arguments for this event.</param>
  public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);
}
