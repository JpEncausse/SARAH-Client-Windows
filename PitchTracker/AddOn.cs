using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;
using Pitch;

namespace net.encausse.sarah.pitch {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "PitchTracker";
    }

    // ------------------------------------------
    //  AddOn life cycle
    // ------------------------------------------

    private PitchTracker tracker;
    public override void Start() {
      base.Start();
      tracker = new PitchTracker();
      tracker.SampleRate = 16000.0f;
      tracker.PitchDetected += OnPitchDetected;
    }

    // ------------------------------------------
    //  Audio Management
    // ------------------------------------------

    public override void BeforeSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      base.BeforeSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);

      byte[] audioBytes = null;
      stream.Position = 0;
      using (MemoryStream audioStream = new MemoryStream()) {
        stream.CopyTo(audioStream);
        audioStream.Position = 0;
        audioBytes = audioStream.ToArray();
      }

      float[] audioBuffer = new float[audioBytes.Length / 2];
      for (int i = 0, j = 0; i < audioBytes.Length / 2; i += 2, j++) {

        // convert two bytes to one short
        short s = BitConverter.ToInt16(audioBytes, i);

        // convert to range from -1 to (just below) 1
        audioBuffer[j] = s / 32768.0f;
      }

      // Reset
      tracker.Reset();
      pitch.Clear();

      // Process
      tracker.ProcessBuffer(audioBuffer);

      // Notify
      AddOnManager.GetInstance().HandleProfile(device, "pitch", pitch.Mean());
    }


    // ------------------------------------------
    //  PITCH
    // ------------------------------------------

    private List<double> pitch = new List<double>();
    private void OnPitchDetected(PitchTracker sender, PitchTracker.PitchRecord pitchRecord) {
      // During the call to PitchTracker.ProcessBuffer, this event will be fired zero or more times,
      // depending how many pitch records will fit in the new and previously cached buffer.
      //
      // This means that there is no size restriction on the buffer that is passed into ProcessBuffer.
      // For instance, ProcessBuffer can be called with one large buffer that contains all of the
      // audio to be processed, or just a small buffer at a time which is more typical for realtime
      // applications. This PitchDetected event will only occur once enough data has been accumulated
      // to do another detect operation.
      /*
      cfg.logInfo("PITCH", "MidiCents: " + pitchRecord.MidiCents
                         + " MidiNote: " + pitchRecord.MidiNote
                         + " Pitch: " + pitchRecord.Pitch
                         + " RecordIndex: " + pitchRecord.RecordIndex);*/
      double d = pitchRecord.Pitch;
      if (d > 0) { pitch.Add(d); }
    }

  }
}
