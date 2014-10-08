using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using System.Xml.XPath;

namespace net.encausse.sarah.profile {
  public class AddOn : AbstractAddOn {

    public AddOn() : base() {
      Name = "ProfileEngine";
    }

    public override void Setup() {
      base.Setup();
      EngageTimeout = TimeSpan.FromSeconds(ConfigManager.GetInstance().Find("profile.engage-timeout", 60));
    }

    // -------------------------------------------
    //  PROFILE
    // -------------------------------------------

    TimeSpan EngageTimeout = TimeSpan.Zero;
    ICollection<Profile> Profiles = new List<Profile>();
    IDictionary<string, Tracker> Devices = new Dictionary<string, Tracker>();

    public override void HandleProfile(string device, string key, object value) {
      base.HandleProfile(device, key, value);
      UpdateProfile(device, key, value);
    }

    public override bool IsEngaged(string device) {
      if (!Devices.ContainsKey(device)) { return false; }

      var tracker = Devices[device];
      if (tracker.IsDeprecated()) { return false;}

      var engaged = tracker.Profile.Get("engaged");
      if (engaged == null) { return false; }

      return DateTime.Now - ((DateTime)engaged) < EngageTimeout;
    }

    // -------------------------------------------
    //  PRIVATE
    // -------------------------------------------

    private void UpdateProfile(string device, string key, object value) {
      lock (Profiles) {
        // 1. Seek for profile identifed by master key
        foreach (var p in Profiles) {
          if (p.Is(key, value)) {
            AddProfile(p, device);
            return;
          }
        }

        // 2. Seek for last know profile on device
        if (Devices.ContainsKey(device)) {
          var tracker = Devices[device];

          // 2.1 Latest profile has less than 10 minutes
          if (!tracker.IsDeprecated()) {
            tracker.Profile.Update(key, value);
            tracker.Update = DateTime.Now;
            return;
          }
        }

        // 3. Setup new Profile
        var profile = new Profile(key, value);
        AddProfile(profile, device);
      }
    }

    private void AddProfile(Profile profile, string device) { 

      // Clean old tracker
      if (Devices.ContainsKey(device)) {
        var t = Devices[device];
        if (t.IsDeprecated()) {
          Profiles.Remove(t.Profile);
        }

        t.Profile = profile;
        t.Update = DateTime.Now;
      }

      // Add new Tracker
      else {
        Devices[device] = new Tracker(profile);
      }

      // Add to profile list
      if (!Profiles.Contains(profile))
        Profiles.Add(profile);
    }

    // ------------------------------------------
    //  HTTP Management
    // ------------------------------------------

    public override void BeforeGET(string device, string url, string token, IDictionary<string, string> parameters) {
      base.BeforeGET(device, url, token, parameters);
      if (!Devices.ContainsKey(device)) { return; }

      var tracker = Devices[device];
      if (tracker.IsDeprecated()) { return; }

      var profile = tracker.Profile;
      foreach (var key in profile.Attributes.Keys) {
        var value = profile.GetString(key);
        parameters.Add("profile_" + key, value);
      }
    }

    // -------------------------------------------
    //  UI
    // -------------------------------------------

    private IDictionary<string,Label> labels = new Dictionary<string,Label>();
    public override void HandleSidebar(string device, StackPanel sidebar) {
      if (sidebar.Name != "Sidebar") { return; }
      var text = new Label { Content = "" };
      var lbl = new Label { Content = "Profile" };
      var box = new GroupBox();
      box.Header = lbl;
      box.Content = text;
      sidebar.Children.Add(box);
      labels.Add(device, text);
    }

    public override void RepaintColorFrame(string device, byte[] bgra, int width, int height) {
      if (!labels.ContainsKey(device)) { return; }
      var label = labels[device];
      label.Content = "";
      foreach (var p in Profiles) {
        foreach (var key in p.Attributes.Keys) {
          label.Content += key + "=" + p.Attributes[key] + "\n";
        }
        label.Content += "----------\n";
      }
    }
  }



  // ==========================================
  //  INNER CLASS
  // ==========================================

  class Tracker {

    public Tracker() { }
    public Tracker(Profile p) {
      Profile = p;
      Update = DateTime.Now;
    }

    public Profile  Profile { get; set; }
    public DateTime Update { get; set; }
    
    public bool IsDeprecated(){
      if (DateTime.Now - Update < TimeSpan.FromMinutes(10)) { return false; }
      return Profile.IsDeprecated();
    }

  }

  class Profile {

    public IDictionary<string, object> Attributes = new Dictionary<string, object>();
    private const string DateTimeOffsetFormatString = "yyyy-MM-ddTHH:mm:sszzz";

    public Profile() { }
    public Profile(string key, Object value) { Update(key, value); }
    
    public void Update(string key, Object value) {

      if (value is double && Attributes.ContainsKey(key)) {
        var v1 = (double) Attributes[key];
        var v2 = (double) value;
        value = Math.Sqrt((v1 * v1 + v2 * v2)/2);
      } 

      Attributes[key] = value;
    }
    
    public Object Get(string key) {
      if (!Attributes.ContainsKey(key)) { return null; }
      return Attributes[key];
    }

    private DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
    public String GetString(string key) {
      if (!Attributes.ContainsKey(key)) { return null; }
      var value = Attributes[key];

      var str = value.ToString(); 
      if (value is double) {
        str = ((double)value).ToString("G", CultureInfo.InvariantCulture);
      }
      
      if (value is DateTime) {
        str = ((DateTime)value).Subtract(epoch).TotalMilliseconds.ToString("G", CultureInfo.InvariantCulture);
      }

      return str;
    }

    public bool Is(string key, Object value) {
      if (!"face".Equals(key)) return false;
      if (!Attributes.ContainsKey(key)) return false;
      return value.Equals(Attributes[key]);
    }

    public bool IsDeprecated() {
      return Attributes.ContainsKey("face");
    }
  }
}
