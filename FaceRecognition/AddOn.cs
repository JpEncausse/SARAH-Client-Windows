using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using System.Collections.Specialized;
using System.Collections;

namespace net.encausse.sarah.face {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "FaceRecognition";
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

      // Start/Stop Face recognition
      var face = parameters.Get("face");
      if (face != null) {
        bool state = Boolean.Parse(face);
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
      var face = xnav.SelectSingleNode("/SML/action/@face");
      if (face != null) {
        bool state = Boolean.Parse(face.Value);
        foreach (var task in Tasks.Values) {
          task.Pause(!state);
        }
      }
    }

    // -------------------------------------------
    //  UI
    // -------------------------------------------

    public override void HandleSidebar(string device, StackPanel sidebar) {
      if (!Tasks.ContainsKey(device+"_detect")) { return; }
      Tasks[device + "_detect"].HandleSidebar(sidebar);

      if (!Tasks.ContainsKey(device + "_reco")) { return; }
      Tasks[device + "_reco"].HandleSidebar(sidebar);
    }

    public override void RepaintColorFrame(string device, byte[] bgra, int width, int height) {
      if (!Tasks.ContainsKey(device + "_detect")) { return; }
      Tasks[device + "_detect"].RepaintColorFrame(bgra, width, height);

      if (!Tasks.ContainsKey(device + "_reco")) { return; }
      Tasks[device + "_reco"].RepaintColorFrame(bgra, width, height);
    }

    // -------------------------------------------
    //  CAMERA
    // -------------------------------------------

    public IDictionary<string, AbstractAddOnTask> Tasks = new Dictionary<string, AbstractAddOnTask>();

    public override void InitColorFrame(string device, byte[] data, Timestamp stamp, int width, int height, int fps) {
      base.InitColorFrame(device, data, stamp, width, height, fps);

      var dueTime = TimeSpan.FromMilliseconds(200);
      var itvDetect = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("face.detect", 100));
      var itvRecognize = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("face.recognize", 500));

      var helper = new FaceHelper(width, height);

      var detect = new DetectTask(device, helper);
      detect.SetColor(data, stamp, width, height, fps);
      detect.Start(dueTime, itvDetect);
      Tasks.Add(device + "_detect", detect);

      var recognize = new RecognizeTask(device, helper);
      recognize.SetColor(data, stamp, width, height, fps);
      recognize.Start(dueTime, itvRecognize);
      Tasks.Add(device + "_reco", recognize);
    }

    public override void InitBodyFrame(string device, ICollection<NBody> data, Timestamp state, int width, int height) {
      // By design, this function is called after InitColorFrame()
      // so task should already exists !

      ((DetectTask)Tasks[device + "_detect"]).SetBodies(data);
      ((RecognizeTask)Tasks[device + "_reco"]).SetBodies(data);
    }

  }
}