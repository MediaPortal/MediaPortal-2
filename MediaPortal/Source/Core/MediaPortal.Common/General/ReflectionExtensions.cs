#region Copyright (C) 2007-2014 Team MediaPortal
/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MediaPortal.Common.General
{
  /// <summary>
  /// These Reflection helper methods have largely been borrowed from the Fasterflect project.
  /// See <see cref="http://fasterflect.codeplex.com"/> for more information.
  /// </summary>
  public static class ReflectionExtensions
  {
    #region Get/Set Property
	  public static T GetPropertyValue<T>( this object obj, string propertyName )
	  {
      if( obj == null || string.IsNullOrEmpty( propertyName ) )
        throw new ArgumentNullException( obj == null ? "obj" : "propertyName" );
	    var property = obj.GetType().GetProperty( propertyName );
	    return (T) property.GetValue( obj );
	  }

	  public static void SetPropertyValue<T>( this object obj, string propertyName, object value )
	  {
      if( obj == null || string.IsNullOrEmpty( propertyName ) )
        throw new ArgumentNullException( obj == null ? "obj" : "propertyName" );
	    var property = obj.GetType().GetProperty( propertyName );
	    property.SetValue( obj, value );
	  }
    #endregion

		#region Implements
		/// <summary>
		/// Returns true if the supplied <paramref name="type"/> implements the given interface <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type (interface) to check for.</typeparam>
		/// <param name="type">The type to check.</param>
		/// <returns>True if the given type implements the specified interface.</returns>
		/// <remarks>This method is for interfaces only. Use <seealso cref="Inherits"/> for class types and <seealso cref="InheritsOrImplements"/> 
		/// to check both interfaces and classes.</remarks>
		public static bool Implements<T>( this Type type )
		{
			return type.Implements( typeof(T) );
		}

		/// <summary>
		/// Returns true of the supplied <paramref name="type"/> implements the given interface <paramref name="interfaceType"/>. If the given
		/// interface type is a generic type definition this method will use the generic type definition of any implemented interfaces
		/// to determine the result.
		/// </summary>
		/// <param name="interfaceType">The interface type to check for.</param>
		/// <param name="type">The type to check.</param>
		/// <returns>True if the given type implements the specified interface.</returns>
		/// <remarks>This method is for interfaces only. Use <seealso cref="Inherits"/> for classes and <seealso cref="InheritsOrImplements"/> 
		/// to check both interfaces and classes.</remarks>
		public static bool Implements( this Type type, Type interfaceType )
		{
			if( type == null || interfaceType == null || type == interfaceType )
				return false;
			if( interfaceType.IsGenericTypeDefinition && type.GetInterfaces()
        .Where( t => t.IsGenericType )
        .Select( t => t.GetGenericTypeDefinition() )
        .Any( gt => gt == interfaceType ) )
			{
				return true;
			}
			return interfaceType.IsAssignableFrom( type );
		}
		#endregion

		#region Inherits
		/// <summary>
		/// Returns true if the supplied <paramref name="type"/> inherits from the given class <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type (class) to check for.</typeparam>
		/// <param name="type">The type to check.</param>
		/// <returns>True if the given type inherits from the specified class.</returns>
		/// <remarks>This method is for classes only. Use <seealso cref="Implements"/> for interface types and <seealso cref="InheritsOrImplements"/> 
		/// to check both interfaces and classes.</remarks>
		public static bool Inherits<T>( this Type type )
		{
			return type.Inherits( typeof(T) );
		}

		/// <summary>
		/// Returns true if the supplied <paramref name="type"/> inherits from the given class <paramref name="baseType"/>.
		/// </summary>
		/// <param name="baseType">The type (class) to check for.</param>
		/// <param name="type">The type to check.</param>
		/// <returns>True if the given type inherits from the specified class.</returns>
		/// <remarks>This method is for classes only. Use <seealso cref="Implements"/> for interface types and <seealso cref="InheritsOrImplements"/> 
		/// to check both interfaces and classes.</remarks>
		public static bool Inherits( this Type type, Type baseType )
		{
			if( baseType == null || type == null || type == baseType )
				return false;
			var rootType = typeof(object);
			if( baseType == rootType )
				return true;
			while( type != null && type != rootType )
			{
				var current = type.IsGenericType && baseType.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type;
				if( baseType == current )
					return true;
				type = type.BaseType;
			}
			return false;
		}
		#endregion

		#region InheritsOrImplements
		/// <summary>
		/// Returns true if the supplied <paramref name="type"/> inherits from or implements the type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The base type to check for.</typeparam>
		/// <param name="type">The type to check.</param>
		/// <returns>True if the given type inherits from or implements the specified base type.</returns>
		public static bool InheritsOrImplements<T>( this Type type )
		{
			return type.InheritsOrImplements( typeof(T) );
		}

		/// <summary>
		/// Returns true of the supplied <paramref name="type"/> inherits from or implements the type <paramref name="baseType"/>.
		/// </summary>
		/// <param name="baseType">The base type to check for.</param>
		/// <param name="type">The type to check.</param>
		/// <returns>True if the given type inherits from or implements the specified base type.</returns>
		public static bool InheritsOrImplements( this Type type, Type baseType )
		{
			if( type == null || baseType == null )
				return false;
			return baseType.IsInterface ? type.Implements( baseType ) : type.Inherits( baseType );
		}
		#endregion

		#region TypesImplementing
		/// <summary>
		/// Gets all types in the given <paramref name="assembly"/> that implement the given <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The interface types should implement.</typeparam>
		/// <param name="assembly">The assembly in which to look for types.</param>
		/// <returns>A list of all matching types. This method never returns null.</returns>
		public static IList<Type> TypesImplementing<T>( this Assembly assembly )
		{
			Type[] types = assembly.GetTypes();
			return types.Where( t => t.Implements<T>() ).ToList();
		}
		#endregion

		#region Constructor Invocation (CreateInstance)
		/// <summary>
		/// Creates an instance of the given <paramref name="type"/> and casts the result to 
		/// the given <typeparamref name="T"/>. The <paramref name="type"/> must have a default
		/// constructor.
		/// </summary>
		public static T CreateInstance<T>( this Type type )
		{
		  return (T) Activator.CreateInstance( type );
		}

		/// <summary>
		/// Finds all types implementing a specific interface or base class <typeparamref name="T"/> in the
		/// given <paramref name="assembly"/> and invokes the default constructor on each to return a list of
		/// instances. Any type that is not a class or does not have a default constructor is ignored.
		/// </summary>
		/// <typeparam name="T">The interface or base class type to look for in the given assembly.</typeparam>
		/// <param name="assembly">The assembly in which to look for types derived from the type parameter.</param>
		/// <returns>A list containing one instance for every unique type implementing T. This will never be null.</returns>
		public static IList<T> CreateInstances<T>( this Assembly assembly )
		{
			var query = from type in assembly.TypesImplementing<T>()
			       	    where type.IsClass && !type.IsAbstract && type.GetConstructor( new Type[0] ) != null
						      select type.CreateInstance<T>();
			return query.ToList();
		}
    #endregion
  }
}
