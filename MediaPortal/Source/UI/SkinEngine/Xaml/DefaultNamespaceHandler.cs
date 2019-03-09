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
using System.Reflection;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;

namespace MediaPortal.UI.SkinEngine.Xaml
{
  /// <summary>
  /// Defines the handler for a standard namespace declaration in the form <c>xmlns:sys="clr-namespace:System;assembly=mscorlib"</c>.
  /// </summary>
  public class DefaultNamespaceHandler : AbstractNamespaceHandler
  {
    protected Assembly _assembly;
    protected string _namespaceName;

    public DefaultNamespaceHandler(Assembly assembly, string namespaceName)
    {
      _assembly = assembly;
      _namespaceName = namespaceName;
    }

    public DefaultNamespaceHandler(string namespaceName)
    {
      _assembly = null;
      _namespaceName = namespaceName;
    }

    /// <summary>
    /// Creates a <see cref="DefaultNamespaceHandler"/> instance for the namespace declaration in the form
    /// <c>"clr-namespace:System;assembly=mscorlib"</c> or <c>"clr-namespace:Media"</c>.
    /// </summary>
    public static DefaultNamespaceHandler createDefaultHandler(string qualifiedNamespace)
    {
      string namespaceToken = qualifiedNamespace;
      string assemblyToken = string.Empty;
      int i = qualifiedNamespace.IndexOf(';');
      if (i > -1)
      { // Full namespace declaration
        namespaceToken = qualifiedNamespace.Substring(0, i);
        assemblyToken = qualifiedNamespace.Substring(i + 1);
      }
      if (!namespaceToken.StartsWith("clr-namespace:"))
        throw new XamlBindingException(
            "This method can only handle namespaces specified in the form 'clr-namespace:[Namespace]<;assembly=[AssemblyName]>'");
      string namespaceName = namespaceToken.Substring("clr-namespace:".Length);
      i = assemblyToken.IndexOf('=');
      string assemblyName = null;
      if (i > -1)
        assemblyName = assemblyToken.Substring(i+1);
      if (assemblyName == null)
        return new DefaultNamespaceHandler(namespaceName);
      return new DefaultNamespaceHandler(AssemblyHelper.LoadAssembly(assemblyName), namespaceName);
    }

    public override Type GetElementType(string typeName, bool includeAbstractTypes)
    {
      string fullName = String.Format("{0}.{1}", _namespaceName, typeName);
      Type type;
      if (_assembly == null)
      {
        type = Type.GetType(fullName);
      }
      else
      {
        type = _assembly.GetType(fullName);
      }
      if (type != null && (!includeAbstractTypes && type.IsAbstract))
      {
        throw new XamlParserException("Element type '{0}' is abstract", typeName);
      }
      return type;
    }
  }
}
