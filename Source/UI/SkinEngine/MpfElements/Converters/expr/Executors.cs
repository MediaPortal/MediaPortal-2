namespace Jyc.Expr
{
    class ConditionalOpExecutor : Executor
    { 
        public override void Reset(IExecutorContext context)
        {
            base.Reset(context); 
        }

        public override ExecuteAction Execute(IExecutorContext context)
        {
            this.CurrentIndex++;
            if (this.CurrentIndex == 0)
            {
                this.SubVisitors[this.CurrentIndex] = context.CreateExecutor((ExprNode)this.Node.Operands[this.CurrentIndex]);
                context.Push(this.SubVisitors[this.CurrentIndex]);
                return ExecuteAction.Continue; 
            }
            else if (this.CurrentIndex == 1)
            {
                Result result = this.SubVisitors[0].Result;
                int resultIndex = -1;
                if ((bool) ConvertHelper.ChangeType(result.Value, typeof(bool)))
                {
                    resultIndex = 1;
                }
                else
                {
                    resultIndex = 2;
                }
                this.SubVisitors[1] = context.CreateExecutor((ExprNode)this.Node.Operands[resultIndex]);
                context.Push(this.SubVisitors[1]);

                return ExecuteAction.Continue;
            }
            else
            {
                this.EndVisit(this.SubVisitors[1].Result); 
                return ExecuteAction.End;
            }
        }
    }

    class BooleanAndExecutor : Executor
    {
        public override ExecuteAction Execute(IExecutorContext context)
        {
            if (this.CurrentIndex == 0)
            {
                Result result = this.SubVisitors[0].Result;
                if (!(bool) ConvertHelper.ChangeType(result.Value, typeof(bool)))
                {
                    this.EndVisit(new Result(false)); 
                    return ExecuteAction.End;
                }
                else
                {
                    return base.Execute(context);
                }
            }
            return base.Execute(context);
        }
    }

    class BooleanOrExecutor : Executor
    {
        public override ExecuteAction Execute(IExecutorContext context)
        { 
            if (this.CurrentIndex == 0)
            {
                Result result = this.SubVisitors[0].Result;
                if ((bool) ConvertHelper.ChangeType(result.Value, typeof(bool)))
                {
                    this.EndVisit(new Result(true)); 
                    return ExecuteAction.End;
                }
                else
                {
                    return base.Execute(context);
                }
            }
            return base.Execute(context);
        }
    }

    class BinaryOpExecutor : Executor
    {
        public override ExecuteAction Execute(IExecutorContext context)
        {
            return base.Execute(context);
        }
    }

    class OperandExecutor : Executor
    {
        public override void Reset(IExecutorContext context)
        { 
        }

        public override ExecuteAction Execute(IExecutorContext context)
        {
            this.EndVisit(this.Node.Expression.Eval((Evaluator)context, null));
            return ExecuteAction.End;
        }
    }
}