#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *	Copyright (C) 2007-2009 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion
using System;
using System.Collections.Generic;
using System.Text;
using ProjectInfinity;
using ProjectInfinity.Localisation;

namespace MyWeather
{
    /// <summary>
    /// class that Localizes static My Weather labels
    /// localized Object is exported by the ViewModel
    /// </summary>
    public class WeatherLocalizer
    {
        /// <summary>
        /// Gets the current date
        /// </summary>
        /// <value>The date label.</value>
        public string Date
        {
            get
            {
                if (Core.IsDesignMode) return "Monday, 3th May 2007";
                return DateTime.Now.ToString("dd-MM HH:mm");
            }
        }
        /// <summary>
        /// Gets the localized version of the location label.
        /// </summary>
        /// <value>The location button label.</value>
        public string Location
        {
            get
            {
                if (Core.IsDesignMode) return "Location";
                return ServiceScope.Get<ILocalisation>().ToString("myweather", 1);//Location
            }
        }

        /// <summary>
        /// Gets the localized version of the refresh label.
        /// </summary>
        /// <value>The refresh button label.</value>
        public string Refresh
        {
            get
            {
                if (Core.IsDesignMode) return "Refresh";
                return ServiceScope.Get<ILocalisation>().ToString("myweather", 2); //Refresh
            }
        }

        /// <summary>
        /// Gets the the localized version of the header label.
        /// </summary>
        /// <value>The header label.</value>
        public string Header
        {
            get
            {
                if (Core.IsDesignMode) return "weather";
                return ServiceScope.Get<ILocalisation>().ToString("myweather", 3); //weather
            }
        }
        /// <summary>
        /// Gets the the localized version of the temperature label.
        /// </summary>
        public string Temperature
        {
            get
            {
                if (Core.IsDesignMode) return "Temp";
                return ServiceScope.Get<ILocalisation>().ToString("myweather", 6); //Temp
            }
        }
        /// <summary>
        /// Gets the the localized version of the feelslike label.
        /// </summary>
        public string FeelsLike
        {
            get
            {
                if (Core.IsDesignMode) return "Feels Like";
                return ServiceScope.Get<ILocalisation>().ToString("myweather", 7); //Feels Like
            }
        }
        /// <summary>
        /// Gets the the localized version of the uvindex label.
        /// </summary>
        public string UVIndex
        {
            get
            {
                if (Core.IsDesignMode) return "UV Index";
                return ServiceScope.Get<ILocalisation>().ToString("myweather", 8); //UVIndex
            }
        }
        /// <summary>
        /// Gets the the localized version of the wind label.
        /// </summary>
        public string Wind
        {
            get
            {
                if (Core.IsDesignMode) return "Wind";
                return ServiceScope.Get<ILocalisation>().ToString("myweather", 9); //Wind
            }
        }
        /// <summary>
        /// Gets the the localized version of the humidity label.
        /// </summary>
        public string Humidity
        {
            get
            {
                if (Core.IsDesignMode) return "Humidity";
                return ServiceScope.Get<ILocalisation>().ToString("myweather", 10); //Humidity
            }
        }
        /// <summary>
        /// Gets the the localized version of the dewpoint label.
        /// </summary>
        public string DewPoint
        {
            get
            {
                if (Core.IsDesignMode) return "Dew Point";
                return ServiceScope.Get<ILocalisation>().ToString("myweather", 11); //Dew Point
            }
        }
        /// <summary>
        /// Gets the the localized version of the sunrise label.
        /// </summary>
        public string Sunrise
        {
            get
            {
                if (Core.IsDesignMode) return "Sunrise";
                return ServiceScope.Get<ILocalisation>().ToString("myweather", 12); //Sunrise
            }
        }
        /// <summary>
        /// Gets the the localized version of the sunset label.
        /// </summary>
        public string Sunset
        {
            get
            {
                if (Core.IsDesignMode) return "Sunset";
                return ServiceScope.Get<ILocalisation>().ToString("myweather", 13); //Sunset
            }
        }
    }
}
