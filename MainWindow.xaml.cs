using Microsoft.Web.WebView2.Core;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace YTMusicLite
{
    public partial class MainWindow : Window
    {
        private const string YouTubeMusicUrl = "https://music.youtube.com/";
        private static readonly string UserDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YTMusicLite",
            "WebView2");

        private const int WmHotKey = 0x0312;
        private const int HotKeyPlayPause = 1;
        private const int HotKeyPrevious = 2;
        private const int HotKeyNext = 3;
        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint ModNoRepeat = 0x4000;
        private const uint VkSpace = 0x20;
        private const uint VkLeft = 0x25;
        private const uint VkRight = 0x27;

        private const string TogglePlaybackScript = """
            (() => {
                const player = document.querySelector('ytmusic-player-bar');
                const button = player?.querySelector(
                    '#play-pause-button, .play-pause-button, [aria-label*="Reproducir"], [aria-label*="Pausar"], [aria-label*="Play"], [aria-label*="Pause"]');

                if (button) {
                    button.click();
                    return true;
                }

                const media = document.querySelector('video, audio');
                if (!media) return false;
                media.paused ? media.play() : media.pause();
                return true;
            })();
            """;

        private const string PreviousTrackScript = """
            (() => {
                const player = document.querySelector('ytmusic-player-bar');
                const button = player?.querySelector(
                    '#previous-button, .previous-button, [aria-label*="Anterior"], [aria-label*="Previous"]');
                if (!button) return false;
                button.click();
                return true;
            })();
            """;

        private const string NextTrackScript = """
            (() => {
                const player = document.querySelector('ytmusic-player-bar');
                const button = player?.querySelector(
                    '#next-button, .next-button, [aria-label*="Siguiente"], [aria-label*="Next"]');
                if (!button) return false;
                button.click();
                return true;
            })();
            """;

        private const string PausePlaybackScript = """
            (() => {
                const player = document.querySelector('ytmusic-player-bar');
                const pauseButton = player?.querySelector(
                    '[aria-label*="Pausar"], [aria-label*="Pause"]');

                if (pauseButton) {
                    pauseButton.click();
                    return true;
                }

                const playingMedia = Array.from(document.querySelectorAll('video, audio'))
                    .find(media => !media.paused);
                if (!playingMedia) return false;
                playingMedia.pause();
                return true;
            })();
            """;

        private readonly DispatcherTimer _sleepTimer = new();
        private readonly Dictionary<int, Forms.ToolStripMenuItem> _sleepMenuItems = new();
        private readonly List<int> _registeredHotKeyIds = new();
        private Forms.NotifyIcon? _trayIcon;
        private HwndSource? _windowSource;
        private IntPtr _windowHandle;
        private bool _exitRequested;
        private bool _trayHintShown;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Activated += MainWindow_Activated;
            Deactivated += MainWindow_Deactivated;
            SourceInitialized += MainWindow_SourceInitialized;
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;
            Closed += MainWindow_Closed;

            _sleepTimer.Tick += SleepTimer_Tick;
            InitializeTrayIcon();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;

            try
            {
                Directory.CreateDirectory(UserDataFolder);

                CoreWebView2Environment environment =
                    await CoreWebView2Environment.CreateAsync(userDataFolder: UserDataFolder);

                await MusicWebView.EnsureCoreWebView2Async(environment);

                MusicWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                SetMemoryUsageTarget(IsActive
                    ? CoreWebView2MemoryUsageTargetLevel.Normal
                    : CoreWebView2MemoryUsageTargetLevel.Low);
                MusicWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                MusicWebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
                MusicWebView.CoreWebView2.Navigate(YouTubeMusicUrl);
            }
            catch (Exception exception)
            {
                StatusText.Text = $"No se pudo iniciar YouTube Music.\n{exception.Message}";
            }
        }

        private void InitializeTrayIcon()
        {
            Forms.ContextMenuStrip menu = new();

            menu.Items.Add("Abrir YTMusicLite", null, (_, _) => RestoreWindow());
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Reproducir / Pausar", null, (_, _) => QueuePlaybackCommand(PlaybackCommand.Toggle));
            menu.Items.Add("Canción anterior", null, (_, _) => QueuePlaybackCommand(PlaybackCommand.Previous));
            menu.Items.Add("Canción siguiente", null, (_, _) => QueuePlaybackCommand(PlaybackCommand.Next));
            menu.Items.Add(new Forms.ToolStripSeparator());

            Forms.ToolStripMenuItem sleepMenu = new("Temporizador");
            AddSleepTimerOption(sleepMenu, "Desactivado", 0, isChecked: true);
            AddSleepTimerOption(sleepMenu, "15 minutos", 15);
            AddSleepTimerOption(sleepMenu, "30 minutos", 30);
            AddSleepTimerOption(sleepMenu, "60 minutos", 60);
            menu.Items.Add(sleepMenu);

            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Salir", null, (_, _) => ExitApplication());

            _trayIcon = new Forms.NotifyIcon
            {
                ContextMenuStrip = menu,
                Icon = GetApplicationIcon(),
                Text = "YTMusicLite",
                Visible = true
            };
            _trayIcon.DoubleClick += (_, _) => RestoreWindow();
        }

        private static Drawing.Icon GetApplicationIcon()
        {
            string? processPath = Environment.ProcessPath;
            return processPath is null
                ? Drawing.SystemIcons.Application
                : Drawing.Icon.ExtractAssociatedIcon(processPath)
                    ?? Drawing.SystemIcons.Application;
        }

        private void AddSleepTimerOption(
            Forms.ToolStripMenuItem parent,
            string label,
            int minutes,
            bool isChecked = false)
        {
            Forms.ToolStripMenuItem item = new(label)
            {
                Checked = isChecked
            };
            item.Click += (_, _) => Dispatcher.Invoke(() => SetSleepTimer(minutes));
            parent.DropDownItems.Add(item);
            _sleepMenuItems[minutes] = item;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            _windowHandle = new WindowInteropHelper(this).Handle;
            _windowSource = HwndSource.FromHwnd(_windowHandle);
            _windowSource?.AddHook(WindowMessageHook);

            RegisterGlobalHotKey(HotKeyPlayPause, VkSpace);
            RegisterGlobalHotKey(HotKeyPrevious, VkLeft);
            RegisterGlobalHotKey(HotKeyNext, VkRight);
        }

        private void RegisterGlobalHotKey(int id, uint virtualKey)
        {
            uint modifiers = ModControl | ModAlt | ModNoRepeat;
            if (RegisterHotKey(_windowHandle, id, modifiers, virtualKey))
            {
                _registeredHotKeyIds.Add(id);
            }
        }

        private IntPtr WindowMessageHook(
            IntPtr hwnd,
            int message,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (message != WmHotKey)
            {
                return IntPtr.Zero;
            }

            PlaybackCommand? command = wParam.ToInt32() switch
            {
                HotKeyPlayPause => PlaybackCommand.Toggle,
                HotKeyPrevious => PlaybackCommand.Previous,
                HotKeyNext => PlaybackCommand.Next,
                _ => null
            };

            if (command.HasValue)
            {
                QueuePlaybackCommand(command.Value);
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void QueuePlaybackCommand(PlaybackCommand command)
        {
            _ = Dispatcher.InvokeAsync(async () => await ExecutePlaybackCommandAsync(command));
        }

        private async Task ExecutePlaybackCommandAsync(PlaybackCommand command)
        {
            if (MusicWebView.CoreWebView2 is null)
            {
                return;
            }

            string script = command switch
            {
                PlaybackCommand.Toggle => TogglePlaybackScript,
                PlaybackCommand.Previous => PreviousTrackScript,
                PlaybackCommand.Next => NextTrackScript,
                _ => throw new ArgumentOutOfRangeException(nameof(command))
            };

            try
            {
                await MusicWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (InvalidOperationException)
            {
                // La página puede estar navegando; el siguiente atajo volverá a funcionar.
            }
        }

        private void SetSleepTimer(int minutes)
        {
            _sleepTimer.Stop();

            foreach ((int value, Forms.ToolStripMenuItem item) in _sleepMenuItems)
            {
                item.Checked = value == minutes;
            }

            if (minutes == 0)
            {
                ShowTrayNotification("Temporizador desactivado.");
                return;
            }

            _sleepTimer.Interval = TimeSpan.FromMinutes(minutes);
            _sleepTimer.Start();
            ShowTrayNotification($"La reproducción se pausará en {minutes} minutos.");
        }

        private async void SleepTimer_Tick(object? sender, EventArgs e)
        {
            _sleepTimer.Stop();
            _sleepMenuItems[0].Checked = true;

            foreach ((int minutes, Forms.ToolStripMenuItem item) in _sleepMenuItems)
            {
                if (minutes != 0)
                {
                    item.Checked = false;
                }
            }

            if (MusicWebView.CoreWebView2 is not null)
            {
                try
                {
                    await MusicWebView.CoreWebView2.ExecuteScriptAsync(PausePlaybackScript);
                }
                catch (InvalidOperationException)
                {
                    // WebView2 se estaba cerrando; no queda reproducción que pausar.
                }
            }

            ShowTrayNotification("Temporizador finalizado. La reproducción se pausó.");
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                HideToTray();
            }
        }

        private void MainWindow_Activated(object? sender, EventArgs e)
        {
            SetMemoryUsageTarget(CoreWebView2MemoryUsageTargetLevel.Normal);
        }

        private void MainWindow_Deactivated(object? sender, EventArgs e)
        {
            SetMemoryUsageTarget(CoreWebView2MemoryUsageTargetLevel.Low);
        }

        private void SetMemoryUsageTarget(CoreWebView2MemoryUsageTargetLevel level)
        {
            if (MusicWebView.CoreWebView2 is not null)
            {
                MusicWebView.CoreWebView2.MemoryUsageTargetLevel = level;
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_exitRequested)
            {
                return;
            }

            e.Cancel = true;
            HideToTray();
        }

        private void HideToTray()
        {
            Hide();
            ShowInTaskbar = false;
            SetMemoryUsageTarget(CoreWebView2MemoryUsageTargetLevel.Low);

            if (!_trayHintShown)
            {
                ShowTrayNotification("YTMusicLite sigue reproduciendo en la bandeja.");
                _trayHintShown = true;
            }
        }

        internal void RestoreWindow()
        {
            Dispatcher.Invoke(() =>
            {
                ShowInTaskbar = true;
                Show();
                WindowState = WindowState.Normal;
                Activate();
                SetForegroundWindow(_windowHandle);
                SetMemoryUsageTarget(CoreWebView2MemoryUsageTargetLevel.Normal);
            });
        }

        private void ExitApplication()
        {
            Dispatcher.Invoke(() =>
            {
                _exitRequested = true;
                if (_trayIcon is not null)
                {
                    _trayIcon.Visible = false;
                }

                Close();
            });
        }

        private void ShowTrayNotification(string message)
        {
            _trayIcon?.ShowBalloonTip(
                3000,
                "YTMusicLite",
                message,
                Forms.ToolTipIcon.Info);
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _sleepTimer.Stop();

            foreach (int id in _registeredHotKeyIds)
            {
                UnregisterHotKey(_windowHandle, id);
            }

            _windowSource?.RemoveHook(WindowMessageHook);
            _trayIcon?.Dispose();
            MusicWebView.Dispose();
        }

        private void CoreWebView2_NavigationCompleted(
            object? sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                StatusOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            StatusText.Text = $"No se pudo cargar YouTube Music.\nError: {e.WebErrorStatus}";
        }

        private void CoreWebView2_DocumentTitleChanged(object? sender, object e)
        {
            string documentTitle = MusicWebView.CoreWebView2.DocumentTitle;
            Title = string.IsNullOrWhiteSpace(documentTitle)
                ? "YTMusicLite"
                : $"{documentTitle} - YTMusicLite";
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(
            IntPtr windowHandle,
            int id,
            uint modifiers,
            uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr windowHandle);

        private enum PlaybackCommand
        {
            Toggle,
            Previous,
            Next
        }
    }
}
