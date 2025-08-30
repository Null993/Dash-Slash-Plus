using System;
using System.Collections.Generic;
using Modding;
using UnityEngine;
using Satchel.BetterMenus;
using HutongGames.PlayMaker;

namespace DashSlashNoCharge
{
    public class GlobalSettings
    {
        public bool Enabled = true;
        public bool DebugLogs = false;
        public bool EntertainmentMode = false;
    }

    public class DashSlashNoChargeMod : Mod, IGlobalSettings<GlobalSettings>, IMenuMod
    {
        public static GlobalSettings GS = new GlobalSettings();
        private HeroController _hero;
        private PlayMakerFSM _nailArtsFsm;

        public DashSlashNoChargeMod() : base("Dash Slash Plus") { }
        public override string GetVersion() { return "1.1.0"; }

        // --- Settings ---
        public void OnLoadGlobal(GlobalSettings s) { GS = s ?? new GlobalSettings(); }
        public GlobalSettings OnSaveGlobal() { return GS; }

        public bool ToggleButtonInsideMenu { get { return true; } }

        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            return new List<IMenuMod.MenuEntry>
            {
                new IMenuMod.MenuEntry(
                    "Enable mod",
                    new string[]{"Off","On"},
                    "Turn the mod on or off",
                    i => GS.Enabled = (i == 1),
                    () => GS.Enabled ? 1 : 0
                ),
                new IMenuMod.MenuEntry(
                    "Debug logs",
                    new string[]{"Off","On"},
                    "Enable extra logging",
                    i => GS.DebugLogs = (i == 1),
                    () => GS.DebugLogs ? 1 : 0
                ),
                new IMenuMod.MenuEntry(
                    "Entertainment mode\n\n\n" +
                    "(Dash slash anytime)",
                    new string[]{"Off","On"},
                    "Trigger dash slash on any attack key press.\n"+
                    "Clicking too quickly will cause the attack to be lost.",
                    i => GS.EntertainmentMode = (i == 1),
                    () => GS.EntertainmentMode ? 1 : 0
                )
            };
        }

        // --- Hooks ---
        public override void Initialize()
        {
            On.HeroController.Awake += Hero_Awake;
            On.HeroController.Update += Hero_Update;
        }

        private void Hero_Awake(On.HeroController.orig_Awake orig, HeroController self)
        {
            orig(self);
            _hero = self;
            TryLocateNailArtsFsm();
        }

        private void Hero_Update(On.HeroController.orig_Update orig, HeroController self)
        {
            orig(self);
            if (!GS.Enabled || self == null) return;

            var ia = InputHandler.Instance?.inputActions;
            bool attackPressed = ia != null && ia.attack != null && ia.attack.WasPressed;
            if (!attackPressed) return;

            // entertainment mode: always trigger
            if (GS.EntertainmentMode)
            {
                TryDashSlash(self);
                return;
            }

            // normal mode: only trigger if actually dashing
            if (self.cState.dashing)
            {
                TryDashSlash(self);
            }
        }

        private void TryDashSlash(HeroController self)
        {
            if (_nailArtsFsm == null) TryLocateNailArtsFsm();
            if (_nailArtsFsm != null)
            {
                DebugLog("Forcing FSM state to Dash Slash Ready, then sending DASH END");
                _nailArtsFsm.SetState("Dash Slash Ready");
                _nailArtsFsm.SendEvent("DASH END");
            }
        }

        private void TryLocateNailArtsFsm()
        {
            if (_hero == null) _hero = HeroController.instance;
            if (_hero == null) return;
            _nailArtsFsm = _hero.gameObject.LocateMyFSM("Nail Arts");
            if (_nailArtsFsm != null)
            {
                DebugLog("Located Nail Arts FSM");
            }
            else
            {
                DebugLog("Nail Arts FSM not found");
            }
        }

        private static void Log(string msg) { Modding.Logger.Log("[DashSlashPlus] " + msg); }
        private static void DebugLog(string msg) { if (GS.DebugLogs) Log(msg); }
    }
}
