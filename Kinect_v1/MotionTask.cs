using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;


namespace net.encausse.sarah.kinect1 {
  public class MotionTask : AbstractAddOnTask {

    private int threshold;
    private TimeSpan timeout;

    public MotionTask(string device) : base(device) {
      Name = "Motion";
      threshold = ConfigManager.GetInstance().Find("kinect_v1.motion.threshold", 10);
      timeout = TimeSpan.FromSeconds(ConfigManager.GetInstance().Find("kinect_v1.motion.timeout", 10));
    }

    protected short[] Depth;
    public void SetDepth(short[] depth, Timestamp stamp, int width, int height) {
      Depth = depth;
      Stamp = stamp;
      Width = width;
      Height = height;
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    public  bool    StandBy  { get; set; }
    public  int     Motion   { get; set; }    
    public  short[] NoMotion { get; set; }
    private short[] depth1;
    private Stopwatch StandByWatch;

    protected override void InitTask() {
      StandByWatch = new Stopwatch();
      NoMotion = new short[Depth.Length];
      depth1   = new short[Depth.Length];
      Array.Copy(Depth, depth1, depth1.Length);
    }

    protected override void DoTask() {
      var tmp = StandBy;

      Motion = CompareDepth(depth1, Depth);
      Array.Copy(Depth, depth1, depth1.Length); // Backup
      if (Motion > threshold) {
        StandByWatch.Restart();
        StandBy = false;
      }
      else if (StandByWatch.Elapsed > timeout) {
        StandByWatch.Stop();
        StandBy = true;
      }
      if (tmp != StandBy) {
        if (StandBy){ Array.Copy(depth1, NoMotion, NoMotion.Length); }
        AddOnManager.GetInstance().MotionDetected(Device, !StandBy);
      }
    }

    // -------------------------------------------
    //  UTIL
    // -------------------------------------------

    public static int CompareDepth(short[] depth1, short[] depth2) {

      int threshold = 50 << 3; // DepthImageFrame.PlayerIndexBitmaskWidth
      int count = 0;

      for (int i = 0; i < depth2.Length; i++) {
        if (Math.Abs(depth1[i] - depth2[i]) > threshold) {
          count++;
        }
      }
      return count * 100 / depth1.Length;
    }

    // -------------------------------------------
    //  BACKUP
    // -------------------------------------------

/*
    public void MaskDepth(short[] depth, byte[] data) {
     
     for (int i = 0; i < depth.Length; i++) {

       var prev = NoMotion[i] >> 3; // DepthImageFrame.PlayerIndexBitmaskWidth
       var next = depth[i] >> 3;    // DepthImageFrame.PlayerIndexBitmaskWidth
       if (prev > 0 || next > 0) { if (prev - next > 50) { continue; }}

       var scale = 640 / Width;
       var row = 640 * 4;
       var x = (i % Width);
       var y = (i / Width);

       int px = x * 4 * scale + y * row * scale;
       for (int j = 0; j < scale; j++) {
         for (int k = 0; k < scale; k++) {
           MaskDepthPixel(data, px + k * 4 + row * j);
         }
       }
     }
    }

    public void MaskDepthPixel(byte[] data, int px){
      if (px < 0 || px > data.Length - 4) return;
      data[px + 0] = 0;
      data[px + 1] = 0;
      data[px + 2] = 0;
      data[px + 3] = 255;
    }
*/

  }
}
