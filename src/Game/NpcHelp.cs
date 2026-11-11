using System;
using System.Collections;
using System.Text;
using RCG;
using UnityEngine;

namespace RCGAccess
{
    /// <summary>Talking to people (quest NPCs). Logs why the game does not start a conversation and, when X is held at a
    /// person the game offers but nothing happens, starts the conversation the way the game would (all of the game's own
    /// checks such as fights still apply).</summary>
    public static class NpcHelp
    {
        private static float _lastInteractAt = -10f;
        private static float _xHeldSince = -1f;
        private static bool _helpedThisHold;

        /// <summary>Postfix of InteractEntity_QuestNPC.Interact(int playerID, out bool).</summary>
        public static void OnInteract(InteractEntity_QuestNPC npc, int player, bool success)
        {
            _lastInteractAt = Time.realtimeSinceStartup;
            if (player != 0) return;
            UiReader.OnNpcTalk(npc, success);
            if (!success) Log.Write("Gespraech abgelehnt: " + State(npc));
        }

        private static string State(InteractEntity_QuestNPC npc)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(npc.name).Append(" canInteract ").Append(npc.CanInteract)
              .Append(" noInteract ").Append(UiReader.Traverse(npc, "_noInteract"))
              .Append(" forced ").Append(UiReader.Traverse(npc, "_forcedCannotInteract"))
              .Append(" angenommen ").Append(UiReader.Traverse(npc, "_questAccepted"))
              .Append(" wartezeit ").Append(UiReader.Traverse(npc, "_timeLastSuccessfulInteraction"))
              .Append(" halten ").Append(npc._howLongToHold.ToString("F2"));
            try { sb.Append(" Kampfsperre ").Append(ScreenLock.Instance != null ? ScreenLock.Instance.GetState.ToString() : "keine"); } catch { }
            return sb.ToString();
        }

        private static string PlayerState(RCG.Player p)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                object st = p.Fsm != null ? UiReader.Traverse(p.Fsm, "_currentState") : null;
                sb.Append("Spieler Zustand ").Append(st != null ? st.GetType().Name : "?")
                  .Append(" CanInteract ").Append(p.CanInteract())
                  .Append(" Haltezeit ").Append(p.GetTimeHoldInteraction().ToString("F2"))
                  .Append(" traegt ").Append(p.PickupObject != null ? p.PickupObject.name : "nichts");
                PlayerInput pi = p.GetComponent<PlayerInput>();
                if (pi != null) sb.Append(" imBereich ").Append(pi.InInteractRange).Append(" InteractQuick ").Append(pi.InteractQuick);
                foreach (InteractableEntityCollider c in p.GetComponentsInChildren<InteractableEntityCollider>())
                {
                    sb.Append(" | ").Append(c.name).Append(" stays ").Append(c.Stays.Count);
                    IList list = UiReader.Traverse(c, "_stayIInteractables") as IList;
                    if (list != null)
                        foreach (object o in list)
                        {
                            MonoBehaviour mb = o as MonoBehaviour;
                            IInteractable ii = o as IInteractable;
                            sb.Append(" [").Append(mb != null ? mb.name : "null").Append(" prio ").Append(ii != null ? ii.InteractPriority : 0).Append("]");
                        }
                }
            }
            catch (Exception e) { sb.Append(" Fehler ").Append(e.Message); }
            return sb.ToString();
        }

        /// <summary>Called every frame (not in the mod menu).</summary>
        public static void Update()
        {
            try
            {
                float now = Time.realtimeSinceStartup;
                bool xHeld = ModInput.PadHeld(Pad.X);
                if (!xHeld) { _xHeldSince = -1f; _helpedThisHold = false; return; }
                if (_xHeldSince < 0) _xHeldSince = now;
                if (_helpedThisHold || now - _xHeldSince < 1.5f) return;
                if (GameInfo.Quiet) return;
                RCG.Player p = GameInfo.Player;
                if (p == null) return;
                // the person the game currently offers
                InteractEntity_QuestNPC npc = null;
                foreach (InteractEntity_QuestNPC n in UnityEngine.Object.FindObjectsOfType<InteractEntity_QuestNPC>())
                    if (n != null && n.isActiveAndEnabled && Navigator.InRange(n)) { npc = n; break; }
                if (npc == null) return;
                _helpedThisHold = true;
                if (now - _lastInteractAt < 2.5f) return; // the game did react
                Log.Write("X gehalten bei Person ohne Gespraech. " + State(npc) + " | " + PlayerState(p));
                if (Combat.ScreenLockRunning) { Speech.Say(Loc.T("npc_fight"), true); return; }
                bool ok;
                npc.Interact(0, out ok);
                Log.Write("Gespraech vom Mod gestartet: " + (ok ? "ja" : "nein"));
                if (!ok) Speech.Say(Loc.T("npc_no"), true);
            }
            catch (Exception e) { Log.Error("NpcHelp", e); }
        }
    }
}
