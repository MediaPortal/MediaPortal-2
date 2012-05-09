/*
 *   MovieDbLib: A library to retrieve information and media from http://TheMovieDb.org
 * 
 *   Copyright (C) 2008  Benjamin Gmeiner
 * 
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using System;
using System.Text;
using System.IO;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib
{
  public class MovieDbUtils
  {
    public static string GetMovieHashString(string filename)
    {
      string hash;
      try
      {
        byte[] moviehash = ComputeMovieHash(filename);
        hash = ToHexadecimal(moviehash);
      }
      catch (Exception e)
      {
        Log.Error("Error while generating FileHash for: " + filename, e);
        hash = null;
      }
      return hash;
    }

    private static byte[] ComputeMovieHash(string filename)
    {
      byte[] result;
      using (Stream input = File.OpenRead(filename))
      {
        result = ComputeMovieHash(input);
      }
      return result;
    }

    private static byte[] ComputeMovieHash(Stream input)
    {
      long streamsize = input.Length;
      ulong lhash = (ulong) streamsize;

      long i = 0;
      byte[] buffer = new byte[sizeof (long)];
      input.Position = 0;
      while (i < 65536/sizeof (long) && (input.Read(buffer, 0, sizeof (long)) > 0))
      {
        i++;
        unchecked
        {
          lhash += BitConverter.ToUInt64(buffer, 0);
        }
      }

      input.Position = Math.Max(0, streamsize - 65536);
      i = 0;
      while (i < 65536/sizeof (long) && (input.Read(buffer, 0, sizeof (long)) > 0))
      {
        i++;
        unchecked
        {
          lhash += BitConverter.ToUInt64(buffer, 0);
        }
      }
      byte[] result = BitConverter.GetBytes(lhash);
      Array.Reverse(result);
      return result;
    }

    private static string ToHexadecimal(byte[] bytes)
    {
      StringBuilder hexBuilder = new StringBuilder();
      for (int i = 0; i < bytes.Length; i++)
      {
        hexBuilder.Append(bytes[i].ToString("x2"));
      }
      return hexBuilder.ToString();
    }
  }
}
