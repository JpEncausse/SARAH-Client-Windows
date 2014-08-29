using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Media3D;

namespace net.encausse.sarah {

  // ------------------------------------------
  //  BODY
  // ------------------------------------------

  public class NBody {

    public Object TrackingId { get; set; }
    public NBody(Object trackingId) {
      TrackingId = trackingId;

      Joints[NJointType.SpineBase] = null;
      Joints[NJointType.SpineMid] = null;
      Joints[NJointType.Neck] = null;
      Joints[NJointType.Head] = null;
      Joints[NJointType.ShoulderLeft] = null;
      Joints[NJointType.ElbowLeft] = null;
      Joints[NJointType.WristLeft] = null;
      Joints[NJointType.HandLeft] = null;
      Joints[NJointType.ShoulderRight] = null;
      Joints[NJointType.ElbowRight] = null;
      Joints[NJointType.WristRight] = null;
      Joints[NJointType.HandRight] = null;
      Joints[NJointType.HipLeft] = null;
      Joints[NJointType.KneeLeft] = null;
      Joints[NJointType.AnkleLeft] = null;
      Joints[NJointType.FootLeft] = null;
      Joints[NJointType.HipRight] = null;
      Joints[NJointType.KneeRight] = null;
      Joints[NJointType.AnkleRight] = null;
      Joints[NJointType.FootRight] = null;
      Joints[NJointType.SpineShoulder] = null;
      Joints[NJointType.HandTipLeft] = null;
      Joints[NJointType.ThumbLeft] = null;
      Joints[NJointType.HandTipRight] = null;
      Joints[NJointType.ThumbRight] = null;
    }

    public NTrackingState Tracking { get; set; }
    public bool IsTracked() {
      return Tracking != NTrackingState.NotTracked;
    }

    public IDictionary<NJointType, NJoint> Joints = new Dictionary<NJointType, NJoint>();
    public NJoint GetJoint(NJointType ntype) {
      if (Joints.ContainsKey(ntype) && Joints[ntype] != null) {
        return Joints[ntype];
      }
      
      var njoint = new NJoint(ntype);
      Joints[ntype] = njoint;
      return njoint;
    }

    public String Name { get; set; }
  }

  // ------------------------------------------
  //  JOINT
  // ------------------------------------------

  public class NJoint {

    public NJoint(NJointType type) { 
      Type = type;
    }

    public NJointType     Type       { get; set; }
    public NTrackingState Tracking;
    public Point Position2D = new Point(0, 0);
    public Point3D Position3D = new Point3D(0, 0, 0);
    public Rectangle Area = new Rectangle();
    public void SetPosition3D(double x, double y, double z) { 
      Position3D.X = x;
      Position3D.Y = y;
      Position3D.Z = z;
    }

    public void SetPosition2D(float x, float y) {
      Position2D.X = (int) x;
      Position2D.Y = (int) y;
    }

    public void SetJointRadius(int radius) {

      // if (radius == 0) { rect.Width = 0; rect.Height = 0; }
      if (radius == 0) { return; }

      var x = Position2D.X - radius;
      var y = Position2D.Y - radius;
      var w = radius * 2;
      var h = radius * 2;

      Area.X = (int)Math.Sqrt((x * x + Area.X * Area.X)/2);
      Area.Y = (int)Math.Sqrt((y * y + Area.Y * Area.Y)/2);
      Area.Width = (int)Math.Sqrt((w * w + Area.Width * Area.Width)/2);
      Area.Height = (int)Math.Sqrt((h * h + Area.Height * Area.Height)/2);
    }

    public bool IsTracked() {
      return Tracking != NTrackingState.NotTracked;
    }
  }

  // ------------------------------------------
  //  ENUMERATE
  // ------------------------------------------

  public enum NTrackingState {
    NotTracked = 0,
    Inferred = 1,
    Tracked = 2,
  }

  public enum NJointType {
    SpineBase = 0,
    SpineMid = 1,
    Neck = 2,
    Head = 3,
    ShoulderLeft = 4,
    ElbowLeft = 5,
    WristLeft = 6,
    HandLeft = 7,
    ShoulderRight = 8,
    ElbowRight = 9,
    WristRight = 10,
    HandRight = 11,
    HipLeft = 12,
    KneeLeft = 13,
    AnkleLeft = 14,
    FootLeft = 15,
    HipRight = 16,
    KneeRight = 17,
    AnkleRight = 18,
    FootRight = 19,
    SpineShoulder = 20,
    HandTipLeft = 21,
    ThumbLeft = 22,
    HandTipRight = 23,
    ThumbRight = 24,
  }

  // SDK 1.x
  /*
  public enum JointType {
    HipCenter = 0,
    Spine = 1,
    ShoulderCenter = 2,
      Head = 3,
      ShoulderLeft = 4,
      ElbowLeft = 5,
      WristLeft = 6,
      HandLeft = 7,
      ShoulderRight = 8,
      ElbowRight = 9,
      WristRight = 10,
      HandRight = 11,
      HipLeft = 12,
      KneeLeft = 13,
      AnkleLeft = 14,
      FootLeft = 15,
      HipRight = 16,
      KneeRight = 17,
      AnkleRight = 18,
      FootRight = 19,
  }
  */
}
