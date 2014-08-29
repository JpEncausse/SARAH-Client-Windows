using System;

namespace net.encausse.sarah {
  public class Timestamp {
    public Timestamp() {
      Time = DateTime.Now;
    }
    public DateTime Time { get; set; }
  }
}
