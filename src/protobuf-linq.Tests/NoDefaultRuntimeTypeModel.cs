using System.Reflection;
using NUnit.Framework;
using ProtoBuf.Meta;

namespace protobuf_linq.Tests
{
    [SetUpFixture]
    public class NoDefaultRuntimeTypeModel
    {
        [SetUp]
        public void BeforeAnyTests()
        {
            //RuntimeTypeModel.Default.Freeze();

            // as freezing RuntimeTypeModel.Default is prohibited
            // but it must be done to keep all tests clear
            // some reflection is used

            const BindingFlags instanceMember = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            const byte freezeFlag = 4;

            var setOption = typeof (RuntimeTypeModel).GetMethod("SetOption", instanceMember);
            setOption.Invoke(RuntimeTypeModel.Default, new object[] {freezeFlag, true});
        }
    }
}