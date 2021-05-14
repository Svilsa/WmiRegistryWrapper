using System;

namespace WmiRegistryWrapper
{
    /// <summary>
    ///     Enum for determining the root key of the registry.
    /// </summary>
    public enum RegistryHive : uint
    {
        ClassesRoot = 0x80000000,
        CurrentUser = 0x80000001,
        LocalMachine = 0x80000002,
        Users = 0x80000003,
        CurrentConfig = 0x80000005
    }

    /// <summary>
    ///     Specifies the data types to use when storing values in the registry,
    ///     or identifies the data type of a value in the registry.
    /// </summary>
    public enum RegistryValueType
    {
        DWord,
        QWord,
        Binary,
        String,
        ExpandedString,
        MultiString
    }

    [Flags]
    public enum AccessPermissions : uint
    {
        /// <summary>
        ///     Required to query the values of a registry key.
        /// </summary>
        KeyQueryValue = 1,

        /// <summary>
        ///     Required to create, delete, or set a registry value.
        /// </summary>
        KeySetValue = 2,

        /// <summary>
        ///     Required to create a SubKey of a registry key.
        /// </summary>
        KeyCreateSubKey = 4,

        /// <summary>
        ///     Required to enumerate the SubKeys of a registry key.
        /// </summary>
        KeyEnumerateSubKeys = 8,

        /// <summary>
        ///     Required to request change notifications for a registry key or for SubKeys of a registry key.
        /// </summary>
        KeyNotify = 16,

        /// <summary>
        ///     Required to create a registry key.
        /// </summary>
        KeyCreate = 32,

        /// <summary>
        ///     Required to delete a registry key.
        /// </summary>
        Delete = 65536,

        /// <summary>
        ///     Combines the rights to query, enumerate and notify values.
        /// </summary>
        ReadControl = 131072,

        /// <summary>
        ///     Required to modify the DACL in the object's security descriptor.
        /// </summary>
        WriteDac = 262144,

        /// <summary>
        ///     Required to change the owner in the object's security descriptor.
        /// </summary>
        WriteOwner = 524288
    }

    internal enum RegCommand
    {
        CheckAccess,
        EnumKey,
        EnumValues,
        DeleteKey,
        CreateKey
    }
}