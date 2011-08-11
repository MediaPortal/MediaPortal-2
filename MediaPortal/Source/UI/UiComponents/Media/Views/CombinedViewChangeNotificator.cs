#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Linq;

namespace MediaPortal.UiComponents.Media.Views
{
  public class CombinedViewChangeNotificator : IViewChangeNotificator
  {
    protected ICollection<IViewChangeNotificator> _changeNotificators;

    public CombinedViewChangeNotificator(IEnumerable<IViewChangeNotificator> changeNotificators)
    {
      _changeNotificators = new List<IViewChangeNotificator>(changeNotificators);
      foreach (IViewChangeNotificator vcn in changeNotificators)
        vcn.Changed += OnSubChangeNotificatorChanged;
    }

    private void OnSubChangeNotificatorChanged()
    {
      ViewChangedDlgt dlgt = Changed;
      if (dlgt != null)
        dlgt();
    }

    public event ViewChangedDlgt Changed;

    public void install()
    {
      foreach (IViewChangeNotificator vcn in _changeNotificators)
        vcn.install();
    }

    public void Dispose()
    {
      foreach (IViewChangeNotificator vcn in _changeNotificators)
      {
        vcn.Changed -= OnSubChangeNotificatorChanged;
        vcn.Dispose();
      }
    }

    public static IViewChangeNotificator CombineViewChangeNotificators<T>(IEnumerable<T> viewSpecifications) where T : ViewSpecification
    {
      ICollection<IViewChangeNotificator> subChangeNotificators = viewSpecifications.Select(vs => vs.GetChangeNotificator()).Where(vcn => vcn != null).ToList();
      if (subChangeNotificators.Count == 0)
        return null;
      return new CombinedViewChangeNotificator(subChangeNotificators);
    }
  }
}