#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.Localization;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  public class FirstLetterGroupingFunction : IGroupingFunctionImpl
  {
    #region Consts

    public const string EMPTY_GROUP_NAME_RES = "[MediaLibrary.EmptyGroupName]";
    public const string SPECIAL_CHARACTER_GROUP_NAME_RES = "[MediaLibrary.SpecialCharacterGroupName]";

    #endregion

    protected string _emptyGroupName;
    protected string _specialCharacterGroupName;
    public FirstLetterGroupingFunction()
    {
      _emptyGroupName = LocalizationHelper.Translate(EMPTY_GROUP_NAME_RES);
      _specialCharacterGroupName = LocalizationHelper.Translate(SPECIAL_CHARACTER_GROUP_NAME_RES);
    }

    public string GetGroup(string elementName)
    {
      if (string.IsNullOrEmpty(elementName))
        return _emptyGroupName;
      char firstChar = elementName[0];
      return char.IsLetterOrDigit(firstChar) ? firstChar.ToString() : _specialCharacterGroupName;
    }
  }
}