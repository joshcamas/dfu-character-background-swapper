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
using DaggerfallWorkshop.Utility;
using DaggerfallConnect.Utility;

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
            public bool lastWasNight;

            public BackgroundScreen(UserInterfaceWindow window, PaperDoll paperDoll)
            {
                this.window = window;
                this.paperDoll = paperDoll;
                this.backgroundPanel = (Panel)GetFieldValue("backgroundPanel", paperDoll);
            }

            private void UpdateCurrentTexture()
            {
                string vanillaName = null;

                //Apply builtin racial effect names
                vanillaName = TryGetVampirismTextureName();
                vanillaName = TryGetLycanthropyTextureName();

                //Get vanilla image string
                if(vanillaName == null)
                    vanillaName = (string)RunMethod("GetPaperDollBackground", paperDoll, GameManager.Instance.PlayerEntity);

                if (vanillaName == null)
                {
                    Debug.LogError("Could not find vanilla background");
                    return;
                }

                Texture2D texture = null;

                //If night time, try finding night variant
                if (DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.IsNight)
                    texture = TryFindTexture(vanillaName, "_NIGHT");

                //Failed to find texture, so pick vanilla texture
                if (texture == null)
                {
                    texture = ImageReader.GetTexture(vanillaName, 0, 0, false);
                }

                //Apply to background panel
                backgroundPanel.BackgroundTexture = ImageReader.GetSubTexture(texture, backgroundSubRect, backgroundFullSize);
                backgroundPanel.Size = new Vector2(paperDollWidth, paperDollHeight);

                //Save last update

                lastSetBackground = backgroundPanel.BackgroundTexture;
                lastWasNight = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.IsNight;
            }

            private Texture2D TryFindTexture(string vanillaName, string postfix)
            {
                //Note removal of IMG. this is to get around a DFU bug
                var newTexture = Path.GetFileNameWithoutExtension(vanillaName) + postfix + Path.GetExtension(vanillaName);

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
                if (!this.window.Enabled)
                    return;

                bool timeChange = lastWasNight != DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.IsNight;

                if (backgroundPanel.BackgroundTexture != lastSetBackground || timeChange)
                {
                    UpdateCurrentTexture();
                }

            }
        }

        private static Mod mod;

        private BackgroundScreen inventoryBackgroundScreen;
        private BackgroundScreen characterSheetBackgroundScreen;

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

        }

        private void LateUpdate()
        {
            inventoryBackgroundScreen.Update();
            characterSheetBackgroundScreen.Update();
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
