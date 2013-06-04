using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using ProtoBuf.Meta;

namespace ProtoBuf.Linq.LinqImpl
{
    /// <summary>
    /// A class building the very first part of LINQ over protobuf.
    /// </summary>
    /// <remarks>
    /// It's needed to separate the selection of <see cref="OfType{TResult}"/> and another way of creating underlying <see cref="ProtobufQueryable{TDeserialized,TSource}"/>.
    /// </remarks>
    /// <typeparam name="TSource">The originally serialized type.</typeparam>
    public class ProtobufQueryableBuilder<TSource> : IProtobufQueryable<TSource>
    {
        private readonly RuntimeTypeModel _model;
        private readonly Stream _source;
        private readonly PrefixStyle _prefix;

        public ProtobufQueryableBuilder(RuntimeTypeModel model, Stream source, PrefixStyle prefix)
        {
            _model = model;
            _source = source;
            _prefix = prefix;
        }

        public IProtobufSimpleQueryable<TResult> OfType<TResult>() where TResult : TSource
        {
            return new ProtobufQueryable<TSource, TResult>(_model, _source, _prefix);
        }

        public IProtobufSimpleQueryable<TSource> Where(Expression<Func<TSource, int, bool>> predicate)
        {
            return new ProtobufQueryable<TSource, TSource>(_model, _source, _prefix).Where(predicate);
        }

        public IEnumerable<TResult> Select<TResult>(Expression<Func<TSource, int, TResult>> selector)
        {
            return new ProtobufQueryable<TSource, TSource>(_model, _source, _prefix).Select(selector);
        }

        public TAccumulate Aggregate<TAccumulate>(TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func)
        {
            return new ProtobufQueryable<TSource, TSource>(_model, _source, _prefix).Aggregate(seed, func);
        }
    }
}