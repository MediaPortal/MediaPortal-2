#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Xml;

namespace MediaPortal.PackageCore.Package
{
  /// <summary>
  /// Exception to use when a package is executed (install, update or remove)
  /// </summary>
  public class PackageExcecutionException : Exception
  {
    /// <summary>
    /// Creates a new exception.
    /// </summary>
    /// <param name="message">Message</param>
    public PackageExcecutionException(string message) : base(message) { }
  }

  /// <summary>
  /// Exception to use when parsing a package.
  /// </summary>
  public class PackageParseException : Exception
  {
    /// <summary>
    /// Creates a new exception.
    /// </summary>
    /// <param name="message">Message.</param>
    public PackageParseException(string message) : base(message) { }

    /// <summary>
    /// Creates a new file related exception.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="filePath">File path.</param>
    public PackageParseException(string message, string filePath)
      : base(message)
    {
      FilePath = filePath;
    }

    /// <summary>
    /// Creates a new XML file related exception.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="filePath">File path.</param>
    /// <param name="lineInfo">XML lien info item.</param>
    public PackageParseException(string message, string filePath, IXmlLineInfo lineInfo)
      : base(message)
    {
      FilePath = filePath;
      LineNumber = lineInfo.LineNumber;
      LinePosition = lineInfo.LinePosition;
    }

    /// <summary>
    /// Creates a new XML file related exception, where the file path is currently unknown.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="lineInfo">XML lien info item.</param>
    public PackageParseException(string message, IXmlLineInfo lineInfo)
      : base(message)
    {
      LineNumber = lineInfo.LineNumber;
      LinePosition = lineInfo.LinePosition;
    }

    /// <summary>
    /// Creates a new XML file related exception.
    /// </summary>
    /// <param name="exception">Inner exception.</param>
    /// <param name="filePath">File path.</param>
    /// <param name="lineInfo">XML lien info item.</param>
    public PackageParseException(PackageParseException exception, string filePath, IXmlLineInfo lineInfo)
      : base(exception.Message, exception)
    {
      FilePath = filePath;
      LineNumber = lineInfo.LineNumber;
      LinePosition = lineInfo.LinePosition;
    }

    /// <summary>
    /// Gets the file path.
    /// </summary>
    public string FilePath { get; internal set; }

    /// <summary>
    /// Gets the line number in the file.
    /// </summary>
    public int LineNumber { get; private set; }

    /// <summary>
    /// Gets the line position in the file.
    /// </summary>
    public int LinePosition { get; private set; }
  }
}
