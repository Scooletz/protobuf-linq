using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ProtoBuf.Linq
{
    /// <summary>
    /// The basic interface for queryable protobuf serialized items, allowing LINQ, not using an <see cref="IQueryable{T}"/> interface.
    /// </summary>
    /// <remarks>
    /// For more information about 'ab'using LINQ, visit: http://bartdesmet.net/blogs/bart/archive/2010/01/01/the-essence-of-linq-minlinq.aspx
    /// 
    /// The interface provides a minimal set, allowing some optimizaitons to occur. The two root methods are <see cref="IProtobufSimpleQueryable{TSource}.Where"/>
    /// and <see cref="IProtobufSimpleQueryable{TSource}.Select{TResult}"/>
    /// All the others can be translated into these two (see <see cref="ProtobufLINQExtensions"/> for more information). 
    /// Future optimizations might unwind the rest of methods.
    /// </remarks>
    /// <typeparam name="TSource">The root type of the deserialized items. As in standard protobuf.</typeparam>
    public interface IProtobufQueryable<TSource> : IOfType<TSource>, IProtobufSimpleQueryable<TSource>
    {
    }

    public interface IProtobufSimpleQueryable<TSource>
    {
        IProtobufSimpleQueryable<TSource> Where(Expression<Func<TSource, int, bool>> predicate);
        IEnumerable<TResult> Select<TResult>(Expression<Func<TSource, int, TResult>> selector);
        TAccumulate Aggregate<TAccumulate>(TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func);
    }

    public interface IOfType<in TSource>
    {
        IProtobufSimpleQueryable<TResult> OfType<TResult>()
            where TResult : TSource;
    }
}