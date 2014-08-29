using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace net.encausse.sarah.face {
  public class RecognizeTask : AbstractAddOnTask {

    private FaceHelper Helper;
    public RecognizeTask(string device, FaceHelper helper)
      : base(device) {
      Name = "Recognize";
      Helper = helper;
    }

    private ICollection<NBody> bodies;
    public void SetBodies(ICollection<NBody> data) {
      bodies = data;
    }

    // -------------------------------------------
    //  TASK
    // -------------------------------------------

    private byte[] thumb = new byte[100 * 100];
    protected override void DoTask() {
      var names = Helper.Recognize();
      if (names == null) { return; }

      for (var i = 0; i < names.Length; i++) {
        if (names[i] == null) continue;
        
        // Try to match a body
        var rect1 = Helper.GetArea(names[i]);
        if (bodies != null) {
          foreach(var body in bodies){
            var head = body.GetJoint(NJointType.Head);
            var rect2 = head.Area;
            if (body.Name != null && names[i] == FaceHelper.UNKNOWN) { continue; }
            if (rect1.IntersectsWith(rect2) || rect1.Contains(rect2) || rect2.Contains(rect1)) {
              head.Area = rect1;
              body.Name = names[i];
            }
          }
        }

        AddOnManager.GetInstance().HandleProfile(Device, "face", names[i]);
      }

      var image = Helper.RecognizeThumbs[0];
      if (thumb != null && image != null) {
        Buffer.BlockCopy(image.Bytes, 0, thumb, 0, thumb.Length);
      }
    }

    // -------------------------------------------
    //  UI
    // -------------------------------------------

    public override void RepaintColorFrame(byte[] bgra, int width, int height) {
      base.RepaintColorFrame(bgra, width, height);

      if (thumb != null) {
        var stride = 100 * format.BitsPerPixel / 8;
        preview.WritePixels(rect, thumb, stride, 0);
      }

      if (lbl != null) { 
        lbl.Content = "Training ("+ (int)Helper.RecognizeDistance +")";
      }
    }

    private PixelFormat format = PixelFormats.Gray8;
    private WriteableBitmap preview;
    private Int32Rect rect = new Int32Rect(0, 0, 100, 100);
    private Label lbl;

    public override void HandleSidebar(StackPanel sidebar) {
      base.HandleSidebar(sidebar);

      var panel = new StackPanel();
      panel.Children.Add(new Label { Content = "Name:", FontWeight = FontWeights.Bold });
      Grid.SetColumn(panel, 0);

      var text = new TextBox { Name = "Name" };
      panel.Children.Add(text);

      var button = new Button { Content = "TrainEngine", Width = Double.NaN };
      button.Click += (sender, e) => { Helper.TrainFace(text.Text); };
      panel.Children.Add(button);


      preview = new WriteableBitmap(100, 100, 96, 96, format, null);
      var thumb = new Image { Name = "Thumbnail", Width = 100, Height = 100, Margin = new Thickness { Left = 10 } };
      thumb.Source = preview;
      Grid.SetColumn(thumb, 1);

      var grid = new Grid();
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Star) });
      grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
      grid.Children.Add(panel);
      grid.Children.Add(thumb);

      lbl = new Label { Content = "Training" };
      var box = new GroupBox();
      box.Header = lbl;
      box.Content = grid;

      sidebar.Children.Add(box);
    }
  }
}
