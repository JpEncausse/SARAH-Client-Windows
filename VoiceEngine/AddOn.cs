using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;

namespace net.encausse.sarah.voice {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "Voice";
    }

    // ------------------------------------------
    //  LIFE CYCLE
    // ------------------------------------------

    public override void Setup() {
      base.Setup();
      VoiceManager.GetInstance();
    }

    public override void Dispose() {
      base.Dispose();
      VoiceManager.GetInstance().Dispose();
    }

    // ------------------------------------------
    //  Voice Management
    // ------------------------------------------

    public override void BeforeHandleVoice(String tts, bool sync) {
      base.BeforeHandleVoice(tts, sync);
      VoiceManager.GetInstance().Speak(tts, sync);
    }

    // ------------------------------------------
    //  HTTP Management
    // ------------------------------------------

    public override void HandleBODY(string device, string body, string token) {
      base.HandleBODY(device, body, token);
      if ("speech".Equals(token)){
        AddOnManager.GetInstance().BeforeHandleVoice(body, true);
      }
    }

    public override void BeforeHTTPRequest(string qs, NameValueCollection parameters, IDictionary files, StreamWriter writer) {
      base.BeforeHTTPRequest(qs, parameters, files, writer);

      // Text to Speech
      String tts = parameters.Get("tts");
      if (tts != null) {
        AddOnManager.GetInstance().BeforeHandleVoice(tts, parameters.Get("sync") != null);
      }
    }

    // ------------------------------------------
    //  Audio Management
    // ------------------------------------------

    public override void BeforeSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      base.BeforeSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);

      var tts = xnav.SelectSingleNode("/SML/action/@tts");
      if (tts != null) {
        AddOnManager.GetInstance().BeforeHandleVoice(tts.Value, true);
      }
    }

  }
}
