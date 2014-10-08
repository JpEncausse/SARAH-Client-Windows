using System;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.Tracking;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace net.encausse.sarah.cmt {
  public class AddOnTask : AbstractAddOnTask {

    public AddOnTask(TimeSpan dueTime, TimeSpan interval)
      : base(dueTime, interval) {
      Name = "CMT";
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    private Tracker tracker;
    private Image<Bgra, Byte> buffer; 

    private Rectangle current = Rectangle.Empty;
    private Rectangle debug = Rectangle.Empty;
    private int ratio = 1;

    protected override void InitTask() {
      buffer = new Image<Bgra, Byte>(Color.Width, Color.Height);
      ratio = Color.Width / ConfigManager.GetInstance().Find("cmt.resize", 640);
      canvas = new Image<Bgra, Byte>(Color.Width, Color.Height);
    }
    
    protected override void DoTask() {
      buffer.Bytes = Color.Pixels;
      var scale = buffer.Resize(Color.Width / ratio, Color.Height / ratio, Emgu.CV.CvEnum.Inter.Cubic);

      if (select != Rectangle.Empty) {
        Log("Initialize ...");
        current = new Rectangle(select.X / ratio, select.Y / ratio, select.Width / ratio, select.Height / ratio);
        debug = new Rectangle(select.X, select.Y, select.Width , select.Height);

        // MIL, BOOSTING, MEDIANFLOW, TLD, CMT
        Mat mat = CvInvoke.CvArrToMat(scale, true);
        tracker = new Tracker(ConfigManager.GetInstance().Find("cmt.algo", "CMT"));
        tracker.init(mat, current);
        select = Rectangle.Empty;
      }

      else if (current != Rectangle.Empty) {
        Mat mat = CvInvoke.CvArrToMat(scale, true);
        tracker.update(mat, ref current);﻿
      }
    }

    // -------------------------------------------
    //  UI
    // -------------------------------------------
    
    private Image<Bgra, Byte> canvas;
    private Bgra brdBlue  = new Bgra(255, 0, 0, 0);
    private Bgra brdGreen = new Bgra(0, 255, 255, 0);
    public override void RepaintColorFrame(byte[] data, int width, int height) {
      base.RepaintColorFrame(data, width, height);

      canvas.Bytes = data;
      if (current != Rectangle.Empty) {
        canvas.Draw(new Rectangle(current.X * ratio, current.Y * ratio, current.Width * ratio, current.Height * ratio), brdGreen, 4);
      }

      if (debug != Rectangle.Empty) {
        canvas.Draw(debug, brdBlue, 4);
      }

      Buffer.BlockCopy(canvas.Bytes, 0, data, 0, data.Length);
    }
    

    private Rectangle select = Rectangle.Empty;
    public override void HandleSelection(Rectangle rect) {
      base.HandleSelection(rect);
      this.select = rect;
    }
    
  }
}
