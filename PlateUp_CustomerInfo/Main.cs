using System;
using System.Reflection;
using HarmonyLib;
using Kitchen;
using Kitchen.Modules;
using KitchenMods;
using Unity.Entities;
using UnityEngine;

namespace KitchenCustomerInfo
{
    public class Main : GenericSystemBase, IModSystem
    {
        public const string MOD_GUID = "Skrip.PlateUp.CustomerInfo";
        public const string MOD_NAME = "CustomerInfo";
        public const string MOD_VERSION = "1.0.1";

        protected override void Initialise()
        {
            base.Initialise();
            if (global::UnityEngine.Object.FindObjectOfType<MainMono>() != null)
            {
                return;
            }

            PreferencesManager.Remove("IndicatorColor");

            GameObject gameObject = new GameObject("CustomerInfoPatch");
            gameObject.AddComponent<MainMono>();
        }

        protected override void OnUpdate()
        {
        }
    }
}
