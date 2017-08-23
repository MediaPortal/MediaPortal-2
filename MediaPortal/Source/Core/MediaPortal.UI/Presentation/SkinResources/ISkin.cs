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

namespace MediaPortal.UI.Presentation.SkinResources
{
  public interface ISkin : ISkinResourceBundle
  {
    /// <summary>
    /// Name of the skin this skin inherits from. If this parameter is <c>null</c>, this skin inherits from the default
    /// skin's theme or from the default skin, if there is no default skin.
    /// </summary>
    string BasedOnSkin { get; }

    /// <summary>
    /// Name of the theme of the <see cref="BasedOnSkin"/> skin, this skin inherits from. If this value is <c>null</c>,
    /// we'll inherit from the <see cref="BasedOnSkin"/>, or, if that is also <c>null</c>, from the default skin's theme or
    /// from the default skin, if there is no default skin.
    /// </summary>
    string BasedOnTheme { get; }

    string UsageNote { get; }
  }
}