using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Controls;
using System.Xml.XPath;


namespace net.encausse.sarah.body {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "BodyEngine";
    }

    // ------------------------------------------
    //  LIFE CYCLE
    // ------------------------------------------

    public override void Dispose() {
      foreach (var task in Tasks.Values) {
        task.Dispose();
      }
      Tasks.Clear();
      base.Dispose();
    }

    // ------------------------------------------
    //  HTTP Management
    // ------------------------------------------

    public override void BeforeHTTPRequest(string qs, NameValueCollection parameters, IDictionary files, StreamWriter writer) {
      base.BeforeHTTPRequest(qs, parameters, files, writer);

      // Start/Stop Gesture recognition
      var gesture = parameters.Get("gesture");
      if (gesture != null) {
        bool state = Boolean.Parse(gesture);
        foreach (var task in Tasks.Values) {
          task.Pause(!state);
        }
      }
    }

    // ------------------------------------------
    //  Audio Management
    // ------------------------------------------

    public override void BeforeSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      base.BeforeSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);

      // Start/Stop Face recognition
      var gesture = xnav.SelectSingleNode("/SML/action/@gesture");
      if (gesture != null) {
        bool state = Boolean.Parse(gesture.Value);
        foreach (var task in Tasks.Values) {
          task.Pause(!state);
        }
      }
    }

    // -------------------------------------------
    //  UI
    // -------------------------------------------

    public override void HandleSidebar(string device, StackPanel sidebar) {
      if (!Tasks.ContainsKey(device)) { return; }
      Tasks[device].HandleSidebar(sidebar);
    }

    public override void RepaintColorFrame(string device, byte[] bgra, int width, int height) {
      if (!Tasks.ContainsKey(device)) { return; }
      Tasks[device].RepaintColorFrame(bgra, width, height);
    }

    // -------------------------------------------
    //  BODY
    // -------------------------------------------

    public IDictionary<string, AddOnTask> Tasks = new Dictionary<string, AddOnTask>();
    public override void InitBodyFrame(string device, ICollection<NBody> data, Timestamp stamp, int width, int height) {
      base.InitBodyFrame(device, data, stamp, width, height);

      var dueTime = TimeSpan.FromMilliseconds(200);
      var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("body.threshold", 30));

      var task = new AddOnTask(device);
      task.SetBodies(data, stamp, width, height);
      task.Start(dueTime, interval);

      Tasks.Add(device, task);
    }
  }
}
