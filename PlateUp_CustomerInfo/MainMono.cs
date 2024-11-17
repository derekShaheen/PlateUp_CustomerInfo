using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KitchenCustomerInfo
{
    internal class MainMono : MonoBehaviour
    {
        public void Awake()
        {
            Harmony harmony = new Harmony("Skrip.Plateup.CustomerInfo");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Debug.Log("[CustomerInfo] Initialized");
        }
    }
}
