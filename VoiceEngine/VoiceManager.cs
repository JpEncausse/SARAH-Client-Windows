using System;
using System.Globalization;
using System.IO;
using System.Speech.Synthesis;

namespace net.encausse.sarah.voice {
  public class VoiceManager : IDisposable {

    // -------------------------------------------
    //  SINGLETON
    // -------------------------------------------

    private static VoiceManager manager = null;
    private VoiceManager() { }

    public static VoiceManager GetInstance() {
      if (manager == null) {
        manager = new VoiceManager();
        manager.Init();
      }
      return manager;
    }

    public void Dispose() {
      if (synthesizer != null) {
        synthesizer.Dispose();
      }
    }

    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log("VoiceManager", msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug("VoiceManager", msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error("VoiceManager", msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error("VoiceManager", ex);
    }

    // -------------------------------------------
    //  VoiceManager
    // -------------------------------------------

    private SpeechSynthesizer synthesizer;
    protected void Init() {
      synthesizer = new SpeechSynthesizer();
      synthesizer.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(synthesizer_SpeakCompleted);

      // List available voices
      foreach (InstalledVoice voice in synthesizer.GetInstalledVoices()) {
        VoiceInfo info = voice.VoiceInfo;
        Log("Name: " + info.Name + " Culture: " + info.Culture);
      }

      // Select voice from properties
      var v = ConfigManager.GetInstance().Find("voice.voice", "");
      if (!String.IsNullOrEmpty(v)) {
        synthesizer.SelectVoice(v);
        Log("Select voice: " + v);
      }
      Log("Voice: " + synthesizer.Voice.Name + " Rate: "+ synthesizer.Rate);
    }
    
    // Synchronous
    protected void synthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e) { }


    public void Speak(String tts, bool sync) {
      Log("Speaking: " + tts);
      try {
        PromptBuilder builder = new PromptBuilder();
        builder.Culture = new CultureInfo(ConfigManager.GetInstance().Find("bot.language", "fr-FR"));
        builder.AppendText(tts);
        
        using (var ms = new MemoryStream()) {
          lock (synthesizer) {
            synthesizer.SetOutputToWaveStream(ms);
            synthesizer.Speak(builder);
          }
          ms.Position = 0;
          if (ms.Length <= 0) { return; }

          AddOnManager.GetInstance().AfterHandleVoice(tts, sync, ms);
        }
      }
      catch (Exception ex) {
        Error(ex);
      }
    }
  }
}
