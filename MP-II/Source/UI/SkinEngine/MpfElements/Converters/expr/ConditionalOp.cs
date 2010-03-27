namespace Jyc.Expr
{
    class ConditionalOp : Operator
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.Conditional; }
        }   

        public override string ToString()
        {
            return "conditional";
        }

        internal override Result Eval(Evaluator evaluater, Result[] argArray)
        {
            if ((bool)argArray[0].Value)
            {
                return argArray[1];
            }
            else
            {
                return argArray[2];
            }
        }
    }
}
