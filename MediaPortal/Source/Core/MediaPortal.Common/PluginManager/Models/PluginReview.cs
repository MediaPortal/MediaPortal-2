#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System;

namespace MediaPortal.Common.PluginManager.Models
{
  /// <summary>
  /// Plugin metadata class representing a single review comment. This class is used in the
  /// <see cref="PluginSocialInfo"/> metadata model.
  /// </summary>
  public class PluginReview
  {
    public string Author { get; private set; }
    public DateTime Created { get; private set; }
    public string Subject { get; private set; }
    public string Body { get; private set; }
    public string PluginVersion { get; private set; }

    public PluginReview( string author, DateTime created, string subject, string body, string pluginVersion )
    {
      Author = author;
      Created = created;
      Subject = subject;
      Body = body;
      PluginVersion = pluginVersion;
    }
  }
}
