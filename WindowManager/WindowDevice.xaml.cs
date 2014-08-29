using System;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Input;

using System.Runtime.InteropServices;
using Emgu.CV.Structure;
using Emgu.CV;

namespace net.encausse.sarah.window {
  public partial class WindowDevice : System.Windows.Window, IDisposable {

    private void WindowLoaded(object sender, System.Windows.RoutedEventArgs e) {
      AddOnManager.GetInstance().HandleSidebar(Name, this.Sidebar);
    }

    // ------------------------------------------
    //  CONSTRUCTOR
    // ------------------------------------------

    private byte[] Pixels;
    private Timestamp stamp;
    private int width, height;

    public WindowDevice(string name, byte[] data, Timestamp stamp, int width, int height) {
      this.Name = name;
      this.Pixels = data;
      this.stamp = stamp;
      this.width = width;
      this.height = height;
      InitializeComponent();
    }

    public void Dispose() {
      if (Source != null) {
        Source.Cancel();
        Source = null;
      }
      this.Dispatcher.InvokeShutdown();
    }

    // ==========================================
    //  WINDOW
    // ==========================================

    private CancellationTokenSource Source;
    private bool visible = false;

    new public void Hide() {
      base.Hide();
      if (!visible) { return; }
      // Sensor.GUI = false;
      visible = false;
      if (null != Source) {
        Source.Cancel();
        Source = null;
      }
    }

    new public void Show() {
      base.Show();
      if (visible) { return; }
      // Sensor.GUI = true;

      visible = true;
      Source = new CancellationTokenSource();

      var dueTime = TimeSpan.FromMilliseconds(200);
      var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("window.threshold", 100));
      RepaintAsync(dueTime, interval, Source.Token);
    }

    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log(Name + " Window", msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug(Name + " Window", msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error(Name + " Window", msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error(Name + " Window", ex);
    }

    // -------------------------------------------
    //  REPAINT 
    // -------------------------------------------

    private WriteableBitmap bitmap;
    private PixelFormat format = PixelFormats.Bgr32;
    private Int32Rect rect;
    private int w, h, stride;
    private Image<Bgra, Byte> scale;
    private byte[] drawer;

    private void InitRepaint() {
      if (bitmap != null) return;
      if (width == 0 || height == 0) return;

      drawer = new byte[Pixels.Length];
      scale = new Image<Bgra, Byte>(width, height);

      // Image Width
      w = (int) this.Image.Width;
      h = (int) this.Image.Height;

      // Preserve ratio
      var ratio = width / w > height / h ? width / w : height / h;
      w = width / ratio;
      h = height / ratio;
      stride = w * format.BitsPerPixel / 8;
      rect = new Int32Rect(0, 0, w, h);

      // Adjuste window height
      this.Height = this.Height - (Image.Height - h);
      this.Wrapper.Height = h;
      this.ImageBorder.Height = h;
      this.Image.Height = h;

      // Set Bitmap source
      bitmap = new WriteableBitmap(w, h, 96, 96, format, null);
      this.Image.Source = bitmap;
    }

    public StopwatchAvg RepaintWatch = new StopwatchAvg();
    private async Task RepaintAsync(TimeSpan dueTime, TimeSpan interval, CancellationToken token) {

      // Initial wait time before we begin the periodic loop.
      if (dueTime > TimeSpan.Zero)
        await Task.Delay(dueTime, token);

      // Repeat this loop until cancelled.
      while (!token.IsCancellationRequested) {

        // Timestamp data
        RepaintWatch.Again();

        // Do Job
        try {
          // Init once
          InitRepaint();

          // Copy frame
          Buffer.BlockCopy(Pixels, 0, drawer, 0, Pixels.Length);

          // Ask repaint to addons
          AddOnManager.GetInstance().RepaintColorFrame(Name, drawer, width, height);

          // Resize
          scale.Bytes = drawer;
          var resize = scale.Resize(w, h, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
          bitmap.WritePixels(rect, resize.Bytes, stride, 0);

          // Ticks
          this.Repaint.Text = "Repaint: " + RepaintWatch.Average();
        } catch (Exception ex) { Error(ex); }
        RepaintWatch.Stop();

        // Wait to repeat again.
        try {
          if (interval > TimeSpan.Zero)
            await Task.Delay(interval, token);
        } catch (ThreadInterruptedException){ break; }
      }
    }

    // -------------------------------------------
    //  SELECT 
    // -------------------------------------------

    bool mouseDown = false; // Set to 'true' when mouse is held down.
    Point mouseDownPos; // The point where the mouse button was clicked down.

    private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
      // Capture and track the mouse.
      mouseDown = true;
      mouseDownPos = e.GetPosition(Wrapper);
      Wrapper.CaptureMouse();

      // Initial placement of the drag selection box.         
      Canvas.SetLeft(selectionBox, mouseDownPos.X);
      Canvas.SetTop(selectionBox, mouseDownPos.Y);
      selectionBox.Width = 0;
      selectionBox.Height = 0;

      // Make the drag selection box visible.
      selectionBox.Visibility = Visibility.Visible;
    }

    private void Grid_MouseUp(object sender, MouseButtonEventArgs e) {
      // Release the mouse capture and stop tracking it.
      mouseDown = false;
      Wrapper.ReleaseMouseCapture();

      // Hide the drag selection box.
      selectionBox.Visibility = Visibility.Collapsed;

      var rect = new System.Drawing.Rectangle(
                   (int)Canvas.GetLeft(selectionBox), (int)Canvas.GetTop(selectionBox),
                   (int)selectionBox.Width, (int)selectionBox.Height);

      AddOnManager.GetInstance().HandleSelection(Name, rect);
    }

    private void Grid_MouseMove(object sender, MouseEventArgs e) {
      if (mouseDown) {
        // When the mouse is held down, reposition the drag selection box.

        Point mousePos = e.GetPosition(Wrapper);

        if (mouseDownPos.X < mousePos.X) {
          Canvas.SetLeft(selectionBox, mouseDownPos.X);
          selectionBox.Width = mousePos.X - mouseDownPos.X;
        } else {
          Canvas.SetLeft(selectionBox, mousePos.X);
          selectionBox.Width = mouseDownPos.X - mousePos.X;
        }

        if (mouseDownPos.Y < mousePos.Y) {
          Canvas.SetTop(selectionBox, mouseDownPos.Y);
          selectionBox.Height = mousePos.Y - mouseDownPos.Y;
        } else {
          Canvas.SetTop(selectionBox, mousePos.Y);
          selectionBox.Height = mouseDownPos.Y - mousePos.Y;
        }
      }
    }


  }
}