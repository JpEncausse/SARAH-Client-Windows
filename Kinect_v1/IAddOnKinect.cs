using System;
using System.Collections.Generic;
using System.Drawing;

using Microsoft.Kinect;

namespace net.encausse.sarah.kinect1 {
  public interface IAddOnKinect : IAddOn {

    void InitDepthFrame(string device, short[] data, Timestamp state, int width, int height, int min, int max);
    void InitDepthFrame(string device, DepthImagePixel[] pixels, short[] data, Timestamp state, int width, int height, int min, int max);
    void InitSkeletonFrame(string device, Skeleton[] data, Timestamp stamp, int width, int height);

  }
}
