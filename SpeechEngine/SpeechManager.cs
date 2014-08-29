using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.XPath;

#if MICRO
using System.Speech;
using System.Speech.Recognition;
using System.Speech.AudioFormat;
#endif

#if KINECT
using Microsoft.Speech;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.AudioFormat;
#endif

namespace net.encausse.sarah.speech {
  class SpeechManager : IDisposable {

    // -------------------------------------------
    //  SINGLETON
    // -------------------------------------------

    private static SpeechManager manager = null;
    private SpeechManager() { }

    public static SpeechManager GetInstance() {
      if (manager == null) {
        manager = new SpeechManager();
      }
      return manager;
    }

    public void Dispose() {

    }

    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log("SpeechManager", msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug("SpeechManager", msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error("SpeechManager", msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error("SpeechManager", ex);
    }

    // -------------------------------------------
    //  ENGINES
    // -------------------------------------------

    public IDictionary<string, SpeechEngine> Engines = new Dictionary<string, SpeechEngine>();
    public ICollection<SpeechEngine> GetEngines() {
      return Engines.Values;
    }

    public void AddSpeechEngine(Stream stream, string format, String device, String language, double confidence) {

      language = (language == null) ? ConfigManager.GetInstance().Find("bot.language", "fr-FR") : language;

      var info = new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo);
      if ("Kinect".Equals(format)) {
        info = new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null);
      }

      SpeechEngine engine = new SpeechEngine(device, language, confidence);
      engine.Load(GrammarManager.GetInstance().Cache, false); 
      engine.Init();
      engine.Engine.SetInputToAudioStream(stream, info);
      engine.Start();

      Engines.Add(device, engine);
    }

    public void Pause(bool state) {
      foreach (var engine in Engines.Values) {
        engine.Pause(state);
      }
    }

    // -------------------------------------------
    //  SPEECH
    // -------------------------------------------

    protected bool Confidence(String device, RecognitionResult rr, XPathNavigator xnav, double threashold) {

      double confidence = rr.Confidence;

      // Override Engine threashold by grammar
      XPathNavigator level = xnav.SelectSingleNode("/SML/action/@threashold");
      if (level != null) {
        Log("Override confidence: " + level.Value);
        threashold = level.ValueAsDouble;
      }

      // Search for bot name
      double match = 0;
      double bot = ConfigManager.GetInstance().Find("bot.confidence", 0.0);
      string name = ConfigManager.GetInstance().Find("bot.name", "SARAH").ToUpper();
      foreach (var word in rr.Words) {
        if (!name.Equals(word.Text.ToUpper())) { continue; }
        if (word.Confidence < threashold + bot) { // Check the bot name with threashold confidence
          Log("REJECTED " + name + ": " + word.Confidence + " < " + (threashold + bot) + " (" + rr.Text + ")");
          return false;
        }
        match = word.Confidence;
        break;
      }

      // Must have bot name in sentence
      var grammar = GrammarManager.GetInstance().FindGrammar(rr.Grammar.Name);
      if (match == 0 && grammar.HasName && !AddOnManager.GetInstance().IsEngaged(device)) { return false; }

      // Check full sentence
      if (confidence < threashold) {
        Log("REJECTED: " + confidence + " (" + match + ") " + " < " + threashold + " (" + rr.Text + ")");
        return false;
      }

      // Speech recognized
      Log("RECOGNIZED: " + confidence + " (" + match + ") " + " > " + threashold + " (" + rr.Text + ")");
      return true;
    }

    private bool IsListening = true;
    public void SpeechRecognized(SpeechEngine engine, RecognitionResult rr) {

      // 1. Handle the Listening global state
      if (!IsListening) {
        Log("REJECTED not listening");
        return;
      }

      // Compute XPath Navigator
      XPathNavigator xnav = rr.ConstructSmlFromSemantics().CreateNavigator();

      // 2. Handle confidence
      if (!Confidence(engine.Name, rr, xnav, engine.Confidence)) {
        return;
      }

      // 3. Set an engagement for valid audio
      AddOnManager.GetInstance().HandleProfile(engine.Name, "engaged", (Object) DateTime.Now);

      // 4. Forward to all addons
      var text    = rr.Text;
      var grammar = rr.Grammar.Name;
      var options = new Dictionary<string, string>();

      using (var stream = new MemoryStream()) {
        rr.Audio.WriteToWaveStream(stream);
        AddOnManager.GetInstance().BeforeSpeechRecognition(engine.Name, text, rr.Confidence, xnav, grammar, stream, options);
        AddOnManager.GetInstance().AfterSpeechRecognition(engine.Name, text, rr.Confidence, xnav, grammar, stream, options);
      }
    }

    public void SpeechRejected(SpeechEngine engine, RecognitionResult rr) {

      // 1. Handle the Listening global state
      if (!IsListening) {
        Log("REJECTED not listening");
        return;
      }

      // 2. Check DYN Grammar
      if (ContextManager.GetInstance().Dynamic() == null) {
        return;
      }

      XPathNavigator xnav = rr.ConstructSmlFromSemantics().CreateNavigator();

      // 3. Forward to all addons
      var text = rr.Text;
      var options = new Dictionary<string, string>();

      using (var stream = new MemoryStream()) {
        rr.Audio.WriteToWaveStream(stream);
        AddOnManager.GetInstance().BeforeSpeechRejected(engine.Name, text, rr.Confidence, xnav, stream, options);
        AddOnManager.GetInstance().AfterSpeechRejected(engine.Name, text, rr.Confidence, xnav, stream, options);
      }
    }

  }
}
