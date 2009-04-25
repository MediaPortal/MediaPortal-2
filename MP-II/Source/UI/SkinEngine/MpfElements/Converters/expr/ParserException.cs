using System; 
using System.Text;

namespace Jyc.Expr
{
    public class ParserException : Exception
    {
        public ParserException(string msg)
            : base(msg)
        {
        }

        public ParserException(Error errorCode,  int pos)
            : base(ErrorFormator.FormatError(errorCode, pos+1))
        {
        }

        internal static ParserException ParenNotMatch(char paren)
        {
            return new ParserException(
                string.Format( SR.ParenNotMatch,paren, (int)Error.ParenNotMatch));
        }

        internal static ParserException NoParenBefore(string paren)
        {
            return new ParserException(
                string.Format(  SR.NoParenBefore, paren, (int)Error.NoParenBefore));
        }

        internal static ParserException InternalError( )
        {
            return new ParserException(  SR.InternalError );
        }
    }
}
