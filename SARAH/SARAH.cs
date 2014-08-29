using System;
using System.IO;

namespace net.encausse.sarah {
  public class SARAH : IDisposable {

    // -------------------------------------------
    //  SINGLETON
    // -------------------------------------------
    
    private static SARAH sarah = null;
    private SARAH() { }

    public static SARAH GetInstance() {
      if (sarah == null) {
        sarah = new SARAH();

        var path = "bootlog.txt";
        if (File.Exists(path)) {
          File.Delete(path);
        }
        var bootlog = new System.Diagnostics.TextWriterTraceListener(File.CreateText(path));
        System.Diagnostics.Debug.Listeners.Add(bootlog);

        sarah.Log("==========================================");
      }
      return sarah;
    }

    // -------------------------------------------
    //  START
    // -------------------------------------------

    public void Start() {
      Log("SARAH","Starting...");
      var path = Environment.CurrentDirectory;

      // Load addon configuration
      ConfigManager.GetInstance().LoadAddOns("AddOns");

      // Load custom configuration
      ConfigManager.GetInstance().Load("custom.ini", true);
      // ConfigManager.GetInstance().Save("custom.ini.test");

      // Load libraries
      AddOnManager.GetInstance().Load("Libraries");

      // Load addons
      AddOnManager.GetInstance().LoadAddOns("AddOns");

      // Start addons
      Environment.CurrentDirectory = path;
      AddOnManager.GetInstance().Start();
    }

    // -------------------------------------------
    //  SETUP
    // -------------------------------------------

    public void Setup() {

      Log("SARAH", "Setup...");

      // Setup addons
      AddOnManager.GetInstance().Setup();

      ready = true;
    }

    private bool ready = false;
    public bool Ready() {
      return ready && AddOnManager.GetInstance().Ready();
    }

    // -------------------------------------------
    //  STOP
    // -------------------------------------------

    public void Dispose() {
      ready = false;
      AddOnManager.GetInstance().Dispose();
      Log("S.A.R.A.H.", "Dispose all resources");
      Log("==========================================");
      System.Diagnostics.Debug.Flush();
    }

    // -------------------------------------------
    //  LOG MANAGER
    // ------------------------------------------
    
    public void Log(string msg) {
      if (!ready) { System.Diagnostics.Debug.WriteLine(msg); }
      AddOnManager.GetInstance().Log(msg);
    }

    public void Log(string ctxt, string msg) {
      if (!ready) { System.Diagnostics.Debug.WriteLine("[" + ctxt + "] " + msg); }
      AddOnManager.GetInstance().Log(ctxt, msg);
    }

    public void Debug(string ctxt, string msg) {
      if (!ready) { System.Diagnostics.Debug.WriteLine("[" + ctxt + "] " + msg); }
      AddOnManager.GetInstance().Debug(ctxt, msg);
    }

    public void Error(string ctxt, string msg) {
      if (!ready) { System.Diagnostics.Debug.WriteLine("[" + ctxt + "] " + msg); }
      AddOnManager.GetInstance().Error(ctxt, msg);
    }

    public void Error(string ctxt, Exception ex) {
      if (!ready) { System.Diagnostics.Debug.WriteLine("[" + ctxt + "] " + ex.Message); }
      AddOnManager.GetInstance().Error(ctxt, ex);
    }
  }
}
