using System;
using System.Windows.Forms;
using System.Diagnostics;
using net.encausse.sarah.Properties;

namespace net.encausse.sarah {
  class MenuTray : IDisposable {

    // The NotifyIcon object.
    public NotifyIcon Notify { get; set; }

    // Initializes a new instance of the <see cref="ProcessIcon"/> class.
    public MenuTray() {
      // Instantiate the NotifyIcon object.
      Notify = new NotifyIcon();
    }

    // Displays the icon in the system tray.
    public void Display() {

      // Put the icon in the system tray and allow it react to mouse clicks.			
      Notify.MouseClick += new MouseEventHandler(ni_MouseClick);
      Notify.Icon = Resources.Home;
      Notify.Text = "SARAH";
      Notify.Visible = true;

      // Attach a context menu.
      Notify.ContextMenuStrip = new MenuCtx().Create();
    }

    // Releases unmanaged and - optionally - managed resources
    public void Dispose() {
      // When the application closes, this will remove the icon from the system tray immediately.
      Notify.Dispose();
    }

    // Handles the MouseClick event of the ni control.
    void ni_MouseClick(object sender, MouseEventArgs e) {
      // Handle mouse button clicks.
      if (e.Button == MouseButtons.Left) {
        // Start Windows Explorer.
        // FIXME
        // Process.Start("explorer", WSRConfig.GetInstance().getDirectory());
      }
    }
  }
}
