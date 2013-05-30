using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Serialization;

namespace ProtoBuf.LinqImpl
{
    public static class ReflectionExtensions
    {
        private readonly static byte?[] GetterBytes = GetGetterBytes();
        private static readonly byte?[] SetterBytes = GetSetterBytes();

        private static byte?[] GetGetterBytes()
        {
            return new OpCode?[]
            {
                OpCodes.Ldarg_0,
                OpCodes.Ldfld,
                // field token
                null,
                null,
                null,
                null,
                OpCodes.Stloc_0,
                OpCodes.Br_S,
                // branch
                null,
                OpCodes.Ldloc_0,
                OpCodes.Ret
            }
                .Select(o => o != null ? (byte) o.Value.Value : default(byte?))
                .ToArray();
        }

        private static byte?[] GetSetterBytes()
        {
            return new OpCode?[]
            {
                OpCodes.Ldarg_0,
                OpCodes.Ldarg_1,
                OpCodes.Stfld,
                // field token
                null,
                null,
                null,
                null,
                OpCodes.Ret
            }
                .Select(o => o != null ? (byte)o.Value.Value : default(byte?))
                .ToArray();
        }

        public static bool IsAutoProperty(this PropertyInfo info)
        {
            FieldInfo backingField;
            return info.IsAutoProperty(out backingField);
        }

        public static bool IsAutoProperty(this PropertyInfo info, out FieldInfo backingField)
        {
            backingField = null;
            var getter = info.GetGetMethod();
            var setter = info.GetSetMethod();
            if (getter == null || setter == null)
                return false;

            var getBody = getter.GetMethodBody();
            var setBody = setter.GetMethodBody();
            if (getBody == null || setBody == null)
                return false;

            var getBytes = ClearNop(getBody.GetILAsByteArray());

            List<byte[]> readBytes;
            if (MatchOpcodes(getBytes, GetterBytes, out readBytes) == false)
                return false;

            var first = readBytes.First();
            var readMetadataToken = BitConverter.ToInt32(first, 0);
            var fieldRead = info.DeclaringType.Module.ResolveField(readMetadataToken);
            
            var setBytes = ClearNop(setBody.GetILAsByteArray());
            if (MatchOpcodes(setBytes, SetterBytes, out readBytes) == false)
                return false;

            var writeMetadataToken = BitConverter.ToInt32(readBytes.First(), 0);
            var fieldWritten = info.DeclaringType.Module.ResolveField(writeMetadataToken);

            if (fieldRead == fieldWritten)
            {
                backingField = fieldRead;
                return true;
            }

            return false;
        }

        private static byte[] ClearNop(byte[] getBytes)
        {
            if (getBytes[0] == OpCodes.Nop.Value)
            {
                getBytes = getBytes.Skip(1).ToArray();
            }
            return getBytes;
        }

        private static bool MatchOpcodes(byte[] bytes, byte?[] maskToCompare, out List<byte[]> bytesMarkedInMaskAsNulls)
        {
            bytesMarkedInMaskAsNulls = new List<byte[]>();

            if (bytes.Length != maskToCompare.Length)
                return false;

            var nullMarked = new List<byte>();
            for (var i = 0; i < bytes.Length; i++)
            {
                if (maskToCompare[i] != null)
                {
                    if (bytes[i] != maskToCompare[i].Value)
                        return false;

                    if (nullMarked.Count > 0)
                    {
                        bytesMarkedInMaskAsNulls.Add(nullMarked.ToArray());
                        nullMarked.Clear();
                    }
                }
                else
                {
                    nullMarked.Add(bytes[i]);
                }
            }

            return true;
        }

        public static bool HasAttribute<TAttribute>(this MemberInfo mi, bool inherited = true)
        {
            return mi.GetCustomAttributes(typeof(TAttribute), inherited).Any();
        }

        public static CustomAttributeBuilder ToBuilder(this CustomAttributeData data)
        {
            var constructorArgs = data.ConstructorArguments.Select(ca => ca.Value).ToArray();
            var named = data.NamedArguments;
            if (named == null)
                return new CustomAttributeBuilder(data.Constructor, constructorArgs);

            var namedFields = named.Where(na => na.MemberInfo is FieldInfo).ToDictionary(na => (FieldInfo)na.MemberInfo, na => na.TypedValue.Value);
            var namedProps = named.Where(na => na.MemberInfo is PropertyInfo).ToDictionary(na => (PropertyInfo)na.MemberInfo, na => na.TypedValue.Value);

            return new CustomAttributeBuilder(data.Constructor, constructorArgs,
                                              namedProps.Keys.ToArray(), namedProps.Values.ToArray(),
                                              namedFields.Keys.ToArray(), namedFields.Values.ToArray());
        }

        public static bool IsSerializationConnectedAttribute(this CustomAttributeData cad)
        {
            var attrType = cad.Constructor.DeclaringType;
//// ReSharper disable PossibleNullReferenceException
            return attrType.Assembly == typeof (ProtoContractAttribute).Assembly ||
//// ReSharper restore PossibleNullReferenceException
                attrType == typeof (XmlTypeAttribute) ||
                attrType.Name == "DataContractAttribute";
        }
    }
}