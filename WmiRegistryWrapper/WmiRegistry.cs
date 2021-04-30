using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management;

namespace WmiRegistryWrapper
{
    /// <summary>
    /// Enum for determining the root key of the registry.
    /// </summary>
    public enum RegistryHive : uint
    {
        ClassesRoot = 0x80000000,
        CurrentUser = 0x80000001,
        LocalMachine = 0x80000002,
        Users = 0x80000003,
        CurrentConfig = 0x80000005
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum RegValueType
    {
        BINARY,
        DWORD,
        EXPANDED_STRING,
        MULTI_STRING,
        STRING
    }

    internal enum RegCommand
    {
        EnumKey,
        EnumValues,
        DeleteKey,
        CreateKey
    }

    /// <summary>
    /// Wrapper for accessing the Windows Registry.
    /// </summary>
    public class WmiRegistry
    {
        /// <summary>
        /// Constructor to create a connection to the local registry.
        /// </summary>
        public WmiRegistry()
        {
            Scope = new ManagementScope(ManagementPath.DefaultPath, Options);
            Registry = new ManagementClass(Scope, new ManagementPath("StdRegProv"), new ObjectGetOptions());
        }

        /// <summary>
        /// Constructor to create a connection to the remote registry.
        /// </summary>
        /// <param name="ipOrMachineName">Local IP or Windows Machine Name inside Workgroup.</param>
        /// <param name="user">Windows User Name.</param>
        /// <param name="password">Windows User Password.</param>
        public WmiRegistry(string ipOrMachineName, string user, string password)
        {
            Options.Username = user;
            Options.Password = password;

            Scope = new ManagementScope(@"\\" + ipOrMachineName + @"\root\CIMV2", Options);
            Registry = new ManagementClass(Scope, new ManagementPath("StdRegProv"), new ObjectGetOptions());
        }

        /// <summary>
        /// Gets a value indicating whether the WmiRegistry is currently bound to a WMI server.
        /// </summary>
        /// <returns>Returns a Boolean value indicating whether the WmiRegistry is currently bound
        /// to a WMI server.</returns>
        public bool IsConnected => Scope.IsConnected;

        private ConnectionOptions Options { get; } = new()
        {
            Impersonation = ImpersonationLevel.Impersonate,
            Authentication = AuthenticationLevel.Default,
            EnablePrivileges = true
        };

        private ManagementScope Scope { get; }

        private ManagementClass Registry { get; }

        /// <summary>
        /// Connects this WmiRegistry to the actual WMI.
        /// </summary>
        public void Connect() => Scope.Connect();
        
        // TODO: CheckAccess method must be here

        /// <summary>
        /// Deletes a SubKey in the specified tree.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">The key to be deleted.</param>
        /// <returns>True if operation was successful, False if not.</returns>
        public bool TryDeleteSubKey(RegistryHive registryHive, string subKeyPath) =>
            (uint)TryExecuteCommand(registryHive, subKeyPath, RegCommand.DeleteKey).GetPropertyValue("ReturnValue") == 0;

        /// <summary>
        /// Creates a SubKey in the specified tree.
        /// This method creates all SubKeys specified in the path that do not exist.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">The key(s) to be created.</param>
        /// <returns>True if operation was successful, False if not.</returns>
        public bool TryCreateSubKey(RegistryHive registryHive, string subKeyPath) =>
            (uint)TryExecuteCommand(registryHive, subKeyPath, RegCommand.CreateKey).GetPropertyValue("ReturnValue") == 0;

        /// <summary>
        /// Enumerates the SubKeys for a path.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A path that contains the SubKeys to be enumerated.</param>
        /// <returns>An IEnumerable of SubKey strings</returns>
        public IEnumerable<string>? EnumerateSubKeys(RegistryHive registryHive, string subKeyPath) =>
            (IEnumerable<string>?) TryExecuteCommand(registryHive, subKeyPath, RegCommand.EnumKey)
                .GetPropertyValue("sNames");

        // TODO: EnumerateValueNames must be here

        /// <summary>
        /// Method returns the data value for a named value whose data type is REG_BINARY.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A path that contains the named values.</param>
        /// <param name="valueName">A named value whose data value you are retrieving.
        /// Specify an empty string to get the default named value.</param>
        /// <returns>An IEnumerable of binary bytes.</returns>
        public IEnumerable<byte>? GetBinaryValue(RegistryHive registryHive, string subKeyPath, string valueName) =>
            (IEnumerable<byte>?) GetValueCommand(registryHive, subKeyPath, valueName, RegValueType.BINARY)
                .GetPropertyValue("uValue");

        /// <summary>
        /// Method returns the data value for a named value whose data type is REG_SZ.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A path that contains the named values.</param>
        /// <param name="valueName">A named value whose data value you are retrieving.
        /// Specify an empty string to get the default named value.</param>
        /// <returns>A data value for the named value.</returns>
        public string? GetStringValue(RegistryHive registryHive, string subKeyPath, string valueName) =>
            GetValueCommand(registryHive, subKeyPath, valueName, RegValueType.STRING).GetPropertyValue("sValue")
                ?.ToString();
        
        // TODO: Here must be methods to retrieve the rest reg data types

        /// <summary>
        /// Method sets the data value for a named value whose data type is REG_BINARY.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A key that contains the named value to be set.</param>
        /// <param name="valueName">A named value whose data value you are setting.
        /// You can specify an existing named value (update) or a new named value (create).
        /// Specify an empty string to set the data value for the default named value.</param>
        /// <param name="value">An IEnumerable of binary data values. The default value is [1,2].</param>
        /// <returns>True if operation was successful, False if not.</returns>
        public bool TrySetValue(RegistryHive registryHive, string subKeyPath, string valueName, IEnumerable<byte> value) =>
            (uint)SetValueCommand(registryHive, subKeyPath, valueName, value, RegValueType.BINARY).GetPropertyValue("ReturnValue") == 0;
        
        /// <summary>
        /// Method sets the data value for a named value whose data type is REG_SZ.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A key that contains the named value to be set.</param>
        /// <param name="valueName">A named value whose data value you are setting.
        /// You can specify an existing named value (update) or a new named value (create).
        /// Specify an empty string to set the data value for the default named value.</param>
        /// <param name="value">A string to be set in a named value</param>
        /// <returns>True if operation was successful, False if not.</returns>
        public bool TrySetValue(RegistryHive registryHive, string subKeyPath, string valueName, string value) =>
            (uint)SetValueCommand(registryHive, subKeyPath, valueName, value, RegValueType.STRING).GetPropertyValue("ReturnValue") == 0;
        
        // TODO: Here must be methods to set the rest reg data types

        private ManagementBaseObject TryExecuteCommand(RegistryHive registryHive, string subKeyPath, RegCommand regCommand)
        {
            if (!IsConnected) throw new Exception("The registry is not connected");
            var methodName = regCommand.ToString();

            var methodParams = Registry.GetMethodParameters(methodName);

            methodParams["hDefKey"] = registryHive;
            methodParams["sSubKeyName"] = subKeyPath;

            return Registry.InvokeMethod(methodName, methodParams, new InvokeMethodOptions()) ?? throw new InvalidOperationException();
        }
        
        private ManagementBaseObject GetValueCommand(RegistryHive registryHive, string subKeyPath,
            string valueName, RegValueType regValueType)
        {
            if (!IsConnected) throw new Exception("The registry is not connected");

            var methodName = ConvertGetValueType(regValueType);
            var methodParams = Registry.GetMethodParameters(methodName);

            methodParams["hDefKey"] = registryHive;
            methodParams["sSubKeyName"] = subKeyPath;
            methodParams["sValueName"] = valueName;

            return Registry.InvokeMethod(methodName, methodParams, new InvokeMethodOptions()) ?? throw new InvalidOperationException();
        }
        
        private ManagementBaseObject SetValueCommand(RegistryHive registryHive, string subKeyPath,
            string valueName, object value, RegValueType regValueType)
        {
            if (!IsConnected) throw new Exception("The registry is not connected");

            var methodName = ConvertSetValueType(regValueType);
            var methodParams = Registry.GetMethodParameters(methodName);

            methodParams["hDefKey"] = registryHive;
            methodParams["sSubKeyName"] = subKeyPath;
            methodParams["sValueName"] = valueName;
            switch (regValueType)
            {
                case RegValueType.BINARY:
                    methodParams["uValue"] = value;
                    break;
                case RegValueType.DWORD:
                    methodParams["uValue"] = value;
                    break;
                case RegValueType.EXPANDED_STRING:
                    methodParams["sValue"] = value;
                    break;
                case RegValueType.MULTI_STRING:
                    methodParams["sValue"] = value;
                    break;
                case RegValueType.STRING:
                    methodParams["sValue"] = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(regValueType), regValueType, null);
            }
            
            return Registry.InvokeMethod(methodName, methodParams, new InvokeMethodOptions()) ?? throw new InvalidOperationException();
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