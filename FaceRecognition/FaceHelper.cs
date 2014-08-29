
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;

namespace net.encausse.sarah.face {
  public class FaceHelper : IDisposable {

    public static String UNKNOWN = "Unknown";

    // -------------------------------------------
    //  CONSTRUCTOR
    // -------------------------------------------

    private Image<Gray, Byte> GrayImage;
    private int width, height, ratio;

    public FaceHelper(int w, int h) {
      width = w; height = h;

      ratio = width / ConfigManager.GetInstance().Find("face.ratio", 640);

      InitHaarCascade();
      InitDetect();

      InitTrainedFaces();
      InitEigenFaceRecognizer();
    }

    public void Dispose() { 
      // FIXME
    }

    // -------------------------------------------
    //  DETECTION
    // -------------------------------------------


    private CascadeClassifier cascade;
    private void InitHaarCascade() {
      var path = ConfigManager.GetInstance().Find("face.HaarCascade","");
      var full = Environment.CurrentDirectory + path;
      cascade = new CascadeClassifier(path);
    }

    private Image<Bgra, Byte> DetectImage;
    private void InitDetect() {
      DetectImage = new Image<Bgra, Byte>(width, height);
    }

    private Rectangle[] DetectFaces;
    private Size DetectSize = new Size(40, 40);
    public void Detect(byte[] pixels) {

      // Build Image
      DetectImage.Bytes = pixels;

      // Convert it to Grayscale
      GrayImage = DetectImage.Convert<Gray, Byte>().Resize(width / ratio, height / ratio, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
      // GrayImage._EqualizeHist();

      // Detect faces
      DetectFaces = cascade.DetectMultiScale(GrayImage, 1.1, 10, DetectSize, Size.Empty);

      // Train if needed
      Train();
    }

    // ------------------------------------------
    //  RECOGNITION
    // see http://www.codeproject.com/Articles/261550/EMGU-Multiple-Face-Recognition-using-PCA-and-Paral
    // ------------------------------------------

    private bool trained = false;
    private FaceRecognizer Recognizer;
    private int Threshold;
    private void InitEigenFaceRecognizer() {
      lock (this) {
        if (Recognizer != null) { return; }

        Threshold = ConfigManager.GetInstance().Find("face.Eigen_Threshold", 0);
        if (Threshold > 0) {
          Recognizer = new EigenFaceRecognizer(80, double.PositiveInfinity);
        }

        Threshold = ConfigManager.GetInstance().Find("face.Fisher_Threshold", 0);
        if (Threshold > 0) {
          Recognizer = new FisherFaceRecognizer(0, 3500);
        }

        Threshold = ConfigManager.GetInstance().Find("face.LBP_Threshold", 0);
        if (Threshold > 0) {
          Recognizer = new LBPHFaceRecognizer(1, 8, 8, 8, 100);
        }

        if (trainedImages.Count <= 0) { return; }

        // Train Images and Labels
        Recognizer.Train(trainedImages.ToArray(), trainedLabelIds.ToArray());
        trained = true;
      }
    }

    private void ResetEigenObjectRecognizer(){
      Recognizer = null;
      trained = false;
      InitEigenFaceRecognizer();
    }
    public float RecognizeDistance = 0;
    public Rectangle[] RecognizeArea = new Rectangle[20];
    public String[] RecognizeNames = new String[20];
    public Image<Gray, byte>[] RecognizeThumbs = new Image<Gray, byte>[20];
    public String[] Recognize() {
      if (null == DetectFaces || null == GrayImage) { return null; }
      Array.Clear(RecognizeNames, 0, RecognizeNames.Length);

      for (int i = 0; i < DetectFaces.Length && i < RecognizeNames.Length; i++) {

        // Build a thumbnail
        RecognizeThumbs[i] = GrayImage.Copy(DetectFaces[i]).Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
        RecognizeThumbs[i]._EqualizeHist();

        // Crop first only if not trained
        if (!trained) { return RecognizeNames; }

        // Recognize
        FaceRecognizer.PredictionResult ER = Recognizer.Predict(RecognizeThumbs[i]);

        RecognizeNames[i] = "Unknow";
        if (ER.Label >= 0) {
          RecognizeDistance = (float) ER.Distance;
          if (RecognizeDistance > Threshold) {
            RecognizeNames[i] = trainedLabels[ER.Label];
            RecognizeArea[i] = DetectFaces[i];
          }
        }

      }
      return RecognizeNames;
    }

    public Rectangle GetArea(String name) { // No check of null, -1, ...
      var index = Array.IndexOf(RecognizeNames, name);
      return RecognizeArea[index];
    }

    public Bitmap LastThumb() { 
      foreach (var thumb in RecognizeThumbs){
        if (thumb == null) continue;
        return ToBitmap(thumb.Bytes, 100, 100, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
      }
      return null;
    }

    // ------------------------------------------
    //  TRAINING
    // ------------------------------------------

    private List<int> trainedLabelIds = new List<int>();
    private List<string> trainedLabels = new List<string>();
    private List<Image<Gray, byte>> trainedImages = new List<Image<Gray, byte>>();
    private void InitTrainedFaces() {
      DirectoryInfo dir = new DirectoryInfo(@"AddOns\face\trained\");
      foreach (FileInfo f in dir.GetFiles("*.bmp")) {
        var lbl = f.Name.Substring(0, f.Name.IndexOf("-"));
        var img = new Image<Gray, byte>(f.FullName);
        trainedLabels.Add(lbl);
        trainedImages.Add(img);
        trainedLabelIds.Add(trainedLabels.IndexOf(lbl));
      }
    }

    
    private String train = null;
    public void TrainFace(String name) {
      this.train = name;
    }

    private void Train() {

      if (null == train || null == RecognizeThumbs[0] || null == Recognizer) { return; }

      var trainedFace = RecognizeThumbs[0];

      // Update trained memory
      String label = Sanitize(train);
      trainedLabels.Add(label);
      trainedImages.Add(trainedFace);
      trainedLabelIds.Add(trainedLabels.IndexOf(label));
      train = null;

      // Save to disk
      var Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      var now = (long)(DateTime.UtcNow - Jan1st1970).TotalSeconds;
      trainedFace.Save(@"AddOns\face\trained\" + label + "-" + now + ".bmp");

      // Update Recognizer
      ResetEigenObjectRecognizer();
    }

    // ------------------------------------------
    //  UI
    // ------------------------------------------

    private Image<Bgra, Byte> drawer;
    private Bgra border = new Bgra(0, 0, 255, 0);
    private Font font = new Font("Arial", 24, System.Drawing.FontStyle.Bold);
    private Image<Bgra, Byte> GetDrawer(int width, int height){
      if (drawer == null) { 
        drawer = new Image<Bgra, Byte>(width, height);
      }
      return drawer;
    }

    private Rectangle tmpRect = new Rectangle();
    public void DrawFaces(byte[] data, int width, int height) {
      var drawer = GetDrawer(width, height);
      drawer.Bytes = data;

      for (int i = 0; i < DetectFaces.Length && i < RecognizeNames.Length; i++) {

        // Draw Rect
        Rectangle r = DetectFaces[i];
        tmpRect.X      = r.X * ratio;
        tmpRect.Y      = r.Y * ratio;
        tmpRect.Width  = r.Width  * ratio;
        tmpRect.Height = r.Height * ratio;
        r = tmpRect;

        drawer.Draw(r, border, 2);

        // Draw text
        var rect = new Rectangle(r.X, r.Y + r.Height + 30, r.Width, r.Height);
        DrawText(drawer, rect, RecognizeNames[i] != null ? RecognizeNames[i] : UNKNOWN);
      }

      Buffer.BlockCopy(drawer.Bytes, 0, data, 0, data.Length);
    }

    // ------------------------------------------
    //  HELPER
    // ------------------------------------------

    private static String invalidChars = System.Text.RegularExpressions.Regex.Escape(new String(System.IO.Path.GetInvalidFileNameChars()) + " -");
    private static String invalidReStr = String.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
    private static String Sanitize(String name) {
      return System.Text.RegularExpressions.Regex.Replace(name, invalidReStr, "_");
    }

    public static Bitmap ToBitmap(byte[] pixels, int width, int height, System.Drawing.Imaging.PixelFormat format) {
      if (pixels == null) { return null; }

      var bitmap = new Bitmap(width, height, format);
      var data = bitmap.LockBits(
          new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
          ImageLockMode.ReadWrite,
          bitmap.PixelFormat);

      Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
      bitmap.UnlockBits(data);

      return bitmap;
    }
    
    private void DrawText(Image<Bgra, Byte> img, Rectangle rect, string text) {
      Graphics g = Graphics.FromImage(img.Bitmap);

      int tWidth = (int) g.MeasureString(text, font).Width;
      int x = (tWidth >= rect.Width) ? rect.Left - ((tWidth - rect.Width) / 2)
                                     : (rect.Width / 2) - (tWidth / 2) + rect.Left;

      g.DrawString(text, font, System.Drawing.Brushes.Red, new PointF(x, rect.Top - 18));
    }

  }
}
