// Source from: http://madreflection.originalcoder.com/2009/12/generic-tryparse.html

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MediaPortal.Plugins.MP2Extended.Utils
{
	/// <summary>Delegate type defining the generic TryParse method signature.</summary>
	/// <typeparam name="T">The type of the value to convert.</typeparam>
	/// <param name="s">A string containing the value to convert.</param>
	/// <param name="result">Upon return, the value converted from the <paramref name="s"/> if conversion succeeded, or the default declared value for the type if the conversion failed.</param>
	/// <returns>true if <paramref name="s"/> was converted successfully; otherwise, false.</returns>
	public delegate bool TryParseDelegate<T>(string s, out T result);

	partial class GenericParsing
	{
		private static readonly Dictionary<Type, MethodInfo> TRY_PARSE_METHODS = new Dictionary<Type, MethodInfo>();


		/// <summary>Converts the string representation of the specified type to its equivalent of the specified type.  A return value indicates whether the conversion succeeded.</summary>
		/// <param name="s">A string containing the value to convert.</param>
		/// <param name="type">The type to which the string is to be converted.</param>
		/// <param name="result">When this method returns, contains the value converted from <paramref name="s"/> if the conversion succeeded, or the default declared value for the type if the conversion failed.</param>
		/// <returns>true if <paramref name="s"/> was converted successfully; otherwise, false.</returns>
		public static bool TryParse(this string s, Type type, out object result)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			result = null;

			MethodInfo method = GetTryParseMethod(type);
			if (method == null)
				throw new Exception(string.Format("No suitable TryParse method found for type '{0}'.", type.FullName));

			object[] parameters = { s, null };
			bool success = (bool)method.Invoke(null, parameters);
			if (success)
				result = parameters[1];
			return success;
		}

		/// <summary>Converts the string representation of type <typeparamref name="T"/> to its equivalent of type <typeparamref name="T"/>.  A return value indicates whether the conversion succeeded.</summary>
		/// <typeparam name="T">The type of the value to convert.</typeparam>
		/// <param name="s">A string containing the value to convert.</param>
		/// <param name="result">When this method returns, contains the value converted from <paramref name="s"/> if the conversion succeeded, or the default declared value for the type if the conversion failed.</param>
		/// <returns>true if <paramref name="s"/> was converted successfully; otherwise, false.</returns>
		public static bool TryParse<T>(this string s, out T result)
		{
			result = default(T);
			object tempResult;
			bool success = TryParse(s, typeof(T), out tempResult);
			if (success)
				result = (T)tempResult;
			return success;
		}

		/// <summary>Supplies a method to parse type <typeparamref name="T"/>.</summary>
		/// <typeparam name="T">The type that the method is able to convert.</typeparam>
		/// <param name="method">A method that can parse type <typeparamref name="T"/>.</param>
		public static void SetTryParseMethod<T>(MethodInfo method)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			Type type = typeof(T);

			if (GetTryParseMethod(type) != null)
				throw new Exception(string.Format("The type '{0}' has a TryParse method available. Either the type defines one or one has already been explicitly provided.", type.FullName));

			if (!HasTryParseSignature(method, type))
				throw new Exception("The provided method does not match the required signature.");

			TRY_PARSE_METHODS[type] = method;
		}

		/// <summary>Supplies a method to parse type <typeparamref name="T"/>.</summary>
		/// <typeparam name="T">The type that the method is able to convert.</typeparam>
		/// <param name="tryParseDelegate">A method that can parse type <typeparamref name="T"/>.</param>
		public static void SetTryParseMethod<T>(TryParseDelegate<T> tryParseDelegate)
		{
			if (tryParseDelegate == null)
				throw new ArgumentNullException("tryParseDelegate");

			SetTryParseMethod<T>(tryParseDelegate.Method);
		}

		private static MethodInfo GetTryParseMethod(Type type)
		{
			if (!TRY_PARSE_METHODS.ContainsKey(type))
			{
				lock (((ICollection)TRY_PARSE_METHODS).SyncRoot)
				{
					if (!TRY_PARSE_METHODS.ContainsKey(type))
					{
						TRY_PARSE_METHODS.Add(type, FindTryParseMethod(type));
					}
				}
			}

			return TRY_PARSE_METHODS[type];
		}

		private static MethodInfo FindTryParseMethod(Type type)
		{
			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static;
			Type[] parameterTypes = { typeof(string), type.MakeByRefType() };

			MethodInfo method = type.GetMethod("TryParse", bindingFlags, null, parameterTypes, null);
			if (method == null)
				return null;

			if (!HasTryParseSignature(method, type))
				return null;

			return method;
		}

		private static bool HasTryParseSignature(MethodInfo method, Type type)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			if (type == null)
				throw new ArgumentNullException("type");

			if (method.ContainsGenericParameters || !method.IsStatic || method.ReturnType != typeof(bool))
				return false;

			ParameterInfo[] parameters = method.GetParameters();

			return parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == type.MakeByRefType();
		}

		private static bool TryParseString(string s, out string result)
		{
			result = s;
			return true;
		}

		private static bool TryParseGuid(string s, out Guid result)
		{
			result = new Guid();
			try
			{
				result = new Guid(s);
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
