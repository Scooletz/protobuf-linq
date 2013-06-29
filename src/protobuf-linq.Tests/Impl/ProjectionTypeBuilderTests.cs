using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Linq.LinqImpl;
using ProtoBuf.Meta;

namespace protobuf_linq.Tests.Impl
{
    public class ProjectionTypeBuilderTests
    {
        private readonly RuntimeTypeModel _model;

        public ProjectionTypeBuilderTests()
        {
            _model = TypeModel.Create();
            _model.Add(typeof(PlainObject), true);
        }

        [ProtoContract(SkipConstructor = true)]
        public class PlainObject
        {
            [ProtoMember(1)]
            public int Id { get; set; }
            [ProtoMember(2)]
            public string Name { get; set; }
            [ProtoMember(3)]
            public string Surname;

            public static PlainObject WithId(int i)
            {
                var po = new PlainObject { Id = i };
                po.Name = po.GetHashCode().ToString(CultureInfo.InvariantCulture);
                po.Surname = po.Name;
                return po;
            }
        }

        [ProtoContract(SkipConstructor = true)]
        [ProtoInclude(10, typeof(DescendantForClear))]
        public class RootForClear
        {
            [ProtoMember(1)]
            public int BaseStruct { get; set; }
            [ProtoMember(2)]
            public string BaseReference { get; set; }
        }

        [ProtoContract(SkipConstructor = true)]
        public class DescendantForClear : RootForClear
        {
            [ProtoMember(1)]
            public int Struct { get; set; }
            [ProtoMember(2)]
            public string Reference { get; set; }
        }

        [Test]
        public void PlainObjectsWithNoInheritance()
        {
            const PrefixStyle prefix = PrefixStyle.Base128;
            const string memberName = "Id";

            using (var ms = new MemoryStream())
            {
                var items = new List<PlainObject>();
                for (var i = 0; i < 10; i++)
                {
                    items.Add(PlainObject.WithId(i));
                }

                foreach (var item in items)
                {
                    _model.SerializeWithLengthPrefix(ms, item, prefix);
                }

                var builder = new ProjectionTypeBuilder(_model);

                var type = builder.GetTypeForProjection(typeof(PlainObject), typeof(PlainObject).GetMember(memberName));

                ms.Seek(0, SeekOrigin.Begin);
                var deserializedItems = _model.DeserializeItems(ms, type, prefix, 0, null).Cast<object>().ToArray();

                Assert.AreEqual(items.Count, deserializedItems.Length);

                var fi = (FieldInfo)type.GetMember(memberName)[0];
                for (var i = 0; i < items.Count; i++)
                {
                    Assert.AreEqual(items[i].Id, (int)fi.GetValue(deserializedItems[i]));
                }
            }
        }

        [Test]
        public void ClearTruelyClearsAllProperties()
        {
            const PrefixStyle prefix = PrefixStyle.Base128;

            using (var ms = new MemoryStream())
            {
                _model.SerializeWithLengthPrefix<RootForClear>(ms, new DescendantForClear
                {
                    Reference = "test",
                    Struct = 1,
                    BaseReference = "test",
                    BaseStruct = 1,
                }, prefix);

                var builder = new ProjectionTypeBuilder(_model);
                var allPropsAndFields = typeof(DescendantForClear).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Where(FieldOrProperty).ToArray();
                var type = builder.GetTypeForProjection(typeof(DescendantForClear), allPropsAndFields);

                ms.Seek(0, SeekOrigin.Begin);
                var item = _model.DeserializeItems(ms, type, prefix, 0, null).Cast<object>().Single();

                var fiStruct = (FieldInfo)type.GetMember("Struct")[0];
                Assert.AreNotEqual(0, (int)fiStruct.GetValue(item));

                var fiReference = (FieldInfo)type.GetMember("Reference")[0];
                Assert.IsNotNullOrEmpty((string)fiReference.GetValue(item));

                var fiBaseStruct = (FieldInfo)type.GetMember("BaseStruct")[0];
                Assert.AreNotEqual(0, (int)fiBaseStruct.GetValue(item));

                var fiBaseReference = (FieldInfo)type.GetMember("BaseReference")[0];
                Assert.IsNotNullOrEmpty((string)fiBaseReference.GetValue(item));

                ((IProtoLinqObject)item).Clear();
                Assert.AreEqual(0, (int)fiStruct.GetValue(item));
                Assert.IsNull(fiReference.GetValue(item));
                Assert.AreEqual(0, (int)fiBaseStruct.GetValue(item));
                Assert.IsNull(fiBaseReference.GetValue(item));
            }
        }

        private static bool FieldOrProperty(MemberInfo mi)
        {
            return mi.MemberType == MemberTypes.Field || mi.MemberType == MemberTypes.Property;
        }
    }
}