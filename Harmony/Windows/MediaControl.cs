using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Windows.Foundation;
using Windows.Media.Control;
using GlobalSystemMediaTransportControlsSessionManager = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager;

namespace Harmony.Windows {
    class MediaControl {

        private static GlobalSystemMediaTransportControlsSessionManager _smtcManager;
        private static GlobalSystemMediaTransportControlsSession _smtc;

        private static GlobalSystemMediaTransportControlsSessionMediaProperties _mediaProperties;

        public static void Reload() {
            if (_smtcManager != null) {
                _smtcManager.CurrentSessionChanged -= SmtcManagerOnCurrentSessionChanged;
                _smtcManager.SessionsChanged -= SmtcManagerOnSessionsChanged;
            }

            if (_smtc != null) {
                _smtc.MediaPropertiesChanged -= SmtcOnMediaPropertiesChanged;
                _smtc.TimelinePropertiesChanged -= SmtcOnTimelinePropertiesChanged;
            }

            //TODO: Implement automatic switching to new playing smtc

            _smtcManager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult();
            _smtcManager.SessionsChanged += SmtcManagerOnSessionsChanged;
            _smtcManager.CurrentSessionChanged += SmtcManagerOnCurrentSessionChanged;
            _smtc = _smtcManager.GetCurrentSession();
            if (_smtc != null) {
                _smtc.MediaPropertiesChanged += SmtcOnMediaPropertiesChanged;
                _smtc.TimelinePropertiesChanged += SmtcOnTimelinePropertiesChanged;
                _mediaProperties = _smtc.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
                UpdateMediaProperties();
            }
        }

        //TODO: This does not trigger
        private static void SmtcOnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args) {
            var pos = sender.GetTimelineProperties().Position.TotalSeconds;
            var max = sender.GetTimelineProperties().EndTime.TotalSeconds;

            var frac = pos / max * 100;
            MainWindow.Window.Dispatcher?.InvokeAsync(new Action(() => MainWindow.Window.Media_ProgressBar.Value = frac));
        }

        //TODO: This does not trigger
        private static void SmtcManagerOnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) {
            Console.WriteLine("Changed");
        }

        //TODO: this does not trigger
        private static void SmtcManagerOnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args) {
            if (_smtc != null) {
                _smtc.MediaPropertiesChanged -= SmtcOnMediaPropertiesChanged;
                _smtc.TimelinePropertiesChanged -= SmtcOnTimelinePropertiesChanged;
            }
            _smtc = sender.GetCurrentSession();
            if (_smtc != null) {
                _smtc.MediaPropertiesChanged += SmtcOnMediaPropertiesChanged;
                _mediaProperties = _smtc.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
            }
        }

        private static void SmtcOnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) {
            _mediaProperties = sender.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
            MainWindow.Window.Dispatcher?.InvokeAsync(UpdateMediaProperties);
        }

        public static void UpdateMediaProperties() {
            MainWindow.Window.Media_NowPlaying.Clear();
            if (_mediaProperties == null) return;
            MainWindow.Window.Media_NowPlaying.Text =
                "Now Playing:\n" + _mediaProperties.Artist + ": " + _mediaProperties.Title + "\n";

            if (_mediaProperties.Thumbnail != null) {
                using var stream = _mediaProperties.Thumbnail.OpenReadAsync().GetAwaiter().GetResult().AsStreamForRead();
                var iS = new BitmapImage();
                iS.BeginInit();
                iS.StreamSource = stream;
                iS.EndInit();

                MainWindow.Window.Media_Thumbnail.Image = iS;
            }
        }

        public static IAsyncOperation<bool> SkipForward() => _smtc.TrySkipNextAsync();

        public static IAsyncOperation<bool> SkipPrevious() => _smtc?.TrySkipPreviousAsync();

        public static IAsyncOperation<bool> PlayPause() => _smtc?.TryTogglePlayPauseAsync();

        public static IAsyncOperation<bool> Stop() => _smtc.TryStopAsync();
        public static void VolumeDown() {
            Keyboard.SendInput(new HarmonyPacket.KeyboardPacket() {
                Key = Keys.VolumeDown,
                wParam = 256
            });
            Keyboard.SendInput(new HarmonyPacket.KeyboardPacket() {
                Key = Keys.VolumeDown,
                wParam = 257
            });
        }

        public static void VolumeUp() {
            Keyboard.SendInput(new HarmonyPacket.KeyboardPacket() {
                Key = Keys.VolumeUp,
                wParam = 256
            });
            Keyboard.SendInput(new HarmonyPacket.KeyboardPacket() {
                Key = Keys.VolumeUp,
                wParam = 257
            });
        }
    }
}
