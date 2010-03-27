namespace Jyc.Expr
{
    abstract class UnaryOp :Operator
    {
        protected UnaryOp( )
        {
        }

        public override OperatorPriority Priority
        {
            get { return OperatorPriority.Unary; }
        }  

        public override string ToString()
        {
            return "UnaryOp";
        }

        public static UnaryOp CreateOp(Token type)
        {
            switch (type)
            {
                case Token.BitwiseNot:
                    return new BitwiseNotOp();
                case Token.BooleanNot:
                    return new BooleanNotOp();
            }

            return null;
        }

        class BooleanNotOp:UnaryOp 
        {
            internal override Result Eval(Evaluator evaluater, Result[] argArray)
            {
                return new Result(typeof(bool), !(bool)argArray[0].Value);
            }
        }

        class BitwiseNotOp : UnaryOp
        {
            internal override Result Eval(Evaluator evaluater, Result[] argArray)
            {
                return new Result(typeof(int), ~((uint)ConvertHelper.ChangeType(argArray[0].Value, typeof(uint))));
            }
        }
    }
}
