namespace ISOReader
{
    /// <summary>
    /// Volume Type
    /// </summary>
    public enum VolumeType : int
    {
        BootRecord = 0,
        PrimaryVolumeDescriptor = 1,
        SupplementaryVolumeDescriptor = 2,
        VolumePartitionDescriptor = 3,
        VolumeDescriptorSetTerminator = 255
    }
}
