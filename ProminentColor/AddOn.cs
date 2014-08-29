using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Controls;

namespace net.encausse.sarah.color {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "ProminentColor";
    }

    // ------------------------------------------
    //  LIFE CYCLE
    // ------------------------------------------

    public override void Dispose() {
      foreach(var task in Tasks.Values){
        task.Dispose();
      }
      Tasks.Clear();
      base.Dispose();
    }


    // -------------------------------------------
    //  UI
    // -------------------------------------------

    public override void HandleSidebar(string device, StackPanel sidebar) {
      if (!Tasks.ContainsKey(device) || Tasks[device] == null) { return; }
      Tasks[device].HandleSidebar(sidebar);
    }

    public override void RepaintColorFrame(string device, byte[] bgr, int width, int height) {
      if (!Tasks.ContainsKey(device) || Tasks[device] == null) { return; }
      Tasks[device].RepaintColorFrame(bgr, width, height);
    }

    // -------------------------------------------
    //  CAMERA
    // -------------------------------------------

    public IDictionary<string, AddOnTask> Tasks = new Dictionary<string, AddOnTask>();

    public override void InitColorFrame(string device, byte[] data, Timestamp stamp, int width, int height, int fps) {
      base.InitColorFrame(device, data, stamp, width, height, fps);

      var dueTime  = TimeSpan.FromMilliseconds(200);
      var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("color.threshold", 30));
      
      var task = new AddOnTask(device);
      task.SetColor(data, stamp, width, height, fps);
      task.Start(dueTime, interval);

      Tasks.Add(device, task);
    }

  }
}
