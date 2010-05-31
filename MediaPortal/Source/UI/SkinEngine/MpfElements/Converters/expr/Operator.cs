namespace Jyc.Expr
{  
    abstract class Operator : Expression
    { 
        protected Operator( )
        { 
        }

        public abstract OperatorPriority Priority
        {
            get;
        }    
    }
}
