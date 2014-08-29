using System;
using System.IO;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Kinect;



namespace net.encausse.sarah.kinect2 {
  public class Kinect : IDisposable {

    public String Name { get; set; }
    private KinectSensor Sensor { get; set; }
    public MotionTask Task { get; set; }

    public Kinect(String name, KinectSensor sensor) {
      this.Sensor = sensor;
      this.Name = name;
    }
    
    public void Dispose() {

      if (reader != null) {
        reader.Dispose(); reader = null;
      }

      if (dfr != null) {
        dfr.Dispose(); dfr = null;
      }
      
      if (cfr != null) {
        cfr.Dispose(); cfr = null;
      }
      
      if (bfr != null) {
        bfr.Dispose(); bfr = null;
      }

      if (Task != null) {
        Task.Dispose(); Task = null;
      }

      if (Sensor != null) {
        Sensor.Close(); Sensor = null;
        // Sensor.Dispose();
      }
    }

    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log(Name, msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug(Name, msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error(Name, msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error(Name, ex);
    }

    // -------------------------------------------
    //  START
    // -------------------------------------------

    private bool init = false;
    public bool Ready() {
      return init;
    }


    private CoordinateMapper coordinateMapper = null;

    // Multi Frame Reader
    private MultiSourceFrameReader reader = null;

    // Single Frame Reader
    private DepthFrameReader     dfr = null;
    private ColorFrameReader     cfr = null;
    private BodyFrameReader      bfr = null;
    private BodyIndexFrameReader ifr = null;

    public void Start() {
      if (Sensor == null) { return; }
      Log("Start sensor...");

      // Mapper
      coordinateMapper = Sensor.CoordinateMapper;

      // Open connection
      Sensor.Open();

      // Audio Stream
      StartAudioStream();

      if (ConfigManager.GetInstance().Find("kinect_v2.speech.speech_only", true)) {
        init = true; return;
      }

      // Init single frame
      StartDepthStream();
      StartColorStream();
      StartBodyStream();
      StartBodyIndexStream();

      // Motion Task
      StartMotionTask();

      // Multi Frame Reader
      // reader = Sensor.OpenMultiSourceFrameReader(FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);
      // reader.MultiSourceFrameArrived += OnMultipleFramesArrivedHandler;

      // Single Frame Reader
      dfr = Sensor.DepthFrameSource.OpenReader();
      dfr.FrameArrived += (object sender, DepthFrameArrivedEventArgs e) => { HandleDepthFrame(e.FrameReference); };

      cfr = Sensor.ColorFrameSource.OpenReader();
      cfr.FrameArrived += (object sender, ColorFrameArrivedEventArgs e) => { HandleColorFrame(e.FrameReference); };

      bfr = Sensor.BodyFrameSource.OpenReader();
      bfr.FrameArrived += (object sender, BodyFrameArrivedEventArgs e) => { HandleBodyFrame(e.FrameReference); };

      ifr = Sensor.BodyIndexFrameSource.OpenReader();
      ifr.FrameArrived += (object sender, BodyIndexFrameArrivedEventArgs e) => { HandleBodyIndexFrame(e.FrameReference); };

      init = true;
    }

    public StopwatchAvg AllFrameWatch = new StopwatchAvg();
    private void OnMultipleFramesArrivedHandler(object sender, MultiSourceFrameArrivedEventArgs e) {
      init = true;

      // Retrieve multisource frame reference
      MultiSourceFrameReference multiRef = e.FrameReference;
      MultiSourceFrame multiFrame = null;

      try {
        AllFrameWatch.Again();

        multiFrame = multiRef.AcquireFrame();
        if (multiFrame == null) {
          AllFrameWatch.Stop();  return;
        }
        
        HandleDepthFrame(multiFrame.DepthFrameReference);

        // Motion check
        if (Task.StandBy) {
          AllFrameWatch.Stop(); return;
        }

        HandleColorFrame(multiFrame.ColorFrameReference);
        HandleBodyFrame(multiFrame.BodyFrameReference);
        HandleBodyIndexFrame(multiFrame.BodyIndexFrameReference);

        AllFrameWatch.Stop();

      } catch (Exception) { /* ignore if the frame is no longer available */ } finally { }
    }

    // ------------------------------------------
    //  AUDIO
    // ------------------------------------------
    
    private void StartAudioStream() {

      IReadOnlyList<AudioBeam> audioBeamList = Sensor.AudioSource.AudioBeams;
      var beam = audioBeamList[0];
      var stream = beam.OpenInputStream();
      var audio = new KinectAudioStream(stream);
      double confidence = ConfigManager.GetInstance().Find("kinect_v2.speech.confidence", 0.6);

      // Let the convertStream know speech is going active
      audio.SpeechActive = true;

      // Build Speech Engine
      AddOnManager.GetInstance().AddAudioSource(Name, audio, "Kinect", null, confidence);
    }

    // ------------------------------------------
    //  DEPTH
    // ------------------------------------------

    private FrameDescription depthDesc = null;
    public ushort[] DepthData { get; set; }
    public int DepthWidth, DepthHeight;
    public Timestamp DepthStamp { get; set; }
    public TimeSpan RelativeTime = TimeSpan.FromMilliseconds(0);
    public StopwatchAvg DepthWatch = new StopwatchAvg();

    private void StartDepthStream() {
      // Get frame description for the depth output
      depthDesc = Sensor.DepthFrameSource.FrameDescription;

      // Init depth buffer
      DepthWidth = depthDesc.Width;
      DepthHeight = depthDesc.Height;
      DepthData = new ushort[DepthWidth * DepthHeight];
      DepthStamp = new Timestamp();
      KinectManager.GetInstance().InitDepthFrame(Name, DepthData, DepthStamp, DepthWidth, DepthHeight);
      Log("DepthWidth: " + DepthWidth + " DepthHeight: " + DepthHeight);
    }

    private void HandleDepthFrame(DepthFrameReference reference) {
      DepthWatch.Again();
      using (DepthFrame depthFrame = reference.AcquireFrame()) {
        if (depthFrame == null) return;

        depthFrame.CopyFrameDataToArray(DepthData);
        DepthStamp.Time = System.DateTime.Now;
      }
      DepthWatch.Stop();
    }

    private void StartMotionTask() {
      var dueTime = TimeSpan.FromMilliseconds(200);
      var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("kinect_v2.motion.ms", 100));
      Task = new MotionTask(Name);
      Task.SetDepth(DepthData, null, DepthWidth, DepthHeight);
      Task.Start(dueTime, interval);
    }

    // ------------------------------------------
    //  COLOR
    // ------------------------------------------

    private FrameDescription colorDesc = null;
    public byte[] ColorDataRaw { get; set; }
    public int ColorWidth, ColorHeight;
    public ColorImageFormat ColorFormat { get; set; }
    public Timestamp ColorStamp { get; set; }
    public StopwatchAvg ColorWatch = new StopwatchAvg();

    private void StartColorStream() {
      // Get frame description for the color output
      colorDesc = Sensor.ColorFrameSource.FrameDescription;

      // Init color buffer
      ColorWidth = colorDesc.Width;
      ColorHeight = colorDesc.Height;
      ColorDataRaw = new byte[colorDesc.LengthInPixels * 4];
      ColorStamp = new Timestamp();

      AddOnManager.GetInstance().InitColorFrame(Name, ColorDataRaw, ColorStamp, ColorWidth, ColorHeight, 15);
      Log("ColorWidth: " + ColorWidth + " ColorHeight: " + ColorHeight);
    }

    private void HandleColorFrame(ColorFrameReference reference) {
      if (Task.StandBy) { ColorWatch.Reset(); return; }

      ColorWatch.Again();
      using (ColorFrame colorFrame = reference.AcquireFrame()) {
        if (colorFrame == null) {
          ColorWatch.Stop(); return;
        }
        
        // Copy data to array based on image format
        if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra) {
          colorFrame.CopyRawFrameDataToArray(ColorDataRaw);
        } else {
          colorFrame.CopyConvertedFrameDataToArray(ColorDataRaw, ColorImageFormat.Bgra);
        }
        
        ColorStamp.Time = System.DateTime.Now;
      }
      ColorWatch.Stop();
    }

    // ------------------------------------------
    //  BODY INDEX
    // -----------------------------------------

    
    private FrameDescription bodyIndexDesc = null;
    public int BodyIndexWidth, BodyIndexHeight;
    public Timestamp BodyIndexStamp { get; set; }
    public StopwatchAvg BodyIndexWatch = new StopwatchAvg();
    public byte[] BodyIndexData { get; set; }

    private void StartBodyIndexStream() {
      // Get frame description for the index output
      bodyIndexDesc = Sensor.BodyIndexFrameSource.FrameDescription;

      // Init color buffer
      BodyIndexWidth = bodyIndexDesc.Width;
      BodyIndexHeight = bodyIndexDesc.Height;
      BodyIndexStamp = new Timestamp();
      BodyIndexData = new byte[bodyIndexDesc.LengthInPixels];

      Log("BodyIndexWidth: " + BodyIndexWidth + " BodyIndexHeight: " + BodyIndexHeight);
    }


    private void HandleBodyIndexFrame(BodyIndexFrameReference reference) {
      if (Task.StandBy) { BodyIndexWatch.Reset(); return; }

      BodyIndexWatch.Again();
      using (BodyIndexFrame indexFrame = reference.AcquireFrame()) {
        if (indexFrame == null) {
          BodyIndexWatch.Stop(); return;
        }
        indexFrame.CopyFrameDataToArray(BodyIndexData);
        /*
        using (Microsoft.Kinect.KinectBuffer bodyIndexBuffer = indexFrame.LockImageBuffer()) {
          IntPtr ptr = bodyIndexBuffer.UnderlyingBuffer;
          RefreshBodyArea(ptr, bodyIndexBuffer.Size);
        }
        */
        BodyIndexStamp.Time = System.DateTime.Now;
      }
      BodyIndexWatch.Stop();
    }


    // ------------------------------------------
    //  BODY
    // -----------------------------------------

    public IList<Body> Bodies { get; set; }
    public List<NBody> BodyData { get; set; }
    public Timestamp BodyStamp { get; set; }
    public StopwatchAvg BodyWatch = new StopwatchAvg();

    private void StartBodyStream() {
      // Init body buffer
      Bodies = new Body[6];
      BodyData = new List<NBody>(6);
      BodyStamp = new Timestamp();
      KinectManager.GetInstance().InitBodyFrame(Name, Bodies, BodyStamp);
      AddOnManager.GetInstance().InitBodyFrame(Name, BodyData, BodyStamp, ColorWidth, ColorHeight);
    }

    private void HandleBodyFrame(BodyFrameReference reference) {
      if (Task.StandBy) { BodyWatch.Reset(); return; }

      BodyWatch.Again();
      using (BodyFrame bodyFrame = reference.AcquireFrame()) {
        if (bodyFrame == null) return;

        // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
        // As long as those body objects are not disposed and not set to null in the array,
        // those body objects will be re-used.
        bodyFrame.GetAndRefreshBodyData(Bodies);
        BodyStamp.Time = System.DateTime.Now;
        RefreshBodyData(Bodies);
      }
      BodyWatch.Stop();
    }

    private ICollection<NBody> cache = new List<NBody>();
    private void RefreshBodyData(IList<Body> bodies){
      cache.Clear();
      foreach (var body in bodies) {
        if (!body.IsTracked) { continue; }
        RefreshBodyData(body);
      }
      lock (BodyData) {
        BodyData.Clear(); 
        BodyData.AddRange(cache);
      }
    }

    private void RefreshBodyData(Body body){
      foreach (var nbody in BodyData) {
        if ((ulong)nbody.TrackingId != body.TrackingId) { continue; }
        RefreshBodyData(body, nbody); return;
      }
      RefreshBodyData(body, new NBody(body.TrackingId));
    }

    private void RefreshBodyData(Body body, NBody nbody) {
      cache.Add(nbody);

      // Joints
      foreach(var joint in body.Joints.Values){
        var ntype  = ResolveJointType(joint.JointType);
        var njoint = nbody.GetJoint(ntype);
        var point  = coordinateMapper.MapCameraPointToColorSpace(joint.Position);
        
        // Hack 
        var nudge = ntype == NJointType.Head ? 50 : 0;


        njoint.Tracking = ResolveTrackingState(joint.TrackingState);
        njoint.SetPosition3D(joint.Position.X, joint.Position.Y, joint.Position.Z);
        njoint.SetPosition2D(point.X, point.Y + nudge);

        if (  njoint.Type == NJointType.Head 
           || njoint.Type == NJointType.HandRight 
           || njoint.Type == NJointType.HandLeft) {
          njoint.SetJointRadius(ComputeJointRadius(joint));
        }
      }

      // Misc
      nbody.Tracking = NTrackingState.Tracked;
    }

    // DepthArea = (avgDepth * inverseFocalLength)² * pixelCount
    // inverseFocalLength = 0.0027089166

    private int ComputeJointRadius(Joint joint) {
      if (joint.TrackingState == TrackingState.NotTracked) { return 0; }
      var pos = coordinateMapper.MapCameraPointToDepthSpace(joint.Position);
      var x0 = (int) pos.X; var y0 = (int) pos.Y;
      if (x0 + y0 * DepthWidth > DepthData.Length) return 0;
      var depth0 = DepthData[x0 + y0 * DepthWidth];

      var x = ComputeJointRadius(x0, y0, depth0, DepthWidth, true);
      var y = ComputeJointRadius(x0, y0, depth0, DepthWidth, false);

      return (x0 - x) > (y0 - y) ? (x0 - x) * ColorWidth  / DepthWidth
                                 : (y0 - y) * ColorHeight / DepthHeight;
    }

    private int ComputeJointRadius(int x0, int y0, int depth0, int width, bool onX) {
      var k0 = onX ? x0 : y0;
      var i = k0;
      for (var cpt = 0; i > 0; i--) {
        var depthI = onX ? DepthData[i + y0 * DepthWidth] 
                         : DepthData[x0 + i * DepthWidth];
        cpt = depth0 - depthI > 200 ? cpt + 1 : 0;
        if (cpt >= 3) { return i; }
        if (k0 - i > 100) { return k0; }
      }
      return k0;
    }


    private NTrackingState ResolveTrackingState(TrackingState state) {
      switch (state) {
        case TrackingState.Tracked: return NTrackingState.Tracked;
        case TrackingState.Inferred: return NTrackingState.Inferred;
        case TrackingState.NotTracked: return NTrackingState.NotTracked;
      }
      return NTrackingState.NotTracked;
    }

    private NJointType ResolveJointType(JointType type) {
      switch (type) {
        case JointType.SpineBase: return NJointType.SpineBase;
        case JointType.SpineMid: return NJointType.SpineMid;
        case JointType.Neck: return NJointType.Neck;
        case JointType.Head: return NJointType.Head;
        case JointType.ShoulderLeft: return NJointType.ShoulderLeft;
        case JointType.ElbowLeft: return NJointType.ElbowLeft;
        case JointType.WristLeft: return NJointType.WristLeft;
        case JointType.HandLeft: return NJointType.HandLeft;
        case JointType.ShoulderRight: return NJointType.ShoulderRight;
        case JointType.ElbowRight: return NJointType.ElbowRight;
        case JointType.WristRight: return NJointType.WristRight;
        case JointType.HandRight: return NJointType.HandRight;
        case JointType.HipLeft: return NJointType.HipLeft;
        case JointType.KneeLeft: return NJointType.KneeLeft;
        case JointType.AnkleLeft: return NJointType.AnkleLeft;
        case JointType.FootLeft: return NJointType.FootLeft;
        case JointType.HipRight: return NJointType.HipRight;
        case JointType.KneeRight: return NJointType.KneeRight;
        case JointType.AnkleRight: return NJointType.AnkleRight;
        case JointType.FootRight: return NJointType.FootRight;
        case JointType.SpineShoulder: return NJointType.SpineShoulder;
        case JointType.HandTipLeft: return NJointType.HandTipLeft;
        case JointType.ThumbLeft: return NJointType.ThumbLeft;
        case JointType.HandTipRight: return NJointType.HandTipRight;
        case JointType.ThumbRight: return NJointType.ThumbRight;
      }
      return NJointType.Head;
    }

    // -------------------------------------------
    //  UI
    // -------------------------------------------

    private TextBlock TextDepth;
    private TextBlock TextColor;
    private TextBlock TextBody;

    public void HandleSidebar(StackPanel sidebar) {
      TextDepth = new TextBlock { FontWeight = FontWeights.Bold };
      TextColor = new TextBlock { FontWeight = FontWeights.Bold };
      TextBody  = new TextBlock { FontWeight = FontWeights.Bold };

      var panel = new StackPanel();
      panel.Children.Add(TextDepth);
      panel.Children.Add(TextColor);
      panel.Children.Add(TextBody);

      var lbl = new Label { Content = this.Name };
      var box = new GroupBox();
      box.Header = lbl;
      box.Content = panel;

      sidebar.Children.Add(box);
    }

    public void RepaintColorFrame(byte[] bgr, int width, int height) {
      if (TextDepth != null) {
        var avg = DepthWatch.Average();
        if (!String.IsNullOrWhiteSpace(avg)) {
          TextDepth.Text = "Depth: " + avg;
        }
      }

      if (TextColor != null) {
        var avg = ColorWatch.Average();
        if (!String.IsNullOrWhiteSpace(avg)) {
          TextColor.Text = "Color: " + avg;
        }
      }

      if (TextBody != null) {
        var avg = BodyWatch.Average();
        if (!String.IsNullOrWhiteSpace(avg)) {
          TextBody.Text = "Body: " + avg;
        }
      }
    }
  }
}
