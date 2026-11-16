// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ShareX.AvaloniaUI.Integration;
using ShareX.AvaloniaUI.Theming;
using ShareX.HelpersLib;
using ShareX.ImageEditor.Integration;
using ShareX.ImageEditor.Presentation.ViewModels;
using ShareX.ImageEditor.Presentation.Views;
using ShareX.ScreenCaptureLib;
using ShareX.ScreenCaptureLib.Presentation.RegionCapture;
using SkiaSharp;
using PreviewBitmap = Avalonia.Media.Imaging.Bitmap;

namespace ShareW;

internal sealed class MainWindow : Window
{
    private readonly Settings _settings;
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _hotkeyStatus = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private readonly Image _preview = new() { Stretch = Stretch.Uniform };
    private readonly StackPanel _actions = new() { Spacing = 8 };
    private readonly List<Button> _imageActions = [];
    private readonly List<Window> _pins = [];
    private readonly List<NativeMenuItem> _trayActions = [];
    private WindowsHotkeyHost? _hotkeys;
    private TrayIcon? _trayIcon;
    private IDisposable? _trayRegistration;
    private SKBitmap? _lastImage;
    private PreviewBitmap? _previewBitmap;
    private bool _busy;
    private bool _closing;
    private bool _servicesInitialized;
    private bool _showAfterOperation;
    private bool _exitRequested;

    public MainWindow()
    {
        _settings = Settings.Load(out string? warning);
        RequestedThemeVariant = ThemeManager.GetCurrentTheme();
        this.Bind(BackgroundProperty, this.GetResourceObservable("ShareX.Brush.Background.Main"));
        this.Bind(ForegroundProperty, this.GetResourceObservable("ShareX.Brush.Text"));
        Title = "shareW · Capturas locales";
        Width = 1000;
        Height = 730;
        MinWidth = 780;
        MinHeight = 580;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var title = new TextBlock { Text = "shareW", FontSize = 34, FontWeight = FontWeight.Bold };
        var subtitle = new TextBlock
        {
            Text = "Captura y anota con las herramientas de ShareX 22.4.0.",
            TextWrapping = TextWrapping.Wrap
        };
        AddAction("Capturar región     Ctrl + Impr Pant", () => CaptureAsync(CaptureKind.Region));
        AddAction("Capturar pantallas     Impr Pant", () => CaptureAsync(CaptureKind.Fullscreen));
        AddAction("Capturar ventana en 3 segundos", () => CaptureAsync(CaptureKind.Window, delayed: true));
        AddAction("Editar imagen del portapapeles…", EditClipboardAsync);
        AddAction("Abrir imagen…", OpenImageAsync);
        AddAction("Editar última imagen…", EditLastAsync, needsImage: true);
        AddAction("Copiar imagen", () =>
        {
            new LocalClipboardService().SetImage(_lastImage!);
            _status.Text = "Imagen copiada al portapapeles.";
            return Task.CompletedTask;
        }, needsImage: true);
        AddAction("Guardar PNG…", SaveLastAsync, needsImage: true);

        var options = new StackPanel { Spacing = 4, Margin = new Thickness(0, 8, 0, 0) };
        AddOption(options, "Mostrar lupa", _settings.Capture.ShowMagnifier,
            value => _settings.Capture.ShowMagnifier = value);
        AddOption(options, "Seleccionar varias regiones", _settings.Capture.MultiRegionMode,
            value => _settings.Capture.MultiRegionMode = value);
        AddOption(options, "Capturar al soltar la selección", _settings.Capture.QuickCapture,
            value => _settings.Capture.QuickCapture = value);
        AddOption(options, "Copiar al confirmar captura", _settings.CopyConfirmedImageToClipboard,
            value => _settings.CopyConfirmedImageToClipboard = value);
        AddOption(options, "Seguir activo al cerrar la ventana", _settings.KeepRunningInTray,
            value => _settings.KeepRunningInTray = value);

        var left = new StackPanel { Spacing = 12, Width = 330 };
        left.Children.Add(title);
        left.Children.Add(subtitle);
        left.Children.Add(_actions);
        left.Children.Add(options);
        left.Children.Add(_hotkeyStatus);
        var help = new TextBlock
        {
            Text = "Ctrl + V en esta ventana abre la imagen del portapapeles en el editor.\n\nEn la captura: Tab cambia entre región y anotación. Enter confirma y, con la opción activada, copia la imagen. Esc cancela.\n\nCon la opción de bandeja activada, cerrar la ventana mantiene shareW disponible. Usa su menú para abrirlo o salir por completo.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        };
        left.Children.Add(help);

        var previewPanel = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#181C24")),
            CornerRadius = new CornerRadius(8), Padding = new Thickness(16), Child = _preview
        };
        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("330,*"), ColumnSpacing = 24 };
        body.Children.Add(new ScrollViewer { Content = left });
        Grid.SetColumn(previewPanel, 1);
        body.Children.Add(previewPanel);
        var root = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 14, Margin = new Thickness(24) };
        root.Children.Add(body);
        Grid.SetRow(_status, 1);
        root.Children.Add(_status);
        Content = root;
        _status.Text = warning ?? "Lista para capturar. Enter confirma la captura; Esc cancela.";
        RefreshActions();
        KeyDown += async (_, e) =>
        {
            // This shortcut belongs to the main window only; the separate editor
            // keeps its existing paste behavior for images, shapes and text.
            if (e.Handled || e.Source is TextBox || e.Key != Key.V || e.KeyModifiers != KeyModifiers.Control) return;
            e.Handled = true;
            await RunAsync(EditClipboardAsync);
        };
        Opened += (_, _) =>
        {
            // Showing a hidden window raises Opened again. Keep one hotkey host
            // and one tray registration for the entire process lifetime.
            if (_servicesInitialized) return;
            _servicesInitialized = true;
            RegisterHotkeys();
            InitializeTray();
        };
    }

    private void AddAction(string label, Func<Task> action, bool needsImage = false)
    {
        var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(12, 9) };
        button.Click += async (_, _) => await RunAsync(action);
        _actions.Children.Add(button);
        if (needsImage) _imageActions.Add(button);
    }

    private void AddOption(StackPanel panel, string label, bool initial, Action<bool> set)
    {
        var check = new CheckBox { Content = label, IsChecked = initial };
        check.IsCheckedChanged += (_, _) =>
        {
            set(check.IsChecked == true);
            PersistSettings();
        };
        panel.Children.Add(check);
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (_busy || _closing) return;
        _showAfterOperation = IsVisible;
        _busy = true;
        RefreshActions();
        try { await action(); }
        catch (Exception ex)
        {
            // A failed clipboard write must leave the image accessible and show
            // its error instead of silently returning to the notification area.
            _showAfterOperation = true;
            _status.Text = "No se pudo completar la operación: " + ex.Message;
        }
        finally
        {
            _busy = false;
            if (!_closing)
            {
                if (_showAfterOperation) ShowMainWindow();
                RefreshActions();
                PersistSettings();
            }
        }
    }

    private void RefreshActions()
    {
        _actions.IsEnabled = !_busy;
        foreach (var button in _imageActions) button.IsEnabled = _lastImage != null;
        foreach (var action in _trayActions) action.IsEnabled = !_busy;
    }

    private void PersistSettings()
    {
        try { _settings.Save(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { _status.Text += " No se pudieron guardar los ajustes."; }
    }

    private void ShowMainWindow()
    {
        if (_closing || _busy) return;
        Show();
        if (WindowState == Avalonia.Controls.WindowState.Minimized)
            WindowState = Avalonia.Controls.WindowState.Normal;
        Activate();
    }

    private void InitializeTray()
    {
        TrayIcon? tray = null;
        try
        {
            var menu = new NativeMenu();
            var open = new NativeMenuItem("Abrir shareW");
            open.Click += (_, _) => ShowMainWindow();
            var capture = new NativeMenuItem("Capturar región");
            capture.Click += async (_, _) => await RunAsync(() => CaptureAsync(CaptureKind.Region));
            var clipboard = new NativeMenuItem("Editar imagen del portapapeles");
            clipboard.Click += async (_, _) => await RunAsync(EditClipboardAsync);
            var exit = new NativeMenuItem("Salir de shareW");
            exit.Click += (_, _) =>
            {
                if (_busy) return;
                _exitRequested = true;
                Close();
            };
            menu.Items.Add(open);
            menu.Items.Add(capture);
            menu.Items.Add(clipboard);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(exit);
            tray = new TrayIcon
            {
                Icon = CreateTrayIcon(), ToolTipText = "shareW · Listo para capturar",
                Menu = menu, IsVisible = true
            };
            tray.Clicked += (_, _) => ShowMainWindow();
            _trayRegistration = DesktopServices.RegisterTrayIcon(tray);
            _trayIcon = tray;
            _trayActions.AddRange([open, capture, clipboard, exit]);
            RefreshActions();
        }
        catch (Exception ex)
        {
            if (_trayRegistration != null) _trayRegistration.Dispose();
            else tray?.Dispose();
            _trayRegistration = null;
            _trayIcon = null;
            _trayActions.Clear();
            _status.Text = "Bandeja no disponible; cerrar la ventana saldrá de shareW. " + ex.Message;
        }
    }

    private static WindowIcon CreateTrayIcon()
    {
        using var bitmap = new SKBitmap(32, 32);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(33, 112, 223) };
        canvas.DrawRoundRect(SKRect.Create(1, 1, 30, 30), 7, 7, paint);
        paint.Color = SKColors.White;
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2;
        canvas.DrawRoundRect(SKRect.Create(6, 10, 20, 15), 3, 3, paint);
        canvas.DrawCircle(16, 17.5f, 4.5f, paint);
        paint.Style = SKPaintStyle.Fill;
        canvas.DrawRoundRect(SKRect.Create(10, 7, 7, 4), 1, 1, paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        // Windows icon loaders expect an ICO container. Its single image is a
        // PNG-compressed 32-bit frame, retaining transparency in the tray.
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)1);
        writer.Write((byte)32);
        writer.Write((byte)32);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write((int)png.Size);
        writer.Write(22);
        writer.Write(png.ToArray());
        writer.Flush();
        stream.Position = 0;
        return new WindowIcon(stream);
    }

    private void RegisterHotkeys()
    {
        if (_hotkeys != null) return;
        try
        {
            _hotkeys = new WindowsHotkeyHost();
            _hotkeys.Initialize();
            var region = new HotkeyInfo(InputKey.Control | InputKey.PrintScreen);
            var fullscreen = new HotkeyInfo(InputKey.PrintScreen);
            var window = new HotkeyInfo(InputKey.Alt | InputKey.PrintScreen);
            foreach (var hotkey in new[] { region, fullscreen, window }) _hotkeys.RegisterHotkey(hotkey);
            _hotkeys.HotkeyPress += async (id, _, _) =>
            {
                if (id == region.ID) await RunAsync(() => CaptureAsync(CaptureKind.Region));
                else if (id == fullscreen.ID) await RunAsync(() => CaptureAsync(CaptureKind.Fullscreen));
                else if (id == window.ID) await RunAsync(() => CaptureAsync(CaptureKind.Window));
            };
            _hotkeyStatus.Text = new[] { region, fullscreen, window }.All(x => x.Status == HotkeyStatus.Registered)
                ? "Atajos globales activos. Alt + Impr Pant captura la ventana activa."
                : "Algún atajo está ocupado por Windows u otra aplicación. Puedes usar los botones.";
        }
        catch (Exception ex)
        {
            _hotkeys?.Dispose();
            _hotkeys = null;
            _hotkeyStatus.Text = "Atajos no disponibles: " + ex.Message;
        }
    }

    private enum CaptureKind { Region, Fullscreen, Window }

    private async Task CaptureAsync(CaptureKind kind, bool delayed = false)
    {
        // Preserve the foreground target before hiding the host for a global window hotkey.
        IntPtr foreground = kind == CaptureKind.Window && !delayed ? NativeMethods.GetForegroundWindow() : IntPtr.Zero;
        Hide();
        await Task.Delay(delayed ? 3000 : 220);
        var screenshot = new Screenshot { CaptureCursor = false };
        if (kind == CaptureKind.Region)
        {
            var bounds = CaptureHelpers.GetScreenBounds();
            SKBitmap frozen = screenshot.CaptureFullscreen() ?? throw new InvalidOperationException("La pantalla no está disponible.");
            // The upstream capture window owns frozen, including cancellation and errors.
            var result = await RegionCaptureIntegration.CaptureAsync(new AvaloniaRegionCaptureRequest
            {
                Screenshot = frozen,
                ScreenBounds = bounds,
                RegionCaptureOptions = _settings.Capture,
                ImageEditorOptions = _settings.Editor,
                EnableAnnotations = true,
                AllowMultipleRegions = true
            });
            if (result == null) { _status.Text = "Captura cancelada."; return; }
            CompleteConfirmedImage(result.Image);
        }
        else
        {
            SKBitmap? image = kind == CaptureKind.Fullscreen ? screenshot.CaptureFullscreen()
                : screenshot.CaptureWindow(delayed ? NativeMethods.GetForegroundWindow() : foreground);
            CompleteConfirmedImage(image ?? throw new InvalidOperationException("No se pudo capturar la ventana."));
        }
    }

    private void CompleteConfirmedImage(SKBitmap image)
    {
        SetLastImage(image);
        if (_settings.CopyConfirmedImageToClipboard)
        {
            // Run only after acceptance, never on Escape or while annotations change.
            new LocalClipboardService().SetImage(image);
            _status.Text = "Imagen copiada al portapapeles. Puedes pegarla con Ctrl + V.";
            if (_trayIcon != null) _trayIcon.ToolTipText = "shareW · Imagen copiada al portapapeles";
            _showAfterOperation = _trayRegistration == null || !_settings.KeepRunningInTray;
        }
        else
        {
            _showAfterOperation = true;
        }
    }

    private void SetLastImage(SKBitmap image)
    {
        PreviewBitmap preview;
        try
        {
            using SKImage encodedImage = SKImage.FromBitmap(image);
            using SKData png = encodedImage.Encode(SKEncodedImageFormat.Png, 100);
            using Stream stream = png.AsStream();
            preview = new PreviewBitmap(stream);
        }
        catch { image.Dispose(); throw; }
        _preview.Source = preview;
        _previewBitmap?.Dispose();
        _previewBitmap = preview;
        _lastImage?.Dispose();
        _lastImage = image;
        _status.Text = $"Imagen de {image.Width} × {image.Height} píxeles. Puedes editarla, copiarla o guardarla.";
    }

    private async Task OpenImageAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Abrir imagen local", AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll]
        });
        if (files.Count == 0) return;
        string path = LocalFilePolicy.RequireLocalPath(files[0].TryGetLocalPath());
        using Stream stream = File.OpenRead(path);
        SetLastImage(SKBitmap.Decode(stream) ?? throw new IOException("Formato de imagen no reconocido."));
        await EditLastAsync();
    }

    private async Task EditClipboardAsync()
    {
        SKBitmap? image = new LocalClipboardService().GetImage();
        if (image == null)
            throw new InvalidOperationException("El portapapeles no contiene una imagen legible. Copia una imagen y vuelve a intentarlo.");

        // The imported pixels become the canvas. Nothing is written to disk or
        // back to the clipboard until the user explicitly confirms or copies.
        SetLastImage(image);
        await EditLastAsync();
    }

    private async Task EditLastAsync()
    {
        if (_lastImage == null) return;
        var editor = new EditorWindow(_settings.Editor) { Title = "shareW · Editor" };
        editor.LoadImage(_lastImage.Copy());
        if (editor.DataContext is not MainViewModel vm) throw new InvalidOperationException("Editor no disponible.");
        vm.ShowFileMenu = true;
        vm.ShowTaskButtons = true;
        vm.UseContinueWorkflow = true;
        vm.ShowBottomToolbar = true;
        vm.ShowStartScreen = false;
        vm.HasHostCopyHandler = true;
        vm.CopyRequested += () =>
        {
            using SKBitmap? bitmap = editor.GetResultBitmap();
            if (bitmap != null) new LocalClipboardService().SetImage(bitmap);
            return Task.CompletedTask;
        };
        vm.PinRequested += () =>
        {
            using SKBitmap? bitmap = editor.GetResultBitmap();
            if (bitmap != null) PinImage(bitmap);
        };
        var completion = new TaskCompletionSource<SKBitmap?>(TaskCreationOptions.RunContinuationsAsynchronously);
        editor.Closed += (_, _) =>
        {
            try
            {
                SKBitmap? result = vm.TaskResult switch
                {
                    MainViewModel.EditorTaskResult.Continue => editor.GetResultBitmap(),
                    MainViewModel.EditorTaskResult.ContinueNoSave => editor.GetSourceBitmap(),
                    _ => null
                };
                completion.TrySetResult(result);
            }
            catch (Exception ex) { completion.TrySetException(ex); }
        };
        Hide();
        editor.Show();
        SKBitmap? edited = await completion.Task;
        if (edited != null) CompleteConfirmedImage(edited);
    }

    private async Task SaveLastAsync()
    {
        if (_lastImage == null) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar captura PNG", SuggestedFileName = $"shareW-{DateTime.Now:yyyyMMdd-HHmmss}.png",
            DefaultExtension = "png", FileTypeChoices = [new FilePickerFileType("PNG") { Patterns = ["*.png"] }],
            ShowOverwritePrompt = true
        });
        if (file == null) return;
        string path = LocalFilePolicy.RequireLocalPath(file.TryGetLocalPath());
        using SKImage image = SKImage.FromBitmap(_lastImage);
        using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (Stream stream = File.Create(temporary)) encoded.SaveTo(stream);
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        _status.Text = "Guardado en " + path;
    }

    private void PinImage(SKBitmap image)
    {
        using SKImage skImage = SKImage.FromBitmap(image);
        using SKData data = skImage.Encode(SKEncodedImageFormat.Png, 100);
        using Stream stream = data.AsStream();
        var bitmap = new PreviewBitmap(stream);
        var window = new Window
        {
            Title = "shareW · Imagen fijada", Topmost = true,
            Width = Math.Clamp(image.Width, 150, 900), Height = Math.Clamp(image.Height + 32, 100, 700),
            Content = new Image { Source = bitmap, Stretch = Stretch.Uniform }
        };
        _pins.Add(window);
        window.Closed += (_, _) => { bitmap.Dispose(); _pins.Remove(window); };
        window.Show();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (e.CloseReason is WindowCloseReason.OSShutdown or WindowCloseReason.ApplicationShutdown)
        {
            _exitRequested = true;
            base.OnClosing(e);
            return;
        }
        if (_busy) { e.Cancel = true; return; }
        PersistSettings();
        if (!_exitRequested && _settings.KeepRunningInTray && _trayRegistration != null)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _closing = true;
        _trayRegistration?.Dispose();
        _trayRegistration = null;
        _trayIcon = null;
        _hotkeys?.Dispose();
        foreach (Window pin in _pins.ToArray()) pin.Close();
        _preview.Source = null;
        _previewBitmap?.Dispose();
        _lastImage?.Dispose();
        base.OnClosed(e);
        AvaloniaBootstrapper.Shutdown();
    }
}
