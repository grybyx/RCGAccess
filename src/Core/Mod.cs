using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RCGAccess
{
    [BepInPlugin("grybyx.rcgaccess", "RCGAccess", Mod.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Mod.Init(Paths.GameRootPath);
        }
    }

    public static class Mod
    {
        public const string Version = "0.1.8";
        public static string GameDir;
        public static string DataDir;
        public static string ProtoDir;
        public static Harmony Harmony;
        public static Runner Runner;
        public static bool Ready;

        public static void Init(string gameRoot)
        {
            GameDir = gameRoot;
            DataDir = Path.Combine(gameRoot, "RCGAccess");
            ProtoDir = Path.Combine(DataDir, "Protokolle");
            try { Directory.CreateDirectory(DataDir); Directory.CreateDirectory(ProtoDir); } catch { }
            Log.Init();
            Log.Write("RCGAccess " + Version + " startet. Unity " + Application.unityVersion + ", Spielordner " + gameRoot);
            Settings.Load();
            Speech.Start();
            try
            {
                Harmony = new Harmony("grybyx.rcgaccess");
                Patches.Apply(Harmony);
            }
            catch (Exception e) { Log.Error("Patches", e); }
            GameObject go = new GameObject("RCGAccessRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            Runner = go.AddComponent<Runner>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            Ready = true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                Log.Write("Szene geladen: " + scene.name + " (" + mode + ")");
                UiReader.OnSceneLoaded(scene.name);
                Navigator.OnSceneLoaded(scene.name);
                Combat.OnSceneLoaded(scene.name);
            }
            catch (Exception e) { Log.Error("OnSceneLoaded", e); }
        }
    }

    /// <summary>Main loop object; lives for the whole game session.</summary>
    public class Runner : MonoBehaviour
    {
        private float _startTime;
        private bool _announced;
        private bool _firstMenuDone;

        private void Start()
        {
            _startTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            try
            {
                ModInput.Poll();
                Speech.Update();
                GameInfo.Update();

                if (!_announced && Time.realtimeSinceStartup - _startTime > 1.0f && (GameInfo.LanguageKnown || Time.realtimeSinceStartup - _startTime > 12f))
                {
                    _announced = true;
                    Speech.ApplyLanguage(true);
                    Speech.Say(Loc.T("start", Mod.Version), true);
                    if (Settings.FirstStartPending())
                    {
                        ModMenu.Open(true);
                    }
                    _firstMenuDone = true;
                }

                if (ModInput.CtrlAlone()) Speech.Stop();
                Bindings.Update();

                if (ModMenu.IsOpen)
                {
                    ModMenu.Update();
                    return;
                }
                if (!_firstMenuDone) return;

                Hotkeys.Update();
                UiReader.Update();
                PhoneHelp.Update();
                Combat.Update();
                Navigator.Update();
            }
            catch (Exception e) { Log.Error("Update", e); }
        }

        private void LateUpdate()
        {
            try
            {
                if (ModMenu.IsOpen) { ModMenu.LateUpdate(); return; }
                UiReader.LateUpdate();
            }
            catch (Exception e) { Log.Error("LateUpdate", e); }
        }

        private void OnApplicationQuit()
        {
            try { Settings.Save(); } catch { }
            try { Speech.Shutdown(); } catch { }
        }
    }
}
