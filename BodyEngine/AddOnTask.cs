using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Emgu.CV;
using Emgu.CV.Structure;

namespace net.encausse.sarah.body {
  public class AddOnTask : AbstractAddOnTask {

    public AddOnTask(TimeSpan dueTime, TimeSpan interval)
      : base(dueTime, interval) {
      Name = "Body";
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    protected override void InitTask() {
      buffer = new Image<Bgra, Byte>(Color.Width, Color.Height);
    }

    protected override void DoTask() {

    }

    // -------------------------------------------
    //  UI
    // -------------------------------------------

    private Int32Rect rect = new Int32Rect(0, 0, 100, 100);
    private PixelFormat format = PixelFormats.Bgra32;
    private int stride;

    private WriteableBitmap prevHead;
    private WriteableBitmap prevLeft;
    private WriteableBitmap prevRight;

    public override void HandleSidebar(StackPanel sidebar) {
      base.HandleSidebar(sidebar);
      if (sidebar.Name != "Footbar") { return; }

      stride     = 100 * format.BitsPerPixel / 8;
      prevHead   = new WriteableBitmap(100, 100, 96, 96, format, null);
      prevLeft   = new WriteableBitmap(100, 100, 96, 96, format, null);
      prevRight  = new WriteableBitmap(100, 100, 96, 96, format, null);

      var thumbHead  = new System.Windows.Controls.Image { Source = prevHead,  Name = "HeadThumb",  Width = 100, Height = 100 };
      var thumbLeft  = new System.Windows.Controls.Image { Source = prevLeft,  Name = "LeftThumb",  Width = 100, Height = 100 };
      var thumbRight = new System.Windows.Controls.Image { Source = prevRight, Name = "RightThumb", Width = 100, Height = 100 };

      sidebar.Children.Add(new GroupBox() { Header = new Label { Content = "Head" }, Content = thumbHead });
      sidebar.Children.Add(new GroupBox() { Header = new Label { Content = "Hand Left" }, Content = thumbLeft });
      sidebar.Children.Add(new GroupBox() { Header = new Label { Content = "Hand Right" }, Content = thumbRight });
    }

    private Font font = new Font("Arial", 24, System.Drawing.FontStyle.Bold);
    private System.Drawing.Brush fontGreen = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 0, 255, 0));

    private Bgra brdBlue  = new Bgra(255, 0, 0, 0);
    private Bgra brdGreen = new Bgra(0, 255, 0, 0);
    private Bgra brdRed   = new Bgra(0, 0, 255, 0);

    private Image<Bgra, Byte> buffer;
    private NBody[] cache = new NBody[6];

    public override void RepaintColorFrame(byte[] data, int width, int height) {
      base.RepaintColorFrame(data, width, height);
      if (Body.Bodies.Count == 0) return;

      // Copy pixels
      buffer.Bytes = data;

      // Draw parts
      foreach (var body in Body.Cache(cache)) {
        if (body == null) { continue; }
        if (!body.IsTracked()) { continue; }

        var head = body.GetJoint(NJointType.Head).Area;
        if (head.Width > 0) {
          prevHead.WritePixels(rect, buffer.Copy(head).Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic).Bytes, stride, 0);
        }

        var left = body.GetJoint(NJointType.HandLeft).Area;
        if (left.Width > 0) {
          prevLeft.WritePixels(rect, buffer.Copy(left).Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic).Bytes, stride, 0);
        }

        var right = body.GetJoint(NJointType.HandRight).Area;
        if (right.Width > 0) {
          prevRight.WritePixels(rect, buffer.Copy(right).Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic).Bytes, stride, 0);
        }

        break;
      } 

      // Draw bodies
      foreach (var body in cache) {
        if (body == null) { continue; }
        if (!body.IsTracked()) { continue;  }
        RepaintColorFrame(buffer, body);
      }

      Buffer.BlockCopy(buffer.Bytes, 0, data, 0, data.Length);
    }

    public void RepaintColorFrame(Image<Bgra, Byte> buffer, NBody body) {

      // Face & Name
      var head = body.GetJoint(NJointType.Head).Area;
      if (head.Width > 0) {
        buffer.Draw(head, brdGreen, 4);
        if (body.Name != null) {
          var txt = new Rectangle(head.X, head.Y + head.Height + 20, head.Width, head.Height); 
          DrawText(buffer, txt, body.Name);
        }
      }

      // Hands
      var right = body.GetJoint(NJointType.HandRight).Area;
      if (right.Width > 0) {
        buffer.Draw(right, brdGreen, 4);
      }

      var left = body.GetJoint(NJointType.HandLeft).Area;
      if (left.Width > 0) {
        buffer.Draw(left, brdGreen, 4);
      }

      // Joints
      foreach (var joint in body.Joints.Values) {
        if (joint == null) { continue; }
        if (joint.Tracking == NTrackingState.NotTracked) { continue; }
        var color = joint.Tracking == NTrackingState.Inferred ? brdRed : brdGreen;
        buffer.Draw(new CircleF(joint.Position2D, 10.0f), color, 10);
      }

      // Torso
      DrawBone(body, NJointType.Head, NJointType.Neck);
      DrawBone(body, NJointType.Neck, NJointType.SpineShoulder);
      DrawBone(body, NJointType.SpineShoulder, NJointType.SpineMid);
      DrawBone(body, NJointType.SpineMid, NJointType.SpineBase);
      DrawBone(body, NJointType.SpineShoulder, NJointType.ShoulderRight);
      DrawBone(body, NJointType.SpineShoulder, NJointType.ShoulderLeft);
      DrawBone(body, NJointType.SpineBase, NJointType.HipRight);
      DrawBone(body, NJointType.SpineBase, NJointType.HipLeft);

      // Right Arm    
      DrawBone(body, NJointType.ShoulderRight, NJointType.ElbowRight);
      DrawBone(body, NJointType.ElbowRight, NJointType.WristRight);
      DrawBone(body, NJointType.WristRight, NJointType.HandRight);
      DrawBone(body, NJointType.HandRight, NJointType.HandTipRight);
      DrawBone(body, NJointType.WristRight, NJointType.ThumbRight);

      // Left Arm
      DrawBone(body, NJointType.ShoulderLeft, NJointType.ElbowLeft);
      DrawBone(body, NJointType.ElbowLeft, NJointType.WristLeft);
      DrawBone(body, NJointType.WristLeft, NJointType.HandLeft);
      DrawBone(body, NJointType.HandLeft, NJointType.HandTipLeft);
      DrawBone(body, NJointType.WristLeft, NJointType.ThumbLeft);

      // Right Leg
      DrawBone(body, NJointType.HipRight, NJointType.KneeRight);
      DrawBone(body, NJointType.KneeRight, NJointType.AnkleRight);
      DrawBone(body, NJointType.AnkleRight, NJointType.FootRight);

      // Left Leg
      DrawBone(body, NJointType.HipLeft, NJointType.KneeLeft);
      DrawBone(body, NJointType.KneeLeft, NJointType.AnkleLeft);
      DrawBone(body, NJointType.AnkleLeft, NJointType.FootLeft);
    }

    private void DrawBone(NBody body, NJointType j0, NJointType j1) {

      NJoint joint0 = body.Joints[j0];
      NJoint joint1 = body.Joints[j1];

      if (j0 == NJointType.Neck && joint0 == null) {
        joint0 = body.Joints[NJointType.SpineShoulder];
      } else if (j1 == NJointType.Neck && joint1 == null) {
        joint1 = body.Joints[NJointType.SpineShoulder];
      }

      if (joint0 == null || joint1 == null) { return; }

      // If we can't find either of these joints, exit
      if (joint0.Tracking == NTrackingState.NotTracked ||
          joint1.Tracking == NTrackingState.NotTracked) { return; }

      // Don't draw if both points are inferred
      if (joint0.Tracking == NTrackingState.Inferred &&
          joint1.Tracking == NTrackingState.Inferred) { return; }

      buffer.DrawPolyline(new System.Drawing.Point[] { joint0.Position2D, joint1.Position2D }, false, brdBlue, 10);
    }

    private void DrawText(Image<Bgra, Byte> img, Rectangle rect, string text) {
      Graphics g = Graphics.FromImage(img.Bitmap);

      int tWidth = (int)g.MeasureString(text, font).Width;
      int x = (tWidth >= rect.Width) ? rect.Left - ((tWidth - rect.Width) / 2)
                                     : (rect.Width / 2) - (tWidth / 2) + rect.Left;

      g.DrawString(text, font, fontGreen, new PointF(x, rect.Top - 18));
    }
  }
}
