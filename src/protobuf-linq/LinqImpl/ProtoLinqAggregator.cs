using System;
using System.IO;
using ProtoBuf.Meta;

namespace ProtoBuf.Linq.LinqImpl
{
    public sealed class ProtoLinqAggregator<TDeserialized, TSource, TAccumulate> : IAggregator<TAccumulate>
    {
        private readonly RuntimeTypeModel _model;
        private readonly QueryableOptions _options;
        private readonly Stream _source;
        private readonly TAccumulate _seed;
        private readonly Func<TSource, int, bool> _where;
        private readonly Func<TAccumulate, TSource, TAccumulate> _func;

        public ProtoLinqAggregator(RuntimeTypeModel model, QueryableOptions options, Stream source, TAccumulate seed, Func<TSource, int, bool> @where, Func<TAccumulate, TSource, TAccumulate> func)
        {
            _source = source;
            _seed = seed;
            _where = @where;
            _func = func;
            _model = model;
            _options = options;
        }

        public TAccumulate Aggregate()
        {
            var i = 0;
            object value;
            var result = _seed;
            while (TryDeserializeWithLengthPrefix(_source, _options.PrefixStyle, null, out value))
            {
                if (value is TSource)
                {
                    var v = (TSource)value;
                    if (_where(v, i))
                    {
                        result = _func(result, v);
                    }
                }

                i += 1;
            }

            return result;
        }

        private bool TryDeserializeWithLengthPrefix(Stream source, PrefixStyle style, Serializer.TypeResolver resolver, out object value)
        {
            value = _model.DeserializeWithLengthPrefix(source, null, typeof(TDeserialized), style, 0, resolver);
            return value != null;
        } 
    }

    public interface IAggregator<out T>
    {
        T Aggregate();
    }
}