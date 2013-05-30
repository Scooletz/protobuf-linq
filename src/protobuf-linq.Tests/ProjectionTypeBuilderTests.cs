using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.LinqImpl;
using ProtoBuf.Meta;

namespace protobuf_linq.Tests
{
    public class ProjectionTypeBuilderTests
    {
        [ProtoContract(SkipConstructor=true)]
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
                var po = new PlainObject {Id = i};
                po.Name = po.GetHashCode().ToString(CultureInfo.InvariantCulture);
                po.Surname = po.Name;
                return po;
            }
        }

        [Test]
        public void PlainObjectsWithNoInheritance()
        {
            const PrefixStyle prefix = PrefixStyle.Base128;

            using (var ms = new MemoryStream())
            {
                for (var i = 0; i < 10; i++)
                {
                    Serializer.SerializeWithLengthPrefix(ms, PlainObject.WithId(i), prefix);
                }

                var builder = new ProjectionTypeBuilder(RuntimeTypeModel.Default);
                var type = builder.GetTypeForProjection(typeof (PlainObject), typeof (PlainObject).GetMember("Id"));

                ms.Seek(0, SeekOrigin.Begin);
                var items = RuntimeTypeModel.Default.DeserializeItems(ms, type, prefix, 0, null).Cast<object>().ToArray();
            }
        }
    }
}