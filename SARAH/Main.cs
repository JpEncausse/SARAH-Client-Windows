using System;

using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;

namespace net.encausse.sarah {

  class Launcher {

    static Thread thread = null;
    static void Main(string[] args) {

      // Create SARAH
      var sarah = SARAH.GetInstance();
      try {
        // Start Process
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.ApplicationExit += new EventHandler(OnApplicationExit);

        // Start SARAH backend
        thread = new Thread(delegate() {
          sarah.Start();
          sarah.Setup();
          Application.Run();
        });
        thread.Start();

        // Wait for ready state
        while (!sarah.Ready()) { Thread.Sleep(100); }

        // Build System Tray
        using (MenuTray tray = new MenuTray()) {
          tray.Display();
          sarah.Log("SARAH", "==========================================");
          Application.Run();
        }
      }
      catch (Exception ex) {
        sarah.Error("CATCH_ALL", ex);
        sarah.Error("CATCH_ALL", ex.StackTrace);
      } 
    }

    static void OnApplicationExit(object sender, EventArgs e) { }
  }
}