using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Drawing;
using System.Collections.Specialized;
using System.Collections;

/**
 * See also
 * http://www.codeproject.com/Articles/6334/Plug-ins-in-C
 */
namespace net.encausse.sarah {
  public interface IAddOn {
    IAddOnHost Host { get; set; }

    string Name    { get; }
    string Author  { get; }
    string Version { get; }

    // AddOn Life
    void Start();
    void Setup();
    void Dispose();
    bool Ready();

    // GUI
    void HandleMenuItem(ContextMenuStrip menu);
    void HandleSidebar(string device, StackPanel sidebar);
    void HandleSelection(string device, Rectangle rect);
    void RepaintColorFrame(string device, byte[] pixels, int width, int height);
    
    // Logs Management
    void Log(string msg);
    void Log(string ctxt, string msg);
    void Debug(string ctxt, string msg);
    void Error(string ctxt, string msg);
    void Error(string ctxt, Exception ex);

    // Audio Management (Stream => WaveStream 16000)
    void HandleAudioSource(string device, Stream stream, string format, string language, double confidence);
    void BeforeSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options);
    void AfterSpeechRecognition(string device,  string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options);
    void BeforeSpeechRejected(string device, string text, double confidence, XPathNavigator xnav, Stream stream, IDictionary<string, string> options);
    void AfterSpeechRejected(string device, string text, double confidence, XPathNavigator xnav, Stream stream, IDictionary<string, string> options);

    // Voice Management (Stream => WaveStream 11000)
    void BeforeHandleVoice(String tts, bool sync);
    void AfterHandleVoice(String tts, bool sync, Stream stream);

    // HTTP Management
    void SendGET(string device, string url, string token, IDictionary<string, string> parameters);
    void SendPOST(string device, string url, string token, string[] keys, string[] values);
    void SendFILE(string device, string url, string token, string path);
    void BeforeGET(string device, string url, string token, IDictionary<string, string> parameters);
    void HandleBODY(string device, string body, string token);
    void BeforeHTTPRequest(string qs, NameValueCollection parameters, IDictionary files, StreamWriter writer);
    void AfterHTTPRequest(string qs, NameValueCollection parameters, IDictionary files);

    // Camera
    void InitFrame(string device, DeviceFrame frame);
    void MotionDetected(string device, bool status);

    // Profile Management
    void HandleProfile(string device, string key, object value);
    bool IsEngaged(string device);
  }

  public interface IAddOnHost {
    void Log(IAddOn addon, String msg);
  }
}
