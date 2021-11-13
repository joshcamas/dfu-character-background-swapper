using System.Reflection;
using System.IO;

using UnityEngine;

using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Utility;
using DaggerfallConnect.Utility;
using System.Collections.Generic;

namespace SpellcastStudios.TODBackgrounds
{
    public class TODBackgrounds : MonoBehaviour
    {
        private class BackgroundScreen
        {
            //TODO: Use reflection instead of hardcoding it here
            const int paperDollWidth = 110;
            const int paperDollHeight = 184;
            readonly DFSize backgroundFullSize = new DFSize(125, 198);
            readonly Rect backgroundSubRect = new Rect(8, 7, paperDollWidth, paperDollHeight);

            public UserInterfaceWindow window;
            public PaperDoll paperDoll;
            public Panel backgroundPanel;

            public Texture2D lastSetBackground;
            public List<string> lastTags;

            public BackgroundScreen(UserInterfaceWindow window, PaperDoll paperDoll)
            {
                UpdateWindow(window, paperDoll);
            }

            public void UpdateWindow(UserInterfaceWindow window, PaperDoll paperDoll)
            {
                this.window = window;
                this.paperDoll = paperDoll;

                if (paperDoll != null)
                    this.backgroundPanel = (Panel)GetFieldValue("backgroundPanel", paperDoll);
            }

            private bool RequiresChange()
            {
                if (lastTags == null)
                    return false;

                var newTags = GetTags();

                for (int i = 0; i < lastTags.Count; i++)
                {
                    if (newTags[i] != lastTags[i])
                        return true;
                }
                return false;
            }

            private List<string> GetTags()
            {
                string custom = "";

                if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeonCastle)
                    // return new List<string> { "_CASTLE", "", "" };
                    custom = "_CASTLE";

                else if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
                    // return new List<string> { "_DUNGEON", "", "" };
                    custom = "_DUNGEON";

                else if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideOpenShop)
                    // return new List<string> { "_OPENSHOP", "", "" };
                    custom = "_OPENSHOP";

                else if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideTavern)
                    custom = "_TAVERN";

                else if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
                    // return new List<string> { "_BUILDING", "", "" };
                    custom = "_BUILDING";


                string weather = "";

                var weatherType = GameManager.Instance.WeatherManager.PlayerWeather.WeatherType;

                if (weatherType == WeatherType.Rain || weatherType == WeatherType.Rain_Normal)
                    weather = "_RAIN";

                else if (weatherType == WeatherType.Snow || weatherType == WeatherType.Snow_Normal)
                    weather = "_SNOW";

                else if (weatherType == WeatherType.Thunder)
                    weather = "_STORM";

                else if (weatherType == WeatherType.Fog)
                    weather = "_FOG";

                else if (weatherType == WeatherType.Overcast)
                    weather = "_OVERCAST";

                string time = "";

                if (DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.IsNight)
                    time = "_NIGHT";

                return new List<string> { custom, weather, time };
            }

            public void UpdateCurrentTexture()
            {
                string vanillaName = null;

                //Apply builtin racial effect names
                vanillaName = TryGetVampirismTextureName();
                vanillaName = TryGetLycanthropyTextureName();

                //Get vanilla image string
                if (vanillaName == null)
                    vanillaName = (string)RunMethod("GetPaperDollBackground", paperDoll, GameManager.Instance.PlayerEntity);

                if (vanillaName == null)
                {
                    Debug.LogError("Could not find vanilla background");
                    return;
                }

                string modVanillaName = vanillaName;

                if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
                    modVanillaName = "interior.IMG";

                Texture2D texture = TryFindTexture(modVanillaName, "");

                var tags = GetTags();

                for (int i = 0; i < tags.Count; i++)
                {
                    for (int k = 0; k < tags.Count; k++)
                    {
                        if (i == k)
                            continue;

                        string tagi = tags[i];
                        string tagk = tags[k];

                        if (tagi == "" && tagk == "")
                            continue;

                        var nt = TryFindTexture(modVanillaName, tagi + tagk);

                        if (nt != null)
                            texture = nt;
                    }
                }
                if (texture == null)
                    texture = TryFindTexture(modVanillaName, tags[0]);

                //Failed to find texture, so pick vanilla texture
                if (texture == null)
                    texture = ImageReader.GetTexture(vanillaName, 0, 0, false);

                //Apply to background panel
                backgroundPanel.BackgroundTexture = ImageReader.GetSubTexture(texture, backgroundSubRect, backgroundFullSize);
                backgroundPanel.Size = new Vector2(paperDollWidth, paperDollHeight);

                //Save last update

                lastSetBackground = backgroundPanel.BackgroundTexture;
                lastTags = tags;
            }

            private Texture2D TryFindTexture(string vanillaName, string postfix)
            {
                //Note removal of IMG. this is to get around a DFU bug
                var newTexture = Path.GetFileNameWithoutExtension(vanillaName) + postfix + Path.GetExtension(vanillaName);

                Debug.Log("Trying to find " + newTexture);

                Texture2D output;

                if (TextureReplacement.TryImportImage(newTexture, false, out output))
                    return output;
                return null;
            }

            //Hardcoded grab of varmpirism background
            private string TryGetVampirismTextureName()
            {
                VampirismEffect racialOverride = GameManager.Instance.PlayerEffectManager.GetRacialOverrideEffect() as VampirismEffect;

                if (racialOverride == null)
                    return null;

                return "SCBG08I0.IMG";
            }

            //Hardcoded grab of lycanthropy background
            private string TryGetLycanthropyTextureName()
            {
                const string werewolfBackground = "WOLF00I0.IMG";
                const string wereboarBackground = "BOAR00I0.IMG";

                LycanthropyEffect racialOverride = GameManager.Instance.PlayerEffectManager.GetRacialOverrideEffect() as LycanthropyEffect;

                if (racialOverride == null)
                    return null;

                // Do nothing if not transformed
                if (!racialOverride.IsTransformed)
                    return null;

                // Get source texture based on lycanthropy type
                string filename;
                switch (racialOverride.InfectionType)
                {
                    case LycanthropyTypes.Werewolf:
                        filename = werewolfBackground;
                        break;
                    case LycanthropyTypes.Wereboar:
                        filename = wereboarBackground;
                        break;
                    default:
                        return null;
                }

                return filename;
            }

            public void Update()
            {
                if (window == null || !window.Enabled)
                    return;

                if (backgroundPanel.BackgroundTexture != lastSetBackground || RequiresChange())
                {
                    UpdateCurrentTexture();
                }

            }
        }

        private static Mod mod;

        private BackgroundScreen inventoryBackgroundScreen;
        private BackgroundScreen characterSheetBackgroundScreen;
        private BackgroundScreen tradeBackgroundScreen;

        private UserInterfaceManager uiManager;
        private bool topWindowIsTradeWindow = false;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject("Character Background Swapper");
            go.AddComponent<TODBackgrounds>();
        }

        private void Start()
        {
            var inventoryWindow = (DaggerfallInventoryWindow)GetFieldValue("dfInventoryWindow", DaggerfallUI.Instance);
            var inventoryPaperDoll = (PaperDoll)GetFieldValue("paperDoll", inventoryWindow);

            inventoryBackgroundScreen = new BackgroundScreen(inventoryWindow, inventoryPaperDoll);

            var characterSheetWindow = (DaggerfallCharacterSheetWindow)GetFieldValue("dfCharacterSheetWindow", DaggerfallUI.Instance);
            var characterSheetPaperDoll = (PaperDoll)GetFieldValue("characterPortrait", characterSheetWindow);

            characterSheetBackgroundScreen = new BackgroundScreen(characterSheetWindow, characterSheetPaperDoll);

            uiManager = (UserInterfaceManager)GetFieldValue("uiManager", DaggerfallUI.Instance);
            uiManager.OnWindowChange += OnWindowChange;

            //Create container for trade window
            tradeBackgroundScreen = new BackgroundScreen(null, null);
        }

        //Detect when window changes and see if it's the trade menu
        private void OnWindowChange(object sender, System.EventArgs e)
        {
            var inventoryWindow = uiManager.TopWindow as DaggerfallTradeWindow;

            if (inventoryWindow != null && tradeBackgroundScreen.window != inventoryWindow)
            {
                var inventoryPaperDoll = (PaperDoll)GetFieldValue("paperDoll", inventoryWindow);

                tradeBackgroundScreen.UpdateWindow(inventoryWindow, inventoryPaperDoll);
                tradeBackgroundScreen.UpdateCurrentTexture();
            }
            else
            {
                inventoryBackgroundScreen = new BackgroundScreen(inventoryBackgroundScreen.window, inventoryBackgroundScreen.paperDoll);
            }
        }

        private void LateUpdate()
        {
            inventoryBackgroundScreen.Update();
            characterSheetBackgroundScreen.Update();
            tradeBackgroundScreen.Update();
        }

        private static object RunMethod(string method, object fromObject, params object[] parameters)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            return (fromObject?.GetType()?.GetMethod(method, flags))?.Invoke(fromObject, parameters);
        }
        private static object GetFieldValue(string field, object fromObject)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            return (fromObject?.GetType()?.GetField(field, flags))?.GetValue(fromObject);
        }
    }
}
