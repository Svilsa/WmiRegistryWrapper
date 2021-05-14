namespace WmiRegistryWrapper
{
    public readonly struct RegEntity
    {
        public string Name { get; }
        public RegistryValueType RegistryValueType { get; } 

        public RegEntity(string name, RegistryValueType registryValueType)
        {
            Name = name;
            RegistryValueType = registryValueType;
        }
    }
}