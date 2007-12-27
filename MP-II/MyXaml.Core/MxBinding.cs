/*
 * Copyright (c) 2004, 2005 MyXaml
 * All Rights Reserved
 * 
 * Licensed under the terms of the GNU General Public License
 * http://www.gnu.org/licenses/licenses.html#GPL
*/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Xml;

using Clifton.Tools.Strings;

using MyXaml.Core.Exceptions;

namespace MyXaml.Core
{
	/// <summary>
	/// Assists with property and event assignment for values that contain a reference.
	/// Also supports binding on objects that implement IExtenderProvider.
	/// </summary>
	public class MxBinding
	{
		private object obj;
		private string propertyName;
		private string refVal;
		private XmlNode node;
		private bool success;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="obj">The target instance.</param>
		/// <param name="propertyName">The property name to assign.</param>
		/// <param name="refVal">The reference, as a string.</param>
		/// <param name="node">The xml node while element is the object instance.</param>
		public MxBinding(object obj, string propertyName, string refVal, XmlNode node)
		{
			this.obj=obj;
			this.propertyName=propertyName;
			this.refVal=refVal;
			this.node=node;
			success=false;
		}

		/// <summary>
		/// Attempt to bind the referenced value to the property of the object.
		/// </summary>
		/// <param name="parser">The calling parser instance.</param>
		/// <returns>True if assignment is successful.</returns>
		public bool Bind(Parser parser)
		{
			bool ret=success;													// Assume last success state.
			if (!success)														// If we haven't succeeded yet...
			{
				string refName=StringHelpers.LeftOf(refVal, ';').Trim();		// Get the reference name.
				string parms=StringHelpers.RightOf(refVal, ';').Trim();			// Get any optional parameters that get assigned to the reference before the reference is assigned to the object.

				EventInfo ei=obj.GetType().GetEvent(propertyName);				// Get the EventInfo for this property.
				if (ei == null)													// If it's not an event...
				{
					ret=parser.ContainsReference(StringHelpers.LeftOf(refName, '.'));		// If the parser contains the reference...
					if (ret)
					{
						object propertyValueObj=parser.ResolveValue(refName);	// Get the referenced instance.
						while (parms != String.Empty)							// For each parameter...
						{
							// parameters further qualify the object being returned
							string pName=StringHelpers.LeftOf(					// Get the parameter name.
								parms, '=').Trim();
							string vName=StringHelpers.LeftOf(					// Get the value to assign to the parameter.
								StringHelpers.RightOf(parms, '='), ';').Trim();				

							if (propertyValueObj is IExtenderProvider)			// If the referenced instance implements IExtenderProvider...
							{
								if (!((IExtenderProvider)propertyValueObj).CanExtend(obj))	// But it doesn't extend the current object...
								{
									throw(new ExtenderProviderException("Cannot extend object."));	// Throw an exception.
								}

								Type t=propertyValueObj.GetType();				// Get the type for the referenced instance.
								MethodInfo mi=t.GetMethod(pName,				// Get the setter method as specified by the parameter name.
									BindingFlags.Public | 
									BindingFlags.NonPublic | 
									BindingFlags.Instance | 
									BindingFlags.Static);
								if (mi != null)									// If the method exists...
								{
									object vNameConverted=null;
									try
									{
										ParameterInfo[] pis=mi.GetParameters();	// Get the parameters that the method takes.
										if (pis.Length != 2)					// Make sure it accepts two, and only two, parameters
										{
											throw(new ExtenderProviderException("Extender provider setter does not take two parameters."));
										}
										TypeConverter tc=TypeDescriptor.GetConverter(			// Get the type converter for the parameter.
											pis[1].ParameterType);
										vNameConverted=tc.ConvertFromInvariantString(vName);	// Convert our value to the parameter type.
									}
									catch
									{
										throw(new ExtenderProviderException("Extender provider value conversion failed."));
									}
									try
									{
										mi.Invoke(propertyValueObj,				// Call the setter method.
											new object[] {obj, vNameConverted});
										ret=true;
										return ret;								// Only one parameter is allowed.
									}
									catch
									{
										throw(new ExtenderProviderException("Extender provider setter failed."));
									}
								}
								else
								{
									throw(new ExtenderProviderException("Unsupported method format for setter."));
								}
							}
							else												// Not an IExtenderProvider class
							{
								parser.SetPropertyValue(						// Set the property value of the referenced instance.
									propertyValueObj, 
									pName, 
									"", 
									vName, 
									node, 
									true);
							}
							parms=StringHelpers.RightOf(parms, ';');			// Move to the next parameter.
						}

						ret=parser.SetPropertyValue(							// Set the property value of the object to the referenced instance.
							obj, 
							propertyName, 
							"", 
							propertyValueObj, 
							node, 
							true);

						if (ret)
						{
							success=true;
						}
					}
				}
				else															// The property is an event handler.
				{
					if (refVal.IndexOf('.') == -1)								// The referenced value must be of the format <instance>.<method>
					{
						throw(new ReferencedEventMissingHandlerException("Missing handler in event reference."));
					}

					string methodName=StringHelpers.RightOf(refVal, '.').Trim();// Get the method name.
					string refObjName=StringHelpers.LeftOf(refName, '.');
					if (parser.ContainsReference(refObjName))
					{
						object propertyValueObj=parser.ResolveValue(refObjName);	// Get the referenced instance.
						Delegate dlgt=parser.FindHandlerForTarget(					// Get the delegate for the specified method.
							propertyValueObj, ei, methodName);

						if (dlgt==null)												// It must exist.
						{
							throw(new EventWireUpException("Couldn't identify an appropriate delegate."));
						}

						try
						{
							ei.AddEventHandler(obj, dlgt);							// Assign the event to the delegate.
							success=true;
							ret=true;
						}
						catch
						{
							throw(new EventWireUpException("Couldn't identify an appropriate delegate."));
						}
					}
				}
			}
			return ret;
		}
	}
}
