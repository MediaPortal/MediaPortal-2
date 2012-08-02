#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MP2_PluginWizard.Model
{
  /// <summary>
  /// Holds the data for Builders .
  /// </summary>
  public class PluginBuilder
  {
    #region Ctor

    public PluginBuilder(string builderName, string builderClassName)
    {
      Name = builderName;
    	ClassName = builderClassName;
    }

    #endregion

    #region Public properties
    ///<summary>
    ///Returns the name of the builder.
    ///</summary>
    public string Name { get; private set; }

    /// <summary>
    /// Returns the class name of the builder.
    /// </summary>
    public string ClassName { get; private set; }

    
    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("{0} [{1}]", Name, ClassName);
    }

    #endregion
  }
}
