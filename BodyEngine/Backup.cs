using System;
using System.Collections.Generic;
using System.Drawing;

using System.Windows.Media.Media3D;

namespace net.encausse.sarah.body {
  /*
  public class Backup : AbstractAddOnTask {

    public AddOnTask(string device) : base(device) {
      Name = "Body";
    }

    // -------------------------------------------
    //  SKELETON
    // -------------------------------------------

    protected SkeletonHelper Helper;
    protected Dictionary<int, Dictionary<JointType, Point>> Joints2D;
    protected Skeleton[] Skeletons;

    public void SetSkeleton(Skeleton[] Skeletons, Timestamp stamp, Dictionary<int, Dictionary<JointType, Point>> joints2D, int width, int height) {
      this.Skeletons = Skeletons;
      this.Joints2D = joints2D;
      this.Helper = new SkeletonHelper(width, height);
      this.Width = width;
      this.Height = height;
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    private List<Log> Logs; 
    protected override void InitTask() {
      Logs = new List<Log>();
    }

    protected override void DoTask() {
      foreach (var skeleton in Skeletons) {
        if (skeleton.TrackingState == SkeletonTrackingState.Tracked) {
          DoTask(skeleton, JointType.HandRight);
          DoTask(skeleton, JointType.HandLeft);
        }
      }
    }

    private void DoTask(Skeleton skeleton, JointType type) {

      var hand = skeleton.Joints[type];
      if (hand.TrackingState == JointTrackingState.NotTracked) { return; }

      var joints = ToList(skeleton.Joints, type);

      // Retrieve closest Joint
      var closest1 = Closest(hand, joints, 200);
      if (closest1 == null) { return; }

      // Retrieve next closest Joint
      var closest2 = Closest(hand, joints, closest1);
      if (closest2 == null) {
        Logs.Add(new Log { delta = 0, start = ((Joint)closest1).JointType });
        return;
      }

      // Compute delta % to first joint
      var delta = Projection(hand, closest1, closest2);
      if (delta < 1) {
        Logs.Add(new Log { delta = delta, start = ((Joint)closest1).JointType, end = ((Joint)closest2).JointType });
      }
    }
    
    // -------------------------------------------
    //  UTILITY
    // -------------------------------------------

    public static List<Joint> ToList(JointCollection joints, JointType type) {
      var list = new List<Joint>();
      foreach (Joint joint in joints) {
        if (joint.TrackingState == JointTrackingState.NotTracked) { continue; }
        if (joint.JointType == type) { continue; }

        if (type == JointType.HandRight) {
          if (joint.JointType == JointType.WristRight) { continue; }
          if (joint.JointType == JointType.ElbowRight) { continue; }
        } else {
          if (joint.JointType == JointType.WristLeft)  { continue; }
          if (joint.JointType == JointType.ElbowLeft)  { continue; }
        }
        list.Add(joint);
      }
      return list;
    }

    // Return the % length from joint1 to hand projection
    public static double Projection(Joint hand, Joint? joint1, Joint? joint2) {

      if (joint1 == null) { return 0; }
      if (joint2 == null) { return 0; }

      var a = 1000 * SkeletalExtensions.Length((Joint) joint1, (Joint) joint2);
      var b = 1000 * SkeletalExtensions.Length((Joint) hand,   (Joint) joint1);
      var c = 1000 * SkeletalExtensions.Length((Joint) hand,   (Joint) joint2);
      var h = b * c / a;
      var cosa = Math.Acos(b / h);
      var d = Math.Tan(cosa) * b;

      return d / a;
    }

    public static Joint? Closest(Joint joint1, IEnumerable<Joint> joints, double threshold) {
      Joint? joint2 = null;
      double latest = double.PositiveInfinity;
      foreach (Joint tmp in joints) {
        var d1 = SkeletalExtensions.Length(joint1, tmp) * 1000;
        if (d1 < latest && d1 < threshold) {
          joint2 = tmp;
          latest = d1;
        }
      }
      return joint2;
    }

    public static Joint? Closest(Joint joint1, IEnumerable<Joint> joints, Joint? joint2) {
      if (joint2 == null) { return null; }
      
      Joint? joint3 = null;
      double latest = double.PositiveInfinity;
      foreach (Joint tmp in joints) {
        if (tmp == joint2) { continue; }
        var d1 = SkeletalExtensions.Length(joint1, tmp);
        var d2 = SkeletalExtensions.Length((Joint)joint2, tmp);
        if (d1 > d2 && d1 < latest) {
          joint3 = tmp;
          latest = d1;
        }
      }
      return joint3;
      
    }

    // -------------------------------------------
    //  UI
    // -------------------------------------------

    public override void RepaintColorFrame(byte[] data, int width, int height) {
      base.RepaintColorFrame(data, width, height);
      Helper.DrawSkeletons(data, Skeletons, Joints2D, new List<Log>(Logs));
    }

  }

  // -------------------------------------------
  //  LOGS
  // -------------------------------------------

    public struct Log {
      public double delta { get; set; }
      public JointType start { get; set; }
      public JointType end { get; set; }
      public DateTime timestamp { get; set; }
    }
*/
}
