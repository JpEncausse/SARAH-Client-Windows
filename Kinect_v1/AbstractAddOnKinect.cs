using System;
using System.Drawing;
using System.Collections.Generic;

using Microsoft.Kinect;

namespace net.encausse.sarah.kinect1 {
  public abstract class AbstractAddOnKinect : AbstractAddOn, IAddOnKinect {

    public virtual void InitDepthFrame(string device, short[] data, Timestamp state, int width, int height, int min, int max) { }

    public virtual void InitDepthFrame(string device, DepthImagePixel[] pixels, short[] data, Timestamp state, int width, int height, int min, int max) { }

    public virtual void InitSkeletonFrame(string device, Skeleton[] data, Timestamp stamp, int width, int height) { }
  }
}
