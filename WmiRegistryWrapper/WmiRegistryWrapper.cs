using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management;

namespace WmiRegistryWrapper
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum RootKey : uint
    {
        HKEY_CLASSES_ROOT = 0x80000000,
        HKEY_CURRENT_USER = 0x80000001,
        HKEY_LOCAL_MACHINE = 0x80000002,
        HKEY_USERS = 0x80000003,
        HKEY_CURRENT_CONFIG = 0x80000005
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum RegValueType
    {
        BINARY,
        DWORD,
        EXPANDED_STRING,
        MULTI_STRING,
        STRING
    }

    public enum RegCommand
    {
        EnumKey,
        EnumValues,
        DeleteKey,
        CreateKey
    }

    public class WmiRegistryWrapper
    {
        // Local connection
        public WmiRegistryWrapper()
        {
            Scope = new ManagementScope(ManagementPath.DefaultPath, Options);
            Registry = new ManagementClass(Scope, new ManagementPath("StdRegProv"), new ObjectGetOptions());
        }

        // Remote connection
        public WmiRegistryWrapper(string ipOrMachineName, string user, string password)
        {
            Options.Username = user;
            Options.Password = password;

            Scope = new ManagementScope(@"\\" + ipOrMachineName + @"\root\CIMV2", Options);
            Registry = new ManagementClass(Scope, new ManagementPath("StdRegProv"), new ObjectGetOptions());
        }

        public bool IsConnected => Scope.IsConnected;

        private ConnectionOptions Options { get; } = new()
        {
            Impersonation = ImpersonationLevel.Impersonate,
            Authentication = AuthenticationLevel.Default,
            EnablePrivileges = true
        };

        private ManagementScope Scope { get; }

        private ManagementClass Registry { get; }

        public void Connect()
        {
            Scope.Connect();
        }

        public void DeleteSubKey(RootKey rootKey, string regPath)
        {
            ExecuteCommand(rootKey, regPath, RegCommand.DeleteKey);
        }

        public void CreateSubKey(RootKey rootKey, string regPath)
        {
            ExecuteCommand(rootKey, regPath, RegCommand.CreateKey);
        }


        public IEnumerable<string>? EnumerateSubKeys(RootKey rootKey, string regPath)
        {
            return (string[]?) ExecuteCommand(rootKey, regPath, RegCommand.EnumKey).GetPropertyValue("sNames");
        }

        public IEnumerable<string>? EnumerateValueNames(RootKey rootKey, string regPath)
        {
            return (string[]?) ExecuteCommand(rootKey, regPath, RegCommand.EnumValues).GetPropertyValue("sNames");
        }

        public IEnumerable<byte>? GetBinaryValue(RootKey rootKey, string regPath, string valueName)
        {
            return (byte[]?) GetValueCommand(rootKey, regPath, valueName, RegValueType.BINARY)
                .GetPropertyValue("uValue");
        }

        public string? GetStringValue(RootKey rootKey, string regPath, string valueName)
        {
            return GetValueCommand(rootKey, regPath, valueName, RegValueType.STRING).GetPropertyValue("sValue")
                ?.ToString();
        }

        public void SetValue(RootKey rootKey, string regPath, string valueName, IEnumerable<byte> value)
        {
            SetValueCommand(rootKey, regPath, valueName, value, RegValueType.BINARY);
        }

        private ManagementBaseObject SetValueCommand(RootKey rootKey, string regPath,
            string valueName, object value, RegValueType regValueType)
        {
            if (!IsConnected) throw new Exception("The registry is not connected");

            var methodName = ConvertSetValueType(regValueType);
            var methodParams = Registry.GetMethodParameters(methodName);

            methodParams["hDefKey"] = rootKey;
            methodParams["sSubKeyName"] = regPath;
            methodParams["sValueName"] = valueName;
            methodParams["uValue"] = value;

            return Registry.InvokeMethod(methodName, methodParams, null) ?? throw new InvalidOperationException();
        }

        private ManagementBaseObject GetValueCommand(RootKey rootKey, string regPath,
            string valueName, RegValueType regValueType)
        {
            if (!IsConnected) throw new Exception("The registry is not connected");

            var methodName = ConvertGetValueType(regValueType);
            var methodParams = Registry.GetMethodParameters(methodName);

            methodParams["hDefKey"] = rootKey;
            methodParams["sSubKeyName"] = regPath;
            methodParams["sValueName"] = valueName;

            return Registry.InvokeMethod(methodName, methodParams, null) ?? throw new InvalidOperationException();
        }

        private ManagementBaseObject ExecuteCommand(RootKey rootKey, string regPath, RegCommand regCommand)
        {
            if (!IsConnected) throw new Exception("The registry is not connected");
            var commandName = regCommand.ToString();

            var methodParams = Registry.GetMethodParameters(commandName);

            methodParams["hDefKey"] = rootKey;
            methodParams["sSubKeyName"] = regPath;

            return Registry.InvokeMethod(commandName, methodParams, null) ?? throw new InvalidOperationException();
        }

        private static string ConvertGetValueType(RegValueType entry)
        {
            return entry switch
            {
                RegValueType.BINARY => "GetBinaryValue",
                RegValueType.DWORD => "GetDWORDValue",
                RegValueType.EXPANDED_STRING => "GetExpandedStringValue",
                RegValueType.MULTI_STRING => "GetMultiStringValue",
                RegValueType.STRING => "GetStringValue",
                _ => throw new ArgumentOutOfRangeException(nameof(entry), entry, null)
            };
        }

        private static string ConvertSetValueType(RegValueType entry)
        {
            return entry switch
            {
                RegValueType.BINARY => "SetBinaryValue",
                RegValueType.DWORD => "SetDWORDValue",
                RegValueType.EXPANDED_STRING => "SetExpandedStringValue",
                RegValueType.MULTI_STRING => "SetMultiStringValue",
                RegValueType.STRING => "SetStringValue",
                _ => throw new ArgumentOutOfRangeException(nameof(entry), entry, null)
            };
        }
    }
}