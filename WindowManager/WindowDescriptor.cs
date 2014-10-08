using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace net.encausse.sarah.window {
  public class WindowDescriptor {

    public string Name;
    public WindowDescriptor(String name) {
      this.Name = name;
    }
    
    public List<DeviceFrame> Frames = new List<DeviceFrame>();
    public DeviceFrame Find(Type type) {
      foreach (var frame in Frames) {
        if (frame.GetType() == type) { return frame; }
      }
      return null;
    }

  }
}
