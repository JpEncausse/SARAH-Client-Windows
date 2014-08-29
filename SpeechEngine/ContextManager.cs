using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace net.encausse.sarah.speech {
  class ContextManager : IDisposable {

    // -------------------------------------------
    //  SINGLETON
    // -------------------------------------------

    private static ContextManager manager = null;
    private ContextManager() { }

    public static ContextManager GetInstance() {
      if (manager == null) {
        manager = new ContextManager();
      }
      return manager;
    }

    public void Dispose() {

    }

    // -------------------------------------------
    //  UTILITY
    // ------------------------------------------

    protected void Log(string msg) {
      SARAH.GetInstance().Log("ContextManager", msg);
    }

    protected void Debug(string msg) {
      SARAH.GetInstance().Debug("ContextManager", msg);
    }

    protected void Error(string msg) {
      SARAH.GetInstance().Error("ContextManager", msg);
    }

    protected void Error(Exception ex) {
      SARAH.GetInstance().Error("ContextManager", ex);
    }

    // -------------------------------------------
    //  DYNAMIC
    // -------------------------------------------

    /**
     * If Context is Dynamic then a Dyn grammar is enabled 
     * and contains a rejected URL to callback.
     */
    public string Dynamic() {
      if (DynGrammar == null) return null;
      if (DynGrammar.Enabled) { return DynGrammar.CallbackURL; }
      return null;
    }

    private SpeechGrammar DynGrammar = null;
    public void LoadDynamicGrammar(String[] sentences, String[] tags, String callbackUrl) {

      if (sentences == null || tags == null) { return; }
      if (sentences.Length  != tags.Length) { return; }

      // Create once
      if (DynGrammar == null) {
        DynGrammar = new SpeechGrammar("Dyn");
        DynGrammar.LastModified = DateTime.Now;
        DynGrammar.Enabled = false;
      }

      // Build XML
      bool garbage = false;
      var xml = "\n<one-of>";
      for (var i = 0; i < sentences.Length; i++) {
        Log("Add to DynGrammar: " + sentences[i] + " => " + tags[i]);
        if (sentences[i].IndexOf('*') >= 0) { garbage = true; }
        if (sentences[i].Equals("*")) {
          if (callbackUrl == null) { callbackUrl = "http://127.0.0.1:8080/askme"; }
          continue; 
        }
        xml += "\n<item>" + sentences[i].Replace("*", "<ruleref special=\"GARBAGE\" />") + "<tag>out.action.tag=\"" + tags[i] + "\"</tag></item>";
      }
      xml += "\n</one-of>";
      xml += "\n<tag>out.action._attributes.uri=\"" + callbackUrl + "\";</tag>";
      if (garbage) {
        xml += "\n<tag>out.action._attributes.dictation=\"true\";</tag>";
      }

      // Fix callback
      DynGrammar.CallbackURL = callbackUrl;

      // Update the XML
      GrammarManager.GetInstance().UpdateXML(DynGrammar, xml);
    }

    // -------------------------------------------
    //  CONTEXT
    // -------------------------------------------

    public ICollection<string> Current { get; set; }
    public ICollection<string> Default = new HashSet<string>();

    public void SetContext(ICollection<string> context) {
      if (context == null) { SetContext("default"); return; }
      if (context.Count < 1) { return; }
      Current = context;
      if (context.Count == 1) {
        var e = context.GetEnumerator();
        e.MoveNext();
        SetContext(e.Current);
        return;
      }

      Log("Context: " + String.Join(", ", context));
      foreach (SpeechGrammar g in GrammarManager.GetInstance().Cache.Values) {
        if (g.Name == "dictation") { continue; }
        g.Enabled = context.Contains(g.Name);
        Debug(g.Name + " = " + g.Enabled);
      }
    }

    public void SetContext(String context) {
      if (context == null) { return; }
      Log("Context: " + context);
      if ("default".Equals(context)) {
        SetContext(Default);
        Current = null;
        return;
      }
      bool all = "all".Equals(context);
      foreach (SpeechGrammar g in GrammarManager.GetInstance().Cache.Values) {
        if (g.Name == "dictation") { continue; }
        g.Enabled = all || context.Equals(g.Name);
        Debug(g.Name + " = " + g.Enabled);
      }
    }

    System.Timers.Timer ctxTimer = null;
    public void StartContextTimeout() {
      if (ctxTimer != null) { return; }
      int timeout = ConfigManager.GetInstance().Find("speech.grammar.timeout", 30000);
      Log("Start context timeout: " + timeout);
      ctxTimer = new System.Timers.Timer();
      ctxTimer.Interval = timeout;
      ctxTimer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
      ctxTimer.Enabled = true;
      ctxTimer.Start();
    }

    protected void timer_Elapsed(object sender, EventArgs e) {
      Log("End context timeout");
      ctxTimer.Stop();
      ctxTimer = null;

      SetContext("default");
      ApplyContextToEngines();
    }

    public void ResetContextTimeout() {
      Log("Reset timeout");
      if (ctxTimer == null) { return; }
      ctxTimer.Stop();
      ctxTimer.Start();
    }

    public void ApplyContextToEngines() {
      Log("Forward Context enable/disable grammar");
      GrammarManager.GetInstance().ApplyGrammarsToEngines();
    }

  }
}