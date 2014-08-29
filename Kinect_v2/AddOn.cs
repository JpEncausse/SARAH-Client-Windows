﻿using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Controls;


namespace net.encausse.sarah.kinect2 {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "Kinect2";
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

    // -------------------------------------------
    //  UI
    // -------------------------------------------

    public override void HandleSidebar(string device, StackPanel sidebar) {
      base.HandleSidebar(device, sidebar);
      KinectManager.GetInstance().HandleSidebar(device, sidebar);
    }

    public override void RepaintColorFrame(string device, byte[] bgr, int width, int height) {
      base.RepaintColorFrame(device, bgr, width, height);
      KinectManager.GetInstance().RepaintColorFrame(device, bgr, width, height);
    }

  }
}
