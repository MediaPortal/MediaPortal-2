using System;

namespace Jyc.Expr
{
    class Executor : IExecutor
    {
        ExprNode _node;
        object _tag;
        Result _result;
        int _currentIndex ;
        IExecutor[] _subVisitors;

        protected IExecutor[] SubVisitors
        {
            get { return _subVisitors; } 
        }

        protected int CurrentIndex
        {
            get { return _currentIndex; }
            set { _currentIndex = value; }
        }

        public ExprNode Node
        {
            get { return _node; }
        }

        protected void EndVisit(Result  result)
        {
            _result = result;
            _subVisitors = null;
        }

        #region IVisitor Members

        public virtual void Initialize(IExecutorContext context, ExprNode node)
        {
            _node = node;
            Reset(context);
        }

        public virtual ExecuteAction Execute(IExecutorContext context)
        {
            _currentIndex++;
            if (_currentIndex == _node.OperandCount)
            {
                Result[] results = new Result[_node.OperandCount];
                for(int i=0;i<_node.OperandCount;i++)
                    results[i] = _subVisitors[i].Result;
                this.EndVisit(_node.Expression.Eval((Evaluator)context, results));                
                return ExecuteAction.End;
            }

            if (_currentIndex > _node.OperandCount)
            {
                throw new InvalidOperationException();
            }

            ExprNode subNode = (ExprNode)_node.Operands[_currentIndex];
            _subVisitors[_currentIndex] = context.CreateExecutor(subNode);
            context.Push(_subVisitors[_currentIndex]);

            return ExecuteAction.Continue; 
        } 

        public virtual void Reset(IExecutorContext context)
        {
            _subVisitors = new IExecutor[_node.OperandCount];
            _currentIndex = -1;
        } 

        public object Tag
        {
            get
            {
                return _tag;
            }
            set
            {
                _tag = value;
            }
        } 

        public Result Result
        {
            get { return _result; }
        }

        #endregion
    }
}
