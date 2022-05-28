

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using Mono.Cecil;
using Mono.Cecil.Cil;

using UnityEditor;
using UnityEngine;

#if H3VR_IMPORTED
using OtherLoader;
using FistVR;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
#endif

namespace MeatKit
{

    [System.Serializable]
    public class BepinexDepPair
    {
        public string guid;
        public string version;
    }


    [CreateAssetMenu(menuName = "MeatKit/Build Items/OtherLoader Root", fileName = "BuildRootNew")]
    public class OtherLoaderBuildRoot : BuildItem
    {
#if H3VR_IMPORTED

        [Tooltip("Build items that should load first, in the order they appear")]
        public List<OtherLoaderBuildItem> BuildItemsFirst = new List<OtherLoaderBuildItem>();

        [Tooltip("Build items that should in parralel, after the first items load")]
        public List<OtherLoaderBuildItem> BuildItemsAny = new List<OtherLoaderBuildItem>();

        [Tooltip("Build items that should load last, in the order they appear")]
        public List<OtherLoaderBuildItem> BuildItemsLast = new List<OtherLoaderBuildItem>();

        [Tooltip("Guids of otherloader mods that must be loaded before these assets will load. Only applies to SelfLoading mods")]
        public List<string> LoadDependancies = new List<string>();

        [Tooltip("Guids of bepinex mods that this mod will depend on")]
        public List<BepinexDepPair> BepinexDependancies = new List<BepinexDepPair>();

        [Tooltip("When true, additional code will be generated that allows the mod to automatically load itself into otherloader")]
        public bool SelfLoading = true;

        public override IEnumerable<string> RequiredDependencies
        {
            get { return new[] { "devyndamonster-OtherLoader-1.3.0" }; }
        }

        public override Dictionary<string, BuildMessage> Validate()
        {
            var messages = base.Validate();

            List<OtherLoaderBuildItem> allBuildItems = BuildItemsFirst.Concat(BuildItemsAny).Concat(BuildItemsLast).ToList();

            ValidateBuildItems(messages, BuildItemsFirst, allBuildItems, "BuildItemsFirst");
            ValidateBuildItems(messages, BuildItemsAny, allBuildItems, "BuildItemsAny");
            ValidateBuildItems(messages, BuildItemsLast, allBuildItems, "BuildItemsLast");

            return messages;
        }

        private void ValidateBuildItems(
            Dictionary<string, BuildMessage> messages,
            List<OtherLoaderBuildItem> targetBuildItems,
            List<OtherLoaderBuildItem> allBuildItems,
            string messageField)
        {
            foreach (OtherLoaderBuildItem buildItem in targetBuildItems)
            {
                if (buildItem == null)
                {
                    messages[messageField] = BuildMessage.Error("Child build item cannot be null!");
                    continue;
                }

                if (allBuildItems.Count(o => o != null && buildItem != null && o.BundleName == buildItem.BundleName) > 1)
                {
                    messages[messageField] = BuildMessage.Error("Child build items must have unique bundle names!");
                }

                var itemMessages = buildItem.Validate();
                itemMessages.ToList().ForEach(o => { messages[messageField] = o.Value; });
            }
        }

        public override List<AssetBundleBuild> ConfigureBuild()
        {
            List<AssetBundleBuild> bundles = new List<AssetBundleBuild>();

            BuildItemsFirst.ForEach(o => { bundles.AddRange(o.ConfigureBuild()); });

            BuildItemsAny.ForEach(o => { bundles.AddRange(o.ConfigureBuild()); });

            BuildItemsLast.ForEach(o => { bundles.AddRange(o.ConfigureBuild()); });

            return bundles;
        }

        public override void GenerateLoadAssets(TypeDefinition plugin, ILProcessor il)
        {
            EnsurePluginDependsOn(plugin, "h3vr.otherloader", "1.3.0");

            foreach (BepinexDepPair dependancy in BepinexDependancies)
            {
                EnsurePluginDependsOn(plugin, dependancy.guid, dependancy.version);
            }

            //If set to self load, we add a bunch of code to load the items
            if (SelfLoading)
            {
                //Create lists of the bundles
                string[] loadFirst = BuildItemsFirst.Select(o => o.BundleName.ToLower()).ToArray();
                string[] loadAny = BuildItemsAny.Select(o => o.BundleName.ToLower()).ToArray();
                string[] loadLast = BuildItemsLast.Select(o => o.BundleName.ToLower()).ToArray();


                // Get references to the path and the method we're calling
                var publicStatic = BindingFlags.Public | BindingFlags.Static;
                FieldReference basePath = plugin.Fields.First(f => f.Name == "BasePath");
                var otherloaderRegisterLoad = typeof(OtherLoader.OtherLoader).GetMethod("RegisterDirectLoad", publicStatic);


                // Now load the path, guid, dependancies, and pass the 3 arrays of bundle names
                il.Emit(OpCodes.Ldsfld, basePath);
                il.Emit(OpCodes.Ldstr, BuildWindow.SelectedProfile.Author + "." + BuildWindow.SelectedProfile.PackageName);
                il.Emit(OpCodes.Ldstr, string.Join(",", LoadDependancies.ToArray()));
                il.Emit(OpCodes.Ldstr, string.Join(",", loadFirst));
                il.Emit(OpCodes.Ldstr, string.Join(",", loadAny));
                il.Emit(OpCodes.Ldstr, string.Join(",", loadLast));
                il.Emit(OpCodes.Call, plugin.Module.ImportReference(otherloaderRegisterLoad));
            }
        }
#endif
    }
}

