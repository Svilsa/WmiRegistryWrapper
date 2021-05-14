using System;
using System.Collections.Generic;
using System.Management;

namespace WmiRegistryWrapper
{
    /// <summary>
    ///     Wrapper for accessing the Windows Registry by WMI.
    /// </summary>
    public class WmiRegistry
    {
        /// <summary>
        ///     Constructor to create a connection to the local registry.
        /// </summary>
        public WmiRegistry()
        {
            Scope = new ManagementScope(ManagementPath.DefaultPath, Options);
            Registry = new ManagementClass(Scope, new ManagementPath("StdRegProv"), new ObjectGetOptions());
        }

        /// <summary>
        ///     Constructor to create a connection to the remote registry.
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
        ///     Gets a value indicating whether the WmiRegistry is currently bound to a WMI server.
        /// </summary>
        /// <returns>
        ///     Returns a Boolean value indicating whether the WmiRegistry is currently bound
        ///     to a WMI server.
        /// </returns>
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
        ///     Connects this WmiRegistry to the actual WMI.
        /// </summary>
        public void Connect()
        {
            Scope.Connect();
        }

        /// <summary>
        ///     Verifies that the user has the specified permissions.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">The key to be deleted.</param>
        /// <param name="accessPermissions"></param>
        /// <returns>True if the operation was successful, False if not.</returns>
        public bool CheckAccess(RegistryHive registryHive, string subKeyPath, AccessPermissions accessPermissions)
        {
            return (bool) ExecuteCommand(registryHive, subKeyPath, RegCommand.CheckAccess, (uint) accessPermissions)
                .GetPropertyValue("bGranted");
        }

        /// <summary>
        ///     Deletes a SubKey in the specified tree.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">The key to be deleted.</param>
        /// <returns>True if the operation was successful, False if not.</returns>
        public bool TryDeleteSubKey(RegistryHive registryHive, string subKeyPath)
        {
            return (uint) ExecuteCommand(registryHive, subKeyPath, RegCommand.DeleteKey)
                .GetPropertyValue("ReturnValue") == 0;
        }

        /// <summary>
        ///     Creates a SubKey in the specified tree.
        ///     This method creates all SubKeys specified in the path that do not exist.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">The key(s) to be created.</param>
        /// <returns>True if the operation was successful, False if not.</returns>
        public bool TryCreateSubKey(RegistryHive registryHive, string subKeyPath)
        {
            return (uint) ExecuteCommand(registryHive, subKeyPath, RegCommand.CreateKey)
                .GetPropertyValue("ReturnValue") == 0;
        }

        /// <summary>
        ///     Enumerates the SubKeys for a path.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A path that contains the SubKeys to be enumerated.</param>
        /// <returns>An IEnumerable of SubKey strings</returns>
        public IEnumerable<string>? EnumerateSubKeys(RegistryHive registryHive, string subKeyPath)
        {
            return (IEnumerable<string>?) ExecuteCommand(registryHive, subKeyPath, RegCommand.EnumKey)
                .GetPropertyValue("sNames");
        }

        public IEnumerable<RegEntity> EnumerateValueNames(RegistryHive registryHive, string subKeyPath)
        {
            var executeResult = ExecuteCommand(registryHive, subKeyPath, RegCommand.EnumValues);

            var valueNames = (string[]) executeResult.GetPropertyValue("sNames");
            var valueTypes = (int[]) executeResult.GetPropertyValue("Types");

            var resultList = new List<RegEntity>(valueNames.Length);

            for (var i = 0; i < valueNames.Length; i++)
            {
                var registryValueType = valueTypes[i] switch
                {
                    1 => RegistryValueType.String,
                    2 => RegistryValueType.ExpandedString,
                    3 => RegistryValueType.Binary,
                    4 => RegistryValueType.DWord,
                    7 => RegistryValueType.MultiString,
                    11 => RegistryValueType.QWord,
                    _ => RegistryValueType.String
                };

                resultList.Add(new RegEntity(valueNames[i], registryValueType));
            }

            return resultList;
        }

        /// <summary>
        ///     Retrieves the value associated with the specified name.
        ///     Returns null if the name/value pair does not exist in the registry.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A path that contains the named values.</param>
        /// <param name="valueName">
        ///     A named value whose data value you are retrieving.
        ///     Specify an empty string to get the default named value.
        /// </param>
        /// <param name="registryValueType"></param>
        /// <returns>The value associated with name, or null if name is not found.</returns>
        public object? GetValue(RegistryHive registryHive, string subKeyPath, string valueName,
            RegistryValueType registryValueType)
        {
            return registryValueType switch
            {
                RegistryValueType.DWord => GetValueCommand(registryHive, subKeyPath, valueName, RegistryValueType.DWord)
                    .GetPropertyValue("uValue"),
                RegistryValueType.QWord => GetValueCommand(registryHive, subKeyPath, valueName, RegistryValueType.QWord)
                    .GetPropertyValue("uValue"),
                RegistryValueType.Binary => GetValueCommand(registryHive, subKeyPath, valueName,
                        RegistryValueType.Binary)
                    .GetPropertyValue("uValue"),
                RegistryValueType.String => GetValueCommand(registryHive, subKeyPath, valueName,
                        RegistryValueType.String)
                    .GetPropertyValue("sValue"),
                RegistryValueType.ExpandedString => GetValueCommand(registryHive, subKeyPath, valueName,
                        RegistryValueType.ExpandedString)
                    .GetPropertyValue("sValue"),
                RegistryValueType.MultiString => GetValueCommand(registryHive, subKeyPath, valueName,
                        RegistryValueType.MultiString)
                    .GetPropertyValue("sValue"),
                _ => throw new ArgumentOutOfRangeException(nameof(registryValueType), registryValueType, null)
            };
        }

        /// <summary>
        ///     Method sets the data value for a named value whose data type is <b>REG_BINARY</b>.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A key that contains the named value to be set.</param>
        /// <param name="valueName">
        ///     A named value whose data value you are setting.
        ///     You can specify an existing named value (update) or a new named value (create).
        ///     Specify an empty string to set the data value for the default named value.
        /// </param>
        /// <param name="value">An IEnumerable of binary data values. The default value is [1,2].</param>
        /// <returns>True if the operation was successful, False if not.</returns>
        public bool TrySetValue(RegistryHive registryHive, string subKeyPath, string valueName, IEnumerable<byte> value)
        {
            return (uint) SetValueCommand(registryHive, subKeyPath, valueName, value, RegistryValueType.Binary)
                .GetPropertyValue("ReturnValue") == 0;
        }

        /// <summary>
        ///     Method sets the data value for a named value whose data type is <b>REG_SZ</b> or <b>REG_EXPAND_SZ</b>.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A key that contains the named value to be set.</param>
        /// <param name="valueName">
        ///     A named value whose data value you are setting.
        ///     You can specify an existing named value (update) or a new named value (create).
        ///     Specify an empty string to set the data value for the default named value.
        /// </param>
        /// <param name="value">A string or string framed with "%" to be set in a named value</param>
        /// <returns>True if the operation was successful, False if not.</returns>
        public bool TrySetValue(RegistryHive registryHive, string subKeyPath, string valueName, string value)
        {
            if (IsExpandedString(value))
                return (uint) SetValueCommand(registryHive, subKeyPath, valueName, value,
                        RegistryValueType.ExpandedString)
                    .GetPropertyValue("ReturnValue") == 0;

            return (uint) SetValueCommand(registryHive, subKeyPath, valueName, value, RegistryValueType.String)
                .GetPropertyValue("ReturnValue") == 0;
        }

        /// <summary>
        ///     Method sets the data value for a named value whose data type is <b>REG_DWORD</b>.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A key that contains the named value to be set.</param>
        /// <param name="valueName">
        ///     A named value whose data value you are setting.
        ///     You can specify an existing named value (update) or a new named value (create).
        ///     Specify an empty string to set the data value for the default named value.
        /// </param>
        /// <param name="value">A DWORD data value.</param>
        /// <returns>True if the operation was successful, False if not.</returns>
        public bool TrySetValue(RegistryHive registryHive, string subKeyPath, string valueName, uint value)
        {
            return (uint) SetValueCommand(registryHive, subKeyPath, valueName, value, RegistryValueType.DWord)
                .GetPropertyValue("ReturnValue") == 0;
        }

        /// <summary>
        ///     Method sets the data value for a named value whose data type is <b>REG_QWORD</b>.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A key that contains the named value to be set.</param>
        /// <param name="valueName">
        ///     A named value whose data value you are setting.
        ///     You can specify an existing named value (update) or a new named value (create).
        ///     Specify an empty string to set the data value for the default named value.
        /// </param>
        /// <param name="value">A QWORD data value for the named value.</param>
        /// <returns>True if the operation was successful, False if not.</returns>
        public bool TrySetValue(RegistryHive registryHive, string subKeyPath, string valueName, ulong value)
        {
            return (uint) SetValueCommand(registryHive, subKeyPath, valueName, value, RegistryValueType.QWord)
                .GetPropertyValue("ReturnValue") == 0;
        }

        /// <summary>
        ///     Method sets the data value for a named value whose data type is <b>REG_MULTI_SZ</b>.
        /// </summary>
        /// <param name="registryHive">A registry tree, also known as a hive, that contains the SubKey path.</param>
        /// <param name="subKeyPath">A key that contains the named value to be set.</param>
        /// <param name="valueName">
        ///     A named value whose data value you are setting.
        ///     You can specify an existing named value (update) or a new named value (create).
        ///     Specify an empty string to set the data value for the default named value.
        /// </param>
        /// <param name="value">An array of string data values.</param>
        /// <returns>True if the operation was successful, False if not.</returns>
        public bool TrySetValue(RegistryHive registryHive, string subKeyPath, string valueName,
            IEnumerable<string> value)
        {
            return (uint) SetValueCommand(registryHive, subKeyPath, valueName, value, RegistryValueType.MultiString)
                .GetPropertyValue("ReturnValue") == 0;
        }

        private ManagementBaseObject ExecuteCommand(RegistryHive registryHive, string subKeyPath, RegCommand regCommand,
            uint required = 0)
        {
            var methodName = regCommand.ToString();
            var methodParams = Registry.GetMethodParameters(methodName);

            methodParams["hDefKey"] = registryHive;
            methodParams["sSubKeyName"] = subKeyPath;
            // This is a workaround for CheckAccess method
            if (required != 0) methodParams["uRequired"] = required;

            return Registry.InvokeMethod(methodName, methodParams, new InvokeMethodOptions()) ??
                   throw new InvalidOperationException();
        }

        private ManagementBaseObject GetValueCommand(RegistryHive registryHive, string subKeyPath,
            string valueName, RegistryValueType registryValueType)
        {
            var methodName = ConvertGetValueType(registryValueType);
            var methodParams = Registry.GetMethodParameters(methodName);

            methodParams["hDefKey"] = registryHive;
            methodParams["sSubKeyName"] = subKeyPath;
            methodParams["sValueName"] = valueName;

            return Registry.InvokeMethod(methodName, methodParams, new InvokeMethodOptions()) ??
                   throw new InvalidOperationException();
        }

        private ManagementBaseObject SetValueCommand(RegistryHive registryHive, string subKeyPath,
            string valueName, object value, RegistryValueType registryValueType)
        {
            var methodName = ConvertSetValueType(registryValueType);
            var methodParams = Registry.GetMethodParameters(methodName);

            methodParams["hDefKey"] = registryHive;
            methodParams["sSubKeyName"] = subKeyPath;
            methodParams["sValueName"] = valueName;
            switch (registryValueType)
            {
                case RegistryValueType.Binary:
                    methodParams["uValue"] = value;
                    break;
                case RegistryValueType.DWord:
                    methodParams["uValue"] = value;
                    break;
                case RegistryValueType.QWord:
                    methodParams["uValue"] = value;
                    break;
                case RegistryValueType.ExpandedString:
                    methodParams["sValue"] = value;
                    break;
                case RegistryValueType.MultiString:
                    methodParams["sValue"] = value;
                    break;
                case RegistryValueType.String:
                    methodParams["sValue"] = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(registryValueType), registryValueType, null);
            }

            return Registry.InvokeMethod(methodName, methodParams, new InvokeMethodOptions()) ??
                   throw new InvalidOperationException();
        }

        private static string ConvertGetValueType(RegistryValueType entry)
        {
            return entry switch
            {
                RegistryValueType.Binary => "GetBinaryValue",
                RegistryValueType.DWord => "GetDWORDValue",
                RegistryValueType.ExpandedString => "GetExpandedStringValue",
                RegistryValueType.MultiString => "GetMultiStringValue",
                RegistryValueType.String => "GetStringValue",
                RegistryValueType.QWord => "GetQWORDValue",
                _ => throw new ArgumentOutOfRangeException(nameof(entry), entry, null)
            };
        }

        private static string ConvertSetValueType(RegistryValueType entry)
        {
            return entry switch
            {
                RegistryValueType.Binary => "SetBinaryValue",
                RegistryValueType.DWord => "SetDWORDValue",
                RegistryValueType.ExpandedString => "SetExpandedStringValue",
                RegistryValueType.MultiString => "SetMultiStringValue",
                RegistryValueType.String => "SetStringValue",
                RegistryValueType.QWord => "SetQWORDValue",
                _ => throw new ArgumentOutOfRangeException(nameof(entry), entry, null)
            };
        }

        private static bool IsExpandedString(string str) => str.StartsWith('%') && str.EndsWith('%');
    }
}