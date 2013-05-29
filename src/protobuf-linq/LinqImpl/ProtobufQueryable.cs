using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using ProtoBuf.Meta;

namespace ProtoBuf.LinqImpl
{
    public class ProtobufQueryable<TSource> : IProtobufQueryable<TSource>
    {
        private static readonly Expression<Func<TSource, int, bool>> TrueWhereClauseNullObject =
            Expression.Lambda<Func<TSource, int, bool>>(Expression.Constant(true),
                                                        Expression.Parameter(typeof(TSource)),
                                                        Expression.Parameter(typeof(int)));

        private readonly Expression<Func<TSource, int, bool>> _whereClause;
        private readonly PrefixStyle _prefix;
        private readonly Stream _source;
        private readonly RuntimeTypeModel _model = RuntimeTypeModel.Default;

        public ProtobufQueryable(PrefixStyle prefix, Stream source)
            : this(TrueWhereClauseNullObject, prefix, source)
        {
        }

        private ProtobufQueryable(Expression<Func<TSource, int, bool>> whereClause, PrefixStyle prefix, Stream source)
        {
            _whereClause = whereClause;
            _prefix = prefix;
            _source = source;
        }

        public IEnumerable<TResult> OfType<TResult>()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TResult> Select<TResult>(Expression<Func<TSource, int, TResult>> selector)
        {
            var visitor = new MemberInfoGatheringVisitor(typeof(TSource));
            visitor.Visit(_whereClause);
            visitor.Visit(selector);

            var members = visitor.Members.Distinct().OrderBy(mi => mi.Name).ToArray();

            var typeWithReducedMembers = GetTypeReplacingTSource(members);

            var newDeserializedItemParam = Expression.Parameter(typeWithReducedMembers);

            // build new where clause, with a parameter of reduced number of ValueMembers
            var newPredicateType = typeof(Func<,,>).MakeGenericType(typeWithReducedMembers, typeof(int), typeof(bool));
            var newPredicateBody = ParameterReplacingVisitor.ReplaceParameter(_whereClause.Body, WhereItemParam, newDeserializedItemParam);
            var newWhere = Expression.Lambda(newPredicateType, newPredicateBody, newDeserializedItemParam, WhereIndexParam).Compile();

            // build new selector, with a parameter of reduced number of ValueMembers
            var newSelectorType = typeof(Func<,,>).MakeGenericType(typeWithReducedMembers, typeof(int), typeof(TResult));
            var newSelectorBody = ParameterReplacingVisitor.ReplaceParameter(selector.Body, selector.Parameters[0], newDeserializedItemParam);
            var newSelector = Expression.Lambda(newSelectorType, newSelectorBody, newDeserializedItemParam, selector.Parameters[1]).Compile();

            var enumerableType = typeof (EnumerableSelector<,>).MakeGenericType(typeWithReducedMembers, typeof (TResult));
            return (IEnumerable<TResult>)enumerableType.GetConstructors()[0].Invoke(new object[] { _model, _prefix, _source, newWhere, newSelector });
        }

        private Type GetTypeReplacingTSource(MemberInfo[] members)
        {
            throw new NotImplementedException();
        }

        public IProtobufQueryable<TSource> Where(Expression<Func<TSource, int, bool>> predicate)
        {
            var body = predicate.Body;

            body = ParameterReplacingVisitor.ReplaceParameter(body, predicate.Parameters[0], WhereItemParam);
            body = ParameterReplacingVisitor.ReplaceParameter(body, predicate.Parameters[1], WhereIndexParam);

            var combinedWhere = Expression.Lambda<Func<TSource, int, bool>>(Expression.And(_whereClause, body), _whereClause.Parameters);

            return new ProtobufQueryable<TSource>(combinedWhere, _prefix, _source);
        }

        private ParameterExpression WhereIndexParam
        {
            get { return _whereClause.Parameters[1]; }
        }

        private ParameterExpression WhereItemParam
        {
            get { return _whereClause.Parameters[0]; }
        }
    }
}