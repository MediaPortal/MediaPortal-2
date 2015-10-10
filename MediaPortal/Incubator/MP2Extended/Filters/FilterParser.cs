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
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.Filters
{
  public class FilterParser
  {
    public string Filter { get; set; }

    private int pos;
    private List<string> tokens;

    public FilterParser()
    {
    }

    public FilterParser(string filter)
    {
      Filter = filter;
    }

    public IFilter Parse()
    {
      var tokenizer = new Tokenizer(Filter);

      tokens = tokenizer.Tokenize();
      pos = -2;

      string conjunction;
      ListFilter.JoinType joinType;
      FilterSet filter = new FilterAndSet();
      IFilter thisFilter;
      IFilter last;
      FilterOrSet orSet;

      while (++pos < tokens.Count)
      {
        conjunction = pos == -1 ? "," : tokens[pos];

        var name = GetNextToken("field name");
        var oper = GetNextToken("operator");
        var value = GetNextToken("value");

        if (Tokens.IsListStart(value))
        {
          joinType = ListFilter.JoinType.And;
          var values = new List<string>();
          while (!Tokens.IsListEnd(tokens[pos]) && !Tokens.IsListEnd(GetNextToken("value")))
          {
            values.Add(tokens[pos]);
            if (GetNextToken("conjunction or list end") == "|")
              joinType = ListFilter.JoinType.Or;
          }
          thisFilter = new ListFilter(name, oper, values, joinType);
        }
        else
          thisFilter = new Filter(name, oper, value);

        if (conjunction == ",") // and
          filter.Add(thisFilter);
        else if (conjunction == "|") // or
        {
          last = filter.Last();
          if (!(last is FilterOrSet))
          {
            filter.RemoveAt(filter.Count - 1);
            orSet = new FilterOrSet { last, thisFilter };
            filter.Add(orSet);
          }
          else
            ((FilterOrSet)last).Add(thisFilter);
        }
        else
          throw new ParseException("Parser: Unexpected '{0}', expected conjunction instead.", conjunction);
      }

      return filter;
    }

    private string GetNextToken(string expectedValue)
    {
      if (++pos >= tokens.Count)
        throw new ParseException("Parser: Unexpected end of string, expected {0} instead.", expectedValue);
      return tokens[pos];
    }
  }
}