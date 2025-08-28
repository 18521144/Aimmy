using Aimmy2.Class;
using Aimmy2.Theme;
using Aimmy2.AILogic;
using Class;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;

namespace Visuality
{
    public partial class DetectedPlayerWindow : Window
    {
        // Windows API for forcing window position
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        private bool _isInitialized = false;

        private SolidColorBrush _strokeBrush = new SolidColorBrush(Colors.Red);

        public DetectedPlayerWindow()
        {
            InitializeComponent();

            ThemeManager.ExcludeWindowFromBackground(this);
            Title = "";

            DisplayManager.DisplayChanged += OnDisplayChanged;

            PropertyChanger.ReceiveDPColor = UpdateDPColor;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Make window click-through
            ClickThroughOverlay.MakeClickThrough(new WindowInteropHelper(this).Handle);

            // Now that we have a window handle, position the window
            if (!_isInitialized)
            {
                _isInitialized = true;
                ForceReposition();
            }
        }

        private void OnDisplayChanged(object? sender, DisplayChangedEventArgs e)
        {

            // Update position when display changes
            Application.Current.Dispatcher.Invoke(() =>
            {
                ForceReposition();
            });
        }

        public void ForceReposition()
        {
            try
            {

                // Get window handle
                var hwnd = _isInitialized ? new WindowInteropHelper(this).Handle : IntPtr.Zero;

                // Set window state to normal first
                this.WindowState = WindowState.Normal;

                // Position window to cover the current display (accounting for DPI scaling)
                this.Left = DisplayManager.ScreenLeft / WinAPICaller.scalingFactorX;
                this.Top = DisplayManager.ScreenTop / WinAPICaller.scalingFactorY;
                this.Width = DisplayManager.ScreenWidth / WinAPICaller.scalingFactorX;
                this.Height = DisplayManager.ScreenHeight / WinAPICaller.scalingFactorY;

                // Force position with Windows API if we have a handle
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, IntPtr.Zero,
                        DisplayManager.ScreenLeft,
                        DisplayManager.ScreenTop,
                        DisplayManager.ScreenWidth,
                        DisplayManager.ScreenHeight,
                        SWP_NOZORDER | SWP_NOACTIVATE);
                }

                // Maximize to cover entire display
                this.WindowState = WindowState.Maximized;

                this.UpdateLayout();

            }
            catch (Exception ex)
            {
            }
        }

        private void UpdateDPColor(Color newColor)
        {
            _strokeBrush = new SolidColorBrush(newColor);
            foreach (var rect in DetectionCanvas.Children.OfType<Rectangle>())
            {
                rect.Stroke = _strokeBrush;
            }
        }

        public void DrawDetections(IEnumerable<Prediction> predictions)
        {
            DetectionCanvas.Children.Clear();
            foreach (var p in predictions)
            {
                var rect = p.Rectangle;
                var box = new Rectangle
                {
                    Width = rect.Width / WinAPICaller.scalingFactorX,
                    Height = rect.Height / WinAPICaller.scalingFactorY,
                    Stroke = _strokeBrush,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(box, (rect.X - DisplayManager.ScreenLeft) / WinAPICaller.scalingFactorX);
                Canvas.SetTop(box, (rect.Y - DisplayManager.ScreenTop) / WinAPICaller.scalingFactorY);
                DetectionCanvas.Children.Add(box);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        // Clean up event subscription
        protected override void OnClosed(EventArgs e)
        {
            DisplayManager.DisplayChanged -= OnDisplayChanged;
            base.OnClosed(e);
        }
    }
}