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
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;

namespace MediaPortal.UI.SkinEngine.MpfElements
{
  public class MpfNamespaceHandler : AbstractNamespaceHandler
  {
    /// <summary>
    /// XAML namespace for the MediaPortal Skin Engine visual's class library.
    /// </summary>
    public const string NS_MEDIAPORTAL_MPF_URI = "www.team-mediaportal.com/2008/mpf/directx";

    public override Type GetElementType(string typeName, bool includeAbstractTypes)
    {
      Type type;
      try
      {
        type = MPF.ObjectClassRegistrations[typeName];
      }
      catch
      {
        throw new XamlParserException("Element type '{0}' is not present in MpfNamespaceHandler", typeName);
      }
      if (!includeAbstractTypes && type.IsAbstract)
      {
        throw new XamlParserException("Element type '{0}' is abstract", typeName);
      }
      return type;
    }
  }
}
