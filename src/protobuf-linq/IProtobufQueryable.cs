using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ProtoBuf
{
    /// <summary>
    /// The basic interface for queryable protobuf serialized items, allowing LINQ, not using an <see cref="IQueryable{T}"/> interface.
    /// </summary>
    /// <remarks>
    /// For more information about 'ab'using LINQ, visit: http://bartdesmet.net/blogs/bart/archive/2010/01/01/the-essence-of-linq-minlinq.aspx
    /// 
    /// The interface provides a minimal set, allowing some optimizaitons to occur. The two root methods are these provided with <see cref="IWhere{TSource}"/> interface
    /// and <see cref="ISelect{TSource}"/> interface. All the others can be translated into these two (see <see cref="ProtobufQueryableExtensions"/> for more information). 
    /// Future optimizations might unwind the rest of methods.
    /// </remarks>
    /// <typeparam name="TSource">The root type of the deserialized items. As in standard protobuf.</typeparam>
    public interface IProtobufQueryable<TSource> : IAny, IOfType, ISelect<TSource>, IWhere<TSource>
    {
        IEnumerable<TSource> AsEnumerable();
    }

    public interface IAny
    {
        bool Any();
    }

    public interface IOfType
    {
        IEnumerable<TResult> OfType<TResult>();
    }

    public interface ISelect<TSource>
    {
        IEnumerable<TResult> Select<TResult>(Expression<Func<TSource, TResult>> selector);
        IEnumerable<TResult> Select<TResult>(Expression<Func<TSource, int, TResult>> selector);
    }

    public interface IWhere<TSource>
    {
        IProtobufQueryable<TSource> Where(Expression<Func<TSource, bool>> predicate);
        IProtobufQueryable<TSource> Where(Expression<Func<TSource, int, bool>> predicate);
    }
}