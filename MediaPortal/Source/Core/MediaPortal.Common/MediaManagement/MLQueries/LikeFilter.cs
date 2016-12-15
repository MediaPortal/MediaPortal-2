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

using System.Text;

namespace MediaPortal.Common.MediaManagement.MLQueries
{
  /// <summary>
  /// Specifies an expression which filters media items by a string attribute which is LIKE a given string.
  /// </summary>
  public class LikeFilter : AbstractExpressionFilter
  {
    private bool _caseSensitive = true;

    public LikeFilter(MediaItemAspectMetadata.AttributeSpecification attributeType,
        string expression, char? escapeChar, bool caseSensitive) : base(attributeType, expression, escapeChar) 
    {
      _caseSensitive = caseSensitive;
    }

    public LikeFilter(MediaItemAspectMetadata.AttributeSpecification attributeType,
        string expression, char? escapeChar) : this(attributeType, expression, escapeChar, true) { }

    /// <summary>
    /// Returns the information if this filter uses a case sensitive comparison.
    /// </summary>
    public bool CaseSensitive
    {
      get { return _caseSensitive; }
      set { _caseSensitive = value; }
    }

    public override string ToString()
    {
      StringBuilder result = new StringBuilder();
      if (!_caseSensitive)
        result.Append("UPPER(");
      result.Append("[");
      result.Append(AttributeTypeToString());
      result.Append("]");
      if (!_caseSensitive)
        result.Append(")");
      result.Append(" LIKE ");
      if (!_caseSensitive)
        result.Append("UPPER(");
      result.Append(_expression);
      if (!_caseSensitive)
        result.Append(")");
      if (_escapeChar.HasValue)
      {
        result.Append(" ESCAPE '");
        result.Append(_escapeChar.Value);
        result.Append("'");
      }
      return result.ToString();
    }

    #region Additional members for the XML serialization

    internal LikeFilter() { }

    #endregion
  }
}
