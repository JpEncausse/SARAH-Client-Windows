using System;
using Microsoft.Kinect;
using System.Collections.Generic;

namespace net.encausse.sarah {
  /// <summary>
  /// Provides some common functionality on skeletal data.
  /// </summary>
  public static class SkeletalExtensions {
    #region Public methods

    /// <summary>
    /// Retruns the height of the specified skeleton.
    /// </summary>
    /// <param name="skeleton">The specified user skeleton.</param>
    /// <returns>The height of the skeleton in meters.</returns>
    public static double Height(this Skeleton skeleton) {
      const double HEAD_DIVERGENCE = 0.1;

      var head = skeleton.Joints[JointType.Head];
      var neck = skeleton.Joints[JointType.ShoulderCenter];
      var spine = skeleton.Joints[JointType.Spine];
      var waist = skeleton.Joints[JointType.HipCenter];
      var hipLeft = skeleton.Joints[JointType.HipLeft];
      var hipRight = skeleton.Joints[JointType.HipRight];
      var kneeLeft = skeleton.Joints[JointType.KneeLeft];
      var kneeRight = skeleton.Joints[JointType.KneeRight];
      var ankleLeft = skeleton.Joints[JointType.AnkleLeft];
      var ankleRight = skeleton.Joints[JointType.AnkleRight];
      var footLeft = skeleton.Joints[JointType.FootLeft];
      var footRight = skeleton.Joints[JointType.FootRight];

      // Find which leg is tracked more accurately.
      int legLeftTrackedJoints = NumberOfTrackedJoints(hipLeft, kneeLeft, ankleLeft, footLeft);
      int legRightTrackedJoints = NumberOfTrackedJoints(hipRight, kneeRight, ankleRight, footRight);

      double legLength = legLeftTrackedJoints > legRightTrackedJoints ? Length(hipLeft, kneeLeft, ankleLeft, footLeft) : Length(hipRight, kneeRight, ankleRight, footRight);

      return Length(head, neck, spine, waist) + legLength + HEAD_DIVERGENCE;
    }

    /// <summary>
    /// Returns the upper height of the specified skeleton (head to waist). Useful whenever Kinect provides a way to track seated users.
    /// </summary>
    /// <param name="skeleton">The specified user skeleton.</param>
    /// <returns>The upper height of the skeleton in meters.</returns>
    public static double UpperHeight(this Skeleton skeleton) {
      var head = skeleton.Joints[JointType.Head];
      var neck = skeleton.Joints[JointType.ShoulderCenter];
      var spine = skeleton.Joints[JointType.Spine];
      var waist = skeleton.Joints[JointType.HipCenter];

      return Length(head, neck, spine, waist);
    }

    /// <summary>
    /// Returns the length of the segment defined by the specified joints.
    /// </summary>
    /// <param name="p1">The first joint (start of the segment).</param>
    /// <param name="p2">The second joint (end of the segment).</param>
    /// <returns>The length of the segment in meters.</returns>
    public static double Length(Joint p1, Joint p2) {
      return Math.Sqrt(
          Math.Pow(p1.Position.X - p2.Position.X, 2) +
          Math.Pow(p1.Position.Y - p2.Position.Y, 2) +
          Math.Pow(p1.Position.Z - p2.Position.Z, 2));
    }

    /// <summary>
    /// Returns the length of the segments defined by the specified joints.
    /// </summary>
    /// <param name="joints">A collection of two or more joints.</param>
    /// <returns>The length of all the segments in meters.</returns>
    public static double Length(params Joint[] joints) {
      double length = 0;

      for (int index = 0; index < joints.Length - 1; index++) {
        length += Length(joints[index], joints[index + 1]);
      }

      return length;
    }

    /// <summary>
    /// Given a collection of joints, calculates the number of the joints that are tracked accurately.
    /// </summary>
    /// <param name="joints">A collection of joints.</param>
    /// <returns>The number of the accurately tracked joints.</returns>
    public static int NumberOfTrackedJoints(params Joint[] joints) {
      int trackedJoints = 0;

      foreach (var joint in joints) {
        if (joint.TrackingState == JointTrackingState.Tracked) {
          trackedJoints++;
        }
      }

      return trackedJoints;
    }

    /// <summary>
    /// Scales the specified joint according to the specified dimensions.
    /// </summary>
    /// <param name="joint">The joint to scale.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="skeletonMaxX">Maximum X.</param>
    /// <param name="skeletonMaxY">Maximum Y.</param>
    /// <returns>The scaled version of the joint.</returns>
    public static Joint ScaleTo(this Joint joint, int width, int height, float skeletonMaxX, float skeletonMaxY) {
      SkeletonPoint position = new SkeletonPoint() {
        X = Scale(width, skeletonMaxX, joint.Position.X),
        Y = Scale(height, skeletonMaxY, -joint.Position.Y),
        Z = joint.Position.Z
      };

      joint.Position = position;

      return joint;
    }

    /// <summary>
    /// Scales the specified joint according to the specified dimensions.
    /// </summary>
    /// <param name="joint">The joint to scale.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <returns>The scaled version of the joint.</returns>
    public static Joint ScaleTo(this Joint joint, int width, int height) {
      return ScaleTo(joint, width, height, 1.0f, 1.0f);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Returns the scaled value of the specified position.
    /// </summary>
    /// <param name="maxPixel">Width or height.</param>
    /// <param name="maxSkeleton">Border (X or Y).</param>
    /// <param name="position">Original position (X or Y).</param>
    /// <returns>The scaled value of the specified position.</returns>
    private static float Scale(int maxPixel, float maxSkeleton, float position) {
      float value = ((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

      if (value > maxPixel) {
        return maxPixel;
      }

      if (value < 0) {
        return 0;
      }

      return value;
    }

    #endregion
  }
}
