using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Controls;

namespace net.encausse.sarah.qrcode {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "QRCode";
    }

    // -------------------------------------------
    //  TASKS
    // -------------------------------------------

    public override AbstractAddOnTask NewTask(string device) {
      var dueTime = TimeSpan.FromMilliseconds(200);
      var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("qrcode.threshold", 30));
      return new AddOnTask(dueTime, interval);
    }

  }
}
