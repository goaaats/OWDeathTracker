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
            ModHelper.Console.WriteLine("Starting PlaceMakers()...");

            var prefab = markerAssetBundle.LoadAsset("assets/deathindicator/dvdeathindicator.prefab");

            var trackedDeaths = ShowOtherProfiles ? tracking.TrackedDeaths : tracking.GetAllForProfile(ActiveProfile);

            foreach (var death in trackedDeaths)
            {
                var ao = WeirdGetAstroObject(death.AstroObjectName);

                var go = Instantiate(prefab, ao.transform) as GameObject;

                //go.transform.parent = ao.transform;
                go.transform.localPosition = new Vector3(death.PositionX, death.PositionY, death.PositionZ);

                go.transform.LookAt(ao.transform);
                go.transform.Rotate(Vector3.right, -90);

                var timeSpan = TimeSpan.FromSeconds(death.SecondsElapsed);
                var marker = go.GetAddComponent<DeathMarker>();
                marker.InfoLabelContent = $"#{death.LoopCount}, {timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                marker.NameLabelContent = death.ProfileName;

                ModHelper.Console.WriteLine($"Placed marker at death bound: {death.AstroObjectName}\nx:{death.PositionX} y:{death.PositionY} z:{death.PositionZ}", MessageType.Success);
            }
        }

        private static Sector.Name[] ignoredSectors = new[]
        {
            Sector.Name.Ship, Sector.Name.Unnamed, Sector.Name.BrambleDimension, Sector.Name.Vessel,
            Sector.Name.VesselDimension, Sector.Name.HourglassTwins
        };

        struct WorkAstroObject
        {
            public AstroObject AstroObject;
            public float Distance;
        }

        private void OnGUI()
        {

        }

        private static AstroObject GetAstroObjectFromSector(IEnumerable<AstroObject> astroObjects, Sector sector)
        {
            switch (sector.GetName())
            {
                case Sector.Name.Unnamed:
                    throw new ArgumentException("Cannot get AstroObject for unnamed sector");
                case Sector.Name.Sun:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.Sun);
                case Sector.Name.HourglassTwin_A:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.CaveTwin);
                case Sector.Name.HourglassTwin_B:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.TowerTwin);
                case Sector.Name.TimberHearth:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.TimberHearth);
                case Sector.Name.BrittleHollow:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.BrittleHollow);
                case Sector.Name.GiantsDeep:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.GiantsDeep);
                case Sector.Name.DarkBramble:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.DarkBramble);
                case Sector.Name.Comet:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.Comet);
                case Sector.Name.QuantumMoon:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.QuantumMoon);
                case Sector.Name.TimberMoon:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.TimberMoon);
                case Sector.Name.BrambleDimension:
                    throw new ArgumentException($"Cannot get AstroObject for {sector}");
                case Sector.Name.VolcanicMoon:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.VolcanicMoon);
                case Sector.Name.OrbitalProbeCannon:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.ProbeCannon);
                case Sector.Name.EyeOfTheUniverse:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.Eye);
                case Sector.Name.Ship:
                    throw new ArgumentException($"Cannot get AstroObject for {sector}");
                case Sector.Name.SunStation:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.SunStation);
                case Sector.Name.WhiteHole:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.WhiteHole);
                case Sector.Name.TimeLoopDevice:
                    throw new ArgumentException($"Cannot get AstroObject for {sector}");
                case Sector.Name.Vessel:
                    throw new ArgumentException($"Cannot get AstroObject for {sector}");
                case Sector.Name.VesselDimension:
                    throw new ArgumentException($"Cannot get AstroObject for {sector}");
                case Sector.Name.HourglassTwins:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.HourglassTwins);
                case Sector.Name.InvisiblePlanet:
                    return astroObjects.First(x => x.GetAstroObjectName() == AstroObject.Name.InvisiblePlanet);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void OnPlayerDeath(DeathType deathType)
        {
            var cc = Locator.GetPlayerController();

            var boundObject = GetBoundAstroObject();
            var boundObjectName = boundObject.GetAstroObjectName();

            var relativePos = boundObject.transform.InverseTransformPoint(cc.transform.position);

            tracking.TrackedDeaths.Add(new DeathTracking.Death
            {
                ProfileName = ActiveProfile,

                AstroObjectName = boundObjectName,
                DeathType = deathType,
                LoopCount = TimeLoop.GetLoopCount(),
                SecondsElapsed = TimeLoop.GetSecondsElapsed(),

                PositionX = relativePos.x,
                PositionY = relativePos.y,
                PositionZ = relativePos.z
            });
        }

        private static AstroObject WeirdGetAstroObject(AstroObject.Name name)
        {
            return Object.FindObjectsOfType(typeof(AstroObject)).Cast<AstroObject>().First(x => x.GetAstroObjectName() == name);
        }

        private static string GetAstroObjectName(AstroObject astroObject)
        {
            return astroObject.GetAstroObjectName() == AstroObject.Name.CustomString
                ? astroObject.GetCustomName()
                : astroObject.GetAstroObjectName().ToString();
        }

        private static AstroObject GetBoundAstroObject()
        {
            var cc = Locator.GetPlayerController();

            if (cc == null)
                throw new Exception("PlayerCharacterController was null.");

            var sectors = Object.FindObjectsOfType(typeof(Sector)).Cast<Sector>();
            var astroObjects = Object.FindObjectsOfType(typeof(AstroObject)).Cast<AstroObject>();

            var candidateSectors = sectors.Where(x => x.transform.gameObject.activeInHierarchy && !x.IsBrambleDimension() &&
                                                      x.ContainsAnyOccupants(DynamicOccupant.Player) && ignoredSectors.All(y => y != x.GetName())).ToArray();

            var candidateObjects = candidateSectors.Select(x =>
            {
                var astroObj = GetAstroObjectFromSector(astroObjects, x);
                var dist = Vector3.Distance(astroObj.transform.position, cc.transform.position);

                return new WorkAstroObject
                {
                    AstroObject = astroObj,
                    Distance = dist
                };
            }).OrderBy(x => x.Distance).ToArray();

            return candidateObjects.Length == 0
                ? Locator.GetAstroObject(AstroObject.Name.Sun)
                : candidateObjects.First().AstroObject;
        }

        public static bool ShowOtherProfiles { get; private set; }

        public override void Configure(IModConfig config)
        {
            ShowOtherProfiles = config.GetSettingsValue<bool>("showOtherProfiles");
        }
    }
}
