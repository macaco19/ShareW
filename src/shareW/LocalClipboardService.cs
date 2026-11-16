// SPDX-License-Identifier: GPL-3.0-or-later
using ShareX.HelpersLib;
using ShareX.AvaloniaUI.Integration;
using ShareX.ImageEditor.Integration;
using SkiaSharp;

namespace ShareW;

internal sealed class LocalClipboardService : IClipboardService
{
    public void SetImage(SKBitmap bitmap)
    {
        if (!ClipboardHelpers.CopyImage(bitmap))
            throw new InvalidOperationException("No se pudo copiar la imagen. El portapapeles puede estar ocupado.");
    }

    public SKBitmap? GetImage()
    {
        // Native image formats (including PNG and Windows bitmap/DIB) take
        // priority over file references from an Explorer copy operation.
        SKBitmap? image = ClipboardHelpers.GetImage();
        if (image != null) return image;

        foreach (string path in ClipboardHelpers.GetFileDropList() ?? [])
        {
            if (!FileHelpers.IsImageFile(path)) continue;
            string localPath = LocalFilePolicy.RequireLocalPath(path);
            using Stream stream = File.OpenRead(localPath);
            image = SKBitmap.Decode(stream);
            if (image != null) return image;
        }

        return null;
    }
}
