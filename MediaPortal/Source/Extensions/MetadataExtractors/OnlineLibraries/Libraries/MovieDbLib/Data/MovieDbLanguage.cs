using System;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data
{
  public class MovieDbLanguage
  {
    public static MovieDbLanguage DefaultLanguage = new MovieDbLanguage("en");
    
    public MovieDbLanguage(String iso2LetterCode)
    {
      Abbreviation = iso2LetterCode;
    }

    public string Abbreviation { get; set; }
  }
}
