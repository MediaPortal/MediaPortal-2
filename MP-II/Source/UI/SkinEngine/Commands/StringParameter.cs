#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Presentation.Commands;

namespace MediaPortal.SkinEngine.Commands
{
  public class StringParameter : ICommandParameter
  {
    #region variables

    private string _text;

    #endregion

    #region ICommandParameter Members

    /// <summary>
    /// Initializes a new instance of the <see cref="StringParameter"/> class.
    /// </summary>
    /// <param name="text">The text.</param>
    public StringParameter(string text)
    {
      _text = text;
    }

    /// <summary>
    /// returns the parameter value
    /// </summary>
    /// <value>The value.</value>
    public object Value
    {
      get { return _text; }
    }

    #endregion
  }
}
