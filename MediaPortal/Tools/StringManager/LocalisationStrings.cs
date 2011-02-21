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

using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace MediaPortal.Tools.StringManager
{
  public class LocalisationStrings
  {

    #region Variables

    /// <summary>
    /// All available languages.
    /// </summary>
    private readonly Dictionary<string, CultureInfo> _availableLanguages;
    /// <summary>
    /// The directory containing all xml-files with strings.
    /// </summary>
    private readonly string _systemDirectory;

    #endregion

    #region Constructors/Destructors

    public LocalisationStrings(string systemDirectory)
    {
      _systemDirectory = systemDirectory;
      _availableLanguages = new Dictionary<string, CultureInfo>();
      SetAvailableLanguages();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the available languages.
    /// </summary>
    /// <returns></returns>
    public CultureInfo[] GetAvailableLanguages()
    {
      CultureInfo[] available = new CultureInfo[_availableLanguages.Count];
      _availableLanguages.Values.CopyTo(available, 0);
      return available;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Sets <see cref="_availableLanguages"/>.
    /// </summary>
    private void SetAvailableLanguages()
    {
      foreach (string filePath in Directory.GetFiles(_systemDirectory, "strings_*.xml"))
      {
        int pos = filePath.IndexOf('_') + 1;
        string cultName = filePath.Substring(pos, filePath.Length - Path.GetExtension(filePath).Length - pos);
        try
        {
          CultureInfo cultInfo = new CultureInfo(cultName);
          _availableLanguages.Add(cultName, cultInfo);
        }
        catch (ArgumentException)
        {
          // Log file error?
        }
      }
    }

    #endregion

  }
}
