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

    // -------------------------------------------
    //  TASKS
    // -------------------------------------------

    public override AbstractAddOnTask NewTask(string device) {
      var dueTime = TimeSpan.FromMilliseconds(200);
      var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("color.threshold", 30));
      return new AddOnTask(dueTime, interval);
    }

  }
}
