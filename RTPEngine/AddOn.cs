using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;

using NAudio.Wave;

namespace net.encausse.sarah.rtp {
  public class AddOn : AbstractAddOn {

    public AddOn() : base(){
      Name = "RTP";
    }

    // ------------------------------------------
    //  AddOn life cycle
    // ------------------------------------------

    private RTPClient client = null;
    private Stream buffer = null;

    public override void Setup() {
      base.Setup();

      buffer = new StreamBuffer();

      int port = ConfigManager.GetInstance().Find("rtp.port", 7887);
      client = new RTPClient(port, buffer);
      client.StartClient();

      double confidence = ConfigManager.GetInstance().Find("rtp.confidence", 0.6);
      AddOnManager.GetInstance().AddAudioSource("RTP", buffer, "RTP", null, confidence);
    }

    public override void Dispose() {
      base.Dispose();
      if (client != null) {
        client.StopClient();
      }

      if (buffer != null) {
        buffer.Dispose();
      }
    }

  }
}