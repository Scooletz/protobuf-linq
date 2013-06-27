using ProtoBuf.Linq.LinqImpl;

namespace ProtoBuf.Linq
{
    /// <summary>
    /// The options object used for <see cref="ProtobufQueryableBuilder{TSource}"/>.
    /// </summary>
    /// <remarks>
    /// The options object introduced to allow non-breaking changes of public, client methods.
    /// </remarks>
    public sealed class QueryableOptions
    {
        public static QueryableOptions GetDefault()
        {
            return new QueryableOptions
            {
                PrefixStyle = PrefixStyle.Base128,
                UseAggresiveNoAllocObjectReuse = false,
            };
        }

        public PrefixStyle PrefixStyle;

        /// <summary>
        /// Use with caution.
        /// Makes protobuf-linq reuse the same instance of the object over and over again during iterating during stream.
        /// </summary>
        public bool UseAggresiveNoAllocObjectReuse;
    }
}