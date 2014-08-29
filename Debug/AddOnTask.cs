using System;
using System.Drawing;
using System.Drawing.Imaging;

using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;

namespace net.encausse.sarah.debug {
  public class AddOnTask : AbstractAddOnTask {

    public AddOnTask(string device) : base(device) {
      Name = "Debug";
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    protected override void InitTask() { }
    protected override void DoTask() { }

    // -------------------------------------------
    //  PICTURE
    // -------------------------------------------

    public String TakePicture() { 
      var date = DateTime.Now.ToString("yyyy.M.d_hh.mm.ss");
      var path = ConfigManager.GetInstance().Find("debug.snapshot", "");
      path = path.Replace("${date}", date);
      path = path.Replace("${device}", Device);
      return TakePicture(path);
    }

    public String TakePicture(string path) {
      if (path == null) { return TakePicture(); }
      if (File.Exists(path)) { File.Delete(path); }

      var factor = Width / ConfigManager.GetInstance().Find("debug.resize", Width); ;
      var frame = new Image<Bgra, Byte>(Width, Height);
      frame.Bytes = Color;
      frame = frame.Resize(Width / factor, Height / factor, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
      frame.Save(path);

      return path;
    }
  }
}
