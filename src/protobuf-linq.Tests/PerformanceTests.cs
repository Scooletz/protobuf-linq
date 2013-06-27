using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Linq;
using ProtoBuf.Linq.LinqImpl;
using ProtoBuf.Meta;

namespace protobuf_linq.Tests
{
    [TestFixture(Category = "Performance")]
    [Explicit]
    public class PerformanceTests
    {
        private const PrefixStyle Prefix = PrefixStyle.Base128;
        private readonly RuntimeTypeModel _model;
        private readonly MemoryStream _stream;

        public PerformanceTests()
        {
            _model = TypeModel.Create();
            _model.Add(typeof(Event), true);
            _stream = new MemoryStream();

            for (var i = 0; i < 1000 * 1000; i++)
            {
                var id = Guid.NewGuid();
                var item = new OrderPlacedEvent(id,
                    "Ordered by customer number" + i.ToString(CultureInfo.InvariantCulture),
                    new[] { new OrderItem("Test product", i) });
                var shipment = new OrderShippedEvent(id, DateTime.Now);

                _model.SerializeWithLengthPrefix(_stream, item, typeof(Event), Prefix, 0);
                _model.SerializeWithLengthPrefix(_stream, shipment, typeof(Event), Prefix, 0);
            }
        }

        [ProtoContract(SkipConstructor = true)]
        [ProtoInclude(10, typeof(OrderPlacedEvent))]
        [ProtoInclude(11, typeof(OrderShippedEvent))]
        public abstract class Event
        {
            [ProtoMember(1)]
            public readonly Guid AggregateId;
            [ProtoMember(2)]
            public readonly DateTime OccuranceDate;

            protected Event(Guid aggregateId)
            {
                AggregateId = aggregateId;
                OccuranceDate = DateTime.Now;
            }
        }

        [ProtoContract(SkipConstructor = true)]
        public class OrderItem
        {
            [ProtoMember(1)]
            public readonly string Name;
            [ProtoMember(2)]
            public readonly int Quantity;

            public OrderItem(string name, int quantity)
            {
                Name = name;
                Quantity = quantity;
            }
        }

        [ProtoContract(SkipConstructor = true)]
        public class OrderPlacedEvent : Event
        {
            [ProtoMember(1)]
            public readonly OrderItem[] Items;
            [ProtoMember(2)]
            public readonly string OrderedBy;

            public OrderPlacedEvent(Guid aggregateId, string orderedBy, OrderItem[] items)
                : base(aggregateId)
            {
                OrderedBy = orderedBy;
                Items = items;
            }
        }

        [ProtoContract(SkipConstructor = true)]
        public class OrderShippedEvent : Event
        {
            public readonly DateTime ShippmentDate;

            public OrderShippedEvent(Guid aggregateId, DateTime shippmentDate)
                : base(aggregateId)
            {
                ShippmentDate = shippmentDate;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _stream.Seek(0, SeekOrigin.Begin);
        }

        [Test]
        [TestCaseSource("GetOptions")]
        public void Count(QueryableOptions options)
        {
            Measure(
                () => _model.DeserializeItems<Event>(_stream, options.PrefixStyle, 0).Count(),
                () => _model.AsQueryable<Event>(_stream, options).Count()
                );
        }

        [Test]
        [TestCaseSource("GetOptions")]
        public void OfTypeCount(QueryableOptions options)
        {
            Measure(
                () => _model.DeserializeItems<Event>(_stream, options.PrefixStyle, 0).OfType<OrderPlacedEvent>().Count(),
                () => _model.AsQueryable<Event>(_stream, options).OfType<OrderPlacedEvent>().Count()
                );
        }

        [Test]
        [TestCaseSource("GetOptions")]
        public void SelectOneProperty(QueryableOptions options)
        {
            Measure(
                () => _model.DeserializeItems<Event>(_stream, options.PrefixStyle, 0).Select(e => e.OccuranceDate).All(td => true),
                () => _model.AsQueryable<Event>(_stream, options).Select(e => e.OccuranceDate).All(td => true)
                );
        }

        public static IEnumerable<TestCaseData> GetOptions()
        {
            yield return new TestCaseData(new QueryableOptions { PrefixStyle = PrefixStyle.Base128, UseAggresiveNoAllocObjectReuse = false })
                .SetName("No reuse"); 
            
            yield return new TestCaseData(new QueryableOptions { PrefixStyle = PrefixStyle.Base128, UseAggresiveNoAllocObjectReuse = false })
                .SetName("With instance reuse");
        }

        private void Measure(Action original, Action linq)
        {
            var originalTime = Measure(original);
            var linqTime = Measure(linq);

            Console.WriteLine("Original took: " + originalTime);
            Console.WriteLine("Linq took:     " + linqTime);

            Assert.GreaterOrEqual(originalTime.Ticks, linqTime.Ticks);
        }

        private TimeSpan Measure(Action a)
        {
            var sw = Stopwatch.StartNew();
            a();
            sw.Stop();

            _stream.Seek(0, SeekOrigin.Begin);

            return sw.Elapsed;
        }
    }
}