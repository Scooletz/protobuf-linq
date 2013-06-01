using System;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Linq.LinqImpl;
using ProtoBuf.Meta;

namespace protobuf_linq.Tests.Impl
{
    public class ProtobufExtensionsTests
    {
        private readonly RuntimeTypeModel _model;

        public ProtobufExtensionsTests()
        {
            _model = TypeModel.Create();
            _model.Add(typeof(A), true);
        }

        [ProtoContract]
        [ProtoInclude(1, typeof(B))]
        public class A
        {
        }
        
        [ProtoContract]
        [ProtoInclude(1, typeof(C))]
        public class B : A
        {
        }

        [ProtoContract]
        public class C : B
        {
        }

        [Test]
        public void WhenGetHierarchyRootCalledForRoot_ThenRootIsReturned()
        {
            Assert.AreEqual(typeof(A), GetRoot<A>());
        }
        
        [Test]
        public void WhenGetHierarchyRootCalledAnyDescendant_ThenRootIsReturned()
        {
            Assert.AreEqual(typeof(A), GetRoot<B>());
            Assert.AreEqual(typeof(A), GetRoot<C>());
        }

        private Type GetRoot<T>()
        {
            return _model[typeof(T)].GetHierarchyRoot().Type;
        }
    }
}