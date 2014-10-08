using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Web;

namespace net.encausse.sarah.http {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "HTTPEngine";
    }

    public override void Start() {
      base.Start();
      HttpManager.GetInstance().StartHttpServer();
    }

    public override void Dispose() {
      base.Dispose();
      HttpManager.GetInstance().Dispose();
    }

    // ------------------------------------------
    //  HTTP Management
    // ------------------------------------------

    bool working = false;
    public override void SendGET(string device, string url, string token, IDictionary<string, string> options) {
      base.SendGET(device, url, token, options);
      if (working) return; working = true;
      HttpManager.GetInstance().SendGET(device, url, token, options);
      working = false;
    }

    public override void SendPOST(string device, string url, string token, string[] keys, string[] values) {
      base.SendPOST(device, url, token, keys, values);
      if (working) return; working = true;
      HttpManager.GetInstance().SendPOST(device, url, token, keys, values);
      working = false;
    }

    public override void SendFILE(string device, string url, string token, string path) {
      base.SendFILE(device, url, token, path);
      if (working) return; working = true;
      HttpManager.GetInstance().SendFILE(device, url, token, path);
      working = false;
    }

    // ------------------------------------------
    //  Audio Management
    // ------------------------------------------

    public override void AfterSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      base.BeforeSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);

      var xurl = xnav.SelectSingleNode("/SML/action/@uri");
      if (xurl == null) return;
      
      var url = xurl.Value;
      url += url.IndexOf("?") > 0 ? "&" : "?";
      url += QueryString(xnav.Select("/SML/action/*"));
      url += "confidence=" + (confidence + "&").Replace(",", ".");
      url += "text=" + HttpUtility.UrlEncode(text) + "&";

      SendGET(device, url, "speech", options);
    }

    public override void AfterSpeechRejected(string device, string text, double confidence, XPathNavigator xnav, Stream stream, IDictionary<string, string> options) {
      AfterSpeechRecognition(device, text, confidence, xnav, null, stream, options);
    }

    protected String QueryString(XPathNodeIterator it) {
      String qs = "";
      while (it.MoveNext()) {
        
        if (it.Current.Name == "confidence") continue;
        if (it.Current.Name == "uri") continue;

        String children = null;
        if (it.Current.HasChildren) {
          QueryString(it.Current.SelectChildren(String.Empty, it.Current.NamespaceURI));
        }
        qs += (children == null) ? (it.Current.Name + "=" + HttpUtility.UrlEncode(it.Current.Value) + "&") : (children);
      }
      return qs;
    }

    // ------------------------------------------
    //  Camera Management
    // ------------------------------------------

    public override void MotionDetected(string device, bool motion) {
      base.MotionDetected(device, motion);
      var options = new Dictionary<string, string>();
      options.Add("motion", "" + motion);
      SendGET(device, "http://127.0.0.1:8080/standby", "motion", options);
    }

  }
}