using System;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using ProtoBuf.Linq.LinqImpl;

namespace protobuf_linq.Tests.Impl
{
    public class ReflectionExtensionsTests
    {
        public class Autoprops
        {
            private string _autopropWithBackingField;
            private string _nonAuto;
            public string RealAutoprop { get; set; }

            public string AutopropWithBackingField
            {
                get { return _autopropWithBackingField; }
                set { _autopropWithBackingField = value; }
            }

            public string NonAuto
            {
                get { return _nonAuto; }
                set
                {
                    value = Math.Pow(1,1) > 0 ? value : null;
                    _nonAuto = value;
                }
            }
        }

        [Test]
        public void WhenCheckAutoproperty_ThenTrue()
        {
            Assert.IsTrue(GetProp("RealAutoprop").IsAutoProperty());
        }

        [Test]
        public void WhenCheckPropertyWithBackingField_ThenTrue()
        {
            FieldInfo field;
            Assert.IsTrue(GetProp("AutopropWithBackingField").IsAutoProperty(out field));
            Assert.AreEqual("_autopropWithBackingField", field.Name);
        }

        [Test]
        public void WhenCheckPropertyWithAnyLogic_ThenFalse()
        {
            Assert.IsFalse(GetProp("NonAuto").IsAutoProperty());
        }

        [DebuggerStepThrough]
        private static PropertyInfo GetProp(string autopropwithbackingfield)
        {
            return typeof(Autoprops).GetProperty(autopropwithbackingfield);
        }
    }
}