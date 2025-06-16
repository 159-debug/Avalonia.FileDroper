using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GTKSystem.Windows.Forms.GTKControls.ControlBase;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Avalonia.FileDroper.Views
{
    public class GtkView : UserControl
    {
        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_window_set_transient_for(IntPtr window, IntPtr parent);

        private FormGtk? _floatingContent = null;

        IDisposable? isVisibleChangedHandler = null;
        IDisposable? isBoundsChangedHandler = null;

        DispatcherTimer _cursorHideTimer;

        /// <inheritdoc />
        public GtkView()
        {
            Loaded += (_, _) =>
            {
                UpdateOverlayPosition();
                ((Window)this.VisualRoot).Closed += (_, _) =>
                {
                    if (_floatingContent != null)
                        ((Gtk.Widget)_floatingContent.GtkControl).Window?.Destroy();
                };
                ((Window)this.VisualRoot).PropertyChanged += (_, e) =>
                {
                    if (e.Property == Window.WindowStateProperty)
                    {
                        ShowNativeOverlay(((Window)this.VisualRoot).WindowState != WindowState.Minimized);
                    }
                };
            };

            isVisibleChangedHandler = IsVisibleProperty.Changed.AddClassHandler<GtkView>((s, _) => s.ShowNativeOverlay(s.IsVisible));
            isBoundsChangedHandler = BoundsProperty.Changed.AddClassHandler<GtkView>((_, _) => UpdateOverlayPosition());

            _cursorHideTimer = new DispatcherTimer
            {
                //避免设置频率太快了导致 视频设置异常
                Interval = TimeSpan.FromSeconds(1)
            };
            _cursorHideTimer.Tick += CursorHideTimer_Tick;
        }

        private async void CursorHideTimer_Tick(object sender, EventArgs e)
        {
            Dispatcher.UIThread.Invoke(new Action(() =>
            {
                if (_floatingContent == null || !_floatingContent.Visible || IsShowIng)
                {
                    return;
                }
                int width = (int)Bounds.Width;
                int height = (int)Bounds.Height;
                var topLeft = new Point();
                var newPosition = this.PointToScreen(topLeft);
                _floatingContent.BeginInvoke(new Action(() =>
                {
                    if (_floatingContent.Opacity != 0)
                    {
                        _floatingContent.Width = width;
                        _floatingContent.Height = height;
                    }
                    var location = new PixelPoint(_floatingContent.Location.X, _floatingContent.Location.Y);
                    if (newPosition != location)
                    {
                        try
                        {
                            ((Gtk.Widget)_floatingContent.GtkControl).Window.Move(newPosition.X, newPosition.Y);
                        }
                        catch (Exception ex) { }
                    }
                }));
            }));
        }

        private void UpdateOverlayPosition()
        {
            if (!_cursorHideTimer.IsEnabled)
                _cursorHideTimer.Start();
            else
                _cursorHideTimer.Interval = TimeSpan.FromMilliseconds(500);
        }

        private void InitializeNativeOverlay()
        {
            if (!this.IsAttachedToVisualTree())
                return;
            if (VisualRoot is not Window visualRoot)
            {
                return;
            }
            visualRoot.LayoutUpdated += VisualRoot_UpdateOverlayPosition;
            visualRoot.PositionChanged += VisualRoot_UpdateOverlayPosition;
            ShowNativeOverlay(IsEffectivelyVisible);
        }

        private void VisualRoot_UpdateOverlayPosition(object sender, EventArgs e) => UpdateOverlayPosition();

        bool IsShowIng = false;

        private void ShowNativeOverlay(bool show)
        {
            if (_floatingContent == null && OperatingSystem.IsLinux())
            {
                _floatingContent = new FormGtk();
                _floatingContent.DragDataReceived += OnDragDataReceived;
            }
            if (_floatingContent == null || _floatingContent.Visible == show || VisualRoot is not Window visualRoot || IsShowIng)
                return;
            IsShowIng = true;
            if (show && this.IsAttachedToVisualTree())
            {
                var windowId = ((Window)this.VisualRoot).TryGetPlatformHandle().Handle.ToString("X");
                Task.Run(() =>
                {
                    Dispatcher.UIThread.Invoke(async () =>
                    {
                        while (true)
                        {
                            await Task.Delay(1);
                            if (_floatingContent.Visible)
                            {
                                await Task.Delay(500);
                                ActivateWindow(windowId);
                                IsShowIng = false;
                                break;
                            }
                        }
                    });
                    _floatingContent.ShowDialog();
                });
            }
            else
            {
                ((Gtk.Widget)_floatingContent.GtkControl).Window?.Destroy();
                _floatingContent = null;
                IsShowIng = false;
            }
        }

        public event EventHandler<DragEventArgs> DragDataReceived;

        private void OnDragDataReceived(object? sender, DragEventArgs e)
        {
            DragDataReceived?.Invoke(sender,e);
        }

        private void ActivateWindow(string windowId)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"wmctrl -i -a 0x{windowId}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(processStartInfo))
            {
                process.WaitForExit();
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            var parent = this.GetVisualParent();
            if (parent != null)
                parent.DetachedFromVisualTree += Parent_DetachedFromVisualTree;
            base.OnAttachedToVisualTree(e);
            InitializeNativeOverlay();
        }

        private void Parent_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (VisualRoot is not Window visualRoot)
            {
                return;
            }
            visualRoot.LayoutUpdated -= VisualRoot_UpdateOverlayPosition;
            visualRoot.PositionChanged -= VisualRoot_UpdateOverlayPosition;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            var parent = this.GetVisualParent();
            if (parent != null)
                parent.DetachedFromVisualTree -= Parent_DetachedFromVisualTree;
            base.OnDetachedFromVisualTree(e);
            ShowNativeOverlay(false);
        }

    }
}
