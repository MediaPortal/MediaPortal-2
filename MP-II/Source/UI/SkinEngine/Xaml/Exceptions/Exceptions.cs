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

namespace MediaPortal.UI.SkinEngine.Xaml.Exceptions
{
  /// <summary>
  /// Base class for all exceptions during the XAML parsing process.
  /// </summary>
  public class XamlException : ApplicationException
  {
    public XamlException(string msg, params object[] args):
      base(string.Format(msg, args)) { }
    public XamlException(string msg, Exception ex, params object[] args):
      base(string.Format(msg, args), ex) { }
  }

  /// <summary>
  /// Thrown if a declared XAML namespace is not supported.
  /// </summary>
  public class XamlNamespaceNotSupportedException : XamlException
  {
    public XamlNamespaceNotSupportedException(string msg, params object[] args):
      base(msg, args) { }
    public XamlNamespaceNotSupportedException(string msg, Exception ex, params object[] args):
      base(msg, ex, args) { }
  }

  /// <summary>
  /// Thrown if a XAML parsing error occurs.
  /// </summary>
  public class XamlParserException : XamlException
  {
    public XamlParserException(string msg, params object[] args):
      base(msg, args) { }
    public XamlParserException(string msg, Exception ex, params object[] args):
      base(msg, ex, args) { }
  }

  /// <summary>
  /// Thrown if the XAML parser cannot bind a property or event to
  /// a visual's element class.
  /// </summary>
  public class XamlBindingException : XamlException
  {
    public XamlBindingException(string msg, params object[] args):
      base(msg, args) { }
    public XamlBindingException(string msg, Exception ex, params object[] args):
      base(msg, ex, args) { }
  }

  /// <summary>
  /// Thrown if a file could not be load.
  /// </summary>
  public class XamlLoadException : XamlException
  {
    public XamlLoadException(string msg, params object[] args):
      base(msg, args) { }
    public XamlLoadException(string msg, Exception ex, params object[] args):
      base(msg, ex, args) { }
  }

  /// <summary>
  /// Thrown if a type conversion didn't succeed.
  /// </summary>
  public class ConvertException : ApplicationException
  {
    public ConvertException(string msg, params object[] args):
      base(string.Format(msg, args)) { }
    public ConvertException(string msg, Exception ex, params object[] args):
      base(string.Format(msg, args), ex) { }
  }
}
