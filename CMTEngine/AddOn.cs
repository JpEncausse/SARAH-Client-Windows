using System;

namespace net.encausse.sarah.cmt {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "CMT";
    }

    // -------------------------------------------
    //  TASKS
    // -------------------------------------------

    public override AbstractAddOnTask NewTask(string device) {
      var dueTime = TimeSpan.FromMilliseconds(200);
      var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("cmt.threshold", 30));
      return new AddOnTask(dueTime, interval);
    }

  }
}
