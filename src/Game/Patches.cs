using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RCG;
using TMPro;
using UnityEngine;

namespace RCGAccess
{
    /// <summary>Harmony hooks into the game.</summary>
    public static class Patches
    {
        private const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static void Post(Harmony h, Type t, string method, string postfix, Type[] args = null)
        {
            try
            {
                MethodInfo m = args == null ? t.GetMethod(method, All) : t.GetMethod(method, All, null, args, null);
                if (m == null) { Log.Write("Patch: Methode nicht gefunden " + t.Name + "." + method); return; }
                h.Patch(m, null, new HarmonyMethod(typeof(Patches).GetMethod(postfix, BindingFlags.Static | BindingFlags.Public)));
                Log.Write("Patch aktiv: " + t.Name + "." + method);
            }
            catch (Exception e) { Log.Error("Patch " + t.Name + "." + method, e); }
        }

        public static void Apply(Harmony h)
        {
            Post(h, typeof(UI_InputSelectable), "ToggleHover", "HoverPost");
            Post(h, typeof(UI_StoreItemDisplayV2), "ToggleHover", "StoreHoverPost");
            Post(h, typeof(UI_CharacterSelectOption), "OnHover", "CharHoverPost");
            Post(h, typeof(UI_FileSelectOption), "OnHover", "FileHoverPost");
            Post(h, typeof(UI_ModeSelectOption), "ToggleHover", "ModeHoverPost");
            Post(h, typeof(UI_DialogSystem), "ResetCurrentDialog", "DialogPost");
            Post(h, typeof(UI_PortraitDialogSystem), "SetCurrentLineValues", "PortraitPost");
            Post(h, typeof(UI_TutorialSlideIn), "Init", "TutorialPost");
            Post(h, typeof(InteractEntity), "ToggleInteractSymbol", "InteractPost");
            Post(h, typeof(InteractEntity_Store), "ToggleInteractSymbol", "InteractPost");
            Post(h, typeof(PlayerInput), "Update", "PlayerInputPost");
            Post(h, typeof(UI_ScreenManager), "DisableOtherScreens", "ScreenPost");
            Post(h, typeof(ScreenLock), "ReportEnemyDeath", "EnemyDeathPost");
            Post(h, typeof(UI_ItemPickup), "SetWhatYouGotText", "RewardPost");
            Post(h, typeof(UI_PhoneScreen_ExtendedMap), "ShowMapIndicators", "MapIndicatorsPost");
            Post(h, typeof(PlayerRecruit), "RecruitLastGrabbed", "RecruitPost");
            try
            {
                MethodInfo rb = typeof(PlayerRecruit).GetMethod("RecruitButtonPressed", BindingFlags.Instance | BindingFlags.Public);
                h.Patch(rb, new HarmonyMethod(typeof(Patches).GetMethod("RecruitButtonPre", BindingFlags.Static | BindingFlags.Public)));
                Log.Write("Patch aktiv: PlayerRecruit.RecruitButtonPressed");
            }
            catch (Exception e) { Log.Error("Patch RecruitButtonPressed", e); }
            try
            {
                MethodInfo set = AccessTools.PropertySetter(typeof(RCG.Player), "Stamina");
                h.Patch(set, new HarmonyMethod(typeof(Patches).GetMethod("StaminaPre", BindingFlags.Static | BindingFlags.Public)));
                Log.Write("Patch aktiv: Player.Stamina (Schutz vor Schaden)");
            }
            catch (Exception e) { Log.Error("Patch Player.Stamina", e); }
            Post(h, typeof(InteractEntity_DropV2), "Interact", "DropV2Post", new Type[] { typeof(int), typeof(bool).MakeByRefType() });
            Post(h, typeof(InteractEntity_Drop), "Interact", "DropPost", new Type[] { typeof(int), typeof(bool).MakeByRefType() });
        }

        public static void HoverPost(UI_InputSelectable __instance, int __0, bool __1)
        {
            try { if (__1) UiReader.OnHover(__instance); } catch (Exception e) { Log.Error("HoverPost", e); }
        }

        public static void StoreHoverPost(UI_StoreItemDisplayV2 __instance, bool __0)
        {
            try
            {
                if (!__0) return;
                string s = UiReader.ReadFields(__instance, "_itemName", "_itemPrice", "_itemDescription");
                try
                {
                    RCG.Player pl = GameInfo.Player;
                    if (__instance.Item != null && pl != null)
                    {
                        float money = Combat.Money(pl);
                        if (money >= 0 && __instance.Item.ItemPrice > money + 0.001f) s += ", " + Loc.T("shop_expensive");
                    }
                }
                catch { }
                UiReader.OnSpecificFocus(__instance, s);
            }
            catch (Exception e) { Log.Error("StoreHoverPost", e); }
        }

        public static void CharHoverPost(UI_CharacterSelectOption __instance, int __0)
        {
            try
            {
                if (__0 != 0) return;
                UiReader.OnSpecificFocus(__instance, __instance.Character.ToString());
            }
            catch (Exception e) { Log.Error("CharHoverPost", e); }
        }

        public static void FileHoverPost(UI_FileSelectOption __instance)
        {
            try
            {
                string s = UiReader.ReadFields(__instance, "_fileText", "_progressText", "_mainAreaText", "_subAreaText");
                if (s.Length > 0) UiReader.OnSpecificFocus(__instance, s);
                else UiReader.OnHover(__instance);
            }
            catch (Exception e) { Log.Error("FileHoverPost", e); }
        }

        public static void ModeHoverPost(UI_ModeSelectOption __instance, bool __0)
        {
            try
            {
                if (!__0) return;
                List<string> parts = new List<string>();
                // the option title (e.g. "Schwierigkeit") is a sibling of the value text
                Transform root = __instance.transform.parent != null ? __instance.transform.parent : __instance.transform;
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(false);
                if (texts.Length > 4) texts = __instance.GetComponentsInChildren<TMP_Text>(false);
                foreach (TMP_Text t in texts)
                {
                    if (t.GetComponentInParent<UI_ModeSelectOption>() != null && t.GetComponentInParent<UI_ModeSelectOption>() != __instance) continue;
                    string s = GameInfo.Clean(t.text);
                    if (s.Length > 0 && !parts.Contains(s)) parts.Add(s);
                }
                // title first, current value last
                string opt = UiReader.ReadFields(__instance, "OptionText");
                if (opt.Length > 0) { parts.Remove(opt); parts.Add(opt); }
                if (parts.Count > 0) UiReader.OnSpecificFocus(__instance, string.Join(", ", parts.ToArray()));
            }
            catch (Exception e) { Log.Error("ModeHoverPost", e); }
        }

        public static void DialogPost(UI_DialogSystem __instance)
        {
            UiReader.OnDialogReset(__instance);
        }

        public static void PortraitPost(UI_PortraitDialogSystem __instance)
        {
            UiReader.OnPortraitLine(__instance);
        }

        public static void TutorialPost(UI_TutorialSlideIn __instance, string __0, string __1)
        {
            UiReader.OnTutorial(__instance, __0, __1);
        }

        public static void InteractPost(InteractEntity __instance, int __0, bool __1)
        {
            Navigator.OnInteractSymbol(__instance, __0, __1);
        }

        /// <summary>Damage help: halves or blocks every loss of health of the players (healing is untouched).</summary>
        public static void StaminaPre(RCG.Player __instance, ref int __0)
        {
            try
            {
                int cur = __instance.Stamina;
                if (__0 >= cur || cur <= 0) return;
                if (GameState.CurrentState != GameStates.Playing && GameState.CurrentState != GameStates.Cinematic) return;
                Combat.OnPlayerHit(__instance, cur - __0);
                if (Settings.DamageHelp == 2) __0 = cur;
                else if (Settings.DamageHelp == 1) __0 = cur - Mathf.CeilToInt((cur - __0) / 2f);
            }
            catch (Exception e) { Log.Error("StaminaPre", e); }
        }

        public static void RecruitPost(PlayerRecruit __instance, bool __result)
        {
            try { if (__instance._Player != null && __instance._Player.PlayerID == 0) Combat.OnRecruit(__result); } catch { }
        }

        public static void RecruitButtonPre(PlayerRecruit __instance)
        {
            try { Combat.OnRecruitButton(__instance._Player); } catch { }
        }

        public static void RewardPost(UI_ItemPickup __instance, int __0)
        {
            PhoneHelp.OnReward(__instance, __0);
        }

        public static void MapIndicatorsPost()
        {
            PhoneHelp.OnMapIndicators();
        }

        public static void EnemyDeathPost(EnemyEnitity __0)
        {
            Combat.OnEnemyDeathReported(__0);
        }

        public static void DropV2Post(InteractEntity_DropV2 __instance, int __0, ref bool __1)
        {
            if (__1 && __0 == 0) Pickups.OnDropV2(__instance);
        }

        public static void DropPost(InteractEntity_Drop __instance, int __0, ref bool __1)
        {
            if (__1 && __0 == 0) Pickups.OnDrop(__instance);
        }

        public static void ScreenPost(UI_ScreenManager __instance)
        {
            try
            {
                UI_MenuScreen s = __instance.CurrentScreen;
                if (s != null) Log.Write("Menue-Bildschirm: " + s.ScreenName + " (" + __instance.name + ")");
            }
            catch { }
        }

        private static MethodInfo _setH = AccessTools.PropertySetter(typeof(CombatInput), "HorizontalDir");
        private static MethodInfo _setV = AccessTools.PropertySetter(typeof(CombatInput), "VerticalDir");
        private static MethodInfo _setRun = AccessTools.PropertySetter(typeof(CombatInput), "Run");
        private static readonly object[] _arg = new object[1];
        private static PlayerInput _cachedInput;
        private static RCG.Player _cachedPlayer;

        public static void PlayerInputPost(PlayerInput __instance)
        {
            try
            {
                if (__instance.PlayerID != 0) return;
                if (ModMenu.IsOpen) return;
                if (GameState.CurrentState != GameStates.Playing || __instance.LockInput) return;
                if (__instance != _cachedInput || _cachedPlayer == null)
                {
                    _cachedInput = __instance;
                    _cachedPlayer = __instance.GetComponent<RCG.Player>();
                    if (_cachedPlayer == null) _cachedPlayer = __instance.GetComponentInParent<RCG.Player>();
                }
                RCG.Player p = _cachedPlayer;
                if (p == null) return;
                int rawH = __instance.HorizontalDir, rawV = __instance.VerticalDir;
                if (__instance.Quick || __instance.Heavy || __instance.Special) Combat.NotePlayerAttack();
                int h, v;
                if (Navigator.AutoEnter(rawH, rawV)) __instance.InteractQuick = true;
                if (Navigator.WalkInput(p, rawH, rawV, out h, out v))
                {
                    _arg[0] = h; _setH.Invoke(__instance, _arg);
                    _arg[0] = v; _setV.Invoke(__instance, _arg);
                    if (Navigator.WalkRun && _setRun != null) { _arg[0] = true; _setRun.Invoke(__instance, _arg); }
                }
                else Navigator.CheckDeadEnd(p, rawH, rawV);
            }
            catch (Exception e) { Log.Error("PlayerInputPost", e); }
        }
    }
}
