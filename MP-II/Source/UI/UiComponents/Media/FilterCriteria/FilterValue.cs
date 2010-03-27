#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.MediaManagement.MLQueries;

namespace UiComponents.Media.FilterCriteria
{
  public class FilterValue
  {
    protected string _title;
    protected object _value;
    protected int _numItems = -1;
    protected MLFilterCriterion _criterion;

    public FilterValue(string title, object value, MLFilterCriterion criterion)
    {
      _title = title;
      _value = value;
      _criterion = criterion;
    }

    public FilterValue(string title, object value, int numItems, MLFilterCriterion criterion)
    {
      _title = title;
      _value = value;
      _numItems = numItems;
      _criterion = criterion;
    }

    public string Title
    {
      get { return _title; }
    }

    public object Value
    {
      get { return _value; }
    }

    public bool HasNumItems
    {
      get { return _numItems > -1; }
    }

    public int NumItems
    {
      get { return _numItems; }
    }

    public MLFilterCriterion Criterion
    {
      get { return _criterion; }
    }

    public IFilter Filter
    {
      get { return _criterion.CreateFilter(this); }
    }
  }
}
