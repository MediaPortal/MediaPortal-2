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
    /// <param name="skinFileUri">The URI to the XAML skin file. This may be an URI with any of
    /// the supported protocols.</param>
    /// <returns><see cref="UIElement"/> descendant corresponding to the root element in the
    /// specified skin file.</returns>
    public static object Load(string skinFileUri)
    {
      try
      {
        using (TextReader reader = OpenURI(skinFileUri))
          return Load(reader);
      }
      catch (XamlLoadException e)
      {
        // Unwrap the exception thrown by Load(TextReader)
        throw new XamlLoadException("XAML loader: Error parsing file '{0}'", e.InnerException, skinFileUri);
      }
      catch (Exception e)
      {
        throw new XamlLoadException("XAML loader: Error parsing file '{0}'", e, skinFileUri);
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
        throw new XamlLoadException("XAML loader: Error reading XAML file from text reader", e);
      }
    }

    // This method might be outsourced in a util project/namespace
    static void SeparateURI(string uri, out string scheme, out string path)
    {
      int i = uri.IndexOf("://");
      if (i == -1)
      { // Default: file
        scheme = "file";
        path = uri;
      }
      else
      {
        scheme = uri.Substring(0, i);
        path = uri.Substring(i + 3);
      }
    }

    static TextReader OpenURI(string skinFileUri)
    {
      string scheme;
      string path;
      SeparateURI(skinFileUri, out scheme, out path);
      // Rudimentary implementation of protocoll handlers - this is sufficient here
      if (scheme == "file")
      {
        while (skinFileUri.StartsWith("/"))
          skinFileUri = skinFileUri.Substring(1);
        return new StreamReader(skinFileUri);
      }
      // TODO: More schemes should be defined here
      throw new XamlLoadException("XAML loader: URI scheme '{0}' is not supported", scheme);
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
