using System;
using Microsoft.Win32;
using System.Linq;
using Xunit;

namespace WmiRegistryWrapper.Tests
{
    public class WmiRegistryTests : IDisposable
    {
        private readonly WmiRegistry _localRegistry = new();
        private readonly string _regPath = "SOFTWARE";
        private readonly string _reqTestSubKey = "WmiRegistryTests";

        private string GetFullTestRegPath => _regPath + "\\" + _reqTestSubKey;

        public WmiRegistryTests()
        {
            _localRegistry.Connect();

            var beforeTestingSubKeys =
                Registry.CurrentUser.OpenSubKey(_regPath)!.GetSubKeyNames();
            if (beforeTestingSubKeys.Contains(_reqTestSubKey))
                Registry.CurrentUser.OpenSubKey(_regPath, true)!.DeleteSubKeyTree(_reqTestSubKey);

            Registry.CurrentUser.OpenSubKey(_regPath, true)!.CreateSubKey(
                _reqTestSubKey);
        }

        public void Dispose()
        {
            Registry.CurrentUser.OpenSubKey(_regPath, true)!.DeleteSubKeyTree(_reqTestSubKey);
            GC.SuppressFinalize(this);
        }

        #region Commands

        [Fact]
        public void EnumerateSubKeysTest()
        {
            var subKeysCount = Registry.CurrentUser.OpenSubKey(_regPath)!.SubKeyCount;
            var subKeys = _localRegistry.EnumerateSubKeys(RegistryHive.CurrentUser, _regPath);

            Assert.Equal(subKeysCount, subKeys?.Count());
        }

        [Fact]
        public void CreateSubKeysTest()
        {
            const string testCreatingSubKey = "TestCreatingSubKey";
            const string innerTestCreatingSubKey = "InnerTestCreatingSubKey";

            var createSubKeyResult =
                _localRegistry.TryCreateSubKey(RegistryHive.CurrentUser,
                    GetFullTestRegPath
                    + "\\" + testCreatingSubKey
                    + "\\" + innerTestCreatingSubKey);

            Assert.True(createSubKeyResult, "SubKey(s) not created");

            var afterCreatingSubKeys =
                Registry.CurrentUser.OpenSubKey(GetFullTestRegPath)!.GetSubKeyNames();
            var afterCreatingInnerSubKeys =
                Registry.CurrentUser.OpenSubKey(GetFullTestRegPath + "\\" + testCreatingSubKey.Split('\\')[0])!
                    .GetSubKeyNames();

            Assert.Contains(testCreatingSubKey, afterCreatingSubKeys);
            Assert.Contains(innerTestCreatingSubKey, afterCreatingInnerSubKeys);
        }

        [Fact]
        public void DeleteSubKeysTest()
        {
            const string testDeletingSubKey = "TestDeletingSubKey";
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testDeletingSubKey);

            var deleteSubKeyResult =
                _localRegistry.TryDeleteSubKey(RegistryHive.CurrentUser,
                    GetFullTestRegPath + "\\" + testDeletingSubKey);

            Assert.True(deleteSubKeyResult);

            var afterDeletingSubKeys =
                Registry.CurrentUser.OpenSubKey(GetFullTestRegPath)!.GetSubKeyNames();

            Assert.DoesNotContain(testDeletingSubKey, afterDeletingSubKeys);
        }

        #endregion

        #region Setters
        
        [Fact]
        public void SetStringValueTest()
        {
            const string testSetStringSubKey = "TestSetStringSubKey";
            const string valueName = "TestStringValueName";
            const string value = "TestStringValue";
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testSetStringSubKey);

            var setStringValueResult = _localRegistry.TrySetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testSetStringSubKey, valueName, value);

            Assert.True(setStringValueResult);

            var afterStringSettingResult =
                Registry.CurrentUser.OpenSubKey(GetFullTestRegPath + "\\" + testSetStringSubKey)!.GetValue(valueName);

            Assert.Equal(value, afterStringSettingResult);
        }
        
        [Fact]
        public void SetExpandedStringValueTest()
        {
            const string testSetExpandedStringSubKey = "TestSetExpandedStringSubKey";
            const string valueName = "TestExpandedStringValueName";
            const string value = "%USERNAME%";
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testSetExpandedStringSubKey);

            var setStringValueResult = _localRegistry.TrySetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testSetExpandedStringSubKey, valueName, value);

            Assert.True(setStringValueResult);

            var afterStringSettingResult =
                Registry.CurrentUser.OpenSubKey(GetFullTestRegPath + "\\" + testSetExpandedStringSubKey)!.GetValue(valueName);

            Assert.Equal(Environment.UserName, afterStringSettingResult);
        }
        
        [Fact]
        public void SetMultiStringValueTest()
        {
            const string testSetMultiStringSubKey = "TestSetMultiStringSubKey";
            const string valueName = "TestStringValueName";
            string[] value = {"TestStringValue1", "TestStringValue2"};
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testSetMultiStringSubKey);

            var setStringValueResult = _localRegistry.TrySetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testSetMultiStringSubKey, valueName, value);

            Assert.True(setStringValueResult);

            var afterStringSettingResult =
                Registry.CurrentUser.OpenSubKey(GetFullTestRegPath + "\\" + testSetMultiStringSubKey)!.GetValue(valueName);

            Assert.Equal(value, afterStringSettingResult);
        }

        [Fact]
        public void SetBinaryValueTest()
        {
            const string testSetBinarySubKey = "TestSetBinarySubKey";
            const string valueName = "TestBinaryValueName";
            byte[] value = {1, 2, 3, 4, 5, 6};
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testSetBinarySubKey);

            var setBinaryValueResult = _localRegistry.TrySetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testSetBinarySubKey, valueName, value);

            Assert.True(setBinaryValueResult);

            var afterBinarySettingResult =
                Registry.CurrentUser.OpenSubKey(GetFullTestRegPath + "\\" + testSetBinarySubKey)!.GetValue(valueName);

            Assert.Equal(value, afterBinarySettingResult);
        }

        [Fact]
        public void SetDWordValueTest()
        {
            const string testSetDWordSubKeys = "TestSetDWORDSubKey";
            const string valueName = "TestDWORDValueName";
            const uint value = int.MaxValue;
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testSetDWordSubKeys);

            var setDWordValueResult = _localRegistry.TrySetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testSetDWordSubKeys, valueName, value);

            Assert.True(setDWordValueResult);

            var afterDWordSettingResult =
                Registry.CurrentUser.OpenSubKey(GetFullTestRegPath + "\\" + testSetDWordSubKeys)!.GetValue(
                    valueName);

            Assert.Equal(value, Convert.ToUInt32(afterDWordSettingResult));
        }
        
        [Fact]
        public void SetQWordValueTest()
        {
            const string testSetQWordSubKeys = "TestSetQWORDSubKey";
            const string valueName = "TestQWORDValueName";
            const ulong value = long.MaxValue;
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testSetQWordSubKeys);

            var setDWordValueResult = _localRegistry.TrySetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testSetQWordSubKeys, valueName, value);

            Assert.True(setDWordValueResult);

            var afterDWordSettingResult =
                Registry.CurrentUser.OpenSubKey(GetFullTestRegPath + "\\" + testSetQWordSubKeys)!.GetValue(
                    valueName);

            Assert.Equal(value, Convert.ToUInt64(afterDWordSettingResult));
        }

        #endregion

        #region Getters

        [Fact]
        public void GetStringValueTest()
        {
            const string testGetStringSubKey = "TestGetStringSubKey";
            const string valueName = "TestStringValueName";
            const string value = "StringValue";
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testGetStringSubKey)
                .SetValue(valueName, value);

            var stringValueResult = _localRegistry.GetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testGetStringSubKey, valueName, RegistryValueType.String);

            Assert.Equal(value, stringValueResult);
        }
        
        [Fact]
        public void GetExpandedStringValueTest()
        {
            const string testGetExpandedStringSubKey = "TestGetExpandedStringSubKey";
            const string valueName = "TestExpandedStringValueName";
            const string value = "%NUMBER_OF_PROCESSORS%";
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testGetExpandedStringSubKey)
                .SetValue(valueName, value, RegistryValueKind.ExpandString);

            var stringValueResult = _localRegistry.GetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testGetExpandedStringSubKey, valueName, RegistryValueType.ExpandedString);
            
            Assert.Equal(Environment.ProcessorCount, Convert.ToInt32(stringValueResult));
        }
        
        [Fact]
        public void GetMultiStringValueTest()
        {
            const string testGetMultiStringSubKey = "TestGetMultiStringSubKey";
            const string valueName = "TestMultiStringValueName";
            string[] value = {"StringValue", "StringValue2"};
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testGetMultiStringSubKey)
                .SetValue(valueName, value);

            var stringValueResult = _localRegistry.GetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testGetMultiStringSubKey, valueName, RegistryValueType.MultiString);

            Assert.Equal(value, stringValueResult);
        }
        
        [Fact]
        public void GetBinaryValueTest()
        {
            const string testGetBinarySubKey = "TestGetBinarySubKey";
            const string valueName = "TestBinaryValueName";
            byte[] value = {1, 2, 3, 4, 5, 6};
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testGetBinarySubKey)
                .SetValue(valueName, value);

            var binaryValueResult = _localRegistry.GetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testGetBinarySubKey, valueName, RegistryValueType.Binary);

            Assert.Equal(value, binaryValueResult);
        }
        
        [Fact]
        public void GetDWordValueTest()
        {
            const string testGetDWordSubKey = "TestGetDWordSubKey";
            const string valueName = "TestDWordValueName";
            const int value = int.MaxValue;
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testGetDWordSubKey)
                .SetValue(valueName, value, RegistryValueKind.DWord);

            var dWordValueResult = _localRegistry.GetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testGetDWordSubKey, valueName, RegistryValueType.DWord);

            Assert.Equal(Convert.ToUInt32(value), dWordValueResult);
        }
        
        [Fact]
        public void GetQWordValueTest()
        {
            const string testGetQWordSubKey = "TestGetQWordSubKey";
            const string valueName = "TestQWordValueName";
            const long value = long.MaxValue;
            Registry.CurrentUser.OpenSubKey(GetFullTestRegPath, true)!.CreateSubKey(testGetQWordSubKey)
                .SetValue(valueName, value, RegistryValueKind.QWord);

            var dWordValueResult = _localRegistry.GetValue(RegistryHive.CurrentUser,
                GetFullTestRegPath + "\\" + testGetQWordSubKey, valueName, RegistryValueType.QWord);

            Assert.Equal(Convert.ToUInt64(value), dWordValueResult);
        }

        #endregion
    }
}