using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Linq;
using ProtoBuf.Linq.LinqImpl;
using ProtoBuf.Meta;

namespace protobuf_linq.Tests
{
    public class IntegrationTests
    {
        private const PrefixStyle Prefix = PrefixStyle.Base128;
        private const int RepetetionCount = 10;
        private const int ManagersCount = RepetetionCount;
        private const int EmployeesCount = RepetetionCount;

        private readonly RuntimeTypeModel _model;

        public IntegrationTests()
        {
            _model = TypeModel.Create();
            _model.Add(typeof(Employee), true);
        }

        public enum SkillLevel
        {
            None,
            Some,
            TheBest
        }

        [ProtoContract]
        [ProtoInclude(10, typeof(Manager))]
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

            public static Employee CreateManager(int id, string name, string city, string street)
            {
                return new Manager
                {
                    Id = id,
                    Name = name,
                    Address = new Address
                    {
                        City = city,
                        Street = street
                    },
                    ManagementSkills = SkillLevel.Some,
                    TeamSize = id,
                };
            }

            [ProtoMember(1)]
            public int Id;

            [ProtoMember(2)]
            public string Name { get; set; }

            [ProtoMember(3)]
            public Address Address { get; set; }
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

        private MemoryStream _stream;

        [SetUp]
        public void SetUp()
        {
            _stream = new MemoryStream();
            for (var i = 0; i < RepetetionCount; i++)
            {
                _model.SerializeWithLengthPrefix(_stream, Employee.Create(2 * i, "Test" + i, "City" + i, "Street+i"),
                    Prefix);
                _model.SerializeWithLengthPrefix(_stream,
                    Employee.CreateManager(2 * i + 1, "Test" + i, "City" + i, "Street+i"), Prefix);
            }
            _stream.Seek(0, SeekOrigin.Begin);
        }

        [TearDown]
        public void TearDown()
        {
            _stream.Dispose();
            _stream = null;
        }

        [Test]
        public void QueryWithWhereAndSelect()
        {
            var q = _model.AsQueryable<Employee>(_stream, Prefix);

            var linq = from e in q
                       where e.Id % 2 == 0
                       select new { e.Id, e.Name };

            foreach (var e in linq)
            {
                Assert.IsTrue(e.Id % 2 == 0);
            }
        }

        [Test]
        public void QueryWithOftypeAndSelect()
        {
            var q = _model.AsQueryable<Employee>(_stream, Prefix);

            // manager test
            Assert.AreEqual(ManagersCount, q.OfType<Manager>().Count());

            // hierarchy test
            _stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(ManagersCount + EmployeesCount, q.OfType<Employee>().Count());
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))] // TODO: currently, it throws
        public void QueryWithSelectOfDeserializedItemReturnsOriginalType()
        {
            var q = _model.AsQueryable<Employee>(_stream, Prefix);

            var firstEmployee = q.Select(e => e).FirstOrDefault();
            Assert.IsInstanceOf<Employee>(firstEmployee);
        }

        [Test]
        public void QueryWithWhereAndAggregate()
        {
            var q = _model.AsQueryable<Employee>(_stream, Prefix);
            var idSum = q.Aggregate(0, (prev, emp) => prev + emp.Id);
            Console.WriteLine(idSum);
        }
    }
}