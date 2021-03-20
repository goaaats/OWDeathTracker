using OWML.ModHelper;
using OWML.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Goaaats.DeathTracker
{
    public class DeathTrackerMod : ModBehaviour
    {
        private void Awake()
        {
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.

            //Application.runInBackground = true;
            
        }

        private void Start()
        {

            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;

            ModHelper.HarmonyHelper.AddPrefix<PlayerCharacterController>("OnPlayerDeath", typeof(DeathTrackerMod), nameof(OnPlayerDeath));

            ModHelper.Console.WriteLine($"{nameof(DeathTrackerMod)} was loaded!asd", MessageType.Success);
        }

        private void OnCompleteSceneLoad(OWScene oldScene, OWScene newScene)
        {
            ModHelper.Console.WriteLine($"COMPLETE SCENE LOAD ({oldScene} -> {newScene})", MessageType.Info);

            var universe = newScene == OWScene.SolarSystem || newScene == OWScene.EyeOfTheUniverse;
            if (universe)
            {
                
            }
        }

        private void OnDestroy()
        {
            //LoadManager.OnCompleteSceneLoad -= OnCompleteSceneLoad;
            //ModHelper.HarmonyHelper.Unpatch<PlayerCharacterController>("OnPlayerDeath", PatchType.Prefix);

            //GlobalMessenger<DeathType>.RemoveListener("PlayerDeath", OnPlayerDeath);

            //ModHelper.Console.WriteLine($"OnDestroy!", MessageType.Success);
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
            if (boundObject == null)
                return;

            GUI.Label(new Rect(0, 0, 400, 400), $"Last death bound: {GetAstroObjectName(boundObject)}");
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

        private static AstroObject boundObject;

        private static void OnPlayerDeath(DeathType deathType)
        {
            boundObject = GetBoundAstroObject();
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

            return candidateObjects.Length == 0 ? Locator.GetAstroObject(AstroObject.Name.Sun) : candidateObjects.First().AstroObject;
        }
    }
}
