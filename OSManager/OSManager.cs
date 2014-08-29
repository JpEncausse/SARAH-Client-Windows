using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using WindowsInput;
using WindowsInput.Native;

using NAudio.Wave;

using SoftMade.IO;

namespace net.encausse.sarah.os {
  public class OSManager : IDisposable {

    // -------------------------------------------
    //  SINGLETON
    // -------------------------------------------

    private static OSManager manager = null;
    private OSManager() { }

    public static OSManager GetInstance() {
      if (manager == null) {
        manager = new OSManager();
        manager.Init();
      }
      return manager;
    }

    private InputSimulator simulator = null;
    protected void Init() {
      simulator = new InputSimulator();
      InitAudioWatcher();
    }

    public void Dispose() {

      if (audioWatcher != null) {
        audioWatcher.EnableRaisingEvents = false;
        audioWatcher.Dispose();
      }

      if (buffer != null) {
        buffer.Dispose();
      }
    }

    // -------------------------------------------
    //  UTILITY
    // -------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log("OS", msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug("OS", msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error("OS", msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error("OS", ex);
    }


    // ------------------------------------------
    //  Audio Watcher
    // ------------------------------------------

    Stream buffer = null;
    AdvancedFileSystemWatcher audioWatcher = null;
    protected void InitAudioWatcher() {

      if (audioWatcher != null) { return; }
      string directory = ConfigManager.GetInstance().Find("os.recognize", "");
      if (!Directory.Exists(directory)) { return; }

      Log("Init Audio Watcher: " + directory);
      audioWatcher = new AdvancedFileSystemWatcher();
      audioWatcher.Path = directory;
      audioWatcher.Filter = "*.wav";
      audioWatcher.IncludeSubdirectories = true;
      audioWatcher.NotifyFilter = NotifyFilters.LastWrite;
      audioWatcher.Changed += new EventHandler<SoftMade.IO.FileSystemEventArgs>(audio_Changed);
      audioWatcher.EnableRaisingEvents = true;

      buffer = new StreamBuffer();
      var confidence = ConfigManager.GetInstance().Find("os.confidence", 0.6);
      var format = ConfigManager.GetInstance().Find("os.format", "Kinect");
      AddOnManager.GetInstance().AddAudioSource("FileSystem", buffer, format, null, confidence);
    }

    protected void audio_Changed(object sender, SoftMade.IO.FileSystemEventArgs e) {
      if (e.ChangeType == SoftMade.IO.WatcherChangeTypes.EndWrite) {
        Recognize(e.FullPath);
      }
    }

    public void Recognize(string path) {
      Log("Recognize: " + Path.GetFullPath(path));
      if (!File.Exists(path)) { return; }
      using (var reader = new WaveFileReader(path)) {
        var tmp = buffer.Position;
        reader.CopyTo(buffer);
        buffer.Position = tmp;
      }
    }

    // -------------------------------------------
    //  INPUT
    // -------------------------------------------

    private VirtualKeyCode KeyCode(String key) {
      int code = int.Parse(key);
      return (VirtualKeyCode)code;
    }

    public void SimulateTextEntry(String text) {
      simulator.Keyboard.TextEntry(text);
    }
    public void SimulateKey(String key, int type, String mod) {
      Log("SimulateKey " + key + " + " + mod);
      if (mod != null && mod != "") {
        simulator.Keyboard.ModifiedKeyStroke(KeyCode(mod), KeyCode(key));
        return;
      }
      if      (type == 0) { simulator.Keyboard.KeyPress(KeyCode(key)); } 
      else if (type == 1) { simulator.Keyboard.KeyDown(KeyCode(key)); } 
      else if (type == 2) { simulator.Keyboard.KeyUp(KeyCode(key)); }
    }

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);
    public void ActivateApp(string processName) {
      // Activate the first application we find with this name
      Process[] p = Process.GetProcessesByName(processName);
      if (p.Length > 0) {
        SetForegroundWindow(p[0].MainWindowHandle);
        Log("Activate " + p[0].ProcessName);
      }
    }

    public void RunApp(String processName, String param) {
      try {
        Process.Start(processName, param);
      } 
      catch (Exception ex) { Error(ex); }
    }
  }
}
