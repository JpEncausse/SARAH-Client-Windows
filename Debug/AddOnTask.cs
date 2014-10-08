using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

using Emgu.CV;
using Emgu.CV.Structure;


namespace net.encausse.sarah.debug {
  public class AddOnTask : AbstractAddOnTask {

    public AddOnTask(TimeSpan dueTime, TimeSpan interval)
      : base(dueTime, interval) {
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

      var factor = Color.Width / ConfigManager.GetInstance().Find("debug.resize", Color.Width);
      var frame = new Image<Bgra, Byte>(Color.Width, Color.Height);
      frame.Bytes = Color.Pixels;
      frame = frame.Resize(Color.Width / factor, Color.Height / factor, Emgu.CV.CvEnum.Inter.Cubic);
      frame.Save(path);

      return path;
    }
  }
}
