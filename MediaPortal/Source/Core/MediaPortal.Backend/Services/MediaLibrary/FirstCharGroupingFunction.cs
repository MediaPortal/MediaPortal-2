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

using System.Collections.Generic;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  public class FirstCharGroupingFunction : IGroupingFunctionImpl
  {
    #region Consts

    public const string EMPTY_OR_MISC_CHAR_GROUP_NAME_RES = "[MediaLibrary.EmptyOrMiscCharGroupName]";

    #endregion

    protected MediaItemAspectMetadata.AttributeSpecification _attributeType;
    protected IFilter _emptyOrMiscGroupFilter = null;
    protected static string _emptyOrMiscCharacterGroupName = null;
    protected static string _letters = null;

    public FirstCharGroupingFunction(MediaItemAspectMetadata.AttributeSpecification attributeType)
    {
      _attributeType = attributeType;
      if (_emptyOrMiscCharacterGroupName == null)
        _emptyOrMiscCharacterGroupName = LocalizationHelper.Translate(EMPTY_OR_MISC_CHAR_GROUP_NAME_RES);
      // TODO: How to get all valid letters in all variants (with umlauts etc.)?
      _letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    }

    protected IFilter GetEmptyOrMiscGroupFilter()
    {
      if (_emptyOrMiscGroupFilter == null)
      {
        ICollection<IFilter> letterAndDigitFilters = new List<IFilter>();
        foreach (char c in _letters)
          letterAndDigitFilters.Add(new LikeFilter(_attributeType, c + "%", null, false));
        for (char c = '0'; c <= '9'; c++)
          letterAndDigitFilters.Add(new LikeFilter(_attributeType, c + "%", null));
        _emptyOrMiscGroupFilter = new BooleanCombinationFilter(BooleanOperator.Or, new IFilter[]
              {
                  new EmptyFilter(_attributeType),
                  new NotFilter(new BooleanCombinationFilter(BooleanOperator.Or, letterAndDigitFilters))
              });
      }
      return _emptyOrMiscGroupFilter;
    }

    protected bool IsLetterOrDigit(char c)
    {
      return (c >= '0' && c <= '9') || _letters.Contains(c.ToString());
    }

    public void GetGroup(object elementValue,
        out object groupKey, out IFilter additionalFilter)
    {
      string elementName = elementValue == null ? string.Empty : elementValue.ToString().Trim();
      char firstChar;
      if (elementName != string.Empty && IsLetterOrDigit(firstChar = elementName[0]))
      {
        string fcs = firstChar.ToString().ToUpperInvariant();
        groupKey = fcs + "*";
        additionalFilter = new LikeFilter(_attributeType, fcs + "%", null, false);
      }
      else
      {
        groupKey = _emptyOrMiscCharacterGroupName;
        additionalFilter = GetEmptyOrMiscGroupFilter();
      }
    }

    public int Compare(object x, object y)
    {
      string s1 = x as string;
      string s2 = y as string;
      return string.Compare(s1, s2);
    }
  }
}