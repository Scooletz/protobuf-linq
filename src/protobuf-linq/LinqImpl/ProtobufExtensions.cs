using System.Reflection;
using ProtoBuf.Meta;

namespace ProtoBuf.LinqImpl
{
    public static class ProtobufExtensions
    {
        public static MetaType GetHierarchyRoot(this MetaType metaType)
        {
            var hierarchyRoot = metaType.BaseType;
            while (metaType.BaseType != null)
            {
                hierarchyRoot = metaType.BaseType;
            }

            return hierarchyRoot ?? metaType;
        }

        public static ValueMember AddFieldCopy(this MetaType metaType, ValueMember originalValueMember)
        {
            var valueMember = metaType.AddField(originalValueMember.FieldNumber, originalValueMember.Name, originalValueMember.ItemType, originalValueMember.DefaultType);

            valueMember.AsReference = originalValueMember.AsReference;
            valueMember.DataFormat = originalValueMember.DataFormat;
            valueMember.DefaultValue = originalValueMember.DefaultValue;
            valueMember.DynamicType = originalValueMember.DynamicType;
            valueMember.IsPacked = originalValueMember.IsPacked;
            valueMember.IsRequired = originalValueMember.IsRequired;
            valueMember.IsStrict = originalValueMember.IsStrict;
            valueMember.SupportNull = originalValueMember.SupportNull;
            
            return valueMember;
        }

        private static readonly FieldInfo DataFormat = typeof (SubType).GetField("dataFormat",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        public static DataFormat GetDataFormat(this SubType subType)
        {
            return (DataFormat) DataFormat.GetValue(subType);
        }
    }
}