using System.IO;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.LinqImpl;
using ProtoBuf.Meta;

namespace protobuf_linq.Tests
{
    public class IntegrationScenarios
    {
        private readonly RuntimeTypeModel _model;

        public IntegrationScenarios()
        {
            _model = TypeModel.Create();
            _model.Add(typeof (Employee), true);
        }

        public enum SkillLevel
        {
            None,
            Some,
            TheBest
        }

        [ProtoContract]
        [ProtoInclude(10, typeof (Manager))]
        public class Employee
        {
            public static Employee Create(int id, string name, string city, string street)
            {
                return new Employee
                {
                    Id = id,
                    Name = name,
                    Address = new Address
                    {
                        City = city,
                        Street = street
                    }
                };
            }

            [ProtoMember(1)] public int Id;

            [ProtoMember(2)]
            public string Name { get; set; }

            [ProtoMember(3)]
            public Address Address{ get; set; }
        }

        [ProtoContract]
        public class Address
        {
            [ProtoMember(1)]
            public string City;
            
            [ProtoMember(2)]
            public string Street;
        }

        [ProtoContract(SkipConstructor = true)]
        public class Manager : Employee
        {
            public SkillLevel ManagementSkills { get; set; }
            public int TeamSize { get; set; }
        }

        [Test]
        public void query_with_where_over_employees()
        {
            const PrefixStyle prefix = PrefixStyle.Base128;

            using (var ms = new MemoryStream())
            {
                for (var i = 0; i < 10; i++)
                {
                    _model.SerializeWithLengthPrefix(ms, Employee.Create(i, "Test" + i, "City" + i, "Street+i"), prefix);
                }
                ms.Seek(0, SeekOrigin.Begin);

                var q = _model.AsQueryable<Employee>(ms, prefix);

                var linq = from e in q
                    where e.Id%2 == 0
                    select new {e.Id, e.Name};

                foreach (var e in linq)
                {
                    Assert.IsTrue(e.Id%2 == 0);
                }
            }
        }
    }
}