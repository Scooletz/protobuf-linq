using System.Collections.Generic;
using System.Linq.Expressions;

namespace ProtoBuf.Linq.LinqImpl
{
    public sealed class NakedParameterReferenceVisitor : ExpressionVisitor
    {
        public List<ParameterExpression> ParametersFound = new List<ParameterExpression>();

        protected override Expression VisitMember(MemberExpression node)
        {
            // if parameter referenced by its member, skip the nested inspection
            if (node.Expression != null && node.Expression.NodeType == ExpressionType.Parameter)
                return node;

            return base.VisitMember(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            ParametersFound.Add(node);
            return base.VisitParameter(node);
        }
    }
}