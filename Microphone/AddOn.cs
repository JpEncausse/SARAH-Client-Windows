using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;

using NAudio.Wave;

namespace net.encausse.sarah.microphone {
  public class AddOn : AbstractAddOn {

    public AddOn() : base(){
      Name = "Microphone";
    }

    // ------------------------------------------
    //  AddOn life cycle
    // ------------------------------------------

    private WaveInEvent waveIn = null;
    private Stream buffer = null;

    public override void Setup() {
      base.Setup();

      int waveInDevices = WaveIn.DeviceCount;
      for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++) {
        WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
        Host.Log(this, "Device " + waveInDevice + ": " + deviceInfo.ProductName + ", " + deviceInfo.Channels + " channels");
      }

      waveIn = new WaveInEvent();
      waveIn.DeviceNumber = ConfigManager.GetInstance().Find("microphone.device", 0);
      waveIn.WaveFormat = new WaveFormat(16000, 2);
      waveIn.DataAvailable += waveIn_DataAvailable;

      buffer = new StreamBuffer();
      waveIn.StartRecording();

      double confidence = ConfigManager.GetInstance().Find("microphone.confidence", 0.6);
      AddOnManager.GetInstance().AddAudioSource("Microphone", buffer, "Microphone", null, confidence);
    }

    int pos = 0;
    void waveIn_DataAvailable(object sender, WaveInEventArgs e) {
      var tmp = buffer.Position;
      buffer.Write(e.Buffer, 0, e.BytesRecorded);
      buffer.Position = tmp;
      pos += e.BytesRecorded;
    }

    public override void Dispose() {
      base.Dispose();
      
      if (waveIn != null) {
        Host.Log(this,"Dispose WaveIn");
        waveIn.StopRecording();
      }

      if (buffer != null) {
        Host.Log(this, "Dispose Stream");
        buffer.Dispose();
      }
    }
  }
}