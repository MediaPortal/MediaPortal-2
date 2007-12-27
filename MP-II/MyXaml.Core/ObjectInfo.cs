/*
 * Copyright (c) 2004, 2005 MyXaml
 * All Rights Reserved
 * 
 * Licensed under the terms of the GNU General Public License
 * http://www.gnu.org/licenses/licenses.html#GPL
*/

using System;
using System.Xml;

namespace MyXaml.Core
{
	/// <summary>
	/// Container for instance information.  Used while processing the object.
	/// </summary>
	public class ObjectInfo
	{
		private object instance;
		private bool hasPropertyDeclaration;
		private string propertyName;
		private XmlAttribute returnType;

		/// <summary>
		/// Get/set the flag indiciating that the instance has a property declaration.
		/// </summary>
		public bool HasPropertyDeclaration
		{
			get {return hasPropertyDeclaration;}
			set {hasPropertyDeclaration=value;}
		}

		/// <summary>
		/// Get/set the property name of the property declaration.
		/// </summary>
		public string PropertyName
		{
			get {return propertyName;}
			set {propertyName=value;}
		}

		/// <summary>
		/// Get/set the instance.
		/// </summary>
		public object Instance
		{
			get {return instance;}
		}

		/// <summary>
		/// Get set the return type attribute.
		/// </summary>
		public XmlAttribute ReturnType
		{
			get {return returnType;}
			set {returnType=value;}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instance">The object instance.</param>
		public ObjectInfo(object instance)
		{
			this.instance=instance;
			hasPropertyDeclaration=false;
			propertyName=String.Empty;
		}
	}
}
