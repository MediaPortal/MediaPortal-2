using System; 
using System.Text;

namespace Jyc.Expr
{
    class Context
    {
        public Context( )
        { 
        }

        public Context( int startPos )
        {
            this.startPos = startPos;
            this.token = Token.None;
        }

        public Context(Token token, int startPos)
        {
            this.startPos = startPos;
            this.token = token; 
        }

        internal Token token;
        internal int startPos;
        internal int endPos;
        internal object value;

        internal string StringValue
        {
            get
            {
                return (string)value;
            }
        }

        public override string ToString()
        {
            if (value == null)
                return this.token.ToString();
            return this.token.ToString() + ":" + value.ToString();
        }
    }

     
}
