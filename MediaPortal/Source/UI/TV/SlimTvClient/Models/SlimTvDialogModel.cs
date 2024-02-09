#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// Model used for displaying a list of items in a dialog.
  /// </summary>
  public class SlimTvDialogModel
  {
    public static readonly Guid MODEL_ID = new Guid("AAED3DED-1396-4997-9C2E-6FBB95BC8FD0");

    protected ItemsList _items = new ItemsList();

    public static SlimTvDialogModel Instance
    {
      get
      {
        return (SlimTvDialogModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(MODEL_ID);
      }
    }

    /// <summary>
    /// Shows the dialog screen with the specified name and sets this
    /// model's <see cref="Items"/> property to the specified items.
    /// </summary>
    /// <param name="dialogName">Name of the dialog screen to show.
    /// The screen should bind to this model to access the current items.</param>
    /// <param name="dialogItems">The items to show.</param>
    public void ShowItemsDialog(string dialogName, IEnumerable<ListItem> dialogItems)
    {
      ItemsList items = new ItemsList();
      CollectionUtils.AddAll(items, dialogItems);
      // Simply replace the existing list, this makes the items static for every dialog
      // and avoids unnecessary updates when closing the previous dialog, etc
      _items = items;

      ServiceRegistration.Get<IScreenManager>().ShowDialog(dialogName);
    }

    public ItemsList Items
    {
      get { return _items; }
    }
  }
}
