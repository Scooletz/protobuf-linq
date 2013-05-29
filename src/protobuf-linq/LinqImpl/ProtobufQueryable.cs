using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ProtoBuf.LinqImpl
{
    public class ProtobufQueryable<TSource> : IProtobufQueryable<TSource>
    {
        private static readonly Expression<Func<TSource, int, bool>> TrueWhereClauseNullObject =
            Expression.Lambda<Func<TSource, int, bool>>(Expression.Constant(true),
                                                        Expression.Parameter(typeof (TSource)),
                                                        Expression.Parameter(typeof (int)));

        private readonly Expression<Func<TSource, int, bool>> _whereClause;

        public ProtobufQueryable()
            : this (TrueWhereClauseNullObject)
        {
        }

        private ProtobufQueryable(Expression<Func<TSource, int, bool>> whereClause)
        {
            _whereClause = whereClause;
        }

        public IEnumerable<TResult> OfType<TResult>()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TResult> Select<TResult>(Expression<Func<TSource, int, TResult>> selector)
        {
            throw new NotImplementedException();
        }

        public IProtobufQueryable<TSource> Where(Expression<Func<TSource, int, bool>> predicate)
        {
            var body = predicate.Body;

            body = ReplaceParameter(body, predicate.Parameters[0], _whereClause.Parameters[0]);
            body = ReplaceParameter(body, predicate.Parameters[1], _whereClause.Parameters[1]);

            var combinedWhere = Expression.Lambda<Func<TSource, int, bool>>(Expression.And(_whereClause, body), _whereClause.Parameters);

            return new ProtobufQueryable<TSource>(combinedWhere);
        }

        private static Expression ReplaceParameter(Expression body, ParameterExpression oldParam, ParameterExpression newParam)
        {
            return new ParameterReplacingVisitor(oldParam, newParam).Visit(body);
        }

        private class ParameterReplacingVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParam;
            private readonly ParameterExpression _newParam;


            public ParameterReplacingVisitor(ParameterExpression oldParam, ParameterExpression newParam)
            {
                _oldParam = oldParam;
                _newParam = newParam;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return ReferenceEquals(node, _oldParam) ? _newParam : node;
            }
        }
    }
}