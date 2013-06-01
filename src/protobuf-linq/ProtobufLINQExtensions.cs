using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using ProtoBuf.Linq.LinqImpl;
using ProtoBuf.Meta;

namespace ProtoBuf.Linq
{
    /// <summary>
    /// An extension methods translating almost all LINQ methods into calls based on the <see cref="IProtobufQueryable{TSource}"/> interface provided methods.
    /// </summary>
    /// <remarks>
    /// This gives a full-blown LINQ for items deserialized with protobufs.
    /// </remarks>
    public static class ProtobufLINQExtensions
    {
        public static IProtobufQueryable<T> AsQueryable<T>(this RuntimeTypeModel model, Stream source, PrefixStyle prefix = PrefixStyle.Base128)
        {
            return new ProtobufQueryable<T>(model, source, prefix);
        }

        public static TSource ElementAtOrDefault<TSource>(this IProtobufQueryable<TSource> source, int index)
        {
            return source.Skip(index).Select(t => t).FirstOrDefault();
        }

        public static TSource ElementAt<TSource>(this IProtobufQueryable<TSource> source, int index)
        {
            return source.Skip(index).Select(t => t).First();
        }

        public static double Average<TSource>(this IProtobufQueryable<TSource> source, Expression<Func<TSource, double>> selector)
        {
            return source.Select(selector).Average();
        }

        public static double? Average<TSource>(this IProtobufQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
        {
            return source.Select(selector).Average();
        }

        public static Decimal Average<TSource>(this IProtobufQueryable<TSource> source, Expression<Func<TSource, Decimal>> selector)
        {
            return source.Select(selector).Average();
        }

        public static Decimal? Average<TSource>(this IProtobufQueryable<TSource> source, Expression<Func<TSource, Decimal?>> selector)
        {
            return source.Select(selector).Average();
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this IProtobufQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func)
        {
            throw new NotImplementedException();
        }

        public static TResult Aggregate<TSource, TAccumulate, TResult>(this IProtobufQueryable<TSource> source,
            TAccumulate seed,
            Expression
                <Func<TAccumulate, TSource, TAccumulate>>
                func,
            Expression<Func<TAccumulate, TResult>> selector)
        {
            throw new NotImplementedException();
        }

        public static IProtobufQueryable<TSource> Skip<TSource>(this IProtobufQueryable<TSource> source, int i)
        {
            return source.Where((e, index) => index >= i);
        }

        public static bool Any<TSource>(this IProtobufQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            return source.Where(predicate).Select(t=>t).Any();
        }

        public static bool All<TSource>(this IProtobufQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            var negatedPredicate = Expression.Lambda<Func<TSource, bool>>(Expression.Negate(predicate.Body), predicate.Parameters);
            return !source.Any(negatedPredicate);
        }

        public static IProtobufQueryable<TSource> Where<TSource>(this IProtobufQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            return source.Where(Expression.Lambda<Func<TSource, int, bool>>(predicate.Body, predicate.Parameters[0], Expression.Parameter(typeof(int))));
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IProtobufQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            return source.Select(Expression.Lambda<Func<TSource, int, TResult>>(selector.Body, selector.Parameters[0], Expression.Parameter(typeof(int))));
        }
    }
}