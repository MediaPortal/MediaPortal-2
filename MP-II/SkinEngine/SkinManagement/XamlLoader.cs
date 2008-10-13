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
using System.IO;
using System.Reflection;
using MediaPortal.SkinEngine.Xaml.Exceptions;
using MediaPortal.SkinEngine.Xaml;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.MpfElements;
using MediaPortal.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.SkinEngine.SkinManagement
{
  /// <summary>
  /// This is the loader class for XAML files. It uses a XAML parser to read the
  /// structure and builds the visual elements tree for the file.
  /// </summary>              
  public class XamlLoader
  {
    /// <summary>
    /// XAML namespace for the MediaPortal Skin Engine visual's class library.
    /// </summary>
    public const string NS_MEDIAPORTAL_MPF_URI = "www.team-mediaportal.com/2008/mpf/directx";

    /// <summary>
    /// Loads the specified skin file and returns the root UIElement.
    /// </summary>
    /// <param name="skinFilePath">The XAML skin file.</param>
    /// <returns><see cref="UIElement"/> descendant corresponding to the root element in the
    /// specified skin file.</returns>
    public static object Load(string skinFilePath)
    {
      try
      {
        using (TextReader reader = new StreamReader(skinFilePath))
          return Load(reader);
      }
      catch (XamlParserException e)
      {
        // Unwrap the exception thrown by Load(TextReader)
        throw new XamlParserException("XAML Parser: Error parsing file '{0}'", e.InnerException, skinFilePath);
      }
      catch (Exception e)
      {
        throw new XamlParserException("XAML Parser: Error parsing file '{0}'", e, skinFilePath);
      }
    }

    /// <summary>
    /// Loads a skin file from the specified <paramref name="reader"/> and returns the root UIElement.
    /// </summary>
    /// <param name="reader">The reader containing the XAML contents of the skin file.</param>
    /// <returns><see cref="UIElement"/> descendant corresponding to the root element in the
    /// specified skin file.</returns>
    public static object Load(TextReader reader)
    {
      try
      {
        Parser parser = new Parser(reader, parser_ImportNamespace, parser_GetEventHandler);
        parser.SetCustomTypeConverter(Registration.ConvertType);
        return parser.Parse();
      }
      catch (Exception e)
      {
        throw new XamlParserException("XAML Parser: Error parsing XAML file from text reader", e);
      }
    }

    static INamespaceHandler parser_ImportNamespace(IParserContext context, string namespaceURI)
    {
      if (namespaceURI == NS_MEDIAPORTAL_MPF_URI)
        return new MpfNamespaceHandler();
      throw new XamlNamespaceNotSupportedException("XAML namespace '{0}' is not supported by the MediaPortal skin engine", namespaceURI);
    }

    static Delegate parser_GetEventHandler(IParserContext context, MethodInfo signature, string value)
    {
      throw new XamlBindingException("GetEventHandler delegate implementation not supported yet");
    }
  }
}
