using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Xml.XPath;

using NAudio.Wave;

namespace net.encausse.sarah.speaker {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "Speaker";
    }

    // ------------------------------------------
    //  AddOn life cycle
    // ------------------------------------------

    public override void Setup() {
      base.Setup();
      int waveOutDevices = WaveOut.DeviceCount;
      for (int waveOutDevice = 0; waveOutDevice < waveOutDevices; waveOutDevice++) {
        WaveOutCapabilities deviceInfo = WaveOut.GetCapabilities(waveOutDevice);
        Host.Log(this, "Device " + waveOutDevice + ": " + deviceInfo.ProductName + ", " + deviceInfo.Channels + " channels");
      }
    }

    public override void Dispose() {
      base.Dispose();
      SpeakerManager.GetInstance().Dispose();
    }

    // ------------------------------------------
    //  Voice Management
    // ------------------------------------------

    public override void AfterHandleVoice(String tts, bool sync, Stream stream) {
      base.AfterHandleVoice(tts, sync, stream);
      SpeakerManager.GetInstance().Speak(stream, !sync);
    }

    // ------------------------------------------
    //  HTTP Management
    // ------------------------------------------

    public override void BeforeHTTPRequest(string qs, NameValueCollection parameters, IDictionary files, StreamWriter writer) {
      base.BeforeHTTPRequest(qs, parameters, files, writer);

      // Status
      if (parameters.Get("speaking") != null) {
        bool status = SpeakerManager.GetInstance().IsSpeaking();
        writer.Write(status ? "speaking" : "");
      }

      // Stop Speaking
      var notts = parameters.Get("notts");
      if (notts != null) {
        var once = "true".Equals(parameters.Get("once"));
        SpeakerManager.GetInstance().Stop(SpeakerManager.SPEAKING_ID, !once);
      }

      // Stop Music
      var stop = parameters.Get("stop");
      if (stop != null) {
        SpeakerManager.GetInstance().Stop(stop, false);
      }

      // Play Music
      var mp3 = parameters.Get("play");
      if (mp3 != null) {
        SpeakerManager.GetInstance().Play(mp3, parameters.Get("sync") == null);
      }
    }

    // ------------------------------------------
    //  Audio Management
    // ------------------------------------------

    public override void BeforeSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      base.BeforeSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);

      // Stop Speaking
      var notts = xnav.SelectSingleNode("/SML/action/@notts");
      if (notts != null) {
        var node = xnav.SelectSingleNode("/SML/action/@once");
        var once = node != null ? "true".Equals(node.Value) : false;
        SpeakerManager.GetInstance().Stop(SpeakerManager.SPEAKING_ID, !once);
      }

      // Stop Music
      var stop = xnav.SelectSingleNode("/SML/action/@stop");
      if (stop != null) {
        SpeakerManager.GetInstance().Stop(stop.Value, false);
      }

      // Play Music
      var play = xnav.SelectSingleNode("/SML/action/@play");
      if (play != null) {
        SpeakerManager.GetInstance().Play(play.Value, false);
      }
    }

  }
}
