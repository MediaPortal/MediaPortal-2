#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml.XamlNamespace
{
  public class StaticExtension : MPFExtensionBase, IEvaluableMarkupExtension
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

    protected object _value = null;

    #endregion

    #region Ctor

    public StaticExtension() { }

    public StaticExtension(string member)
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

    void IEvaluableMarkupExtension.Initialize(IParserContext context)
    {
      string localName;
      string namespaceURI;
      context.LookupNamespace(_typeName, out localName, out namespaceURI);
      Type type = context.GetNamespaceHandler(namespaceURI).GetElementType(localName, true); // static classes are labeled as IsAbstract
      IDataDescriptor result;
      try
      {
        if (PathExpression.Compile(context, _staticMemberName).Evaluate(
            new ValueDataDescriptor(type), out result))
          _value = result.Value;
      }
      catch (XamlBindingException e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Error evaluating Static markup", e);
      }
    }

    bool IEvaluableMarkupExtension.Evaluate(out object value)
    {
      value = _value;
      return true;
    }

    #endregion
  }
}
