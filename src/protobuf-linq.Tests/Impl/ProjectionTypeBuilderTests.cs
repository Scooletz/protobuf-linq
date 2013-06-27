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
                _model.SerializeWithLengthPrefix(ms, PlainObject.WithId(1), prefix);

                var builder = new ProjectionTypeBuilder(_model);
                var allPropsAndFields = typeof(PlainObject).GetMembers().Where(FieldOrProperty).ToArray();
                var type = builder.GetTypeForProjection(typeof(PlainObject), allPropsAndFields);

                ms.Seek(0, SeekOrigin.Begin);
                var item = _model.DeserializeItems(ms, type, prefix, 0, null).Cast<object>().Single();

                var fiId = (FieldInfo)type.GetMember("Id")[0];
                Assert.AreNotEqual(0, (int)fiId.GetValue(item));

                var fiName = (FieldInfo)type.GetMember("Name")[0];
                Assert.IsNotNullOrEmpty((string) fiName.GetValue(item));

                var fiSurname = (FieldInfo)type.GetMember("Surname")[0];
                Assert.IsNotNullOrEmpty((string)fiSurname.GetValue(item));

                ((IProtoLinqObject)item).Clear();
                Assert.AreEqual(0, (int)fiId.GetValue(item));
                Assert.IsNull(fiName.GetValue(item));
                Assert.IsNull(fiSurname.GetValue(item));
            }
        }

        private static bool FieldOrProperty(MemberInfo mi)
        {
            return mi.MemberType == MemberTypes.Field || mi.MemberType == MemberTypes.Property;
        }
    }
}