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

namespace MediaPortal.Common.Localization
{
  public static class LocalizationHelper
  {
    /// <summary>
    /// Creates an instance implementing <see cref="IResourceString"/>, holding a
    /// localized or unlocalized string. This method will check, if the specified string
    /// references a localized string resource. If so, the return value will be a localized
    /// <see cref="IResourceString"/>, else it will not be localized.
    /// </summary>
    public static IResourceString CreateResourceString(string maybeLocalizationResource)
    {
      if (StringId.IsResourceString(maybeLocalizationResource))
        return new LocalizedStringBuilder(new StringId(maybeLocalizationResource));
      return new StaticStringBuilder(maybeLocalizationResource);
    }

    public static IResourceString CreateStaticString(string unlocalizedString)
    {
      return new StaticStringBuilder(unlocalizedString);
    }

    public static string Translate(string maybeLocalizationResource, params object[] parameters)
    {
      ILocalization localization = ServiceRegistration.Get<ILocalization>();
      return localization.ToString(maybeLocalizationResource, parameters);
    }
  }
}