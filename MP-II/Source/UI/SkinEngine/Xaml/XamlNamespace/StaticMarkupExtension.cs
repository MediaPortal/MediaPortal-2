#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml.XamlNamespace
{
  public class StaticMarkupExtension: IEvaluableMarkupExtension
  {

    #region Protected fields

    /// <summary>
    /// Holds the qualified or unqualified type name, whose static
    /// member will be read.
    /// </summary>
    /// <remarks>
    /// The type name can be an unqualified name. In this case, the type will be
    /// searched in the default XAML namespace. A qualified type name will
    /// contain the XAML namespace, in the form <c>[NamespacePrefix]:[TypeName]</c>.
    /// </remarks>
    protected string _typeName = null;

    /// <summary>
    /// Holds the name of the static member in the specified <see cref="_typeName">type</see>,
    /// as specified in the x:Static Markup Extension documentation at MSDN.
    /// In addition, we support complete paths.
    /// </summary>
    protected string _staticMemberName = null;

    #endregion

    #region Ctor

    public StaticMarkupExtension() { }

    public StaticMarkupExtension(string member)
    {
      Member = member;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Returns the full member name as specified in the constructing element
    /// for this markup extension instance.
    /// </summary>
    public string Member
    {
      get
      {
        return String.Format("{0}.{1}", _typeName, _staticMemberName);
      }
      set
      {
        int i = value.IndexOf('.');
        if (i == -1)
          throw new XamlParserException("The static member has to be specified in the form [TypeName].[MemberName]<.[MemberName]...>");
        _typeName = value.Substring(0, i);
        _staticMemberName = value.Substring(i+1);
      }
    }

    #endregion

    #region IEvaluableMarkupExtension implementation

    object IEvaluableMarkupExtension.Evaluate(IParserContext context)
    {
      string localName;
      string namespaceURI;
      context.LookupNamespace(_typeName, out localName, out namespaceURI);
      Type type = context.GetNamespaceHandler(namespaceURI).GetElementType(localName, namespaceURI);
      IDataDescriptor result;
      try
      {
        if (PathExpression.Compile(context, _staticMemberName).Evaluate(
            new ValueDataDescriptor(type), out result))
          return result.Value;
      }
      catch (XamlBindingException e)
      {
        ServiceScope.Get<ILogger>().Warn("Error evaluating Static markup", e);
      }
      return null;
    }

    #endregion
  }
}
