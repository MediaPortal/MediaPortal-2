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

using MediaPortal.Common.PluginManager;

namespace MediaPortal.Common.PluginItemBuilders
{
  /// <summary>
  /// This class registers additional plugin item builders at the <see cref="IPluginManager">plugin manager</see> which
  /// are provided by the <c>MediaPortal.Common</c> project.
  /// </summary>
  public class AdditionalPluginItemBuilders
  {
    public const string MIA_TYPE_REGISTRATION_BUILDER_NAME = "MIATypeRegistration";

    public static void Register()
    {
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      pluginManager.RegisterSystemPluginItemBuilder(MIA_TYPE_REGISTRATION_BUILDER_NAME, new MIATypeRegistrationBuilder());
    }
  }
}