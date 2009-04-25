using System; 
using System.Text;
using System.Collections;

namespace Jyc.Expr
{
    public abstract class ExecutorContext : IExecutorContext
    {
        object _tag;
        IExecutor _top;
        Stack  _stack = new Stack ();  

        public ExecutorContext()
        { 
        } 

        public IExecutor Top
        {
            get
            {
                return _top;
            }
        }

        public virtual IExecutor CreateExecutor(ExprNode node)
        {
            IExecutor executor = CreateExecutorCore(node);
            executor.Initialize(this, node);
            return executor;
        }

        protected abstract IExecutor CreateExecutorCore(ExprNode node);

        public virtual void Push(IExecutor executor)
        {
            _top = executor;
            _stack.Push(_top);
        }

        public virtual void Pop()
        {
            _stack.Pop();
            _top = (IExecutor)_stack.Peek();
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

        protected virtual void Reset()
        {
            _stack.Clear();
            _stack.Push(null);
        }

        protected virtual void Done()
        {
        }

        #region IExecutorContext Members

        public Result Execute(ExprNode node)
        {
            if ((node == null)||(node.Expression ==null ))
                return new Result(string.Empty); 
            IExecutor start = this.CreateExecutor(node);
            IExecutor executor = start;
            this.Push(executor);
            while (executor != null)
            {   
                ExecuteAction result = executor.Execute(this); 
                if (result == ExecuteAction.End)
                {
                    this.Pop();
                }
                else if (result == ExecuteAction.Exit)
                {
                    return start.Result;
                }
                executor = this.Top;
            }

            return start.Result;
        }

        #endregion
    }
}
