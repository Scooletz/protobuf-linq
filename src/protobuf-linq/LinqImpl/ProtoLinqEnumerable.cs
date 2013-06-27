using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ProtoBuf.Meta;

namespace ProtoBuf.Linq.LinqImpl
{
    public class ProtoLinqEnumerable<TDeserialized, TSource, TResult> : IEnumerable<TResult>
    {
        private readonly RuntimeTypeModel _model;
        private readonly QueryableOptions _options;
        private readonly Stream _source;
        private readonly Func<TSource, int, bool> _where;
        private readonly Func<TSource, int, TResult> _selector;

        public ProtoLinqEnumerable(RuntimeTypeModel model, QueryableOptions options, Stream source, Func<TSource, int, bool> @where, Func<TSource, int, TResult> selector)
        {
            _options = options;
            _source = source;
            _where = @where;
            _selector = selector;
            _model = model;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            var i = 0;
            object value = null;
            while (TryDeserializeWithLengthPrefix(_source, _options.PrefixStyle, null, ref value))
            {
                if (value is TSource)
                {
                    var v = (TSource)value;
                    if (_where(v, i))
                    {
                        yield return _selector(v, i);
                    }
                }

                if (_options.UseAggresiveNoAllocObjectReuse)
                {
                    ((IProtoLinqObject)value).Clear();
                }
                else
                {
                    value = null;
                }

                i += 1;
            }
        }

        private bool TryDeserializeWithLengthPrefix(Stream source, PrefixStyle style, Serializer.TypeResolver resolver, ref object value)
        {
            value = _model.DeserializeWithLengthPrefix(source, value, typeof(TDeserialized), style, 0, resolver);
            return value != null;
        } 

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}