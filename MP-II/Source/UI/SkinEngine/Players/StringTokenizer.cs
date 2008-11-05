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
using System.Collections;
using System.Text;

namespace MediaPortal.SkinEngine.Players
{
	/// <summary>
	/// A String Tokenizer that accepts Strings as source and delimiter. Only 1 delimiter is supported (either String or char[]).
	/// </summary>
	public class StringTokenizer
	{
		private int CurrIndex;
		private int NumTokens;
		private ArrayList tokens;
		private string StrSource;
		private string StrDelimiter;
		
		/// <summary>
		/// Constructor for StringTokenizer Class.
		/// </summary>
		/// <param name="source">The Source String.</param>
		/// <param name="delimiter">The Delimiter String. If a 0 length delimiter is given, " " (space) is used by default.</param>
		public StringTokenizer(string source, string delimiter) 
		{
			this.tokens = new ArrayList(10);
			this.StrSource = source;
			this.StrDelimiter = delimiter;

			if(delimiter.Length == 0)
			{
				this.StrDelimiter = " ";
			}
			this.Tokenize();
		}

		/// <summary>
		/// Constructor for StringTokenizer Class.
		/// </summary>
		/// <param name="source">The Source String.</param>
		/// <param name="delimiter">The Delimiter String as a char[].  Note that this is converted into a single String and
		/// expects Unicode encoded chars.</param>
		public StringTokenizer(string source, char[] delimiter) : this(source,new string(delimiter))
		{
		}

		/// <summary>
		/// Constructor for StringTokenizer Class.  The default delimiter of " " (space) is used.
		/// </summary>
		/// <param name="source">The Source String.</param>
		public StringTokenizer(string source) : this(source,"")
		{
		}

		/// <summary>
		/// Empty Constructor.  Will create an empty StringTokenizer with no source, no delimiter, and no tokens.
		/// If you want to use this StringTokenizer you will have to call the NewSource(string s) method.  You may
		/// optionally call the NewDelim(string d) or NewDelim(char[] d) methods if you don't with to use the default
		/// delimiter of " " (space).
		/// </summary>
		public StringTokenizer() : this("","")
		{
		}

		private void Tokenize()
		{
			string TempSource = this.StrSource;
			string Tok = "";
			this.NumTokens = 0;
			this.tokens.Clear();
			this.CurrIndex = 0;
			
			if(TempSource.IndexOf(this.StrDelimiter) < 0 && TempSource.Length > 0)
			{
				this.NumTokens = 1;
				this.CurrIndex = 0;
				this.tokens.Add(TempSource);
				this.tokens.TrimToSize();
				TempSource = "";
			}
			else if(TempSource.IndexOf(this.StrDelimiter) < 0 && TempSource.Length <=0)
			{
				this.NumTokens = 0;
				this.CurrIndex =0;
				this.tokens.TrimToSize();
			}
			while(TempSource.IndexOf(this.StrDelimiter) >= 0)
			{
				//Delimiter at beginning of source String.
				if(TempSource.IndexOf(this.StrDelimiter) == 0)
				{
					if(TempSource.Length > this.StrDelimiter.Length)
					{
						TempSource = TempSource.Substring(this.StrDelimiter.Length);
					}
					else
					{
						TempSource = "";
					}
				}
				else
				{
					Tok = TempSource.Substring(0,TempSource.IndexOf(this.StrDelimiter));
					this.tokens.Add(Tok);
					if(TempSource.Length > (this.StrDelimiter.Length + Tok.Length))
					{
						TempSource = TempSource.Substring(this.StrDelimiter.Length + Tok.Length);
					}
					else
					{
						TempSource = "";
					}
				}
			}
			//we may have a string leftover.
			if(TempSource.Length > 0)
			{
				this.tokens.Add(TempSource);
			}
			this.tokens.TrimToSize();
			this.NumTokens = this.tokens.Count;
		}

		/// <summary>
		/// Method to add or change this Instance's Source string.  The delimiter will
		/// remain the same (either default of " " (space) or whatever you constructed 
		/// this StringTokenizer with or added with NewDelim(string d) or NewDelim(char[] d) ).
		/// </summary>
		/// <param name="newSrc">The new Source String.</param>
		public void NewSource(string newSrc)
		{
			this.StrSource = newSrc;
			this.Tokenize();
		}

		/// <summary>
		/// Method to add or change this Instance's Delimiter string.  The source string
		/// will remain the same (either empty if you used Empty Constructor, or the 
		/// previous value of source from the call to a parameterized constructor or
		/// NewSource(string s)).
		/// </summary>
		/// <param name="newDel">The new Delimiter String.</param>
		public void NewDelim(string newDel)
		{
			if(newDel.Length == 0)
			{
				this.StrDelimiter = " ";
			}
			else
			{
				this.StrDelimiter = newDel;
			}
			this.Tokenize();
		}

		/// <summary>
		/// Method to add or change this Instance's Delimiter string.  The source string
		/// will remain the same (either empty if you used Empty Constructor, or the 
		/// previous value of source from the call to a parameterized constructor or
		/// NewSource(string s)).
		/// </summary>
		/// <param name="newDel">The new Delimiter as a char[].  Note that this is converted into a single String and
		/// expects Unicode encoded chars.</param>
		public void NewDelim(char[] newDel)
		{
			string temp = new String(newDel);
			if(temp.Length == 0)
			{
				this.StrDelimiter = " ";
			}
			else
			{
				this.StrDelimiter = temp;
			}
			this.Tokenize();
		}

		public int Count
		{
			get { return tokens.Count; }
		}

		/// <summary>
		/// Method to probe for more tokens.
		/// </summary>
		/// <returns>true if there are more tokens; false otherwise.</returns>
		public bool HasMore
		{
			get { return this.CurrIndex <= tokens.Count - 1; }
		}

		/// <summary>
		/// Method to get the next (string)token of this StringTokenizer.
		/// </summary>
		/// <returns>A string representing the next token; null if no tokens or no more tokens.</returns>
		public string NextToken()
		{
			String RetString = "";
			if(this.CurrIndex <= (this.tokens.Count-1))
			{
				RetString = (string)tokens[CurrIndex];
				this.CurrIndex ++;
				return RetString;
			}
			else
			{
				return null;
			}
		}

		public string this[int index]
		{
			get { if(index >= tokens.Count) throw new ArgumentOutOfRangeException(); return (string)tokens[index]; }
		}

		/// <summary>
		/// Gets the Source string of this Stringtokenizer.
		/// </summary>
		/// <returns>A string representing the current Source.</returns>
		public string Source
		{
			get
			{
				return this.StrSource;
			}
		}

		/// <summary>
		/// Gets the Delimiter string of this StringTokenizer.
		/// </summary>
		/// <returns>A string representing the current Delimiter.</returns>
		public string Delim
		{
			get
			{
				return this.StrDelimiter;
			}
		}

	}
}
