#region Copyright (C) 2012-2013 MPExtended

// Copyright (C) 2012-2013 MPExtended Developers, http://www.mpextended.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.Filters
{
  public class Operator
  {
    public string Syntax { get; private set; }
    public string Name { get; private set; }
    public string[] Types { get; private set; }

    public static List<Operator> GetAll()
    {
      return new List<Operator>
      {
        new Operator() { Syntax = "==", Name = "equals", Types = new string[] { "string", "number", "boolean" } },
        new Operator() { Syntax = "~=", Name = "equals (case-insensitive)", Types = new string[] { "string" } },
        new Operator() { Syntax = "!=", Name = "not equals", Types = new string[] { "string", "number", "boolean" } },
        new Operator() { Syntax = ">", Name = "greater than", Types = new string[] { "number" } },
        new Operator() { Syntax = "<", Name = "less than", Types = new string[] { "number" } },
        new Operator() { Syntax = ">=", Name = "greater or equal than", Types = new string[] { "number" } },
        new Operator() { Syntax = "<=", Name = "less or equal than", Types = new string[] { "number" } },
        new Operator() { Syntax = "*=", Name = "contains", Types = new string[] { "list", "string" } },
        new Operator() { Syntax = "^=", Name = "starts with", Types = new string[] { "string" } },
        new Operator() { Syntax = "$=", Name = "ends with", Types = new string[] { "string" } }
      };
    }
  }
}