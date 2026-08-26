using BigRetail.Core.Session;
using BigRetail.Departments.Unity;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.Sidewalks;
using BigRetail.Purchasing.Unity;
using BigRetail.Receiving.Unity;
using BigRetail.StoreLayouts.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BigRetail.StoreLayouts.Editor
{
    /// <summary>
    /// Resolves the real active-scene composition for editor-only Workshop
    /// commands. It never creates substitute runtime state.
    /// </summary>
    public static class StoreLayoutWorkshopRuntime
    {
        public static bool TryCreateLoader(
            out StoreLayoutRuntimeLoader loader,
            out string error)
        {
            loader = null;

            if (!EditorApplication.isPlaying)
            {
                error = "Enter Play Mode through Map Workshop first.";
                return false;
            }

            if (!MapWorkshopSession.IsActive)
            {
                error =
                    "The current Play Mode session was not launched by Map "
                    + "Workshop.";
                return false;
            }

            Scene scene = SceneManager.GetActiveScene();

            if (!scene.IsValid() || !scene.isLoaded)
            {
                error = "The active Workshop scene is not loaded.";
                return false;
            }

            GridMapHost mapHost = FindInScene<GridMapHost>(scene);
            FoundationRuntimeHost foundationHost =
                FindInScene<FoundationRuntimeHost>(scene);
            SidewalkRuntimeHost sidewalkHost =
                FindInScene<SidewalkRuntimeHost>(scene);
            FloorRuntimeHost floorHost =
                FindInScene<FloorRuntimeHost>(scene);
            FixtureRuntimeHost fixtureHost =
                FindInScene<FixtureRuntimeHost>(scene);
            FixtureEquipmentRuntimeHost fixtureEquipmentHost =
                FindInScene<FixtureEquipmentRuntimeHost>(scene);
            DepartmentRuntimeHost departmentHost =
                FindInScene<DepartmentRuntimeHost>(scene);
            ReceivingAreaRuntimeHost receivingHost =
                FindInScene<ReceivingAreaRuntimeHost>(scene);

            if (mapHost == null
                || foundationHost == null
                || sidewalkHost == null
                || floorHost == null
                || fixtureHost == null
                || fixtureEquipmentHost == null
                || departmentHost == null
                || receivingHost == null)
            {
                error =
                    "The active scene is missing one or more permanent store "
                    + "runtime hosts required by Map Workshop.";
                return false;
            }

            if (!fixtureEquipmentHost.TryInitialize()
                || fixtureEquipmentHost.Plans == null)
            {
                error = string.IsNullOrWhiteSpace(
                    fixtureEquipmentHost.InitializationError)
                        ? "The fixture equipment planning runtime could not "
                            + "initialize for Map Workshop."
                        : fixtureEquipmentHost.InitializationError;
                return false;
            }

            loader =
                new StoreLayoutRuntimeLoader(
                    mapHost,
                    foundationHost,
                    sidewalkHost,
                    floorHost,
                    fixtureHost,
                    fixtureEquipmentHost.Plans,
                    departmentHost,
                    receivingHost);
            error = string.Empty;
            return true;
        }


        private static T FindInScene<T>(
            Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int index = 0;
                 index < roots.Length;
                 index++)
            {
                T component =
                    roots[index].GetComponentInChildren<T>(true);

                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
