using System.Threading;
using System.Windows;

namespace YTMusicLite
{
    public partial class App : System.Windows.Application
    {
        private const string MutexName = @"Local\YTMusicLite.SingleInstance";
        private const string ActivationEventName = @"Local\YTMusicLite.Activate";

        private Mutex? _singleInstanceMutex;
        private EventWaitHandle? _activationEvent;
        private Thread? _activationListener;
        private bool _ownsMutex;
        private volatile bool _isExiting;

        protected override void OnStartup(StartupEventArgs e)
        {
            _activationEvent = new EventWaitHandle(
                false,
                EventResetMode.AutoReset,
                ActivationEventName);

            _singleInstanceMutex = new Mutex(
                initiallyOwned: true,
                MutexName,
                out bool isFirstInstance);

            if (!isFirstInstance)
            {
                _activationEvent.Set();
                Shutdown();
                return;
            }

            _ownsMutex = true;
            base.OnStartup(e);

            _activationListener = new Thread(ListenForActivation)
            {
                IsBackground = true,
                Name = "YTMusicLite activation listener"
            };
            _activationListener.Start();
        }

        private void ListenForActivation()
        {
            while (!_isExiting)
            {
                _activationEvent?.WaitOne();

                if (!_isExiting)
                {
                    Dispatcher.BeginInvoke(new Action(RestoreExistingWindow));
                }
            }
        }

        private void RestoreExistingWindow()
        {
            if (Current.MainWindow is MainWindow window)
            {
                window.RestoreWindow();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _isExiting = true;
            _activationEvent?.Set();
            _activationListener?.Join(TimeSpan.FromSeconds(1));
            _activationEvent?.Dispose();

            if (_ownsMutex)
            {
                _singleInstanceMutex?.ReleaseMutex();
            }

            _singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
