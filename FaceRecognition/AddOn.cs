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
      if (sidebar.Name != "Sidebar") { return; }
      base.HandleSidebar(device + "_detect", sidebar);
      base.HandleSidebar(device + "_reco", sidebar);
    }

    public override void RepaintColorFrame(string device, byte[] bgra, int width, int height) {
      base.RepaintColorFrame(device + "_detect", bgra, width, height);
      base.RepaintColorFrame(device + "_reco", bgra, width, height);
    }

    // -------------------------------------------
    //  CAMERA
    // -------------------------------------------

    private FaceHelper helper;
    public override void InitFrame(string device, DeviceFrame frame) {
      if (helper == null) {
        helper = new FaceHelper(frame.Width, frame.Height);
      }
      base.InitFrame(device + "_detect", frame);
      base.InitFrame(device + "_reco", frame);
    }

    public override AbstractAddOnTask NewTask(string device) {
      var dueTime = TimeSpan.FromMilliseconds(200);

      if (device.EndsWith("detect")) {
        var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("face.detect", 200));
        return new DetectTask(dueTime, interval, helper);
      } else {
        var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("face.recognize", 600));
        return new RecognizeTask(dueTime, interval, helper);
      }
    }

  }
}