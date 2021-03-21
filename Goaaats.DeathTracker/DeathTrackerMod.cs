using OWML.ModHelper;
using OWML.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Harmony;
using OWML.Utils;
using Tessellation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Goaaats.DeathTracker
{
    public class DeathTrackerMod : ModBehaviour
    {
        private void Awake()
        {
            //Application.runInBackground = true;
        }

        private AssetBundle markerAssetBundle;
        private static readonly DeathTracking tracking = new DeathTracking();

        private void Start()
        {
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;

            StandaloneProfileManager.SharedInstance.OnProfileReadDone += OnProfileReadDone;
            StandaloneProfileManager.SharedInstance.OnProfileDataSaved += OnProfileDataSaved;

            ModHelper.HarmonyHelper.AddPrefix<PlayerCharacterController>("OnPlayerDeath", typeof(DeathTrackerMod), nameof(OnPlayerDeath));
            ModHelper.HarmonyHelper.AddPrefix(typeof(PlayerData).GetMethod("ResetGame"), typeof(DeathTrackerMod), nameof(ResetGame));
            ModHelper.HarmonyHelper.AddPrefix(typeof(StandaloneProfileManager).GetMethod("DeleteProfile"), typeof(DeathTrackerMod), nameof(DeleteProfile));

            markerAssetBundle = ModHelper.Assets.LoadBundle("deathvizassets");

            ModHelper.Console.WriteLine($"{nameof(DeathTrackerMod)} was loaded! {SavePath}", MessageType.Success);
        }

        private void OnDestroy()
        {
            LoadManager.OnCompleteSceneLoad -= OnCompleteSceneLoad;

            StandaloneProfileManager.SharedInstance.OnProfileReadDone -= OnProfileReadDone;
            StandaloneProfileManager.SharedInstance.OnProfileDataSaved -= OnProfileDataSaved;
        }

        private static string SavePath => StandaloneProfileManager.SharedInstance.GetValue<string>("_profilesPath");
        private static string ActiveProfile => StandaloneProfileManager.SharedInstance.GetActiveProfile().profileName;

        private void OnProfileDataSaved(bool success)
        {
            var name = ActiveProfile;

            tracking.Save(SavePath);
            ModHelper.Console.WriteLine($"{tracking.TrackedDeaths.Count} Deaths saved.", MessageType.Success);
        }

        private void OnProfileReadDone()
        {
            var name = ActiveProfile;

            tracking.Load(SavePath);
            ModHelper.Console.WriteLine($"{tracking.TrackedDeaths.Count} Deaths loaded.", MessageType.Success);
        }

        private static void ResetGame()
        {
            tracking.Reset(ActiveProfile);
        }

        private static void DeleteProfile(string profileName)
        {
            tracking.Reset(profileName);
        }

        private void OnCompleteSceneLoad(OWScene oldScene, OWScene newScene)
        {
            ModHelper.Console.WriteLine($"COMPLETE SCENE LOAD ({oldScene} -> {newScene})", MessageType.Info);

            var universe = newScene == OWScene.SolarSystem || newScene == OWScene.EyeOfTheUniverse;
            if (universe)
            {
                PlaceMarkers();
            }
        }

        private void PlaceMarkers()
        {
            var prefab = markerAssetBundle.LoadAsset("assets/deathindicator/dvdeathindicator.prefab");

            var trackedDeaths = ShowOtherProfiles ? tracking.TrackedDeaths : tracking.GetAllForProfile(ActiveProfile);

            foreach (var death in trackedDeaths)
            {
                var ao = GetSectorFromScene(death.SectorName);

                var go = Instantiate(prefab, ao.transform) as GameObject;

                go.transform.localPosition = new Vector3(death.PositionX, death.PositionY, death.PositionZ);

                go.transform.LookAt(ao.transform);
                go.transform.Rotate(Vector3.right, -90);

                var timeSpan = TimeSpan.FromSeconds(death.SecondsElapsed);
                var marker = go.GetAddComponent<DeathMarker>();
                marker.InfoLabelContent = $"#{death.LoopCount}, {timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                marker.NameLabelContent = death.ProfileName;

                ModHelper.Console.WriteLine($"Placed marker at death bound: {death.SectorName}\nx:{death.PositionX} y:{death.PositionY} z:{death.PositionZ}", MessageType.Success);
            }
        }

        private static void OnPlayerDeath(DeathType deathType)
        {
            var cc = Locator.GetPlayerController();

            var boundSector = GetBoundSector();
            var boundSectorName = boundSector.GetName();

            var relativePos = boundSector.transform.InverseTransformPoint(cc.transform.position);

            tracking.TrackedDeaths.Add(new DeathTracking.Death
            {
                ProfileName = ActiveProfile,

                SectorName = boundSectorName,
                DeathType = deathType,
                LoopCount = TimeLoop.GetLoopCount(),
                SecondsElapsed = TimeLoop.GetSecondsElapsed(),

                PositionX = relativePos.x,
                PositionY = relativePos.y,
                PositionZ = relativePos.z
            });

            Debug.Log($"Death: {boundSectorName}");
        }

        #region SectorHandling

        private static readonly Sector.Name[] ignoredSectors = new[]
        {
            Sector.Name.Ship, Sector.Name.Unnamed, Sector.Name.BrambleDimension, Sector.Name.Vessel,
            Sector.Name.VesselDimension, Sector.Name.HourglassTwins
        };

        struct WorkSector
        {
            public Sector Sector;
            public float Distance;
        }

        private static Sector GetSectorFromScene(Sector.Name name)
        {
            return Object.FindObjectsOfType(typeof(Sector)).Cast<Sector>().First(x => x.GetName() == name);
        }

        private static Sector GetBoundSector()
        {
            var cc = Locator.GetPlayerController();

            if (cc == null)
                throw new Exception("PlayerCharacterController was null.");

            var sectors = Object.FindObjectsOfType(typeof(Sector)).Cast<Sector>().ToArray();

            var candidateSectors = sectors.Where(x => x.transform.gameObject.activeInHierarchy && !x.IsBrambleDimension() &&
                                                      x.ContainsAnyOccupants(DynamicOccupant.Player) && ignoredSectors.All(y => y != x.GetName())).ToArray();

            var candidateObjects = candidateSectors.Select(x =>
            {
                var dist = Vector3.Distance(x.transform.position, cc.transform.position);

                return new WorkSector
                {
                    Sector = x,
                    Distance = dist
                };
            }).OrderBy(x => x.Distance).ToArray();

            return candidateObjects.Length == 0
                ? sectors.First(x => x.GetName() == Sector.Name.Sun)
                : candidateObjects.First().Sector;
        }

        #endregion

        public static bool ShowOtherProfiles { get; private set; }

        public override void Configure(IModConfig config)
        {
            ShowOtherProfiles = config.GetSettingsValue<bool>("showOtherProfiles");
        }
    }
}
