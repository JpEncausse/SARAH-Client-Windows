using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;

namespace net.encausse.sarah.kinect1 {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "Kinect1";
    }

    // ------------------------------------------
    //  LIFE CYCLE
    // ------------------------------------------

    public override void Start() {
      base.Start();
      KinectManager.GetInstance().InitSensors();
    }

    public override void Setup() {
      base.Setup();
      KinectManager.GetInstance().StartSensors();
    }

    public override void Dispose() {
      base.Dispose();
      KinectManager.GetInstance().Dispose();
    }

    public override bool Ready() {
      var ready = KinectManager.GetInstance().Ready();
      Host.Log(this, "Ready: " + ready);
      return ready;
    }

  }
}
