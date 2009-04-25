using System; 
using System.Text;

namespace Jyc.Expr
{
    class MemberId:Identifier
    {
        public MemberId(string name):base(name)
        {
        }

        public override string ToString()
        {
            return "member." + this.Name;
        }

        internal override Result Eval(Evaluator evaluater, Result[] argArray)
        {
            return new Result(typeof(string), this.Name);
        }
    }
}
