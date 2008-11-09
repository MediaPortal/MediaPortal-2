#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Utilities.CommandLine;

namespace MediaPortal.Tools.BuildReport
{
  [Serializable]
  public class CommandLineOptions : ICommandLineOptions
  {
    #region Enums
    public enum Option
    {
      Input,
      Output,
      Solution,
      Title,
      VS2005
      // put options here
    }
    #endregion

    #region Variables
    private readonly Dictionary<Option, object> _options;
    #endregion

    #region Constructors/Destructors
    public CommandLineOptions()
    {
      _options = new Dictionary<Option, object>();
    }
    #endregion

    #region Public Methods
    public bool IsOption(Option option)
    {
      return _options.ContainsKey(option);
    }

    public int Count
    {
      get { return _options.Count; }
    }

    public object GetOption(Option option)
    {
      return _options[option];
    }
    #endregion

    #region ICommandLineOptiosn Implementations
    public void SetOption(string optionName, string argument)
    {
      Option option = (Option)Enum.Parse(typeof(Option), optionName, true);
      object value = argument;
      _options.Add(option, value);
    }

    public void DisplayOptions()
    {
      Console.WriteLine("Valid options:");
      Console.WriteLine("/Input=<input filename>");
      Console.WriteLine("/Output=<output html filename>");
      Console.WriteLine("/Solution=<solution name>/tOptional");
      Console.WriteLine("/Title=<Report title>/tOptional");
    }
    #endregion
  }
}
