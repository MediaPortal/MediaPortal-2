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
using System.Text.RegularExpressions;

namespace MediaPortal.Utilities.CommandLine
{
  /// <summary>
  /// Parser for command line arguments
  /// </summary>
  public class CommandLine
  {
    /// <summary>
    /// Parses the specified command line args.
    /// </summary>
    /// <param name="commandLineArgs">The command line args.</param>
    /// <param name="options">The options.</param>
    public static void Parse(string[] commandLineArgs, ICommandLineOptions options)
    {
      Regex Spliter = new Regex(@"^-{1,2}|^/|=|:",
                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

      Regex Remover = new Regex(@"^['""]?(.*?)['""]?$",
                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

      string parameter = null;

      // Valid parameters forms:
      // {-,/,--}param{ ,=,:}((",')value(",'))
      // Examples: 
      // -param1 value1 --param2 /param3:"Test" 
      //   /param4=happy -param5 '--=nice=--'
      foreach (string txt in commandLineArgs)
      {
        // Look for new parameters (-,/ or --) and a
        // possible enclosed value (=,:)
        string[] parts = Spliter.Split(txt, 3);

        switch (parts.Length)
        {
            // Found a value (for the last parameter 
            // found (space separator))
          case 1:
            if (parameter != null)
            {
              options.SetOption(parameter, Remover.Replace(parts[0], "$1"));
              parameter = null;
            }
            else
            {
              // else Error: no parameter waiting for a value (skipped)
              throw (new ArgumentException());
            }
            break;

            // Found just a parameter
          case 2:
            // The last parameter is still waiting. 
            // With no value, set it to null.
            if (parameter != null)
            {
              options.SetOption(parameter, null);
            }

            parameter = parts[1];
            break;

            // Parameter with enclosed value
          case 3:
            // The last parameter is still waiting. 
            // With no value, set it to null
            if (parameter != null)
            {
              options.SetOption(parameter, null);
            }

            // Set Option
            options.SetOption(parts[1], Remover.Replace(parts[2], "$1"));

            // clear parameter
            parameter = null;
            break;
        }
      }

      // In case a parameter is still waiting
      if (parameter != null)
      {
        options.SetOption(parameter, null);
      }
    }
  }
}
