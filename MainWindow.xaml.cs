using System.Diagnostics;
using System.Windows;
using CmlLib.Core.Auth;
using LeipzigCraft.Launcher.Services;

namespace LeipzigCraft.Launcher;

public partial class MainWindow : Window
{
    private readonly MinecraftService _minecraft = new();
    private readonly FabricService _fabric = new();
    private readonly PackService _pack = new();

    private MSession? _session;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();

        AppPaths.Ensure();
        InstancePathText.Text = AppPaths.Game;

        if (_minecraft.IsLeipzigCraftRunning())
            StatusText.Text = "LeipzigCraft läuft bereits.";
        else if (_pack.HasLocalMods())
            StatusText.Text = "LeipzigCraft ist bereit.";
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
            return;

        await RunBusyAsync(async () =>
        {
            SetStatus("Öffne Microsoft-Anmeldung …");

            _session = await _minecraft.LoginAsync();

            AccountText.Text =
                string.IsNullOrWhiteSpace(_session.Username)
                    ? "Microsoft-Konto verbunden"
                    : _session.Username;

            LoginButton.Content = "ANGEMELDET";
            SetStatus("Microsoft-Konto erfolgreich verbunden.");
        });
    }

    private async void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
            return;

        if (_minecraft.IsLeipzigCraftRunning())
        {
            const string message =
                "LeipzigCraft läuft bereits.\n\n" +
                "Bitte schließe zuerst Minecraft, bevor du es erneut startest.";

            SetStatus("LeipzigCraft läuft bereits.");

            MessageBox.Show(
                message,
                "LeipzigCraft Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        await RunBusyAsync(async () =>
        {
            if (_session is null)
            {
                SetStatus("Microsoft-Anmeldung …");
                _session = await _minecraft.LoginAsync();

                AccountText.Text =
                    string.IsNullOrWhiteSpace(_session.Username)
                        ? "Microsoft-Konto verbunden"
                        : _session.Username;

                LoginButton.Content = "ANGEMELDET";
            }

            await _minecraft.EnsureMinecraftAsync(SetStatus);
            await _fabric.EnsureProfileInstalledAsync(SetStatus);

            var packResult = await _pack.SyncAsync(SetStatus);
            SetStatus(packResult);

            if (!_pack.HasLocalMods())
            {
                throw new InvalidOperationException(
                    "Das LeipzigCraft-Modpack wurde nicht installiert.\n\n" +
                    "Prüfe https://leipzigcraft.com/launcher/pack.json, " +
                    "pack.sig und die GitHub-Release-Dateien.");
            }

            SetStatus("Trage LeipzigCraft in die Serverliste ein …");
            ServerListService.EnsureLeipzigCraftFirst();

            SetStatus("Bereite LeipzigCraft vor …");

            var process = await _minecraft.CreateFabricProcessAsync(
                _session,
                SetStatus);

            SetStatus("Starte LeipzigCraft …");

            process.Start();
            _minecraft.RegisterRunningProcess(process);

            SetStatus("LeipzigCraft läuft. Viel Spaß in Grünau!");
        });
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        try
        {
            SetBusy(true);
            await action();
        }
        catch (Exception ex)
        {
            SetStatus("Fehler: " + ex.Message);

            MessageBox.Show(
                ex.Message,
                "LeipzigCraft Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;

        BusyProgress.Visibility =
            busy ? Visibility.Visible : Visibility.Collapsed;

        LoginButton.IsEnabled = !busy;
        PlayButton.IsEnabled = !busy;
    }

    private void SetStatus(string text)
    {
        Dispatcher.Invoke(() => StatusText.Text = text);
    }
}
