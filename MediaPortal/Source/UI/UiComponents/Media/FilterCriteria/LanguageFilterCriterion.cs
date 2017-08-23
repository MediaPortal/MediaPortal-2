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

using System;
using System.Globalization;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.UiComponents.Media.FilterCriteria
{
  /// <summary>
  /// Filter criterion which creates a filter by a simple attribute value.
  /// </summary>
  public class LanguageFilterCriterion : SimpleMLFilterCriterion
  {
    public LanguageFilterCriterion(MediaItemAspectMetadata.AttributeSpecification attributeType)
      : base(attributeType)
    { }

    #region Base overrides

    protected override string GetDisplayName(object groupKey)
    {
      if (groupKey == null)
        return string.Empty;

      string lang2 = groupKey.ToString();
      try
      {
        CultureInfo cultureInfo = new CultureInfo(lang2);
        return cultureInfo.DisplayName;
      }
      catch (ArgumentException)
      { 
        return string.Empty;
      }
    }

    #endregion
  }
}
