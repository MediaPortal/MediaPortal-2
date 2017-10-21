export interface ISystemInformation {
    Drives: IDiskInformation;
    CpuUsage: number;
    Ram: IRamInformation;
}

export interface IRamInformation {
    /// <summary>
    /// Free Ram in bytes
    /// </summary>
    Free: number;

    /// <summary>
    /// Total available Ram in bytes
    /// </summary>
    Total: number;

    /// <summary>
    /// Ram usage in %
    /// </summary>
    Used: number;
}

export interface IDiskInformation {
    /// <summary>
    /// Volume Label, e.g. "Media 01"
    /// </summary>
    Name: string;

    /// <summary>
    /// Drive Letter, e.g. "C:\"
    /// </summary>
    Letter: string;

    /// <summary>
    /// File System, e.g. "NTFS"
    /// </summary>
    FileSystem: string;

    /// <summary>
    /// Free Diskspace in bytes
    /// </summary>
    TotalFreeSpace: number;

    /// <summary>
    /// Total disk size in bytes
    /// </summary>
    TotalSize: number;
}