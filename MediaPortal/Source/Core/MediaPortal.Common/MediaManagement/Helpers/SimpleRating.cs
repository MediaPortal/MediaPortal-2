#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MediaPortal.Common.MediaManagement.Helpers
{
  public struct SimpleRating
  {
    public double? RatingValue;
    public int? VoteCount;

    public SimpleRating(double? rating)
    {
      if (rating.HasValue && rating.Value >= 0)
        RatingValue = rating;
      else
        RatingValue = 0;
      if (rating.HasValue)
        VoteCount = 1;
      else
        VoteCount = null;
    }

    public SimpleRating(double? rating, int? voteCount)
    {
      if (rating.HasValue && rating.Value >= 0)
        RatingValue = rating;
      else
        RatingValue = 0;
      VoteCount = null;
      if (rating.HasValue)
      {
        if (!voteCount.HasValue)
          VoteCount = 1;
        else
          VoteCount = voteCount;
      }
    }

    public static implicit operator SimpleRating(double value)
    {
      return new SimpleRating(value);
    }

    public bool IsEmpty
    {
      get
      {
        return !RatingValue.HasValue;
      }
    }

    public override string ToString()
    {
      return string.Format("{0} ({1})", 
        RatingValue.HasValue ? RatingValue.Value.ToString() : "0",
        VoteCount.HasValue ? VoteCount.Value.ToString() : "0");
    }
  }
}
