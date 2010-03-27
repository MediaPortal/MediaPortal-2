using System;

namespace Jyc.Expr
{
    abstract class BinaryOp : Operator
    {
        protected BinaryOp( )
        {
        } 

        public static BinaryOp CreateOp(Token type)
        {
            switch(type)
            { 
                case Token.BooleanOr:
                    return new BooleanOr();
                case Token.BooleanAnd:
                    return new BooleanAnd();
                case Token.Inequality:
                    return new CompareOp(CompareResult.Greater | CompareResult.Less);
                case Token.Equality:
                    return new CompareOp(CompareResult.Equal);
                case Token.LessThan:
                    return new CompareOp(CompareResult.Less );
                case Token.LessThanOrEqual:
                    return new CompareOp(CompareResult.Less | CompareResult.Equal);
                case Token.GreaterThan:
                    return new CompareOp(CompareResult.Greater  );
                case Token.GreaterThanOrEqual:
                    return new CompareOp(CompareResult.Greater | CompareResult.Equal);
        //bit 
                case Token.BitwiseOr:
                    return new BitwiseOr();
                case Token.BitwiseAnd:
                    return new BitwiseAnd();
                case Token.BitwiseXor:
                    return new BitwiseXor();
                case Token.BitwiseShiftLeft:
                    return new BitwiseShift(true);
                case Token.BitwiseShiftRight:
                    return new BitwiseShift(false);
        //arithmetic 
                case Token.Add:
                    return new AdditiveOp();
                case Token.Subtract:
                    return new ArithmeticSubtract();
                case Token.Multiply:
                    return new ArithmeticMultiply();
                case Token.Divide:
                    return new ArithmeticDivide();
                case Token.Modulus:      
                    return new ArithmeticModulus(); 
                default:
                    throw new InvalidOperationException("Not a BinayOP");
            }
        } 

        protected abstract Result DoEval(Evaluator evaluater, Result[] argArray);

        protected abstract bool IsEvalableType(Type type); 

        protected virtual void ProcessArg(Evaluator evaluater, Result[] argArray)
        {
            if (argArray.Length != 2)
            {
                throw new ArgumentException();
            }

            foreach (Result r in argArray)
            {
                if (!IsEvalableType(r.Type))
                {
                    throw new NotSupportedException();
                }
            }
        }

        internal override Result Eval(Evaluator evaluater, Result[] argArray)
        {
            ProcessArg(evaluater, argArray);
            return DoEval(evaluater, argArray );
        }

       
    }
}
