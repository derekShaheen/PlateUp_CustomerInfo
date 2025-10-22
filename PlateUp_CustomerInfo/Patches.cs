using HarmonyLib;
using Kitchen;
using Kitchen.Modules;
using Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace KitchenCustomerInfo
{
    [HarmonyPatch(typeof(LocalViewRouter), "GetPrefab")]
    internal static class LocalViewRouter_Patch
    {
        [HarmonyPostfix]
        private static void GetPrefab_Postfix(ViewType view_type, ref GameObject __result)
        {
            if ((view_type == ViewType.CustomerIndicator || view_type == ViewType.QueueIndicator) &&
                __result != null &&
                __result.GetComponent<CustomerInfo>() == null)
            {
                // Add PatiencePercentController component
                var CustomerInfoObject = __result.AddComponent<CustomerInfo>();
                CustomerInfoObject.Indicator = __result.GetComponent<CustomerIndicatorView>();

                // Find the Text GameObject in the hierarchy
                Transform textTransform = __result.transform.Find("Container/GameObject/Container/GameObject (1)/Text");

                if (textTransform != null)
                {
                    CustomerInfoObject.Icon = textTransform;
                    CustomerInfoObject.DefaultIconPosition = textTransform.localPosition;

                    // Instantiate a new Text GameObject
                    GameObject newTextObject = UnityEngine.Object.Instantiate(textTransform.gameObject);
                    newTextObject.transform.SetParent(textTransform.parent);
                    newTextObject.transform.Reset();

                    // Get TextMeshPro component
                    CustomerInfoObject.textMeshPro = newTextObject.GetComponent<TextMeshPro>();

                    if (CustomerInfoObject.textMeshPro != null)
                    {
                        CustomerInfoObject.textMeshPro.enabled = false;
                        CustomerInfoObject.textMeshPro.color = Color.white;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CustomerIndicatorView), "Initialise")]
    public static class CustomerIndicatorView_Initialise_Patch
    {
        // Cache the FieldInfo for efficiency
        private static readonly FieldInfo PatienceField = AccessTools.Field(typeof(CustomerIndicatorView), "Patience");

        [HarmonyPostfix]
        public static void Postfix(CustomerIndicatorView __instance)
        {
            if (__instance == null)
            {
                //Debug.LogError("[KitchenCustomerInfo] CustomerIndicatorView instance is null in Postfix.");
                return;
            }

            // Retrieve the preferred color key from PreferencesManager
            string colorKey = PreferencesManager.Get<string>("StandardIndicatorColor", "Green"); // Default to "Yellow" if not set

            if(colorKey.Equals("Green"))
            {
                return;
            }

            // Fetch the corresponding Color from colorOptions
            if (CustomerPatienceMenu.colorOptions.TryGetValue(colorKey, out Color selectedColor))
            {
                // Access the private 'Patience' field using reflection
                Rectangle patienceRect = (Rectangle)PatienceField.GetValue(__instance);
                if (patienceRect != null)
                {
                    patienceRect.Color = selectedColor;
                    //Debug.Log($"[KitchenCustomerInfo] Set Patience rect color to '{colorKey}'.");
                }
            }
        }
    }
}
