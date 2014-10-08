using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Drawing;
using System.Collections.Specialized;
using System.Collections;

namespace net.encausse.sarah {
  public abstract class AbstractAddOn : IAddOn {

    public IAddOnHost Host { get; set; }

    public String Name { get; set; }
    public String Author { get; set; }
    public String Version { get; set; }

    public AbstractAddOn() {
      Author = "Jean-Philippe Encausse";
      Version = "4.0";
    }

    // ------------------------------------------
    //  AddOn life cycle
    // ------------------------------------------

    public virtual void Start() {
      Host.Log(this, "Start");
    }

    public virtual void Setup() {
      Host.Log(this, "Setup");
    }


    public virtual bool Ready() {
      Host.Log(this, "Ready");
      return true;
    }

    // ------------------------------------------
    //  AddOn GUI
    // ------------------------------------------

    public virtual void HandleMenuItem(ContextMenuStrip menu) { }

    public virtual void HandleSidebar(string device, StackPanel sidebar) {
      if (!Tasks.ContainsKey(device) || Tasks[device] == null) { return; }
      Tasks[device].HandleSidebar(sidebar);
    }

    public virtual void HandleSelection(string device, Rectangle rect) {
      if (!Tasks.ContainsKey(device) || Tasks[device] == null) { return; }
      Tasks[device].HandleSelection(rect);
    }

    public virtual void RepaintColorFrame(string device, byte[] pixels, int width, int height) {
      if (!Tasks.ContainsKey(device) || Tasks[device] == null) { return; }
      Tasks[device].RepaintColorFrame(pixels, width, height);
    }
    

    // ------------------------------------------
    //  Logs Management
    // ------------------------------------------

    public virtual void Log(string msg) { }
    public virtual void Log(string ctxt, string msg) { }
    public virtual void Debug(string ctxt, string msg) { }
    public virtual void Error(string ctxt, string msg) { }
    public virtual void Error(string ctxt, Exception ex) { }

    // ------------------------------------------
    //  Audio Management
    // ------------------------------------------

    public virtual void HandleAudioSource(string device, Stream stream, string format, string language, double confidence) { }
    public virtual void BeforeSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) { }
    public virtual void AfterSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) { }
    public virtual void BeforeSpeechRejected(string device, string text, double confidence, XPathNavigator xnav, Stream stream, IDictionary<string, string> options) { }
    public virtual void AfterSpeechRejected(string device, string text, double confidence, XPathNavigator xnav, Stream stream, IDictionary<string, string> options) { }

    // ------------------------------------------
    //  Voice Management
    // ------------------------------------------

    public virtual void BeforeHandleVoice(string tts, bool sync) { }
    public virtual void AfterHandleVoice(string tts, bool sync, Stream stream) { }

    // ------------------------------------------
    //  HTTP Management
    // ------------------------------------------

    public virtual void SendGET(string device, string url, string token, IDictionary<string, string> parameters) { }
    public virtual void SendPOST(string device, string url, string token, string[] keys, string[] value) { }
    public virtual void SendFILE(string device, string url, string token, string path) { }
    public virtual void BeforeGET(string device, string url, string token, IDictionary<string, string> parameters) { }
    public virtual void HandleBODY(string device, string body, string token) { }
    public virtual void BeforeHTTPRequest(string qs, NameValueCollection parameters, IDictionary files, StreamWriter writer) { }
    public virtual void AfterHTTPRequest(string qs, NameValueCollection parameters, IDictionary files) { }

    // ------------------------------------------
    //  Camera Management
    // ------------------------------------------

    public virtual void MotionDetected(string device, bool status) { }

    // ------------------------------------------
    //  Profile Management
    // ------------------------------------------

    public virtual void HandleProfile(string device, string key, object value) { }
    public virtual bool IsEngaged(string device) { return false; }

    // -------------------------------------------
    //  TASKS
    // -------------------------------------------

    public IDictionary<string, AbstractAddOnTask> Tasks = new Dictionary<string, AbstractAddOnTask>();

    public virtual void Dispose() {
      Host.Log(this, "Dispose");
      foreach (var task in Tasks.Values) {
        task.Dispose();
      }
      Tasks.Clear();
    }

    public virtual void InitFrame(string device, DeviceFrame frame) {
      if (Tasks.ContainsKey(device)) {
        Tasks[device].AddFrame(frame);
        return;
      }

      var task = NewTask(device);
      if (task == null) { return; }

      task.Device = device;
      task.AddFrame(frame);
      task.Start();

      Tasks.Add(device, task);
    }

    public virtual AbstractAddOnTask NewTask(string device) {
      return null;
    }

  }
}