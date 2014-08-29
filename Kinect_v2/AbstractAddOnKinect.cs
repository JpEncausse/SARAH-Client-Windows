using System;
using System.Drawing;
using System.Collections.Generic;

using Microsoft.Kinect;

namespace net.encausse.sarah.kinect2 {
  public abstract class AbstractAddOnKinect : AbstractAddOn, IAddOnKinect {

    public virtual void InitDepthFrame(string device, ushort[] data, Timestamp state, int width, int height) { }

    public virtual void InitBodyFrame(string device, IList<Body> bodies, Timestamp stamp) { }
  }
}
