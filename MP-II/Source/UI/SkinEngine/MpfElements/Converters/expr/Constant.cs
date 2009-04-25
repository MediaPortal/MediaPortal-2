using System; 
using System.Text;

namespace Jyc.Expr
{
    class Constant : Operand
    {
        object _value;
        Type _type;

        public Constant(object value)
        {
            _value = value;
            if (_value != null)
                _type = value.GetType();
        }

        public Constant(Type type, object value)
        {
            _value = value;
            _type = type;
        }

        public Type Type
        {
            get { return _type; }
        }

        public override string ToString()
        {
            if (_value == null)
                return "null";
            return _value.ToString(); 
        }

        internal override Result Eval(Evaluator evaluater, Result[] argArray)
        {
            return new Result(_type,_value);
        }
    }
}
