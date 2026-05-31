using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace VanillaPlus.Utilities;

public static class FileHelpers {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        WriteIndented = true,
        IncludeFields = true,
    };

    public static async Task<T> LoadFile<T>(string filePath, T? defaultObject = null) where T : class, new() {
        var fileInfo = new FileInfo(filePath);
        if (fileInfo is { Exists: true }) {
            try {
                var fileText = await Services.ReliableFileStorage.ReadAllTextAsync(fileInfo.FullName);
                var dataObject = JsonSerializer.Deserialize<T>(fileText, SerializerOptions);

                // If deserialize result is null, create a new instance instead and save it.
                if (dataObject is null) {
                    dataObject = defaultObject ?? new T();
                    await SaveFile(dataObject, filePath);
                }

                return dataObject;
            }
            catch (Exception e) {
                // If there is any kind of error loading the file, generate a new one instead and save it.
                Services.PluginLog.InternalError(e, $"Error trying to load file {filePath}, creating a new one instead.");

                await SaveFile(defaultObject ?? new T(), filePath);
            }
        }

        var newFile = defaultObject ?? new T();
        await SaveFile(newFile, filePath);

        return newFile;
    }

    public static async Task SaveFile<T>(T? file, string filePath) {
        try {
            if (file is null) {
                Services.PluginLog.InternalError("Null file provided.");
                return;
            }

            var fileText = JsonSerializer.Serialize(file, file.GetType(), SerializerOptions);
            await Services.ReliableFileStorage.WriteAllTextAsync(filePath, fileText);
        }
        catch (Exception e) {
            Services.PluginLog.InternalError(e, $"Error trying to save file {filePath}");
        }
    }

    public static async Task<byte[]> LoadBinaryFile(int length, string filePath) {
        var fileInfo = new FileInfo(filePath);
        if (fileInfo is { Exists: true }) {
            try {
                var dataObject = await Services.ReliableFileStorage.ReadAllBytesAsync(fileInfo.FullName);

                // If deserialize result is null, create a new instance instead and save it.
                if (dataObject.Length != length) {
                    dataObject = new byte[length];
                    await SaveFile(dataObject, filePath);
                }

                return dataObject;
            }
            catch (Exception e) {
                // If there is any kind of error loading the file, generate a new one instead and save it.
                Services.PluginLog.InternalError(e, $"Error trying to load file {filePath}, creating a new one instead.");

                await SaveFile(new byte[length], filePath);
            }
        }

        var newFile = new byte[length];
        await SaveFile(newFile, filePath);

        return newFile;
    }

    public static async Task SaveBinaryFile(byte[] data, string filePath) {
        try {
            await Services.ReliableFileStorage.WriteAllBytesAsync(filePath, data);
        }
        catch (Exception e) {
            Services.PluginLog.InternalError(e, $"Error trying to save binary data {filePath}");
        }
    }

    public static async Task SaveBinaryFile(nint pointer, int size, string filePath) {
        try {
            var managedArray = new byte[size];
            Marshal.Copy(pointer, managedArray, 0, size);
            await Services.ReliableFileStorage.WriteAllBytesAsync(filePath, managedArray);
        }
        catch (Exception e) {
            Services.PluginLog.InternalError(e, $"Error trying to save binary data {filePath}");
        }
    }

    public static FileInfo GetFileInfo(params string[] path) {
        var directory = Services.PluginInterface.ConfigDirectory;

        for (var index = 0; index < path.Length - 1; index++) {
            directory = new DirectoryInfo(Path.Combine(directory.FullName, path[index]));
            if (!directory.Exists) {
                directory.Create();
            }
        }

        return new FileInfo(Path.Combine(directory.FullName, path[^1]));
    }

    public static string GetCharacterPath() {
        if (!Services.ClientState.IsLoggedIn) {
            throw new Exception("Character is not logged in.");
        }

        return Services.PlayerState.ContentId.ToString("X");
    }
}
