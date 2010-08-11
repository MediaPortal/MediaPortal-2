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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  public class FirstCharGroupingFunction : IGroupingFunctionImpl
  {
    #region Consts

    public const string EMPTY_GROUP_NAME_RES = "[MediaLibrary.EmptyGroupName]";
    public const string SPECIAL_CHARACTER_GROUP_NAME_RES = "[MediaLibrary.MiscCharacterGroupName]";

    #endregion

    protected string _emptyGroupName;
    protected string _miscCharacterGroupName;

    public FirstCharGroupingFunction()
    {
      _emptyGroupName = LocalizationHelper.Translate(EMPTY_GROUP_NAME_RES);
      _miscCharacterGroupName = LocalizationHelper.Translate(SPECIAL_CHARACTER_GROUP_NAME_RES);
    }

    public void GetGroup(MediaItemAspectMetadata.AttributeSpecification attributeType, string elementName,
        out string groupName, out IFilter additionalFilter)
    {
      elementName = elementName == null ? string.Empty : elementName.Trim();
      if (elementName == string.Empty)
      {
        groupName = _emptyGroupName;
        additionalFilter = null;
        return;
      }
      char firstChar = elementName[0];
      if (char.IsLetterOrDigit(firstChar))
      {
        string fcs = firstChar.ToString().ToUpperInvariant();
        groupName = fcs + "*";
        additionalFilter = new LikeFilter(attributeType, fcs + "%", null, false);
      }
      else
      {
        groupName = _miscCharacterGroupName;
        additionalFilter = new NotFilter(new SimilarToFilter(attributeType, "[^A-Za-z0-9].*", null));
      }
    }
  }
}