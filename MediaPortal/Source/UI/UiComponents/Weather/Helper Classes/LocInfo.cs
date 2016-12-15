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

using MediaPortal.Common.General;

namespace MediaPortal.UiComponents.Weather
{
  /// <summary>
  /// Provides data of a location.
  /// </summary>
  public class LocInfo
  {
    protected readonly AbstractProperty _time = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _lat = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _lon = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _sunRise = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _sunSet = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _zone = new WProperty(typeof(string), string.Empty);

    public string Time
    {
      get { return (string) _time.GetValue(); }
      set { _time.SetValue(value); }
    }

    public string Lat
    {
      get { return (string) _lat.GetValue(); }
      set { _lat.SetValue(value); }
    }

    public string Lon
    {
      get { return (string) _lon.GetValue(); }
      set { _lon.SetValue(value); }
    }

    public string SunRise
    {
      get { return (string) _sunRise.GetValue(); }
      set { _sunRise.SetValue(value); }
    }

    public string SunSet
    {
      get { return (string) _sunSet.GetValue(); }
      set { _sunSet.SetValue(value); }
    }

    public string Zone
    {
      get { return (string) _zone.GetValue(); }
      set { _zone.SetValue(value); }
    }

    public AbstractProperty TimeProperty
    {
      get { return _time; }
    }

    public AbstractProperty LatProperty
    {
      get { return _lat; }
    }

    public AbstractProperty LonProperty
    {
      get { return _lon; }
    }

    public AbstractProperty SunRiseProperty
    {
      get { return _sunRise; }
    }

    public AbstractProperty SunSetProperty
    {
      get { return _sunSet; }
    }

    public AbstractProperty ZoneProperty
    {
      get { return _zone; }
    }
  }
}