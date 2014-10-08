using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using ZXing;


namespace net.encausse.sarah.qrcode {
  public class AddOnTask : AbstractAddOnTask {

    public AddOnTask(TimeSpan dueTime, TimeSpan interval)
      : base(dueTime, interval) {
      Name = "QRCode";
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    private BarcodeReader reader;
    protected override void InitTask() {
      reader = new BarcodeReader {
        AutoRotate = true,
        TryHarder = true,
        PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
      };

      buffer = new Image<Bgra, Byte>(Color.Width, Color.Height);
    }

    private NBody[] cache = new NBody[6];
    private Image<Bgra, Byte> buffer;
    protected override void DoTask() {

      buffer.Bytes = Color.Pixels;

      foreach (var body in Body.Cache(cache)) {
        if (body == null) { continue; }
        if (!body.IsTracked()) { continue; }
        
        var left  = body.GetJoint(NJointType.HandLeft).Area;
        if (left.Width > 0) {
          var tmp = buffer.Copy(left).Convert<Gray, Byte>().PyrUp().PyrDown();
          var qrcode = CheckQRCode(tmp.Bytes, left.Width, left.Height);
          if (qrcode != null) Log(qrcode);
        }

        var right = body.GetJoint(NJointType.HandRight).Area;
        if (right.Width > 0) {
          var tmp = buffer.Copy(right).Convert<Gray, Byte>().PyrUp().PyrDown();
          var qrcode = CheckQRCode(tmp.Bytes, right.Width, right.Height);
          if (qrcode != null) Log(qrcode);
        }
      }
    }

    public String CheckQRCode(byte[] data, int w, int h) {

      Result result = reader.Decode(data, w, h, RGBLuminanceSource.BitmapFormat.Gray8);
      if (result == null) { return null; }

      String type = result.BarcodeFormat.ToString();
      return result.Text;
    }

  }
}
