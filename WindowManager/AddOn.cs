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

    private static Dictionary<String, WindowDescriptor> Descriptors = new Dictionary<String, WindowDescriptor>();
    public override void InitFrame(string device, DeviceFrame frame) {
      base.InitFrame(device, frame);
      if (!Descriptors.ContainsKey(device)) {
        Descriptors.Add(device, new WindowDescriptor(device));
      }
      Descriptors[device].Frames.Add(frame);
    }

    // -------------------------------------------
    //  MENU ITEMS 
    // ------------------------------------------

    public static Dictionary<String, WindowDevice> Windows = new Dictionary<String, WindowDevice>();
    private void BuildWindow(WindowDescriptor descriptor) {
      Thread thread = new Thread(() => {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        WindowDevice window = new WindowDevice(descriptor);
        window.Closing += (sender2, e2) => { window.Hide(); e2.Cancel = true; };

        Log("Building : " + descriptor.Name);
        lock (Windows) { Windows.Add(descriptor.Name, window); }
        System.Windows.Threading.Dispatcher.Run();
      });

      thread.Name = descriptor.Name;
      thread.SetApartmentState(ApartmentState.STA);
      thread.IsBackground = true;
      thread.Start();
    }

    public override void HandleMenuItem(ContextMenuStrip menu) {
      base.HandleMenuItem(menu);
      MenuItems(menu);
    }

    public void MenuItems(ContextMenuStrip menu) {
      foreach (var descriptor in Descriptors) {

        // Build Window
        BuildWindow(descriptor.Value);

        // Build MenuItem
        var item = new ToolStripMenuItem();
        item.Text = descriptor.Key;
        item.Click += (sender, e) => { Windows[descriptor.Key].Dispatcher.Invoke(new Action(() => Windows[descriptor.Key].Show())); };
        item.Image = Properties.Resources.Kinect;
        menu.Items.Add(item);
      }
    }

  }
}
