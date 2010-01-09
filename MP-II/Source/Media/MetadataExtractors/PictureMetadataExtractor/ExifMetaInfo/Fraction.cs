#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

namespace Media.Importers.PictureImporter
{
	///<summary>Some values according EXIF specifications are stored as Fraction numbers.
 	///This Class helps to manipulate and display those numbers.</summary>
	public class Fraction 
	{
   		long num = 0;
   		long den = 1;
		///<summary>Creates a Fraction Number having Numerator and Denumerator.</summary>
		///<param name="num">Numerator</param>
		///<param name="den">Denumerator</param>
   		public Fraction(long num, long den) 
   		{
      		this.num = num;
      		this.den = den;
   		}
		///<summary>Creates a Fraction Number having Numerator and Denumerator.</summary>
		///<param name="num">Numerator</param>
		///<param name="den">Denumerator</param>
   		public Fraction(ulong num, ulong den) 
   		{
   			this.num = Convert.ToInt32(num);
   			this.den = Convert.ToInt32(den);
   		}
		
		///<summary>Creates a Fraction Number having only Numerator and assuming Denumerator=1.</summary>
		///<param name="num">Numerator</param>
   		public Fraction(int num) 
   		{
      		this.num = num;
   		}
   		
		///<summary>Used to display Fraction numbers like 12/17.</summary>
		public override string ToString()
		{
			if (den==1) return String.Format("{0}", num);
			//if ((den % 10) == 0 ) return String.Format("{0}", num / den);
			return String.Format("{0}/{1}", num, den);
		}
		
		///<summary>The Numerator</summary>
		public long Numerator 
		{
			get {return num;}
			set {num = value;}
		}

		///<summary>The Denumerator</summary>
		public long Denumerator 
		{
			get {return den;}
			set {den = value;}
		}
		
   		///<summary>Overloades operator + </summary>
   		public static Fraction operator +(Fraction a, Fraction b) 
   		{
      		return new Fraction(a.num * b.den + b.num * a.den, a.den * b.den);
   		}

   		///<summary>Overloades operator * </summary>
   		public static Fraction operator *(Fraction a, Fraction b) 
   		{
      		return new Fraction(a.num * b.num, a.den * b.den);
   		}

   		///<summary>Retrives double value of a Frction number. Enables casting to double.</summary>
   		public static implicit operator double(Fraction f) 
   		{
        if (f != null) return (double)f.num / f.den;
        else return (double)0;
   		}
	}
}
