using System;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;

using NLog.Config;
using NLog.Targets;
using NLog;


namespace net.encausse.sarah.debug {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "Debug";
    }
    
    public override void Start() {
      base.Start();
      SetupLogging();
    }

    // ------------------------------------------
    //  Logs Management
    // ------------------------------------------

    protected void SetupLogging() {
      var path = ConfigManager.GetInstance().Find("debug.log-file", null);
      if (null == path) { return; }
      
      LoggingConfiguration config = new LoggingConfiguration();

      FileTarget fileTarget = new FileTarget();
      fileTarget.FileName = path;
      fileTarget.Layout = "${message}";
      fileTarget.CreateDirs = true;

      config.AddTarget("file", fileTarget);

      LoggingRule rule2 = new LoggingRule("*", LogLevel.Info, fileTarget);
      config.LoggingRules.Add(rule2);

      // LoggingRule rule3 = new LoggingRule("*", LogLevel.Debug, fileTarget);
      // config.LoggingRules.Add(rule3);

      LoggingRule rule4 = new LoggingRule("*", LogLevel.Warn, fileTarget);
      config.LoggingRules.Add(rule4);

      // Ready
      LogManager.ReconfigExistingLoggers();
      LogManager.Configuration = config;
      Host.Log(this, "STARTING LOGGING:" + fileTarget.FileName);
    }

    public override void Log(string msg) {
      Logger logger = LogManager.GetLogger("SARAH");
      logger.Info("[{0}] [{1}]\t {2}", DateTime.Now.ToString("HH:mm:ss"), "SARAH", msg);
    }
    public override void Log(string ctxt, string msg) {
      Logger logger = LogManager.GetLogger("SARAH");
      logger.Info("[{0}] [{1}]\t {2}", DateTime.Now.ToString("HH:mm:ss"), ctxt, msg);
    }
    public override void Debug(string ctxt, string msg) {
      Logger logger = LogManager.GetLogger("SARAH");
      logger.Debug("[{0}] [{1}]\t {2}", DateTime.Now.ToString("HH:mm:ss"), ctxt, msg);
    }
    public override void Error(string ctxt, string msg) {
      Logger logger = LogManager.GetLogger("SARAH");
      logger.Error("[{0}] [{1}]\t {2}", DateTime.Now.ToString("HH:mm:ss"), ctxt, msg);
    }
    public override void Error(string ctxt, Exception ex) {
      Logger logger = LogManager.GetLogger("SARAH");
      logger.Error("[{0}] [{1}]\t {2}", DateTime.Now.ToString("HH:mm:ss"), ctxt, ex);
    }


    // -------------------------------------------
    //  TASKS
    // -------------------------------------------

    public IDictionary<string, AddOnTask> Tasks = new Dictionary<string, AddOnTask>();

    public override void InitColorFrame(string device, byte[] data, Timestamp stamp, int width, int height, int fps) {
      base.InitColorFrame(device, data, stamp, width, height, fps);

      var dueTime = TimeSpan.FromMilliseconds(200);
      var interval = TimeSpan.FromMilliseconds(ConfigManager.GetInstance().Find("debug.threshold", 1000));

      var task = new AddOnTask(device);
      task.SetColor(data, stamp, width, height, fps);
      task.Start(dueTime, interval);

      Tasks.Add(device, task);
    }

    // ------------------------------------------
    //  HTTP Management
    // ------------------------------------------

    public override void BeforeHTTPRequest(string qs, NameValueCollection parameters, IDictionary files, StreamWriter writer) {
      base.BeforeHTTPRequest(qs, parameters, files, writer);

      // Take a Picture
      var picture = parameters.Get("picture");
      if (picture != null) {
        picture = "true".Equals(picture) ? null : picture;

        var device = parameters.Get("device");
        if (device != null) {
          var path = Tasks[device].TakePicture(picture);
          writer.Write(path);
        } 
        else {
          foreach (var task in Tasks.Values) {
            var path = task.TakePicture(picture);
            writer.Write(path);
          }
        }
      }
    }


    // ------------------------------------------
    //  Audio Management
    // ------------------------------------------

    public override void AfterSpeechRecognition(string device, string text, double confidence, XPathNavigator xnav, string grammar, Stream stream, IDictionary<string, string> options) {
      base.AfterSpeechRecognition(device, text, confidence, xnav, grammar, stream, options);

      string path = ConfigManager.GetInstance().Find("debug.dump", null);
      if (path == null) { return; }

      // Build path
      string date = DateTime.Now.ToString("yyyy.M.d_hh.mm.ss");
      path = path.Replace("${date}", date);

      // Clean path
      if (File.Exists(path)) { File.Delete(path); }

      // Dump File
      stream.Position = 0;
      using (FileStream fileStream = new FileStream(path, FileMode.CreateNew)) {
        stream.CopyTo(fileStream);
      }

      // Clean XML data
      path += ".xml";
      if (File.Exists(path)) { File.Delete(path); }

      String xml = "";
      xml += "<match=\"" + confidence + "\" text\"" + text + "\">\r\n";
      xml += xnav.OuterXml;

      // Dump to XML
      System.IO.File.WriteAllText(path, xml);

      Host.Log(this, "Dump audio to: "+path);
    }

    // ------------------------------------------
    //  Voice Management
    // ------------------------------------------

    public override void AfterHandleVoice(String tts, bool sync, Stream stream) {
      base.AfterHandleVoice(tts, sync, stream);

      string path = ConfigManager.GetInstance().Find("debug.voice", null);
      if (path == null) { return; }

      // Build path
      string date = DateTime.Now.ToString("yyyy.M.d_hh.mm.ss");
      path = path.Replace("${date}", date);

      // Clean path
      if (File.Exists(path)) { File.Delete(path); }

      // Dump File
      stream.Position = 0;
      using (FileStream fileStream = new FileStream(path, FileMode.CreateNew)) {
        stream.CopyTo(fileStream);
      }

      Host.Log(this, "Dump voice to: " + path);
    }

  }
}