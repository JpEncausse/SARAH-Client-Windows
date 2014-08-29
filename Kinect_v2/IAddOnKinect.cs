using System;
using System.Collections.Generic;
using System.Drawing;

using Microsoft.Kinect;

namespace net.encausse.sarah.kinect2 {
  public interface IAddOnKinect : IAddOn {

    void InitDepthFrame(string device, ushort[] data, Timestamp state, int width, int height);

    void InitBodyFrame(string device, IList<Body> bodies, Timestamp stamp);

  }
}
