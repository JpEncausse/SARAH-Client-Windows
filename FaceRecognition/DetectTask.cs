using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace net.encausse.sarah.face {
  public class DetectTask : AbstractAddOnTask {

    private FaceHelper Helper;
    public DetectTask(string device, FaceHelper helper)  : base(device) {
      Name = "Detection";
      Helper = helper;
    }

    private ICollection<NBody> bodies;
    public void SetBodies(ICollection<NBody> data) { 
      bodies = data;
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    protected override void InitTask() { }

    protected override void DoTask() {
      Helper.Detect(Color);
    }

    // -------------------------------------------
    //  UI
    // -------------------------------------------

    public override void RepaintColorFrame(byte[] data, int width, int height) {
      base.RepaintColorFrame(data, width, height);
      Helper.DrawFaces(data, width, height);
    }

  }
}
