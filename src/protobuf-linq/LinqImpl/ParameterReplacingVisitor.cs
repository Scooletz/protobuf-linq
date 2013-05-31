using System.Linq.Expressions;

namespace ProtoBuf.LinqImpl
{
    public class ParameterReplacingVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParam;
        private readonly ParameterExpression _newParam;

        public static Expression ReplaceParameter(Expression body, ParameterExpression oldParam, ParameterExpression newParam)
        {
            return new ParameterReplacingVisitor(oldParam, newParam).Visit(body);
        }

        private ParameterReplacingVisitor(ParameterExpression oldParam, ParameterExpression newParam)
        {
            _oldParam = oldParam;
            _newParam = newParam;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return ReferenceEquals(node, _oldParam) ? _newParam : node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (ReferenceEquals(node.Expression, _oldParam))
            {
                var newMember = _newParam.Type.GetMember(node.Member.Name)[0];
                return Expression.MakeMemberAccess(_newParam, newMember);
            }

            return base.VisitMember(node);
        }
    }
}