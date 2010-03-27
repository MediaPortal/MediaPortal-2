namespace Jyc.Expr
{
    abstract class Expression
    {
        internal abstract Result Eval(Evaluator evaluater, Result[] argArray);
    }
}
