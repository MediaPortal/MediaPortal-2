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
using System.Reflection;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using Presentation.SkinEngine.Xaml.Exceptions;
using Presentation.SkinEngine.Xaml;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.MpfElements;
using System.IO;
using Presentation.SkinEngine.Xaml.Interfaces;

namespace Presentation.SkinEngine.SkinManagement
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
    /// <param name="skinFile">The XAML skin file.</param>
    /// <returns><see cref="UIElement"/> descendant corresponding to the root element in the
    /// specified skin file.</returns>
    public static object Load(FileInfo skinFile)
    {
      Parser parser = new Parser(skinFile, parser_ImportNamespace, parser_GetEventHandler);
      parser.SetCustomTypeConverter(Registration.ConvertType);
      DateTime dt = DateTime.Now;
      object obj = parser.Parse();
      TimeSpan ts = DateTime.Now - dt;
      ServiceScope.Get<ILogger>().Info("XAML file {0} loaded in {1} msec", skinFile, ts.TotalMilliseconds);
      return obj;
    }

    static INamespaceHandler parser_ImportNamespace(IParserContext context, string namespaceURI)
    {
      if (namespaceURI == NS_MEDIAPORTAL_MPF_URI)
        return new MpfNamespaceHandler();
      else
        throw new XamlNamespaceNotSupportedException("XAML namespace '{0}' is not supported by the MediaPortal skin engine", namespaceURI);
    }

    static Delegate parser_GetEventHandler(IParserContext context, MethodInfo signature, string value)
    {
      throw new XamlBindingException("GetEventHandler delegate implementation not supported yet");
    }
  }
}
