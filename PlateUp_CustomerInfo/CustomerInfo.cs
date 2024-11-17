// CustomerInfo.cs
using Kitchen;
using KitchenData;
using Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace KitchenCustomerInfo
{
    public class CustomerInfo : MonoBehaviour
    {
        public CustomerIndicatorView Indicator;

        private static FieldInfo cacheDataFieldInfo = typeof(CustomerIndicatorView).GetField("Data", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo cacheBarFieldInfo = typeof(CustomerIndicatorView).GetField("Patience", BindingFlags.Instance | BindingFlags.NonPublic);

        public TextMeshPro textMeshPro;

        public Transform Icon;

        public Vector3 DefaultIconPosition;

        private float previousPatience = -1f;
        private float smoothedDecreaseRate = 0f;
        private float timeAccumulator = 0f; // Time since last text update
        private const float updateInterval = 0.5f; // Update text every 0.5 seconds
        private const float alpha = 0.2f; // Smoothing factor for EMA

        public bool isHiddden = false;

        private float timeRemaining = 0f; // Store our own timeRemaining

        public static HashSet<CustomerInfo> AllCustomerInfos = new HashSet<CustomerInfo>();

        private Color defaultBarColor;

        // Variables to accumulate deltaPatience and deltaTime
        private float accumulatedDeltaPatience = 0f;
        private float accumulatedDeltaTime = 0f;

        // Track previous to detect changes
        public PatienceReason PatienceReason;
        private PatienceReason previousPatienceReason = PatienceReason.Queue;

        public DisplayedPatienceFactor DisplayedPatienceFactor;
        private DisplayedPatienceFactor previousPatienceFactor = DisplayedPatienceFactor.None;

        private void Awake()
        {
            defaultBarColor = new Color(0.588f, 0.988f, 0.184f, 1.000f); // Limeish green
            AllCustomerInfos.Add(this);
        }

        private void OnEnable()
        {
            AllCustomerInfos.Add(this);
        }

        private void OnDisable()
        {
            AllCustomerInfos.Remove(this);
        }

        private void OnDestroy()
        {
            AllCustomerInfos.Remove(this);
        }

        private void Update()
        {
            bool timerEnabled = PreferencesManager.Get<bool>("CustomerPatienceEnabled", true);

            FieldInfo fieldDataInfo = cacheDataFieldInfo;
            CustomerIndicatorView.ViewData viewData = (CustomerIndicatorView.ViewData)((fieldDataInfo != null) ? fieldDataInfo.GetValue(Indicator) : null);
            isHiddden = viewData.IsHidden;
            PatienceReason = viewData.PatienceReason;
            DisplayedPatienceFactor = viewData.PatienceFactors;

            // Check if PatienceReason has changed
            if (PatienceReason != previousPatienceReason
                || DisplayedPatienceFactor != previousPatienceFactor)
            {
                ResetAccumulators();
            }

            if (viewData.HasPatience && !viewData.IsHidden && !viewData.IsObfuscated)
            {
                FieldInfo fieldBarInfo = cacheBarFieldInfo;
                Rectangle BarData = (Rectangle)((fieldBarInfo != null) ? fieldBarInfo.GetValue(Indicator) : null);

                float currentPatience = viewData.Patience;

                float deltaTime = Time.deltaTime;

                // Accumulate time since last text update
                timeAccumulator += deltaTime;

                float deltaPatience = 0f;

                if (previousPatience >= 0f)
                {
                    deltaPatience = previousPatience - currentPatience;

                    if (deltaTime > 0f)
                    {
                        accumulatedDeltaPatience += deltaPatience;
                        accumulatedDeltaTime += deltaTime;
                    }
                }

                previousPatience = currentPatience;

                // Update the text once per interval
                if (timeAccumulator >= updateInterval)
                {
                    // Calculate decrease rate once per interval
                    float decreaseRate = 0f;
                    if (accumulatedDeltaTime > 0f)
                    {
                        decreaseRate = accumulatedDeltaPatience / accumulatedDeltaTime;
                        // Update smoothedDecreaseRate using EMA
                        if (smoothedDecreaseRate <= 0f)
                        {
                            smoothedDecreaseRate = decreaseRate;
                        }
                        else
                        {
                            smoothedDecreaseRate = alpha * decreaseRate + (1f - alpha) * smoothedDecreaseRate;
                        }
                    }

                    // Reset accumulators
                    timeAccumulator -= updateInterval;
                    accumulatedDeltaPatience = 0f;
                    accumulatedDeltaTime = 0f;

                    // Calculate time remaining until patience reaches zero
                    timeRemaining = 0f;
                    if (smoothedDecreaseRate > 0f)
                    {
                        timeRemaining = currentPatience / smoothedDecreaseRate;
                    }

                    bool HighlightEnabled = PreferencesManager.Get<bool>("HighlightEnabled", true);
                    string standardColorKey = PreferencesManager.Get<string>("StandardIndicatorColor", "Green");
                    string priorityColorKey = PreferencesManager.Get<string>("HighlightIndicatorColor", "Cyan");
                    Color standardColor = CustomerPatienceMenu.colorOptions.ContainsKey(standardColorKey) ? CustomerPatienceMenu.colorOptions[standardColorKey] : defaultBarColor;
                    Color priorityColor = CustomerPatienceMenu.colorOptions.ContainsKey(priorityColorKey) ? CustomerPatienceMenu.colorOptions[priorityColorKey] : Color.cyan;

                    if(standardColorKey.Equals("Green"))
                    {
                        standardColor = defaultBarColor; // Limeish green
                    }
                    if(priorityColorKey.Equals("Green"))
                    {
                        priorityColor = defaultBarColor; // Limeish green
                    }

                    if (HighlightEnabled)
                    {
                        // Sort customers based on timeRemaining -- only inside and in queue for items
                        List<CustomerInfo> sortedCustomers = AllCustomerInfos
                            .Where(c => c.timeRemaining > 1f)
                            .Where(c => !c.isHiddden)
                            .Where(c => c.PatienceReason == PatienceReason.Service
                                        || c.PatienceReason == PatienceReason.WaitForFood
                                        || c.PatienceReason == PatienceReason.GetFoodDelivered)
                            .OrderBy(c => c.timeRemaining)
                            .ToList();
                        int queuePosition = sortedCustomers.IndexOf(this) + 1; // Positions start at 1

                        // Update the text to display time remaining
                        if (timeRemaining > 0f)
                        {
                            textMeshPro.text = $"{Helpers.FormatTime(timeRemaining)}";
                            textMeshPro.enabled = timerEnabled;

                            if(queuePosition == 1)
                            {
                                BarData.Color = priorityColor;
                            } else
                            {
                                BarData.Color = standardColor;
                            }

                            // Ensure the text and icon are positioned correctly
                            textMeshPro.gameObject.transform.localRotation = Quaternion.identity;
                            textMeshPro.gameObject.transform.localPosition = new Vector3(0f, 0.42f, -0.055f);//new Vector3(0f, 0.38f, -0.05f);
                            textMeshPro.gameObject.transform.localScale = Vector3.one * 0.007f;
                            if (Icon != null)
                            {
                                if (timerEnabled)
                                {
                                    Icon.localPosition = new Vector3(0f, 0.68f, -0.055f);

                                    if (PreferencesManager.Get<bool>("SwapPositions", false))
                                    {
                                        Vector3 tempPosition = textMeshPro.gameObject.transform.localPosition;
                                        textMeshPro.gameObject.transform.localPosition = Icon.localPosition;
                                        Icon.localPosition = tempPosition;
                                    }
                                }
                                else
                                {
                                    Icon.localPosition = DefaultIconPosition;
                                }
                            }
                        }
                        else
                        {
                            textMeshPro.text = "";
                            textMeshPro.enabled = false;
                            if (Icon != null)
                            {
                                Icon.localPosition = DefaultIconPosition;
                            }
                        }
                    }
                    else
                    {
                        // Highlight is disabled
                        if (timeRemaining > 0f)
                        {
                            textMeshPro.text = $"{Helpers.FormatTime(timeRemaining)}";
                            textMeshPro.enabled = timerEnabled;
                            BarData.Color = standardColor; // Limeish green

                            // Ensure the text and icon are positioned correctly
                            textMeshPro.gameObject.transform.localRotation = Quaternion.identity;
                            textMeshPro.gameObject.transform.localPosition = new Vector3(0f, 0.42f, -0.055f);
                            textMeshPro.gameObject.transform.localScale = Vector3.one * 0.007f;
                            if (Icon != null)
                            {
                                if (timerEnabled)
                                {
                                    Icon.localPosition = new Vector3(0f, 0.68f, -0.055f);

                                    if (PreferencesManager.Get<bool>("SwapPositions", false))
                                    {
                                        Vector3 tempPosition = textMeshPro.gameObject.transform.localPosition;
                                        textMeshPro.gameObject.transform.localPosition = Icon.localPosition;
                                        Icon.localPosition = tempPosition;
                                    }
                                }
                                else
                                {
                                    Icon.localPosition = DefaultIconPosition;
                                }
                            }
                        }
                        else
                        {
                            textMeshPro.text = "";
                            textMeshPro.enabled = false;
                            if (Icon != null)
                            {
                                Icon.localPosition = DefaultIconPosition;
                            }
                        }
                    }
                }
            }
        }

        public void ResetAccumulators()
        {
            accumulatedDeltaPatience = 0f;
            accumulatedDeltaTime = 0f;
            smoothedDecreaseRate = 0f;
            timeRemaining = 0f;
            previousPatienceReason = PatienceReason;
            previousPatienceFactor = DisplayedPatienceFactor;
            textMeshPro.text = $"";
        }
    }
}
