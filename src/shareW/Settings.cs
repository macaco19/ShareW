// SPDX-License-Identifier: GPL-3.0-or-later
using Newtonsoft.Json;
using ShareX.ImageEditor.Integration;
using ShareX.ScreenCaptureLib;

namespace ShareW;

internal sealed class Settings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "shareW", "settings.json");

    public RegionCaptureOptions Capture { get; set; } = new() { QuickCapture = false };
    public ImageEditorOptions Editor { get; set; } = new();
    public bool CopyConfirmedImageToClipboard { get; set; } = true;
    public bool KeepRunningInTray { get; set; } = true;

    public static Settings Load(out string? warning)
    {
        warning = null;
        try
        {
            if (!File.Exists(FilePath)) return new();
            var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(FilePath)) ?? new();
            settings.Capture ??= new() { QuickCapture = false };
            settings.Editor ??= new();
            settings.Editor.AutoCopyImageToClipboard = false;
            settings.Editor.RecentImageFiles.Clear();
            return settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            warning = "No se pudieron leer los ajustes; se usarán los valores iniciales.";
            return new();
        }
    }

    public void Save()
    {
        Editor.AutoCopyImageToClipboard = false;
        Editor.RecentImageFiles.Clear();
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        string temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonConvert.SerializeObject(this, Formatting.Indented));
            File.Move(temporary, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
