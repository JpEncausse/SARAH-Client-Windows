using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Globalization;

namespace net.encausse.sarah.google {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "Google";
    }

    private String GoogleKey = "";
    public override void Setup(){
      base.Setup();
      GoogleKey = ConfigManager.GetInstance().Find("google.key", GoogleKey);
    }

    // ------------------------------------------
    //  Audio Management
    // ------------------------------------------

    public override void BeforeSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      base.BeforeSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);

      XPathNavigator dictation = xnav.SelectSingleNode("/SML/action/@dictation");
      if (dictation == null) { return; }

      Host.Log(this, "DYN process speech2text: ...");
      String language = ConfigManager.GetInstance().Find("bot.language", "fr-FR");
      String speech2text = ProcessAudioStream(stream, language, text);
      if (String.IsNullOrEmpty(speech2text)) { } else {
        options.Add("dictation", speech2text);
      }
    }

    public override void BeforeSpeechRejected(string device, string text, double confidence, XPathNavigator xnav, Stream stream, IDictionary<string, string> options) {
      base.BeforeSpeechRejected(device, text, confidence, xnav, stream, options);

      Host.Log(this, "DYN process speech2text: ...");
      String language = ConfigManager.GetInstance().Find("bot.language","fr-FR");
      String speech2text = ProcessAudioStream(stream, language, text);
      if (!String.IsNullOrEmpty(speech2text)) {
        options.Add("dictation", speech2text);
      }
    }

    // ------------------------------------------
    //  GOOGLE
    // ------------------------------------------

    protected String ProcessAudioStream(Stream stream, String language, String text) {
      Host.Log(this,"ProcessAudioStream: " + language + " " + text);

      // See: https://github.com/gillesdemey/google-speech-v2
      CultureInfo culture = new System.Globalization.CultureInfo(language);
      var stt = new SpeechToText("https://www.google.com/speech-api/v2/recognize?output=json&xjerr=1&client=chromium&maxresults=2&key=" + GoogleKey, culture);

      using (var audio = new MemoryStream()) {
        stream.Position = 0;
        stream.CopyTo(audio);

        audio.Position = 0;
        return stt.Recognize(audio);
      }
    }
  }
}
