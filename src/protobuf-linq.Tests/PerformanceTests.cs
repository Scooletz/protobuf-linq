using System;
using System.IO;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Linq.LinqImpl;
using ProtoBuf.Meta;

namespace protobuf_linq.Tests
{
    public class PerformanceTests
    {
        private const PrefixStyle Prefix = PrefixStyle.Base128;

        private readonly RuntimeTypeModel _model;

        public PerformanceTests()
        {
            _model = TypeModel.Create();
            _model.Add(typeof (Event), true);
        }

        [ProtoContract(SkipConstructor = true)]
        [ProtoInclude(10, typeof (OrderPlacedEvent))]
        [ProtoInclude(11, typeof (OrderShippedEvent))]
        public abstract class Event
        {
            [ProtoMember(1)] public readonly Guid AggregateId;
            [ProtoMember(2)] public readonly DateTime OccuranceDate;

            protected Event(Guid aggregateId)
            {
                AggregateId = aggregateId;
                OccuranceDate = DateTime.Now;
            }
        }

        [ProtoContract]
        public class OrderItem
        {
            [ProtoMember(1)] public readonly string Name;
            [ProtoMember(2)] public readonly int Quantity;

            public OrderItem(string name, int quantity)
            {
                Name = name;
                Quantity = quantity;
            }
        }

        [ProtoContract(SkipConstructor = true)]
        public class OrderPlacedEvent : Event
        {
            [ProtoMember(1)] public readonly OrderItem[] Items;
            [ProtoMember(2)] public readonly string OrderedBy;

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

        private MemoryStream _stream;

        [SetUp]
        public void SetUp()
        {
            _stream = new MemoryStream();
        }

        [TearDown]
        public void TearDown()
        {
            _stream.Dispose();
            _stream = null;
        }

        [Test]
        public void compare_count()
        {
            
        }
    }

    
}