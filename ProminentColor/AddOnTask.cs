using System;

namespace net.encausse.sarah.color {
  public class AddOnTask : AbstractAddOnTask {

    public AddOnTask(string device) : base(device) {
        Name = "Color";
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    ColorHelper helper;
    protected override void InitTask() {
      helper = new ColorHelper();
    }

    public RGB RGB { get; set; }
    protected override void DoTask() {
      var rgb = helper.GetMostProminentColor(this.Color);
      if (RGB == null || rgb.r > 50 && rgb.g > 50 && rgb.b > 50) { RGB = rgb; }
    }

    // -------------------------------------------
    //  UI
    // -------------------------------------------

    public override void RepaintColorFrame(byte[] data, int width, int height) {
      base.RepaintColorFrame(data, width, height);

      for (var j = 0; j < 50; j++) { 
        for (var i = 0; i < 50; i++) {
          var x = i * 4 + j * width * 4;
          data[x]     = (byte) RGB.b;
          data[x + 1] = (byte) RGB.g;
          data[x + 2] = (byte) RGB.r;
        }
      }
    }

  }
}
