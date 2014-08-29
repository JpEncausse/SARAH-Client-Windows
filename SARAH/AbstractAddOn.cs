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

    public virtual void Dispose() {
      Host.Log(this, "Dispose");
    }

    public virtual bool Ready() {
      Host.Log(this, "Ready");
      return true;
    }

    // ------------------------------------------
    //  AddOn GUI
    // ------------------------------------------

    public virtual void HandleMenuItem(ContextMenuStrip menu) { }
    public virtual void HandleSidebar(string device, StackPanel sidebar) { }
    public virtual void HandleSelection(string device, Rectangle rect) { }
    public virtual void RepaintColorFrame(string device, byte[] bgra, int width, int height) { }
    

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

    // Called 1st
    public virtual void InitColorFrame(string device, byte[] data, Timestamp stamp, int width, int height, int fps) { }

    // Called 2nd
    public virtual void InitBodyFrame(string device, ICollection<NBody> data, Timestamp stamp, int width, int height) { }

    public virtual void MotionDetected(string device, bool status) { }

    // ------------------------------------------
    //  Profile Management
    // ------------------------------------------

    public virtual void HandleProfile(string device, string key, object value) { }
    public virtual bool IsEngaged(string device) { return false; }
  }
}