// CustomerPatienceMenu.cs
using HarmonyLib;
using Kitchen;
using Kitchen.Modules;
using KitchenData;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace KitchenCustomerInfo
{
    public class CustomerPatienceMenu : Menu<MenuAction>
    {
        // Define the available color options as a dictionary
        public static readonly Dictionary<string, Color> colorOptions = new Dictionary<string, Color>
        {
            { "Cyan", Color.cyan },
            { "Red", Color.red },
            { "Yellow", Color.yellow },
            { "Green", Color.green },
            { "Blue", Color.blue },
            { "Purple", Color.magenta },
            { "White", Color.white },
        };

        // Current selected color keys
        private string currentHighlightColorKey;
        private string currentIndicatorColorKey;

        // Options for selection
        private Option<string> indicatorTimerOption;
        private Option<string> highlightOption;
        private Option<string> highlightColorOption;
        private Option<string> indicatorColorOption;
        private Option<string> layoutOption;

        public CustomerPatienceMenu(Transform container, ModuleList module_list)
            : base(container, module_list)
        {

        }

        public override void Setup(int player_id)
        {
            // Retrieve current preferences
            bool prefCPEnabled = PreferencesManager.Get<bool>("CustomerPatienceEnabled", true);
            bool prefHighlightEnabled = PreferencesManager.Get<bool>("HighlightEnabled", true);
            currentHighlightColorKey = PreferencesManager.Get<string>("HighlightIndicatorColor", "Cyan");
            currentIndicatorColorKey = PreferencesManager.Get<string>("StandardIndicatorColor", "Green");
            bool prefSwapPositions = PreferencesManager.Get<bool>("SwapPositions", false);

            // Initialize the selection options
            InitializeIndicatorTimerOption(prefCPEnabled, player_id);
            InitializeHighlightOption(prefHighlightEnabled, player_id);
            InitializeHighlightColorOption(player_id);
            InitializeIndicatorColorOption(player_id);
            InitializeLayoutOption(prefSwapPositions, player_id);

            // Add informational text
            this.AddInfoText(GameData.Main.GlobalLocalisation.GetIcon(PatienceReason.Service) + " Patience Indicator Settings");

            // Add Select Element for Layout
            this.AddLabel("Layout");
            this.AddSelect<string>(layoutOption);

            // Add Select Element for Indicator Timer
            this.AddLabel("Display Timer");
            this.AddSelect<string>(indicatorTimerOption);

            // Add Select Element for Indicator Color
            this.AddLabel("Indicator Color");
            this.AddSelect<string>(indicatorColorOption);

            // Add Select Element for Highlight Feature
            this.AddLabel("Priority Highlight");
            this.AddSelect<string>(highlightOption);
            // Add Select Element for Highlight Color
            if (prefHighlightEnabled)
            {
                this.AddSelect<string>(highlightColorOption);
            } else
            {
                this.New<SpacerElement>(true);
                this.New<SpacerElement>(true);
            }

            if (currentIndicatorColorKey == currentHighlightColorKey && prefHighlightEnabled)
            {
                this.AddLabel("<color=red>Indicator and Highlight colors are the same.</color>");
            } else
            {
                this.AddLabel("");
            }

            // Add Back Button to return to the previous menu
            this.AddButton("Back", delegate (int i)
            {
                this.RequestPreviousMenu();
            });
        }

        /// <summary>
        /// Initializes the color selection option for Highlight.
        /// </summary>
        /// <param name="player_id">Player ID.</param>
        private void InitializeHighlightColorOption(int player_id)
        {
            // Extract the list of color names
            List<string> colorKeys = new List<string>(colorOptions.Keys);
            // Create display names with color formatting
            List<string> colorDisplayNames = new List<string>();
            foreach (string key in colorKeys)
            {
                if (key.Equals("Cyan", StringComparison.OrdinalIgnoreCase))
                {
                    // Special formatting for Cyan to ensure proper color display
                    colorDisplayNames.Add($"<color=#00FFFFFF>{key}</color>");
                }
                else
                {
                    colorDisplayNames.Add($"<color=\"{key.ToLower()}\">{key}</color>");
                }
            }

            // Create the Option object for highlight color selection
            highlightColorOption = new Option<string>(
                colorKeys,                 // List of color keys
                currentHighlightColorKey,  // Currently selected color
                colorDisplayNames,         // Display names with color formatting
                null                        // No comparison delegate needed for string
            );

            // Subscribe to the OnChanged event to handle selection changes
            highlightColorOption.OnChanged += delegate (object _, string selectedColor)
            {
                currentHighlightColorKey = selectedColor;
                PreferencesManager.Set("HighlightIndicatorColor", selectedColor);
                Debug.Log($"[KitchenCustomerInfo] HighlightIndicatorColor set to: {selectedColor}");
                this.ModuleList.Clear();
                this.Setup(player_id);
            };
        }

        /// <summary>
        /// Initializes the color selection option for the standard indicator.
        /// </summary>
        /// <param name="player_id">Player ID.</param>
        private void InitializeIndicatorColorOption(int player_id)
        {
            // Extract the list of color names
            List<string> colorKeys = new List<string>(colorOptions.Keys);
            // Create display names with color formatting
            List<string> colorDisplayNames = new List<string>();
            foreach (string key in colorKeys)
            {
                if (key.Equals("Cyan", StringComparison.OrdinalIgnoreCase))
                {
                    // Special formatting for Cyan to ensure proper color display
                    colorDisplayNames.Add($"<color=#00FFFFFF>{key}</color>");
                }
                else
                {
                    colorDisplayNames.Add($"<color=\"{key.ToLower()}\">{key}</color>");
                }
            }

            // Create the Option object for indicator color selection
            indicatorColorOption = new Option<string>(
                colorKeys,                 // List of color keys
                currentIndicatorColorKey,  // Currently selected color
                colorDisplayNames,         // Display names with color formatting
                null                        // No comparison delegate needed for string
            );

            // Subscribe to the OnChanged event to handle selection changes
            indicatorColorOption.OnChanged += delegate (object _, string selectedColor)
            {
                currentIndicatorColorKey = selectedColor;
                PreferencesManager.Set("StandardIndicatorColor", selectedColor);
                Debug.Log($"[KitchenCustomerInfo] StandardIndicatorColor set to: {selectedColor}");
                this.ModuleList.Clear();
                this.Setup(player_id);
            };
        }

        /// <summary>
        /// Initializes the Indicator Timer selection option.
        /// </summary>
        /// <param name="prefCPEnabled">Current preference value.</param>
        /// <param name="player_id">Player ID.</param>
        private void InitializeIndicatorTimerOption(bool prefCPEnabled, int player_id)
        {
            // Define the options
            List<string> options = new List<string> { "Enabled", "Disabled" };
            // Determine the currently selected option based on preference
            string selectedOption = prefCPEnabled ? "Enabled" : "Disabled";

            // Create the Option object for Indicator Timer
            indicatorTimerOption = new Option<string>(
                options,                 // List of options
                selectedOption,          // Currently selected option
                options,                 // Display names
                null                     // No comparison delegate needed for string
            );

            // Subscribe to the OnChanged event to handle selection changes
            indicatorTimerOption.OnChanged += delegate (object _, string selected)
            {
                bool isEnabled = selected.Equals("Enabled", StringComparison.OrdinalIgnoreCase);
                PreferencesManager.Set("CustomerPatienceEnabled", isEnabled);
                Debug.Log($"[KitchenCustomerInfo] CustomerPatienceEnabled set to: {isEnabled}");
                //this.ModuleList.Clear();
                //this.Setup(player_id);
            };
        }

        /// <summary>
        /// Initializes the Highlight selection option.
        /// </summary>
        /// <param name="prefHighlightEnabled">Current preference value.</param>
        /// <param name="player_id">Player ID.</param>
        private void InitializeHighlightOption(bool prefHighlightEnabled, int player_id)
        {
            // Define the options
            List<string> options = new List<string> { "Enabled", "Disabled" };
            // Determine the currently selected option based on preference
            string selectedOption = prefHighlightEnabled ? "Enabled" : "Disabled";

            // Create the Option object for Highlight
            highlightOption = new Option<string>(
                options,                 // List of options
                selectedOption,          // Currently selected option
                options,                 // Display names
                null                     // No comparison delegate needed for string
            );

            // Subscribe to the OnChanged event to handle selection changes
            highlightOption.OnChanged += delegate (object _, string selected)
            {
                bool isEnabled = selected.Equals("Enabled", StringComparison.OrdinalIgnoreCase);
                PreferencesManager.Set("HighlightEnabled", isEnabled);
                Debug.Log($"[KitchenCustomerInfo] HighlightEnabled set to: {isEnabled}");
                //this.ModuleList.Clear();
                //this.Setup(player_id);
            };
        }

        /// <summary>
        /// Initializes the layout selection option.
        /// </summary>
        /// <param name="prefSwapPositons">Current preference value.</param>
        /// <param name="player_id">Player ID.</param>
        private void InitializeLayoutOption(bool prefSwapPositons, int player_id)
        {
            // Define the options
            List<string> options = new List<string> { "Icon / Timer", "Timer / Icon" };
            // Determine the currently selected option based on preference
            string selectedOption = prefSwapPositons ? "Timer / Icon" : "Icon / Timer";

            // Create the Option object for Layout
            layoutOption = new Option<string>(
                options,                 // List of options
                selectedOption,          // Currently selected option
                options,                 // Display names
                null                     // No comparison delegate needed for string
            );

            // Subscribe to the OnChanged event to handle selection changes
            layoutOption.OnChanged += delegate (object _, string selected)
            {
                bool isSwapped = selected.Equals("Timer / Icon", StringComparison.OrdinalIgnoreCase);
                PreferencesManager.Set("SwapPositions", isSwapped);
                Debug.Log($"[KitchenCustomerInfo] SwapPositions set to: {isSwapped}");
                //this.ModuleList.Clear();
                //this.Setup(player_id);
            };
        }
    }

    // Harmony Patch to add the CustomerPatienceMenu to the Options Menu
    [HarmonyPatch(typeof(OptionsMenu<MenuAction>), "CreateSubmenus")]
    internal static class OptionsMenu_CreateSubmenus_Patch
    {
        [HarmonyPostfix]
        private static void CreateSubmenus_Postfix(OptionsMenu<MenuAction> __instance, ref Dictionary<Type, Menu<MenuAction>> menus)
        {
            try
            {
                if (!menus.ContainsKey(typeof(CustomerPatienceMenu)))
                {
                    menus.Add(typeof(CustomerPatienceMenu), new CustomerPatienceMenu(__instance.Container, __instance.ModuleList));
                    Debug.Log("[KitchenCustomerInfo] CustomerPatienceMenu added to OptionsMenu submenus.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KitchenCustomerInfo] Exception in CreateSubmenus_Postfix: {ex}");
            }
        }
    }

    // Harmony Patch to add the submenu button directly within the OptionsMenu
    [HarmonyPatch(typeof(OptionsMenu<MenuAction>), "Setup")]
    internal static class OptionsMenu_Setup_Patch
    {
        [HarmonyPrefix]
        private static void Setup_Prefix(OptionsMenu<MenuAction> __instance, int player_id)
        {
            try
            {
                MethodInfo addSubmenuButtonMethod = AccessTools.Method(
                    typeof(Menu<MenuAction>),
                    "AddSubmenuButton",
                    new Type[] { typeof(string), typeof(Type), typeof(bool) }
                );

                if (addSubmenuButtonMethod == null)
                {
                    Debug.LogError("[KitchenCustomerInfo] Failed to find AddSubmenuButton method in Menu<MenuAction>.");
                    return;
                }

                // Parameters for AddSubmenuButton
                object[] parameters = new object[]
                {
                    GameData.Main.GlobalLocalisation.GetIcon(PatienceReason.Service) + " Patience Indicator",          // Button label
                    typeof(CustomerPatienceMenu), // Submenu type
                    true                           // Show arrow (adjust as needed)
                };

                // Invoke the protected AddSubmenuButton method on the OptionsMenu instance
                addSubmenuButtonMethod.Invoke(__instance, parameters);

                Debug.Log("[KitchenCustomerInfo] Customer Patience submenu added to OptionsMenu.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KitchenCustomerInfo] Exception in OptionsMenu_Setup_Patch: {ex}");
            }
        }
    }
}
