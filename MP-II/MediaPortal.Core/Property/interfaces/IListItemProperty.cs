#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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

using MediaPortal.Core.Collections;

namespace MediaPortal.Core.Properties
{
  /// <summary>
  /// interface to a property
  /// </summary>
  public interface IProperty
  {
    object Evaluate(object container);
  }

  /// <summary>
  /// interface to a list item property
  /// </summary>
  public interface IListItemProperty
  {
    ListItem Evaluate(IControl container, int index);
  }

  public class ListItemProperty : IListItemProperty
  {
    private int _index;
    private IListItemProperty _property;

    public ListItemProperty(int index, IListItemProperty prop)
    {
      _index = index;
      _property = prop;
    }

    public ListItem Evaluate(IControl container, int x)
    {
      return _property.Evaluate(container, _index);
    }
  }
}