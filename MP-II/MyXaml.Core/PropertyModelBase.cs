/*
 * Copyright (c) 2004, 2005 MyXaml
 * All Rights Reserved
 * 
 * Licensed under the terms of the GNU General Public License
 * http://www.gnu.org/licenses/licenses.html#GPL
*/

using System;
using System.Reflection;
using System.Xml;

namespace MyXaml.Core
{
	/// <summary>
	/// The base class for all model managers.
	/// </summary>
	public abstract class PropertyModelBase
	{
		protected Parser parser;
		protected XmlNode currentNode;
		protected bool attributesProcessed;

		public Parser Parser
		{
			set {parser=value;}
		}

		public XmlNode CurrentNode
		{
			set {currentNode=value;}
		}

		public bool AttributesProcessed
		{
			get {return attributesProcessed;}
			set {attributesProcessed=value;}
		}

		public PropertyModelBase()
		{
			attributesProcessed=false;
		}

		public abstract object SetValue(object obj, PropertyInfo pi, object val);
	}
}
