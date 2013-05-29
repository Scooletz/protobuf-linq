using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ProtoBuf.LinqImpl
{
    public class ProtobufQueryable<TSource> : IProtobufQueryable<TSource>
    {
        public IEnumerable<TSource> AsEnumerable()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}