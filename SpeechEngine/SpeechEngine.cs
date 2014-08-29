using System;
using System.Collections.Generic;

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
  class SpeechEngine : IDisposable {

    // -------------------------------------------
    //  CONSTRUCTOR
    // ------------------------------------------

    public SpeechRecognitionEngine Engine { get; set; }
    public String Name { get; set; }
    public double Confidence { get; set; }

    public SpeechEngine(String name, String language, double confidence) {
      this.Name = name;
      this.Confidence = confidence;
      this.Engine = new SpeechRecognitionEngine(new System.Globalization.CultureInfo(language));
    }

    private bool trace = false;
    public void Init() {

      var Config = ConfigManager.GetInstance();
      trace = Config.Find("speech.trace",false);

      Log("Init recognizer");

      Engine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
      Engine.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(recognizer_RecognizeCompleted);
      Engine.AudioStateChanged += new EventHandler<AudioStateChangedEventArgs>(recognizer_AudioStateChanged);
      Engine.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(recognizer_SpeechHypothesized);
      Engine.SpeechDetected += new EventHandler<SpeechDetectedEventArgs>(recognizer_SpeechDetected);
      Engine.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(recognizer_SpeechRecognitionRejected);

      // http://msdn.microsoft.com/en-us/library/microsoft.speech.recognition.speechrecognitionengine.updaterecognizersetting(v=office.14).aspx
      Engine.UpdateRecognizerSetting("CFGConfidenceRejectionThreshold", (int)(this.Confidence * 100));

      Engine.MaxAlternates = Config.Find("speech.engine.MaxAlternates",10);
      Engine.InitialSilenceTimeout = TimeSpan.FromSeconds(Config.Find("speech.engine.InitialSilenceTimeout", 0));
      Engine.BabbleTimeout = TimeSpan.FromSeconds(Config.Find("speech.engine.BabbleTimeout", 0));
      Engine.EndSilenceTimeout = TimeSpan.FromSeconds(Config.Find("speech.engine.EndSilenceTimeout", 0.150));
      Engine.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(Config.Find("speech.engine.EndSilenceTimeoutAmbiguous", 0.500));

      // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
      // This will prevent recognition accuracy from degrading over time.
      if (!Config.Find("speech.engine.Adaptation", false)) {
        Engine.UpdateRecognizerSetting("AdaptationOn", 0);
      }

      Log("AudioLevel: " + Engine.AudioLevel);
      Log("MaxAlternates: " + Engine.MaxAlternates);
      Log("BabbleTimeout: " + Engine.BabbleTimeout);
      Log("InitialSilenceTimeout: " + Engine.InitialSilenceTimeout);
      Log("EndSilenceTimeout: " + Engine.EndSilenceTimeout);
      Log("EndSilenceTimeoutAmbiguous: " + Engine.EndSilenceTimeoutAmbiguous);
    }

    // -------------------------------------------
    //  INTERFACE
    // ------------------------------------------

    public void Start() {
      try {
        Engine.RecognizeAsync(RecognizeMode.Multiple);
        Log("Start listening...");
      }
      catch (Exception ex) {
        Error("No device found");
        Error(ex);
      }
    }

    public void Stop(bool dispose) {
      Engine.RecognizeAsyncStop();
      if (dispose) { Engine.Dispose(); }
      Log("Stop listening...done");
    }

    public void Dispose() {
      Engine.RecognizeAsyncStop();
      Engine.Dispose();
      Log("Dispose engine...");
    }

    private static int STATUS_INIT  = 0;
    private static int STATUS_START = 1;
    private static int STATUS_STOP  = 2;
    private int status = 0;
    public void Pause(bool state) {
      Log("Pause listening... " + state);

      if (state && status == STATUS_START) {
        Engine.RecognizeAsyncStop();
        status = STATUS_STOP;
      } else if (!state && status == STATUS_STOP) {
        Engine.RecognizeAsync(RecognizeMode.Multiple);
        status = STATUS_START;
      }
    }

    // -------------------------------------------
    //  GRAMMAR
    // ------------------------------------------

    private DateTime loading = DateTime.MinValue;
    public void Load(IDictionary<string, SpeechGrammar> cache, bool reload) {
      if (reload && Name.Equals("FileSystem")) { return; }
      Log("Loading grammar cache");
      foreach (SpeechGrammar g in cache.Values) {
        if (g.LastModified < loading) { continue; }
        Load(g.Name, g.Build());
      }
      loading = DateTime.Now;
    }

    public void Load(String name, Grammar grammar) {
      if (grammar == null) { Log("ByPass " + name + " wrong grammar"); return; }
      foreach (Grammar g in Engine.Grammars) {
        if (g.Name != name) { continue; }
        Engine.UnloadGrammar(g);
        break;
      }

      Log("Load Grammar to Engine: " + name);
      Engine.LoadGrammar(grammar);
    }


    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Trace(string msg) {
      if (!trace) return;
      SARAH.GetInstance().Log("SpeechEngine][" + Name, msg);
    }

    protected void Log(string msg) {
      SARAH.GetInstance().Log("SpeechEngine][" + Name, msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug("SpeechEngine][" + Name, msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error("SpeechEngine][" + Name, msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error("SpeechEngine][" + Name, ex);
    }

    // -------------------------------------------
    //  CALLBACKS
    // ------------------------------------------

    protected void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
      SpeechRecognized(e.Result);
    }
    protected void recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e) {
      String resultText = e.Result != null ? e.Result.Text : "<null>";
      Log("RecognizeCompleted (" + DateTime.Now.ToString("mm:ss.f") + "): " + resultText);
      Debug("BabbleTimeout: " + e.BabbleTimeout + "; InitialSilenceTimeout: " + e.InitialSilenceTimeout + "; Result text: " + resultText);
    }
    protected void recognizer_AudioStateChanged(object sender, AudioStateChangedEventArgs e) {
      Trace("AudioStateChanged (" + DateTime.Now.ToString("mm:ss.f") + "):" + e.AudioState);
    }
    protected void recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e) {
      Trace("recognizer_SpeechHypothesized " + e.Result.Text + " => " + e.Result.Confidence);
    }
    protected void recognizer_SpeechDetected(object sender, SpeechDetectedEventArgs e) {
      Trace("recognizer_SpeechDetected");
    }
    protected void recognizer_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e) {
      try {
        SpeechManager.GetInstance().SpeechRejected(this, e.Result);
      }
      catch (Exception ex) {
        Error(ex);
      }
    }

    // -------------------------------------------
    //  HANDLER
    // ------------------------------------------

    private bool IsWorking = false;
    protected void SpeechRecognized(RecognitionResult rr) {

      // 1. Handle the Working local state
      if (IsWorking) {
        Log("REJECTED Speech while working: " + rr.Confidence + " Text: " + rr.Text); return;
      }

      // 2. Start
      IsWorking = true;
      var start = DateTime.Now;

      // 3. Handle Results
      try {
        SpeechManager.GetInstance().SpeechRecognized(this, rr); 
      }
      catch (Exception ex) {
        Error(ex);
      }

      // 4. End
      IsWorking = false;
      Debug("SpeechRecognized: " + (DateTime.Now - start).TotalMilliseconds + "ms Text: " + rr.Text);
    }
  }
}
