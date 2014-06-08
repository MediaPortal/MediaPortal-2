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
using System.Security.Cryptography;
using System.Text;

namespace MediaPortal.Common.General
{
  /// <summary>
  /// This class provides cryptographic extensions.
  /// </summary>
	public static class CryptoExtensions
	{
    /// <summary>
    /// Computes the SHA384 hash of the given <paramref name="value"/> and returns
    /// the result as a Base64-encoded string.
    /// </summary>
		public static string Hash( this string value )
		{
			var hash = SHA384.Create();
			byte[] input = Encoding.UTF8.GetBytes( value );
			byte[] output = hash.ComputeHash( input );
			return Convert.ToBase64String( output );
		}
	}
}