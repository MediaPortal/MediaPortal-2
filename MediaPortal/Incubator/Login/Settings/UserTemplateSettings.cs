#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.Settings;
using System.Collections.Generic;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.UiComponents.Login.Settings
{
  public class UserTemplateSettings
  {
    public static ICollection<UserProfileTemplate> DEFAULT_USER_PROFILE_TEMPLATES = new List<UserProfileTemplate>
    {
      new UserProfileTemplate
      {
        TemplateId = new Guid("{0D5E6C2E-07DA-4427-964F-A92C497BCE04}"),
        TemplateName = "[UserProfileTemplate.Admin]",
        EnableRestrictionGroups = false,
        RestrictionGroups = new List<string>(),
        RestrictAges = false,
        AllowedAge = null
      },
      new UserProfileTemplate
      {
        TemplateId = new Guid("{56477003-A827-461D-92CB-E91FADD69B7D}"),
        TemplateName = "[UserProfileTemplate.Teenager]",
        EnableRestrictionGroups = true,
        RestrictionGroups = new List<string> { "Settings", "Settings.UserProfile.ManageOwn" },
        RestrictAges = true,
        AllowedAge = 13
      },
      new UserProfileTemplate
      {
        TemplateId = new Guid("{24172540-6A51-49E7-B95C-A41D3DBECCCE}"),
        TemplateName = "[UserProfileTemplate.Child]",
        EnableRestrictionGroups = true,
        RestrictionGroups = new List<string>(),
        RestrictAges = true,
        AllowedAge = 5
      },
    };

    [Setting(SettingScope.Global)]
    public ICollection<UserProfileTemplate> UserProfileTemplates { get; set; }
  }
}
