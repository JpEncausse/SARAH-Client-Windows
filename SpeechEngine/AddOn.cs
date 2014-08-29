using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Web;
using System.Text.RegularExpressions;

namespace net.encausse.sarah.speech {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "SpeechEngine";
    }

    // ------------------------------------------
    //  AddOn life cycle
    // ------------------------------------------

    public override void Start() {
      base.Start();

      // Seek plugin folder
      var plugins = ConfigManager.GetInstance().Find("bot.plugins", ConfigManager.GetInstance().PATH_PLUGIN);

      // Load all Grammar
      GrammarManager.GetInstance().Load(plugins);

      // Watch grammar folder
      GrammarManager.GetInstance().Watch(plugins);
    }

    public override void Dispose() {
      base.Dispose();
      GrammarManager.GetInstance().Dispose();
      SpeechManager.GetInstance().Dispose();
    }

    // ------------------------------------------
    //  Audio Management
    // ------------------------------------------

    public override void HandleAudioSource(string device, Stream stream, string format, string language, double confidence) {
      base.HandleAudioSource(device, stream, format, language, confidence);
      SpeechManager.GetInstance().AddSpeechEngine(stream, format, device, language, confidence);
    }

    public override void BeforeSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      base.BeforeSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);

      var listen = xnav.SelectSingleNode("/SML/action/@listen");
      if (listen != null) {
        bool state = Boolean.Parse(listen.Value);
        SpeechManager.GetInstance().Pause(!state);
      }
    }

    public override void AfterSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      base.AfterSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);

      // Reset context timeout 
      ContextManager.GetInstance().ResetContextTimeout();

      // Update Context
      var context = xnav.SelectSingleNode("/SML/action/@context");
      if (context != null) {
        ContextManager.GetInstance().SetContext(context.Value);
        ContextManager.GetInstance().ApplyContextToEngines();
        ContextManager.GetInstance().StartContextTimeout();
      }

      // Update a Grammar with XML
      var asknext = xnav.SelectSingleNode("/SML/action/@asknext");
      if (asknext != null) {
        var example = GrammarManager.GetInstance().FindExample(asknext.Value);
        AddOnManager.GetInstance().BeforeHandleVoice(example, true);
      }
    }


    public override void AfterSpeechRejected(string device, string text, double confidence, XPathNavigator xnav, Stream stream, IDictionary<string, string> options) {
      base.AfterSpeechRejected(device, text, confidence, xnav, stream, options);

      if (!options.ContainsKey("dictation")) { return; }
      Host.Log(this, "DYN reset to default context (in SpeechRecognitionRejected)");

      ContextManager.GetInstance().SetContext("default");
      ContextManager.GetInstance().ApplyContextToEngines();
    }

    // ------------------------------------------
    //  HTTP Management
    // ------------------------------------------

    public override void BeforeHTTPRequest(string qs, NameValueCollection parameters, IDictionary files, StreamWriter writer) {
      base.BeforeHTTPRequest(qs, parameters, files, writer);

      // Start/Stop SpeechEngine
      var listen = parameters.Get("listen");
      if (listen != null) {
        bool state = Boolean.Parse(listen);
        SpeechManager.GetInstance().Pause(!state);
      }
    }

    public override void AfterHTTPRequest(string qs, NameValueCollection parameters, IDictionary files) {
      base.AfterHTTPRequest(qs, parameters, files);
      var querystring = System.Web.HttpUtility.ParseQueryString(qs);

      // Update Context
      var context = querystring.GetValues("context");
      if (context != null) {
        ContextManager.GetInstance().SetContext(context);
        ContextManager.GetInstance().ApplyContextToEngines();
        ContextManager.GetInstance().StartContextTimeout();
      }

      // Update a Grammar with XML
      var grammar = parameters.Get("grammar");
      if (grammar != null) {
        GrammarManager.GetInstance().UpdateXML(grammar, parameters.Get("xml"));
      }

      // AskMe: start a Dynamic Grammar
      String[] sentences = querystring.GetValues("sentences");
      String[] tags = querystring.GetValues("tags");
      String callbackUrl = parameters.Get("callbackUrl");
      if (sentences != null && tags != null) {
        ContextManager.GetInstance().LoadDynamicGrammar(sentences, tags, callbackUrl);
        ContextManager.GetInstance().SetContext("Dyn");
        ContextManager.GetInstance().ApplyContextToEngines();
      }

      // AskNext: TTS Acording to ruleId
      var asknext = parameters.Get("asknext");
      if (asknext != null) {
        var example = GrammarManager.GetInstance().FindExample(asknext);
        if (example != null) {
          var regexp = new Regex("\\[([^\\]]+)\\]");
          var  matches = regexp.Matches(example);
          foreach (Match match in matches) {
            var key = match.Groups[1];
            var value = parameters.Get(key.ToString());
            if (value == null) { value = ""; }
            example = example.Replace("["+key+"]",value);
          }
          AddOnManager.GetInstance().BeforeHandleVoice(example, true);
        }
      }

    }

  }
}