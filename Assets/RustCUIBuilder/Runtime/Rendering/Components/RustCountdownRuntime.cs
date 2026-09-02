using System;
using RustCUIBuilder.Runtime.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RustCUIBuilder.Runtime.Rendering.Components
{
    /// <summary>
    /// Live runtime simulation for CuiCountdownComponent matching Rust client-side countdown timer behavior.
    /// </summary>
    public class RustCountdownRuntime : MonoBehaviour
    {
        public float startTime = 60f;
        public float endTime = 0f;
        public float step = 1f;
        public float interval = 1f;
        public CuiTimerFormat timerFormat = CuiTimerFormat.None;
        public string numberFormat = "0.####";
        public bool destroyIfDone = true;
        public string command = "";

        private float _currentVal;
        private float _lastTickTime;
        private Text _legacyText;
        private TextMeshProUGUI _tmpText;

        private void Awake()
        {
            _legacyText = GetComponent<Text>();
            _tmpText = GetComponent<TextMeshProUGUI>();
            _currentVal = startTime;
            _lastTickTime = Time.time;
            UpdateDisplay();
        }

        private void Update()
        {
            if (interval <= 0.001f) return;

            if (Time.time - _lastTickTime >= interval)
            {
                _lastTickTime = Time.time;
                if (startTime > endTime)
                {
                    _currentVal -= step;
                    if (_currentVal <= endTime)
                    {
                        _currentVal = endTime;
                        UpdateDisplay();
                        if (destroyIfDone) Destroy(gameObject);
                        return;
                    }
                }
                else
                {
                    _currentVal += step;
                    if (_currentVal >= endTime)
                    {
                        _currentVal = endTime;
                        UpdateDisplay();
                        if (destroyIfDone) Destroy(gameObject);
                        return;
                    }
                }
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            string formatted = FormatTimer(_currentVal, timerFormat, numberFormat);
            if (_legacyText != null) _legacyText.text = formatted;
            if (_tmpText != null) _tmpText.text = formatted;
        }

        public static string FormatTimer(float val, CuiTimerFormat format, string numFormat)
        {
            switch (format)
            {
                case CuiTimerFormat.MinutesSeconds:
                {
                    var ts = TimeSpan.FromSeconds(Math.Max(0, val));
                    return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
                }
                case CuiTimerFormat.HoursMinutesSeconds:
                {
                    var ts = TimeSpan.FromSeconds(Math.Max(0, val));
                    return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
                }
                case CuiTimerFormat.HoursMinutes:
                {
                    var ts = TimeSpan.FromSeconds(Math.Max(0, val));
                    return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}";
                }
                case CuiTimerFormat.SecondsHundreth:
                {
                    var ts = TimeSpan.FromSeconds(Math.Max(0, val));
                    return $"{ts.Seconds:D2}.{ts.Milliseconds / 10:D2}";
                }
                default:
                    return val.ToString(string.IsNullOrEmpty(numFormat) ? "0.####" : numFormat, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
