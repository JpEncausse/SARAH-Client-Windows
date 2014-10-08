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
    public DetectTask(TimeSpan dueTime, TimeSpan interval, FaceHelper helper)
      : base(dueTime, interval) {
      Name = "Detection";
      Helper = helper;
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    protected override void InitTask() { }

    protected override void DoTask() {
      Helper.Detect(Color.Pixels);
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
