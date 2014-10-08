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
    private InfraredFrameReader  xfr = null;
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
      StartColorStream();
      StartDepthStream();
      StartInfraredStream();
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

      xfr = Sensor.InfraredFrameSource.OpenReader();
      xfr.FrameArrived += (object sender, InfraredFrameArrivedEventArgs e) => { HandleInfraredFrame(e.FrameReference); };

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

    private DepthFrame Depth = null;
    public StopwatchAvg DepthWatch = new StopwatchAvg();
    public TimeSpan RelativeTime = TimeSpan.FromMilliseconds(0);

    private void StartDepthStream() {
      // Get frame description for the infr output
      var description = Sensor.DepthFrameSource.FrameDescription;

      // Init infr buffer
      DepthFrame frame = Depth = new DepthFrame();
      frame.Width = description.Width;
      frame.Height = description.Height;
      frame.Pixels = new ushort[description.LengthInPixels];
      frame.Stamp = new Timestamp();

      AddOnManager.GetInstance().InitFrame(Name, frame);
      Log(frame.ToString());

      // Start Watch
      DepthWatch = new StopwatchAvg();
    }

    private void HandleDepthFrame(DepthFrameReference reference) {
      DepthWatch.Again();
      using (var frame = reference.AcquireFrame()) {
        if (frame == null) return;
        frame.CopyFrameDataToArray(Depth.Pixels);
        Depth.Stamp.Time = System.DateTime.Now;
      }
      DepthWatch.Stop();
    }

    private void StartMotionTask() {
      var dueTime = TimeSpan.FromMilliseconds(200);
      var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("kinect_v2.motion.ms", 100));
      Task = new MotionTask(dueTime, interval);
      Task.Device = "";
      Task.AddFrame(Depth);
      Task.Start();
    }

    // ------------------------------------------
    //  INFRARED
    // ------------------------------------------

    private InfraredFrame Infrared = null;
    public StopwatchAvg InfraredWatch = new StopwatchAvg();

    private void StartInfraredStream() {
      // Get frame description for the infr output
      var description = Sensor.InfraredFrameSource.FrameDescription;

      // Init infr buffer
      InfraredFrame frame = Infrared = new InfraredFrame();
      frame.Width = description.Width;
      frame.Height = description.Height;
      frame.Pixels = new ushort[description.LengthInPixels];
      frame.Stamp = new Timestamp();

      AddOnManager.GetInstance().InitFrame(Name, frame);
      Log(frame.ToString());

      // Start Watch
      InfraredWatch = new StopwatchAvg();
    }

    private void HandleInfraredFrame(InfraredFrameReference reference) {
      if (Task.StandBy) { InfraredWatch.Reset(); return; }

      InfraredWatch.Again();
      using (var frame = reference.AcquireFrame()) {
        if (frame == null) return;

        frame.CopyFrameDataToArray(Infrared.Pixels);
        Infrared.Stamp.Time = System.DateTime.Now;
      }
      InfraredWatch.Stop();
    }

    // ------------------------------------------
    //  COLOR
    // ------------------------------------------

    private ColorFrame Color = null;
    public StopwatchAvg ColorWatch = new StopwatchAvg();
    public ColorImageFormat ColorFormat { get; set; }

    private void StartColorStream() {
      // Get frame description for the infr output
      var description = Sensor.ColorFrameSource.FrameDescription;

      // Init infr buffer
      ColorFrame frame = Color = new ColorFrame();
      frame.Width = description.Width;
      frame.Height = description.Height;
      frame.Pixels = new byte[description.LengthInPixels * 4];
      frame.Stamp = new Timestamp();
      frame.Fps = 15;

      AddOnManager.GetInstance().InitFrame(Name, frame);
      Log(frame.ToString());

      // Start Watch
      ColorWatch = new StopwatchAvg();
    }

    private void HandleColorFrame(ColorFrameReference reference) {
      if (Task.StandBy) { ColorWatch.Reset(); return; }

      ColorWatch.Again();
      using (var frame = reference.AcquireFrame()) {
        if (frame == null) return;

        // Copy data to array based on image format
        if (frame.RawColorImageFormat == ColorImageFormat.Bgra) {
          frame.CopyRawFrameDataToArray(Color.Pixels);
        } else {
          frame.CopyConvertedFrameDataToArray(Color.Pixels, ColorImageFormat.Bgra);
        }

        Color.Stamp.Time = System.DateTime.Now;
      }
      ColorWatch.Stop();
    }

    // ------------------------------------------
    //  BODY INDEX
    // -----------------------------------------

    private BodyIndexFrame BodyIndex = null;
    public StopwatchAvg BodyIndexWatch = new StopwatchAvg();

    private void StartBodyIndexStream() {
      // Get frame description for the infr output
      var description = Sensor.BodyIndexFrameSource.FrameDescription;

      // Init infr buffer
      BodyIndexFrame frame = BodyIndex = new BodyIndexFrame();
      frame.Width = description.Width;
      frame.Height = description.Height;
      frame.Pixels = new byte[description.LengthInPixels];
      frame.Stamp = new Timestamp();

      AddOnManager.GetInstance().InitFrame(Name, frame);
      Log(frame.ToString());

      // Start Watch
      BodyIndexWatch = new StopwatchAvg();
    }

    private void HandleBodyIndexFrame(BodyIndexFrameReference reference) {
      if (Task.StandBy) { BodyIndexWatch.Reset(); return; }

      BodyIndexWatch.Again();
      using (var frame = reference.AcquireFrame()) {
        if (frame == null) return;

        frame.CopyFrameDataToArray(BodyIndex.Pixels);
        /*
        using (Microsoft.Kinect.KinectBuffer buffer = indexFrame.LockImageBuffer()) {
          IntPtr ptr = buffer.UnderlyingBuffer;
          RefreshBodyArea(ptr, buffer.Size);
        }
        */
        BodyIndex.Stamp.Time = System.DateTime.Now;
      }
      BodyIndexWatch.Stop();
    }

    // ------------------------------------------
    //  BODY
    // -----------------------------------------

    private BodyFrame Body = null;
    public StopwatchAvg BodyWatch = new StopwatchAvg();

    private void StartBodyStream() {

      // Init infr buffer
      BodyFrame frame = Body = new BodyFrame();
      frame.Width   = Color.Width;
      frame.Height  = Color.Height;
      frame.RawData = new Body[6]; // FIXME
      frame.Bodies  = new List<NBody>(6);
      frame.Stamp   = new Timestamp();

      AddOnManager.GetInstance().InitFrame(Name, frame);
      Log(frame.ToString());

      // Start Watch
      BodyWatch = new StopwatchAvg();
    }

    private void HandleBodyFrame(BodyFrameReference reference) {
      if (Task.StandBy) { BodyWatch.Reset(); return; }

      BodyWatch.Again();
      using (var frame = reference.AcquireFrame()) {
        if (frame == null) return;

        // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
        // As long as those body objects are not disposed and not set to null in the array,
        // those body objects will be re-used.
        var tmp = (IList<Body>)Body.RawData;
        frame.GetAndRefreshBodyData(tmp);
        Body.Stamp.Time = System.DateTime.Now;
        RefreshBodyData(Body);
      }
      BodyWatch.Stop();
    }

    private ICollection<NBody> cache = new List<NBody>();
    private void RefreshBodyData(BodyFrame frame){
      cache.Clear();
      foreach (var body in (IList<Body>) frame.RawData) {
        if (!body.IsTracked) { continue; }

        NBody nbody = frame.Find(body.TrackingId);
        if (nbody == null) {
          nbody = new NBody(body.TrackingId, frame.Width, frame.Height);
        }

        cache.Add(nbody);
        RefreshBodyData(body, nbody);
      }

      lock (frame.Bodies) {
        frame.Bodies.Clear();
        frame.Bodies.AddRange(cache);
      }
    }

    private void RefreshBodyData(Body body, NBody nbody) {
      // Joints
      foreach(var joint in body.Joints.Values){
        var ntype  = ResolveJointType(joint.JointType);
        var njoint = nbody.GetJoint(ntype);
        var point  = coordinateMapper.MapCameraPointToColorSpace(joint.Position);
        
        njoint.Tracking = ResolveTrackingState(joint.TrackingState);
        njoint.SetPosition2D(point.X, point.Y);
        njoint.SetPosition3D(joint.Position.X, joint.Position.Y, joint.Position.Z);
      }

      // Misc
      nbody.Tracking = NTrackingState.Tracked;
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
