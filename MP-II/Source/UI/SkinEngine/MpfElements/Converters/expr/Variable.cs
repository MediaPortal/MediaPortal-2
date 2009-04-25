using System; 
using System.Text;

namespace Jyc.Expr
{
    class Variable : Identifier
    {
        public Variable(string name)
            : base(name)
        { 
        } 

        public override string ToString()
        { 
            return "variable." + this.Name;
        }

        internal override Result Eval(Evaluator evaluater, Result[] argArray)
        {
            if (!evaluater.VariableHolder.Exists(this.Name))
            {
                throw EvalException.VariableNotExist(this.Name);
            }
             
            object o = evaluater.VariableHolder.GetVariable(this.Name);
            if (o == null)
            {
                return new Result(null, null);
            }
            return new Result(o.GetType(), o);    
        }
    }

    class TypeNamePart
    { 
        public TypeNamePart(string part)
        {
            this.part = part;
        }

        public string part;
    }

}
