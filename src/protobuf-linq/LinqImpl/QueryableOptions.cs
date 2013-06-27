namespace ProtoBuf.Linq.LinqImpl
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
        public bool UseAggresiveNoAllocObjectReuse;
    }
}