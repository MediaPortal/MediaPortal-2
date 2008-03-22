#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.MediaManager.Views;

namespace MediaManager
{
  public class ViewItem : IRootContainer
  {
    private readonly IView _View;
    private readonly IMediaItem _Item;

    public event MediaItemChangedHandler Changed;

    public ViewItem(IView view, IMediaItem item)
    {
      _View = view;
      _Item = item;
    }

    public string Title
    {
      get { return _Item.Title; }
    }

    public IRootContainer Parent
    {
      get { return _View; }
    }

    public IView View
    {
      get { return _View; }
    }

    public List<IAbstractMediaItem> Items
    {
      get { return _View.Get(_Item); }
    }

    public IRootContainer Root
    {
      get
      {
        if (Parent==null)
          return null;
        return Parent.Root;
      }
    }
  }
}
