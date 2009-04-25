using System; 
using System.Text;

namespace Jyc.Expr
{  
    abstract class Operator : Expression
    { 
        public Operator( )
        { 
        }

        public abstract OperatorPriority Priority
        {
            get;
        }    
    }
}
