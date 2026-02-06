using System;
using System.Runtime.InteropServices;

namespace VanillaPlus.Utilities;

public static class Data {
    public static string DataPath => FileHelpers.GetFileInfo("Data").FullName;
    public static string CharacterDataPath => FileHelpers.GetFileInfo("Data", FileHelpers.GetCharacterPath()).FullName;
    
    /// <summary>
    /// Loads a data file from PluginConfigs\VanillaPlus\Data\{FileName}
    /// </summary>
    public static T LoadData<T>(string fileName) where T : class, new()
        => FileHelpers.LoadFile<T>(FileHelpers.GetFileInfo("Data", fileName).FullName);
    
    /// <summary>
    /// Loads a data file from PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName}
    /// </summary>
    public static T LoadData<T>(string folderName, string fileName) where T : class, new()
        => FileHelpers.LoadFile<T>(FileHelpers.GetFileInfo("Data", folderName, fileName).FullName);
    
    /// <summary>
    /// Loads a character specific data file from PluginConfigs\VanillaPlus\Data\{ContentId}\{FileName}
    /// Creates a `new T` if the file can't be loaded
    /// </summary>
    /// <remarks>Requires the character to be logged in</remarks>
    public static T LoadCharacterData<T>(string fileName) where T : class, new()
        => FileHelpers.LoadFile<T>(FileHelpers.GetFileInfo("Data", FileHelpers.GetCharacterPath(), fileName).FullName);

    /// <summary>
    /// Saves a data file to PluginConfigs\VanillaPlus\Data\{FileName}
    /// </summary>
    public static void SaveData<T>(T data, string fileName)
        => FileHelpers.SaveFile(data, FileHelpers.GetFileInfo("Data", fileName).FullName);
    
    /// <summary>
    /// Saves a data file to PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName}
    /// </summary>
    public static void SaveData<T>(T data, string folderName, string fileName)
        => FileHelpers.SaveFile(data, FileHelpers.GetFileInfo("Data", folderName, fileName).FullName);
    
    /// <summary>
    /// Saves a character specific data file to PluginConfigs\VanillaPlus\Data\{ContentId}\{FileName}
    /// </summary>
    /// <remarks>Requires the character to be logged in</remarks>
    public static void SaveCharacterData<T>(T data, string fileName)
        => FileHelpers.SaveFile(data, FileHelpers.GetFileInfo("Data", FileHelpers.GetCharacterPath(), fileName).FullName);

    /// <summary>
    /// Loads a binary file from PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName}
    /// </summary>
    public static byte[] LoadBinaryData(int length, string folderName, string fileName)
        => FileHelpers.LoadBinaryFile(length, FileHelpers.GetFileInfo("Data", folderName, fileName).FullName);

    /// <summary>
    /// Loads a binary file from PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName} directly into game memory.
    /// </summary>
    public static unsafe void LoadBinaryData<T>(T* targetMemoryAddress, int memorySize, string folderName, string fileName) where T : unmanaged {
        var result = LoadBinaryData(memorySize, folderName, fileName);
        Marshal.Copy(result, 0, (nint)targetMemoryAddress, memorySize);
    }

    /// <summary>
    /// Saves a binary file to PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName}
    /// </summary>
    public static void SaveBinaryData(byte[] data, string folderName, string fileName)
        => FileHelpers.SaveBinaryFile(data, FileHelpers.GetFileInfo("Data", folderName, fileName).FullName);

    /// <summary>
    /// Saves a memory block to PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName}
    /// </summary>
    public static unsafe void SaveBinaryData<T>(T* dataPointer, int dataSize, string folderName, string fileName) where T : unmanaged
        => FileHelpers.SaveBinaryFile(new Span<byte>(dataPointer, dataSize).ToArray(), FileHelpers.GetFileInfo("Data", folderName, fileName).FullName);
}
