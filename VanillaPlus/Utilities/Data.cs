using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace VanillaPlus.Utilities;

public static class Data {
    public static string DataPath => FileHelpers.GetFileInfo("Data").FullName;
    public static string CharacterDataPath => FileHelpers.GetFileInfo("Data", FileHelpers.GetCharacterPath()).FullName;

    /// <summary>
    /// Loads a data file from PluginConfigs\VanillaPlus\Data\{FileName}
    /// </summary>
    public static async Task<T> LoadData<T>(string fileName) where T : class, new()
        => await FileHelpers.LoadFile<T>(FileHelpers.GetFileInfo("Data", fileName).FullName);

    /// <summary>
    /// Loads a data file from PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName}
    /// </summary>
    public static async Task<T> LoadData<T>(string folderName, string fileName) where T : class, new()
        => await FileHelpers.LoadFile<T>(FileHelpers.GetFileInfo("Data", folderName, fileName).FullName);

    /// <summary>
    /// Loads a character specific data file from PluginConfigs\VanillaPlus\Data\{ContentId}\{FileName}
    /// Creates a `new T` if the file can't be loaded
    /// </summary>
    /// <remarks>Requires the character to be logged in</remarks>
    public static async Task<T> LoadCharacterData<T>(string fileName) where T : class, new()
        => await FileHelpers.LoadFile<T>(FileHelpers.GetFileInfo("Data", FileHelpers.GetCharacterPath(), fileName).FullName);

    /// <summary>
    /// Saves a data file to PluginConfigs\VanillaPlus\Data\{FileName}
    /// </summary>
    public static async Task SaveData<T>(T data, string fileName)
        => await FileHelpers.SaveFile(data, FileHelpers.GetFileInfo("Data", fileName).FullName);

    /// <summary>
    /// Saves a data file to PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName}
    /// </summary>
    public static async Task SaveData<T>(T data, string folderName, string fileName)
        => await FileHelpers.SaveFile(data, FileHelpers.GetFileInfo("Data", folderName, fileName).FullName);

    /// <summary>
    /// Saves a character specific data file to PluginConfigs\VanillaPlus\Data\{ContentId}\{FileName}
    /// </summary>
    /// <remarks>Requires the character to be logged in</remarks>
    public static async Task SaveCharacterData<T>(T data, string fileName)
        => await FileHelpers.SaveFile(data, FileHelpers.GetFileInfo("Data", FileHelpers.GetCharacterPath(), fileName).FullName);

    /// <summary>
    /// Loads a binary file from PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName}
    /// </summary>
    public static async Task<byte[]> LoadBinaryData(int length, string folderName, string fileName)
        => await FileHelpers.LoadBinaryFile(length, FileHelpers.GetFileInfo("Data", folderName, fileName).FullName);

    /// <summary>
    /// Loads a binary file from PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName} directly into game memory.
    /// </summary>
    public static async Task LoadBinaryData(nint targetMemoryAddress, int memorySize, string folderName, string fileName) {
        var result = await LoadBinaryData(memorySize, folderName, fileName);
        Marshal.Copy(result, 0, targetMemoryAddress, memorySize);
    }

    /// <summary>
    /// Saves a binary file to PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName}
    /// </summary>
    public static async Task SaveBinaryData(byte[] data, string folderName, string fileName)
        => await FileHelpers.SaveBinaryFile(data, FileHelpers.GetFileInfo("Data", folderName, fileName).FullName);

    /// <summary>
    /// Saves a memory block to PluginConfigs\VanillaPlus\Data\{FolderName}\{FileName}
    /// </summary>
    public static async Task SaveBinaryData(nint dataPointer, int dataSize, string folderName, string fileName)
        => await FileHelpers.SaveBinaryFile(dataPointer, dataSize, FileHelpers.GetFileInfo("Data", folderName, fileName).FullName);
}
