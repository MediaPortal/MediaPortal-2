#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.UiComponents.Media.Views
{
  public class CombinedViewChangeNotificator : IViewChangeNotificator
  {
    protected ICollection<IViewChangeNotificator> _changeNotificators;

    public CombinedViewChangeNotificator(IEnumerable<IViewChangeNotificator> changeNotificators)
    {
      _changeNotificators = new List<IViewChangeNotificator>(changeNotificators);
    }

    private void OnSubChangeNotificatorChanged()
    {
      ViewChangedDlgt dlgt = Changed;
      if (dlgt != null)
        dlgt();
    }

    public event ViewChangedDlgt Changed;

    public void Install()
    {
      foreach (IViewChangeNotificator vcn in _changeNotificators)
      {
        vcn.Changed += OnSubChangeNotificatorChanged;
        vcn.Install();
      }
    }

    public void Dispose()
    {
      foreach (IViewChangeNotificator vcn in _changeNotificators)
      {
        vcn.Changed -= OnSubChangeNotificatorChanged;
        vcn.Dispose();
      }
    }

    public static IViewChangeNotificator CombineViewChangeNotificators(params IViewChangeNotificator[] changeNotificators)
    {
      return CombineViewChangeNotificators((IEnumerable<IViewChangeNotificator>) changeNotificators);
    }

    public static IViewChangeNotificator CombineViewChangeNotificators(IEnumerable<IViewChangeNotificator> changeNotificators)
    {
      IList<IViewChangeNotificator> changeNotificatorsCopy = new List<IViewChangeNotificator>(changeNotificators);
      if (changeNotificatorsCopy.Count == 0)
        return null;
      if (changeNotificatorsCopy.Count == 1)
        return changeNotificatorsCopy[0];
      return new CombinedViewChangeNotificator(changeNotificatorsCopy);
    }
  }
}