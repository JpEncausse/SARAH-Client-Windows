using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;


namespace net.encausse.sarah.kinect2 {
  public class MotionTask : AbstractAddOnTask {

    private int threshold;
    private TimeSpan timeout;

    public MotionTask(TimeSpan dueTime, TimeSpan interval)
      : base(dueTime, interval) {
      Name = "Motion";
      threshold = ConfigManager.GetInstance().Find("kinect_v2.motion.threshold", 10);
      timeout = TimeSpan.FromSeconds(ConfigManager.GetInstance().Find("kinect_v2.motion.timeout", 10));
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    public  bool    StandBy  { get; set; }
    public  int     Motion   { get; set; }    
    public  ushort[] NoMotion { get; set; }
    private ushort[] depth1;
    private Stopwatch StandByWatch;

    protected override void InitTask() {
      StandByWatch = new Stopwatch();
      NoMotion = new ushort[Depth.Pixels.Length];
      depth1 = new ushort[Depth.Pixels.Length];
      Array.Copy(Depth.Pixels, depth1, depth1.Length);
    }

    protected override void DoTask() {
      var tmp = StandBy;

      Motion = CompareDepth(depth1, Depth.Pixels);
      Array.Copy(Depth.Pixels, depth1, depth1.Length); // Backup

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

    public static int CompareDepth(ushort[] depth1, ushort[] depth2) {

      int threshold = 50;
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
