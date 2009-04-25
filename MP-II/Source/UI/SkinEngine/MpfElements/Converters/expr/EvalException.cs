using System; 
using System.Text;

namespace Jyc.Expr
{
    public class EvalException : Exception
    {
        public EvalException(string msg):base(msg)
        {
        }

        internal static EvalException VariableNotExist(string varName)
        {
            return new EvalException(string.Format(SR.VariableNotExist, varName));
        }
    }
}
