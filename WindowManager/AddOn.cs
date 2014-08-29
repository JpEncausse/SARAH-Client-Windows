using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;

namespace net.encausse.sarah.window
{
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "Window";
    }

    private struct WindowDescriptor {
      public string Name;
      public byte[] Pixels;
      public Timestamp Stamp;
      public int w, h;
    }

    // ------------------------------------------
    //  LIFE CYCLE
    // ------------------------------------------

    public override void Setup() {
      base.Setup();
    }

    public override void Dispose() {
      base.Dispose();

      foreach (var window in Windows.Values) {
        window.Dispose();
      }
    }

    // -------------------------------------------
    //  CAMERA
    // -------------------------------------------

    private static List<WindowDescriptor> Descriptor = new List<WindowDescriptor>();

    public override void InitColorFrame(string device, byte[] data, Timestamp stamp, int width, int height, int fps) {
      base.InitColorFrame(device, data, stamp, width, height, fps);
      Descriptor.Add(new WindowDescriptor {
        Name = device, Pixels = data, Stamp = stamp, w = width, h = height
      });
    }

    // -------------------------------------------
    //  MENU ITEMS 
    // ------------------------------------------

    public static Dictionary<String, WindowDevice> Windows = new Dictionary<String, WindowDevice>();
    private void BuildWindow(WindowDescriptor d) {
      Thread thread = new Thread(() => {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        WindowDevice window = new WindowDevice(d.Name, d.Pixels, d.Stamp, d.w, d.h);
        window.Closing += (sender2, e2) => { window.Hide(); e2.Cancel = true; };

        Log("Building : " + d.Name);
        lock (Windows) {
          Windows.Add(d.Name, window);
        }

        System.Windows.Threading.Dispatcher.Run();
      });

      thread.Name = d.Name;
      thread.SetApartmentState(ApartmentState.STA);
      thread.IsBackground = true;
      thread.Start();
    }

    public override void HandleMenuItem(ContextMenuStrip menu) {
      base.HandleMenuItem(menu);
      MenuItems(menu);
    }

    public void MenuItems(ContextMenuStrip menu) {
      foreach (var descriptor in Descriptor) {

        // Build Window
        BuildWindow(descriptor);

        // Build MenuItem
        var item = new ToolStripMenuItem();
        item.Text = descriptor.Name;
        item.Click += (sender, e) => { Windows[descriptor.Name].Dispatcher.Invoke(new Action(() => Windows[descriptor.Name].Show())); };
        item.Image = Properties.Resources.Kinect;
        menu.Items.Add(item);
      }
    }

  }
}
