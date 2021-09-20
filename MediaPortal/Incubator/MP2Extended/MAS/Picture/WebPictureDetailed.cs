#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

namespace MediaPortal.Plugins.MP2Extended.MAS.Picture
{
    public class WebPictureDetailed : WebPictureBasic, IRatingSortable
    {     
        public string Subject { get; set; }
        public string Comment { get; set; }
        public string CameraManufacturer { get; set; }
        public string CameraModel { get; set; }
        public string Copyright { get; set; }
        public double Mpixel { get; set; }
        public string Height { get; set; }
        public string Width { get; set; }
        public string Dpi { get; set; }
        public string Author { get; set; }
        public float Rating { get; set; }
    }
}
