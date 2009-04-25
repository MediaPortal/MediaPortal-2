using System; 
using System.Text;
 
namespace Jyc.Expr
{ 
    public class Parser
    { 
        Scanner _scanner;
         
        Context _current;
        Tree _tree;

        public Parser()
        {
        }

        internal Token PeekToken()
        {
            int curPos = _scanner._curPos;
            Context context = _scanner._context;
            Token token = Token.None;
            try
            {
                token = _scanner.NextToken();
            }
            finally
            {
                _scanner._curPos = curPos;
                _scanner._context = context;
            }

            return token;
        }

        internal Token NextToken()
        {
            Token token = _scanner.NextToken();
            _current = _scanner._context;
            return token;
        } 

        #region parse

        ParserException BuildException(Error errorCode)
        {
            return new ParserException(errorCode, this._current.startPos);
        }

        ParserException BuildException(Error errorCode, Context token)
        {
            return new ParserException(errorCode, token.startPos);
        }

        public Tree Parse(string text)
        { 
            _scanner = new Scanner(text);
            _tree = new Tree();

            Token token = this.NextToken();
            if (token == Token.EndOfFile)
                return _tree;
            this.CheckStartableToken(token);

            while (token != Token.EndOfFile)
            {
                if (Parser.IsConstTocken(token))
                    ParseConst();
                else if (_tree._isStart &&
                    ((token == Token.Subtract) || (token == Token.Add)))
                {
                    Constant constant = new Constant((int)0);
                    _tree.AddOperand(constant);
                    BinaryOp bo = BinaryOp.CreateOp(token);
                    _tree.AddOperator(bo);
                }
                else if (Parser.IsBinaryOp(token))
                    ParseBinaryOp();
                else if (Parser.IsUnaryOp(token))
                    ParseUnaryOp();
                else
                {
                    switch (token)
                    {
                        case Token.Identifier:
                            ParseIdentifier();
                            break;
                        case Token.LeftIndexer:
                            this.ParseIndexer();
                            break;
                        case Token.RightIndexer:
                            _tree.Pop('[');
                            break;
                        case Token.ConditionalIf:
                            ParseConditional();
                            break;
                        case Token.ConditionalSemicolon:
                            ParseConditionalSemicolon();
                            break;
                        case Token.Member:
                            ParseMember();
                            break;
                        case Token.LeftParen:
                            if (_tree._top.Expression is MemberOp)
                            {
                                MemberOp memberOp = (MemberOp)_tree._top.Expression;
                                if (memberOp.IsFunction)
                                    throw new ParserException("()() not allowed");
                                memberOp.IsFunction = true;
                            }
                            this._tree.Push('(');
                            break;
                        case Token.RightParen:
                            _tree.Pop('(');
                            break;
                        case Token.Comma:
                            this._tree.Pop(',');
                            this._tree.Push(',');
                            break;
                        default:
                            throw BuildException(Error.InternalError);
                    }
                }  
                token = this.NextToken(); 
            }

            _tree.Complete();
            return _tree;
        }

        void ParseConst()
        {             
            Token token = this.PeekToken();
            if (!Parser.IsCanFollowConst(token))
            {
                throw this.BuildException(Error.BinaryCommaRightParenRightIndexerExpected);
            }
            Constant expr = new Constant(_current.value);
            _tree.AddOperand( expr);
        }
 
        void ParseBinaryOp( )
        {
            Token token = this.PeekToken();
            this.CheckStartableToken(token);
            BinaryOp bo = BinaryOp.CreateOp(_current.token);
            _tree.AddOperator(bo); 
        }

        void ParseUnaryOp()
        {
            Token token = this.PeekToken();
            this.CheckStartableToken(token);
            UnaryOp uo = UnaryOp.CreateOp(_current.token);
            _tree.AddOperator(uo);
        }

        void ParseIdentifier()
        {
            Token token = this.PeekToken();
            if (!Parser.IsCanFollowIdentifier(token))
            {
                throw this.BuildException(Error.BinaryCommaMemberParenIndexerExpected);
            }
            if (this._tree._top.Expression is MemberOp)
            {
                MemberId memberId = new MemberId(this._current.StringValue);
                _tree.AddOperand(memberId);
            }
            else
            {
                Variable variable = new Variable(this._current.StringValue);
                _tree.AddOperand(variable);
            }
        }

        void ParseIndexer()
        {
            Token token = this.PeekToken();
            this.CheckStartableToken(token);
            IndexerOp io= new IndexerOp();
            _tree.AddOperator(io);
            _tree.Push('[');
        }

        void ParseConditional()
        {
            Token token = this.PeekToken();
            this.CheckStartableToken(token);
            ConditionalOp co= new ConditionalOp();
            _tree.AddOperator(co);
            _tree.Push('?');
        }

        void ParseConditionalSemicolon()
        {
            Token token = this.PeekToken();
            this.CheckStartableToken(token);

            this._tree.Pop('?');
            this._tree.Push(':');
            this._tree.Pop(':');           
        }

        void ParseMember()
        {
            Token token = this.PeekToken(); 
            if ((token != Token.Identifier) && (token != Token.LeftParen))
            {  
                throw BuildException(Error.IndentiferExpected);
            }
            MemberOp mo = new MemberOp();
            _tree.AddOperator(mo);
            if (token == Token.LeftParen)
            {
                this.NextToken();
                this._tree.Push('('); 
            } 
        }

        internal void CheckStartableToken(Token token)
        {
            if (!IsStartableToken(token))
            {
                throw this.BuildException(Error.IdentfierConstUnaryOrLeftParenExpected);
            }
        }

        internal static bool IsStartableToken(Token token)
        {
            if (token >= Token.Identifier)
                return true;

            switch (token)
            {
                case Token.LeftParen:
                case Token.BooleanNot:
                case Token.BitwiseNot:
                case Token.Subtract:
                    return true;
            }

            return false;
        }

        internal static bool IsCanFollowIdentifier(Token token)
        {
            if (IsBinaryOp(token))
                return true;
            switch (token)
            {
                case Token.Member:
                case Token.LeftIndexer:
                case Token.RightIndexer:
                case Token.LeftParen:
                case Token.RightParen:
                case Token.Comma:
                case Token.ConditionalIf:
                case Token.ConditionalSemicolon:
                case Token.EndOfFile:
                    return true;
            }

            return false;
        }

        internal static bool IsCanFollowConst(Token token)
        {
            if (IsBinaryOp(token))
                return true;
            switch (token)
            {
                case Token.Member:
                case Token.LeftIndexer:
                case Token.RightIndexer:
                case Token.RightParen:
                case Token.Comma:
                case Token.ConditionalIf:
                case Token.ConditionalSemicolon:
                case Token.EndOfFile:
                    return true;
            }

            return false;
        }


        internal static bool IsBinaryOp(Token token)
        {
            return (token > Token.BinaryFirst)
                && (token < Token.BinaryLast);
        }

        internal static bool IsUnaryOp(Token token)
        {
            return (token == Token.BitwiseNot)
                    || (token == Token.BooleanNot);
        }

        internal static bool IsConstTocken(Token token)
        {
            return (token > Token.ConstFirst)
                && (token < Token.ConstLast);
        }

        #endregion

    }
}
