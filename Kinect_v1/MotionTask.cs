using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;


namespace net.encausse.sarah.kinect1 {
  public class MotionTask : AbstractAddOnTask {

    private int threshold;
    private TimeSpan timeout;

    public MotionTask(TimeSpan dueTime, TimeSpan interval)
      : base(dueTime, interval) {
      Name = "Motion";
      threshold = ConfigManager.GetInstance().Find("kinect_v1.motion.threshold", 10);
      timeout = TimeSpan.FromSeconds(ConfigManager.GetInstance().Find("kinect_v1.motion.timeout", 10));
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
      NoMotion = new short[Depth.Pixelss.Length];
      depth1 = new short[Depth.Pixelss.Length];
      Array.Copy(Depth.Pixelss, depth1, depth1.Length);
    }

    protected override void DoTask() {
      var tmp = StandBy;

      Motion = CompareDepth(depth1, Depth.Pixelss);
      Array.Copy(Depth.Pixelss, depth1, depth1.Length); // Backup

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

  }
}
