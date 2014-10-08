using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace net.encausse.sarah {

  public abstract class DeviceFrame {
    public Timestamp Stamp;
    public int Width, Height;
    public Object RawData;
  }

  public class ColorFrame : DeviceFrame {
    public byte[] Pixels;
    public int Fps;
    public override String ToString() {
      return "ColorFrame: " + Width + "x" + Height + " at " + Fps;
    }
  }

  public class InfraredFrame : DeviceFrame {
    public ushort[] Pixels;
    public override String ToString() {
      return "InfraredFrame: " + Width + "x" + Height;
    }
  }

  public class DepthFrame : DeviceFrame {
    public ushort[] Pixels;
    public  short[] Pixelss;
    public override String ToString() {
      return "DepthFrame: " + Width + "x" + Height;
    }
  }

  public class BodyFrame : DeviceFrame {
    public List<NBody> Bodies;
    public override String ToString() {
      return "BodyFrame: " + Width + "x" + Height;
    }
    public NBody Find(ulong id) {
      foreach (var nbody in Bodies) {
        if (nbody.TrackingId == id) { return nbody; }
      }
      return null;
    }
    public NBody[] Cache(NBody[] cache) {
      Array.Clear(cache, 0, cache.Length);
      lock (Bodies) {
        Bodies.CopyTo(cache, 0);
      }
      return cache;
    }
  }

  public class BodyIndexFrame : DeviceFrame {
    public byte[] Pixels;
    public override String ToString() {
      return "BodyIndexFrame: " + Width + "x" + Height;
    }
  }
}
