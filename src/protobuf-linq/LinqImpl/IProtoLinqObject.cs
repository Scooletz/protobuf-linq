namespace ProtoBuf.Linq.LinqImpl
{
    /// <summary>
    /// The common interface of all objects created by protobuf-linq.
    /// </summary>
    public interface IProtoLinqObject
    {
        /// <summary>
        /// Clears all fields of this instance setting them to their default values;
        /// </summary>
        void Clear();
    }
}