using System.Reflection;
using NUnit.Framework;
using ProtoBuf.Filters;

namespace protobuf_linq.Tests
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
                set { var sthElse = _nonAuto = value; }
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

        private static PropertyInfo GetProp(string autopropwithbackingfield)
        {
            return typeof(Autoprops).GetProperty(autopropwithbackingfield);
        }
    }
}