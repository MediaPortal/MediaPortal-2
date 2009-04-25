using System;
using System.Collections;
using System.Text;
using System.Globalization;

namespace Jyc.Expr
{
    sealed class Scanner
    {
        string _text;
        internal int _curPos;
        internal Context _context;
        int _length;

        public Scanner(string text)
        {
            _text = text;
            _curPos = 0;
            _length = _text.Length;
        }

        char GetChar(int pos)
        {
            if (pos >= _length)
            {
                return '\0';
            }
            return _text[pos];
        }


        private void SkipBlanks()
        {
            char c = GetChar(this._curPos);
            while (IsBlankSpace(c))
                c = GetChar(++this._curPos);
        }

        public Token NextToken()
        {
            SkipBlanks();

            _context = new Context();
            _context.startPos = this._curPos;
            Token token = ScanToken();
            _context.token = token;
            _context.endPos = this._curPos;

            switch (token)
            {
                case Token.StringMarker:
                    _context.value = ScanString();
                    token = Token.Const;
                    _context.token = token;
                    _context.endPos = this._curPos;
                    break;
                case Token.LeftCurly:
                    string identifier = ScanIdentifier();
                    token = Keywords.GetToken(identifier);
                    if (token == Token.None)
                    {
                        token = Token.Identifier;
                        _context.value = identifier;
                    }
                    else
                    {
                        switch (token)
                        {
                            case Token.True:
                                _context.value = true;
                                break;
                            case Token.False:
                                _context.value = false;
                                break;
                            case Token.Null:
                                break;
                        }
                    }
                    _context.token = token;
                    _context.endPos = this._curPos;
                    if (ScanToken() != Token.RightCurly)
                      throw new ScannerException(Error.ParameterAccessNotEnded);
                    break;
                case Token.CharMarker:
                    _context.value = ScanChar();
                    token = Token.Const;
                    _context.token = token;
                    _context.endPos = this._curPos;
                    break;
                case Token.HexStart:
                    _context.value = this.ScanHexNumber();
                    token = Token.Const;
                    _context.token = token;
                    _context.endPos = this._curPos;
                    break;
                case Token.DateMarker:
                    _context.value = this.ScanDate();
                    token = Token.Const;
                    _context.token = token;
                    _context.endPos = this._curPos;
                    break;
                case Token.None:
                    {
                        char ch = this.GetChar(_curPos);
                        if (Scanner.IsDigit(ch))
                        {
                            token = Token.Const;
                            _context.token = token;
                            _context.value = this.ScanNumber();
                            _context.endPos = this._curPos;
                        }
                        else if (ch == '\0')
                        {
                            token = Token.EndOfFile;
                            _context.token = token;
                        }
                        else
                        {
                            throw new ScannerException(Error.UnrecogniseChar);
                        }
                    }
                    break;
            }
            return token;
        }

        Token ScanToken()
        {
            Token token = Token.None;
            int pos = _curPos;
            char ch = GetChar(pos);
            switch (ch)
            {
                case '\'':
                    token = Token.CharMarker;
                    pos++;
                    break;
                case '-':
                    token = Token.Subtract;
                    pos++;
                    break;
                case '!':
                    if (this.GetChar(pos + 1) == '=')
                    {
                        token = Token.Inequality;
                        pos += 2;
                    }
                    else
                    {
                        token = Token.BooleanNot;
                        pos++;
                    }
                    break;
                case '"':
                    token = Token.StringMarker;
                    pos++;
                    break;
                case '#':
                    token = Token.DateMarker;
                    pos++;
                    break;
                case '%':
                    token = Token.Modulus;
                    pos++;
                    break;
                case '&':
                    if (this.GetChar(pos + 1) == '&')
                    {
                        token = Token.BooleanAnd;
                        pos += 2;
                    }
                    else
                    {
                        token = Token.BitwiseAnd;
                        pos++;
                    }
                    break;
                case '(':
                    token = Token.LeftParen;
                    pos++;
                    break;
                case ')':
                    token = Token.RightParen;
                    pos++;
                    break;
                case '{':
                    token = Token.LeftCurly;
                    pos++;
                    break;
                case '}':
                    token = Token.RightCurly;
                    pos++;
                    break;
                case '*':
                    token = Token.Multiply;
                    pos++;
                    break;
                case ',':
                    token = Token.Comma;
                    pos++;
                    break;
                case '.':
                    token = Token.Member;
                    pos++;
                    break;
                case '/':
                    token = Token.Divide;
                    pos++;
                    break;
                case ':':
                    token = Token.ConditionalSemicolon;
                    pos++;
                    break;
                case '?':
                    token = Token.ConditionalIf;
                    pos++;
                    break;
                case '[':
                    token = Token.LeftIndexer;
                    pos++;
                    break;
                case ']':
                    token = Token.RightIndexer;
                    pos++;
                    break;
                case '^':
                    token = Token.BitwiseXor;
                    pos++;
                    break;
                case '|':
                    if (this.GetChar(pos + 1) == '|')
                    {
                        token = Token.BooleanOr;
                        pos += 2;
                    }
                    else
                    {
                        token = Token.BitwiseOr;
                        pos++;
                    }
                    break;
                case '~':
                    token = Token.BitwiseNot;
                    pos++;
                    break;
                case '+':
                    token = Token.Add;
                    pos++;
                    break;
                case '<':
                    ch = this.GetChar(pos + 1);
                    if (ch == '<')
                    {
                        token = Token.BitwiseShiftLeft;
                        pos += 2;
                    }
                    else if (ch == '=')
                    {
                        token = Token.LessThanOrEqual;
                        pos += 2;
                    }
                    else
                    {
                        token = Token.LessThan;
                        pos++;
                    }
                    break;
                case '=':
                    if (this.GetChar(pos + 1) == '=')
                    {
                        token = Token.Equality;
                        pos += 2;
                    }
                    break;
                case '>':
                    ch = this.GetChar(pos + 1);
                    if (ch == '=')
                    {
                        token = Token.GreaterThanOrEqual;
                        pos += 2;
                    }
                    else if (ch == '>')
                    {
                        token = Token.BitwiseShiftRight;
                        pos += 2;
                    }
                    else
                    {
                        token = Token.GreaterThan;
                        pos++;
                    }
                    break;
                case '0':
                    ch = this.GetChar(pos + 1);
                    if (ch == 'x')
                    {
                        token = Token.HexStart;
                        pos += 2;
                    }
                    else if (ch == 'X')
                    {
                        token = Token.HexStart;
                        pos += 2;
                    }
                    break;

            }
            if (token != Token.None)
            {
                _curPos = pos;
            }
            return token;
        }


        private StringToken ScanStringToken()
        {
            StringToken token = StringToken.None;
            int pos = _curPos;
            char ch = GetChar(pos);
            switch (ch)
            {
                case '"':
                    token = StringToken.StringEnd;
                    pos++;
                    break;
                case '\\':
                    ch = this.GetChar(pos + 1);
                    switch (ch)
                    {
                        case '\'':
                            token = StringToken.SingleQuote;
                            pos += 2;
                            break;
                        case '"':
                            token = StringToken.DoubleQuote;
                            pos += 2;
                            break;
                        case '\\':
                            token = StringToken.Backslash;
                            pos += 2;
                            break;
                        case 'a':
                            token = StringToken.Alert;
                            pos += 2;
                            break;
                        case 'b':
                            token = StringToken.Backspace;
                            pos += 2;
                            break;
                        case 'f':
                            token = StringToken.FormFeed;
                            pos += 2;
                            break;
                        case 'n':
                            token = StringToken.Newline;
                            pos += 2;
                            break;
                        case 'r':
                            token = StringToken.CarriageReturn;
                            pos += 2;
                            break;
                        case 't':
                            token = StringToken.HorizontalTab;
                            pos += 2;
                            break;
                        case 'u':
                            token = StringToken.UnicodeSequence;
                            pos += 2;
                            break;
                        case 'U':
                            token = StringToken.LongUnicodeSequence;
                            pos += 2;
                            break;
                        case 'v':
                            token = StringToken.VerticalQuote;
                            pos += 2;
                            break;
                        case 'x':
                            token = StringToken.UnicodeSequence;
                            pos += 2;
                            break;
                        default:
                            token = StringToken.Erorr;
                            pos++;
                            break;
                    }
                    break;

            }

            if (token != StringToken.None)
            {
                _curPos = pos;
            }

            return token;
        }

        private object ScanNumber()
        {
            StringBuilder sb = new StringBuilder();
            NumberStyles style = NumberStyles.Integer;
            while (_curPos < _length)
            {
                char ch = this.GetChar(_curPos);
                if (Scanner.IsDigit(ch))
                {
                    sb.Append(ch);
                } 
                else if (ch == '.')
                {
                    char chNext = this.GetChar(_curPos + 1);
                    if (Scanner.IsDigit(chNext))
                    {
                        style = NumberStyles.Number;
                        sb.Append(ch);
                        _curPos++;
                        while (_curPos < _length)
                        {
                            ch = this.GetChar(_curPos);
                            if (Scanner.IsDigit(ch))
                            {
                                sb.Append(ch);
                                _curPos++;
                            } 
                            else if (ch == 'f')
                            {
                                style = NumberStyles.Float;
                                _curPos++;
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
                _curPos++;
            }
            string num = sb.ToString();
            if (style == NumberStyles.Integer)
            {
                int ivalue;
                if (int.TryParse(num, out ivalue))
                    return ivalue;
                long lvalue;
                if (long.TryParse(num, out lvalue))
                    return lvalue;
                return decimal.Parse(num);
            }
            else if (style == NumberStyles.Integer)
                return double.Parse(num);
            return float.Parse(num);
        }

        private object ScanHexNumber()
        {
            StringBuilder sb = new StringBuilder();
            while (_curPos < _length)
            {
                char ch = this.GetChar(_curPos);
                if (Scanner.IsHexDigit(ch))
                {
                    sb.Append(ch);
                }
                else
                {
                    break;
                }
                _curPos++;
            }
            string num = sb.ToString();
            int ivalue;
            if (int.TryParse(num, NumberStyles.HexNumber, null, out ivalue))
                return ivalue;
            long lvalue;
            if (long.TryParse(num, NumberStyles.HexNumber, null, out lvalue))
                return lvalue;
            return decimal.Parse(num);
        }

        private char ScanChar()
        {
            char ch;
            StringToken token = ScanStringToken();
            if (token == StringToken.None)
            {
                ch = this.GetChar(_curPos);
                if (ch == '\0')
                {
                    throw new ScannerException(Error.StringNotEnded);
                }
                _curPos++;
            }
            else
            {
                if (token == StringToken.Erorr)
                {
                    throw new ScannerException(Error.IllegalEscapeChar);
                }
                else if ((token == StringToken.UnicodeSequence) || (token == StringToken.LongUnicodeSequence))
                {
                    int length = 4;
                    if (token == StringToken.LongUnicodeSequence)
                    {
                        length = 8;
                    }
                    StringBuilder hex = new StringBuilder(length);
                    while (length > 0)
                    {
                        char c = this.GetChar(_curPos);
                        if (!IsHexDigit(c))
                        {
                            throw new ScannerException(Error.IllegalHexCharInChar);
                        }
                        hex.Append(c);
                        _curPos++;
                        if (_curPos >= _length)
                        {
                            throw new ScannerException(Error.CharNotEnd);
                        }
                        length--;
                    }
                    int chValue = int.Parse(hex.ToString(), NumberStyles.HexNumber);
                    ch = (char)chValue;
                }
                else
                {
                    ch = (char)token;
                }
            }
            if (this.GetChar(_curPos) != '\'')
                throw new ScannerException(Error.CharNotEnd);
            _curPos++;
            return ch;
        }

        private DateTime ScanDate()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                char ch = this.GetChar(_curPos);
                if (ch == '#')
                {
                    this._curPos++;
                    break;
                }
                sb.Append(ch);
                this._curPos++;
                if (_curPos >= _length)
                {
                    throw new ScannerException(Error.StringNotEnded);
                }
            }

            return DateTime.Parse(sb.ToString());
        }

        private string ScanIdentifier()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                char ch = this.GetChar(_curPos);
                if (!IsIdentifierPartChar(ch))
                {
                    break;
                }
                sb.Append(ch);
                this._curPos++;
                if (_curPos >= _length)
                {
                    break;
                }
            }
            return sb.ToString();
        }

        private string ScanString()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                StringToken token = ScanStringToken();
                if (token == StringToken.None)
                {
                    char ch = this.GetChar(_curPos);
                    if (ch == '\0')
                    {
                        throw new ScannerException(Error.StringNotEnded);
                    }
                    sb.Append(ch);
                    _curPos++;
                }
                else
                {
                    if (token == StringToken.StringEnd)
                    {
                        break;
                    }
                    else if (token == StringToken.Erorr)
                    {
                        throw new ScannerException(Error.IllegalEscapeChar);
                    }
                    else if ((token == StringToken.UnicodeSequence) || (token == StringToken.LongUnicodeSequence))
                    {
                        int length = 4;
                        if (token == StringToken.LongUnicodeSequence)
                        {
                            length = 8;
                        }
                        StringBuilder hex = new StringBuilder(length);
                        while (length > 0)
                        {
                            char ch = this.GetChar(_curPos);
                            if (!IsHexDigit(ch))
                            {
                                throw new ScannerException(Error.IllegalHexCharInString);
                            }
                            hex.Append(ch);
                            _curPos++;
                            if (_curPos >= _length)
                            {
                                throw new ScannerException(Error.StringNotEnded);
                            }
                            length--;
                        }
                        int chValue = int.Parse(hex.ToString(), NumberStyles.HexNumber);
                        sb.Append((char)chValue);
                    }
                    else
                    {
                        sb.Append((char)token);
                    }
                }
            }
            return sb.ToString();
        }

        #region char properties

        internal static bool IsDigit(char c)
        {
            return '0' <= c && c <= '9';
        }

        internal static bool IsHexDigit(char c)
        {
            return IsDigit(c) || 'A' <= c && c <= 'F' || 'a' <= c && c <= 'f';
        }

        private bool IsIdentifierStartChar(char c)
        {
            if (('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || ('_' == c))
                return true;
            if ('0' <= c && c <= '9')
                return false;
            if (c < 128)
                return false;
            UnicodeCategory ccat = Char.GetUnicodeCategory(c);
            switch (ccat)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsIdentifierPartChar(char c)
        {
            if (this.IsIdentifierStartChar(c))
                return true;
            if ('0' <= c && c <= '9')
                return true;
            if (c < 128)
                return false;
            UnicodeCategory ccat = Char.GetUnicodeCategory(c);
            switch (ccat)
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.ConnectorPunctuation:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBlankSpace(char c)
        {
            switch (c)
            {
                case (char)0x09:
                case (char)0x0B:
                case (char)0x0C:
                case (char)0x20:
                case (char)0xA0:
                    return true;
                default:
                    if (c >= 128)
                        return Char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator;
                    else
                        return false;
            }
        }

        #endregion

        class Keywords
        {
            static Hashtable _dict;
            static Keywords()
            {
                _dict = new Hashtable(11);
                Keywords.Add("true", Token.True);
                Keywords.Add("false", Token.False);
                Keywords.Add("null", Token.Null);
                //Keywords.Add("typeof", Token.Typeof);
            }

            public static void Add(string name, Token tokenType)
            {
                _dict[name] = tokenType;
            }

            public static Token GetToken(string name)
            {
                if (_dict.ContainsKey(name))
                {
                    return (Token)_dict[name];
                }
                return Token.None;
            }
        }
    } 
}

