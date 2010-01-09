#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.Core.Configuration;

namespace MediaPortal.Configuration.ConfigurationManagement
{
  public class Constants
  {
    /// <summary>
    /// Location to start searching for configuration items, in the plugintree.
    /// </summary>
    public const string PLUGINTREE_BASELOCATION = "/Configuration/Settings";

    /// <summary>
    /// Section <see cref="ConfigBaseMetadata.Text"/> which will be used when child config objects are
    /// located under a section which was not explicitly defined.
    /// </summary>
    public const string INVALID_SECTION_TEXT = "[System.Invalid]";
  }
}