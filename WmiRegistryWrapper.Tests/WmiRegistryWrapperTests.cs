using System.Linq;
using Xunit;

namespace WmiRegistryWrapper.Tests
{
    public class WmiRegistryWrapperTests
    {
        [Fact]
        public void EnumerateSubKeysTest()
        {
            var localRegistry = new WmiRegistryWrapper();
            localRegistry.Connect();

            var subKeys = localRegistry.EnumerateSubKeys(RootKey.HKEY_LOCAL_MACHINE, "SOFTWARE");

            Assert.True(subKeys?.Any());
        }
    }
}