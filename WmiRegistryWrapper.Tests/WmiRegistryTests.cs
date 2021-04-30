using Microsoft.Win32;
using System.Linq;
using Xunit;

namespace WmiRegistryWrapper.Tests
{
    public class WmiRegistryTests
    {
        private readonly WmiRegistry _localRegistry = new();

        public WmiRegistryTests()
        {
            _localRegistry.Connect();
        }

        #region Commands

        [Fact]
        public void EnumerateSubKeysTest()
        {
            var subKeysCount = Registry.LocalMachine.OpenSubKey("SOFTWARE")!.SubKeyCount;
            var subKeys = _localRegistry.EnumerateSubKeys(RegistryHive.LocalMachine, "SOFTWARE");

            Assert.Equal(subKeysCount, subKeys?.Count());
        }

        [Fact]
        public void CreateSubKeysTest()
        {
            var beforeCreatingSubKeys =
                Registry.LocalMachine.OpenSubKey("SOFTWARE")!.GetSubKeyNames();
            if (beforeCreatingSubKeys.Contains("WmiRegistryTests"))
                Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.DeleteSubKey(
                    "WmiRegistryTests");

            var createSubKeyResult =
                _localRegistry.TryCreateSubKey(RegistryHive.LocalMachine,
                    @"SOFTWARE\WmiRegistryTests");
            var afterCreatingSubKeys =
                Registry.LocalMachine.OpenSubKey("SOFTWARE")!.GetSubKeyNames();

            Assert.Contains("WmiRegistryTests", afterCreatingSubKeys);
            Assert.True(createSubKeyResult);

            Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.DeleteSubKey(
                "WmiRegistryTests");
        }

        [Fact]
        public void DeleteSubKeysTest()
        {
            var beforeDeletingSubKeys =
                Registry.LocalMachine.OpenSubKey("SOFTWARE")!.GetSubKeyNames();
            if (!beforeDeletingSubKeys.Contains("WmiRegistryTests"))
                Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.CreateSubKey(
                    "WmiRegistryTests");

            var deleteSubKeyResult =
                _localRegistry.TryDeleteSubKey(RegistryHive.LocalMachine,
                    @"SOFTWARE\WmiRegistryTests");
            var afterDeletingSubKeys =
                Registry.LocalMachine.OpenSubKey("SOFTWARE")!.GetSubKeyNames();

            Assert.DoesNotContain("WmiRegistryTests", afterDeletingSubKeys);
            Assert.True(deleteSubKeyResult);

            if (afterDeletingSubKeys.Contains("WmiRegistryTests"))
                Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.DeleteSubKey(
                    "WmiRegistryTests");
        }

        #endregion


        #region Setters

        [Fact]
        public void SetStringValueTest()
        {
            const string valueName = "TestStringValueName";
            const string value = "TestValue";

            var subKeyNames =
                Registry.LocalMachine.OpenSubKey("SOFTWARE")!.GetSubKeyNames();
            if (!subKeyNames.Contains("WmiRegistryTests"))
                Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.CreateSubKey(
                    "WmiRegistryTests");


            var setStringValueResult = _localRegistry.TrySetValue(RegistryHive.LocalMachine,
                @"SOFTWARE\WmiRegistryTests", valueName, value);

            Assert.Equal(value,
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WmiRegistryTests")!.GetValue(valueName)!.ToString());
            Assert.True(setStringValueResult);

            Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.DeleteSubKey(
                "WmiRegistryTests");
        }

        [Fact]
        public void SetBinaryValueTest()
        {
            const string valueName = "TestBinaryValueName";
            byte[] value = {1, 2, 3, 4, 5, 6};

            var subKeyNames =
                Registry.LocalMachine.OpenSubKey("SOFTWARE")!.GetSubKeyNames();
            if (!subKeyNames.Contains("WmiRegistryTests"))
                Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.CreateSubKey(
                    "WmiRegistryTests");

            var setStringValueResult = _localRegistry.TrySetValue(RegistryHive.LocalMachine,
                @"SOFTWARE\WmiRegistryTests", valueName, value);

            Assert.Equal(value,
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WmiRegistryTests")!.GetValue(valueName));
            Assert.True(setStringValueResult);

            Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.DeleteSubKey(
                "WmiRegistryTests");
        }

        #endregion

        #region Getters

        [Fact]
        public void GetBinaryValueTest()
        {
            const string valueName = "TestBinaryValueName";
            byte[] value = {1, 2, 3, 4, 5, 6};

            var subKeyNames =
                Registry.LocalMachine.OpenSubKey("SOFTWARE")!.GetSubKeyNames();
            if (!subKeyNames.Contains("WmiRegistryTests"))
                Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.CreateSubKey(
                    "WmiRegistryTests");

            var valueNames =
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WmiRegistryTests")!.GetValueNames();
            if (!valueNames.Contains(valueName))
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WmiRegistryTests", true)!.SetValue(valueName, value);

            var binaryValueResult = _localRegistry.GetBinaryValue(RegistryHive.LocalMachine,
                @"SOFTWARE\WmiRegistryTests", valueName);

            Assert.Equal(value, binaryValueResult);

            Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.DeleteSubKey(
                "WmiRegistryTests");
        }

        [Fact]
        public void GetStringValueTest()
        {
            const string valueName = "TestBinaryValueName";
            const string value = "TestValue";

            var subKeyNames =
                Registry.LocalMachine.OpenSubKey("SOFTWARE")!.GetSubKeyNames();
            if (!subKeyNames.Contains("WmiRegistryTests"))
                Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.CreateSubKey(
                    "WmiRegistryTests");

            var valueNames =
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WmiRegistryTests")!.GetValueNames();
            if (!valueNames.Contains(valueName))
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WmiRegistryTests", true)!.SetValue(valueName, value);

            var stringValueResult = _localRegistry.GetStringValue(RegistryHive.LocalMachine,
                @"SOFTWARE\WmiRegistryTests", valueName);

            Assert.Equal(value, stringValueResult);

            Registry.LocalMachine.OpenSubKey("SOFTWARE", true)!.DeleteSubKey(
                "WmiRegistryTests");
        }

        #endregion
    }
}