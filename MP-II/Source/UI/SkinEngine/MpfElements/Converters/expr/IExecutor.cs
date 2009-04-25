using System; 
using System.Text;

namespace Jyc.Expr 
{
    public interface IExecutor
    {
        ExprNode Node { get;} 
        void Initialize(IExecutorContext context, ExprNode node);
        void Reset(IExecutorContext context);
        ExecuteAction Execute(IExecutorContext context);
        Result Result{ get;} 
    }

    public interface IExecutorContext
    {
        IExecutor Top
        {
            get;
        }

        object Tag
        {
            get;
        }

        IExecutor CreateExecutor(ExprNode node);

        void Push(IExecutor executor);

        void Pop();

        Result Execute(ExprNode node);
    }

    public enum ExecuteAction
    {
        Continue,
        End,
        Exit
    }
}
