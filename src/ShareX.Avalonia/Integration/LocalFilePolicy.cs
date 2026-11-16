// SPDX-License-Identifier: GPL-3.0-or-later
namespace ShareX.AvaloniaUI.Integration;

/// <summary>shareW only accepts ordinary files on local drives.</summary>
public static class LocalFilePolicy
{
    public static string RequireLocalPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) ||
            path.StartsWith(@"\\", StringComparison.Ordinal) ||
            path.StartsWith("//", StringComparison.Ordinal))
            throw new IOException("shareW requiere un archivo en una unidad local.");

        string fullPath = Path.GetFullPath(path);
        string root = Path.GetPathRoot(fullPath) ?? throw new IOException("Ruta de archivo no válida.");
        if (OperatingSystem.IsWindows() && new DriveInfo(root).DriveType == DriveType.Network)
            throw new IOException("shareW no admite unidades de red.");
        return fullPath;
    }
}
