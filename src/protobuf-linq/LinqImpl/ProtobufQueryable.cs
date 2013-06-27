using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ProtoBuf.Meta;

namespace ProtoBuf.Linq.LinqImpl
{
    public class ProtobufQueryable<TDeserialized, TSource> : IProtobufSimpleQueryable<TSource>
        where TSource : TDeserialized
    {
        private static readonly Expression<Func<TSource, int, bool>> TrueWhereClauseNullObject =
            Expression.Lambda<Func<TSource, int, bool>>(Expression.Constant(true),
                                                        Expression.Parameter(typeof(TSource)),
                                                        Expression.Parameter(typeof(int)));

        private readonly Expression<Func<TSource, int, bool>> _whereClause;
        private readonly QueryableOptions _options;
        private readonly Stream _source;
        private readonly RuntimeTypeModel _model;

        public ProtobufQueryable(RuntimeTypeModel model, Stream source, QueryableOptions options)
            : this(model, source, options, TrueWhereClauseNullObject)
        {
        }

        private ProtobufQueryable(RuntimeTypeModel model, Stream source, QueryableOptions options, Expression<Func<TSource, int, bool>> whereClause)
        {
            _whereClause = whereClause;
            _options = options;
            _source = source;
            _model = model;
        }

        public IEnumerable<TResult> Select<TResult>(Expression<Func<TSource, int, TResult>> selector)
        {
            var foundParameters = NakedParameterReferenceVisitor.GetParameters(selector.Body);
            if (foundParameters.Any(param => ReferenceEquals(param, selector.Parameters[0])))
                throw new InvalidOperationException("A reference to a deserialized type found in the select clause. Currently, only projections selecting type fields are allowed.");

            var visitor = new MemberInfoGatheringVisitor(typeof(TSource));
            visitor.Visit(_whereClause);
            visitor.Visit(selector);

            var members = visitor.Members.Distinct().OrderBy(mi => mi.Name).ToArray();

            var typeDeserialized = GetTypeReplacingTSource(typeof(TDeserialized), members);
            var typeWithReducedMembers = GetTypeReplacingTSource(typeof(TSource), members);

            var newDeserializedItemParam = Expression.Parameter(typeWithReducedMembers);

            // build new where clause, with a parameter of reduced number of ValueMembers
            var newWhere = BuildNewWhere(typeWithReducedMembers, newDeserializedItemParam);

            // build new selector, with a parameter of reduced number of ValueMembers
            var newSelectorType = typeof(Func<,,>).MakeGenericType(typeWithReducedMembers, typeof(int), typeof(TResult));
            var newSelectorBody = ParameterReplacingVisitor.ReplaceParameter(selector.Body, selector.Parameters[0], newDeserializedItemParam);
            var newSelector = Expression.Lambda(newSelectorType, newSelectorBody, newDeserializedItemParam, selector.Parameters[1]).Compile();

            var enumerableType = typeof(ProtoLinqEnumerable<,,>).MakeGenericType(typeDeserialized, typeWithReducedMembers, typeof(TResult));
            return (IEnumerable<TResult>)enumerableType.GetConstructors()[0].Invoke(new object[] { _model, _options, _source, newWhere, newSelector });
        }

        private Delegate BuildNewWhere(Type typeWithReducedMembers, ParameterExpression newDeserializedItemParam)
        {
            var newPredicateType = typeof(Func<,,>).MakeGenericType(typeWithReducedMembers, typeof(int), typeof(bool));
            var newPredicateBody = ParameterReplacingVisitor.ReplaceParameter(_whereClause.Body, WhereItemParam,
                newDeserializedItemParam);
            return Expression.Lambda(newPredicateType, newPredicateBody, newDeserializedItemParam, WhereIndexParam).Compile();
        }

        public TAccumulate Aggregate<TAccumulate>(TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func)
        {
            var foundParameters = NakedParameterReferenceVisitor.GetParameters(func.Body);
            if (foundParameters.Any(param => ReferenceEquals(param, func.Parameters[1])))
                throw new InvalidOperationException("A reference to a deserialized type found in the select clause. Currently, only aggregations selecting type fields are allowed.");

            var visitor = new MemberInfoGatheringVisitor(typeof(TSource));
            visitor.Visit(_whereClause);
            visitor.Visit(func);

            var members = visitor.Members.Distinct().OrderBy(mi => mi.Name).ToArray();

            var typeDeserialized = GetTypeReplacingTSource(typeof(TDeserialized), members);
            var typeWithReducedMembers = GetTypeReplacingTSource(typeof(TSource), members);

            var newDeserializedItemParam = Expression.Parameter(typeWithReducedMembers);

            // build new where clause, with a parameter of reduced number of ValueMembers
            var newWhere = BuildNewWhere(typeWithReducedMembers, newDeserializedItemParam);

            // build new selector, with a parameter of reduced number of ValueMembers
            var newFuncType = typeof(Func<,,>).MakeGenericType(typeof(TAccumulate), typeWithReducedMembers, typeof(TAccumulate));
            var newSelectorBody = ParameterReplacingVisitor.ReplaceParameter(func.Body, func.Parameters[1], newDeserializedItemParam);
            var newSelector = Expression.Lambda(newFuncType, newSelectorBody, func.Parameters[0], newDeserializedItemParam).Compile();

            var aggregatorType = typeof(ProtoLinqAggregator<,,>).MakeGenericType(typeDeserialized, typeWithReducedMembers, typeof(TAccumulate));
            var aggregator = (IAggregator<TAccumulate>)aggregatorType.GetConstructors()[0].Invoke(new object[] { _model, _options, _source, seed, newWhere, newSelector });
            return aggregator.Aggregate();
        }

        private Type GetTypeReplacingTSource(Type type, MemberInfo[] members)
        {
            return ProjectionTypeBuilder.GetCachedFor(_model)
                .GetTypeForProjection(type, members);
        }

        public IProtobufSimpleQueryable<TSource> Where(Expression<Func<TSource, int, bool>> predicate)
        {
            var body = predicate.Body;

            body = ParameterReplacingVisitor.ReplaceParameter(body, predicate.Parameters[0], WhereItemParam);
            body = ParameterReplacingVisitor.ReplaceParameter(body, predicate.Parameters[1], WhereIndexParam);

            var combinedWhere = Expression.Lambda<Func<TSource, int, bool>>(Expression.And(_whereClause.Body, body), _whereClause.Parameters);

            return new ProtobufQueryable<TDeserialized, TSource>(_model, _source, _options, combinedWhere);
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