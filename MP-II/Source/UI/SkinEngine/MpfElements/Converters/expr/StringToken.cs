using System; 
using System.Text;

namespace Jyc.Expr
{
    //C# defines the following character escape sequences: 

    //\' - single quote, needed for character literals 
    //\" - double quote, needed for string literals 
    //\\ - backslash 
    //\0 - Unicode character 0 
    //\a - Alert (character 7) 
    //\b - Backspace (character 8) 
    //\f - Form feed (character 12) 
    //\n - New line (character 10) 
    //\r - Carriage return (character 13) 
    //\t - Horizontal tab (character 9) 
    //\v - Vertical quote (character 11) 
    //\uxxxx - Unicode escape sequence for character with hex value xxxx 
    //\xn[n][n][n] - Unicode escape sequence for character with hex value nnnn (variable length version of \uxxxx) 
    //\Uxxxxxxxx - Unicode escape sequence for character with hex value xxxxxxxx (for generating surrogates) 

    /// <summary>
    /// 
    /// </summary>
    enum StringToken : int
    {
        None,
        Alert = '\a',
        Backslash = '\\',
        Backspace = '\b',
        CarriageReturn = '\n',
        DoubleQuote = '\"',
        FormFeed = '\f',
        HorizontalTab = '\t',
        Newline = '\r',
        SingleQuote = '\'',
        VerticalQuote = '\v',
        StringEnd = 100,
        UnicodeSequence = 200,
        LongUnicodeSequence = 300,
        Erorr = 400,
    }
}
