using System;

namespace Jyc.Expr
{
    public class Evaluator : ExecutorContext
    {  
        public Evaluator()
        { 
        }

        IVariableHolder _variableHolder;

        public IVariableHolder  VariableHolder
        {
            get { return _variableHolder; }
            set { _variableHolder = value; }
        }

        public object Eval(string text)
        {
            Parser ep = new Parser();
            Tree tree = ep.Parse(text);
            return Eval(tree);
        }  

        public object Eval(Tree tree)
        {
            Reset();
            Result result = this.Execute(tree.Root);
            Done();
            return result.Value;
        }

        protected override IExecutor CreateExecutorCore(ExprNode node)
        {
            Expression expr = node.Expression;
            if (expr is ConditionalOp)
            {
                return new ConditionalOpExecutor();
            }
            else if (expr is BooleanAnd)
            {
                return new BooleanAndExecutor();
            }
            else if (expr is BooleanOr)
            {
                return new BooleanOrExecutor();
            }
            else if (expr is BinaryOp)
            {
                return new BinaryOpExecutor();
            }
            else if (expr is Operand)
            {
                return new OperandExecutor();
            }
            else
            {
                return new Executor();
            } 
        }
    }

    public interface IVariableHolder
    {
        bool Exists(string name);
        object GetVariable(string name);
    }

    public class Result
    {
        object _value;
        Type _type;

        public Result(  object value)
        {
            _value = value;
            _type = value.GetType();
        }

        public Result(Type type, object value)
        {
            _value = value;
            _type = type;
        }

        public Type Type
        {
            get { return _type; }
            set
            {
                if (_value != null)
                {
                    throw new InvalidOperationException();
                }
                _type = value;
            }
        }

        public object Value
        {
            get { return _value; }
            set
            {
                _value = value;
                if (_value == null)
                    _type = null;
                else
                {
                    _type = _value.GetType();
                }
            }
        }

        public bool IsNull()
        {
            return _value == null;
        }
        public override string ToString()
        {
            if (_value == null)
                return "(null)";
            return _value.ToString();
        }
    }
}
