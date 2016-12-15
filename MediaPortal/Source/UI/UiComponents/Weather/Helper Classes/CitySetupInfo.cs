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
using MediaPortal.UiComponents.Weather.Grabbers;

namespace MediaPortal.UiComponents.Weather
{
  /// <summary>
  /// This is a basic class where the information for the provider are stored (usually the location name and unique id).
  /// </summary>
  public class CitySetupInfo
  {
    protected string _grabber;
    protected readonly AbstractProperty _id = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _name = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _detail = new WProperty(typeof(string), string.Empty);

    public CitySetupInfo(string name, string id, string grabber)
    {
      Name = name;
      Id = id;
      _grabber = grabber;
    }

    public CitySetupInfo(string name, string id) :
      this(name, id, WorldWeatherOnlineCatcher.SERVICE_NAME)
    { }

    public CitySetupInfo() {}

    /// <summary>
    /// Gets the name of the City.
    /// </summary>
    public string Name
    {
      get { return (string) _name.GetValue(); }
      set { _name.SetValue(value); }
    }

    /// <summary>
    /// Get the Location ID.
    /// </summary>
    public string Id
    {
      get { return (string) _id.GetValue(); }
      set { _id.SetValue(value); }
    }

    /// <summary>
    /// Get additional detail information to allow to distinguish multiple location matches. Content depends on the available data of the grabber.
    /// </summary>
    public string Detail
    {
      get { return (string) _detail.GetValue(); }
      set { _detail.SetValue(value); }
    }

    /// <summary>
    /// Get the Grabber Service that should be used to fetch data for this city.
    /// </summary>
    public string Grabber
    {
      get { return _grabber; }
      set { _grabber = value; }
    }

    public override string ToString()
    {
      return Name;
    }
  }
}