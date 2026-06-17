using Microsoft.Win32;
using System;
using System.Text;
using System.Windows.Forms;

namespace CaptureImage
{
    public class AiOcrSettings
    {
        public const string RegistryPath = @"Software\AIOCRTRANS";

        public const int ModAlt = 0x0001;
        public const int ModControl = 0x0002;
        public const int ModShift = 0x0004;
        public const int ModWin = 0x0008;

        public const int DefaultHotkeyModifiers = ModAlt;
        public const int DefaultHotkeyKey = (int)Keys.X;
        public const string DefaultAiModel = "models/gemini-2.5-flash";

        public int HotkeyModifiers { get; set; } = DefaultHotkeyModifiers;
        public int HotkeyKey { get; set; } = DefaultHotkeyKey;
        public string GoogleApiKey { get; set; } = "";
        public string AiModel { get; set; } = DefaultAiModel;

        public static AiOcrSettings Load()
        {
            var settings = new AiOcrSettings();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key == null)
                    {
                        return settings;
                    }

                    settings.HotkeyModifiers = Convert.ToInt32(key.GetValue("HotkeyModifiers", DefaultHotkeyModifiers));
                    settings.HotkeyKey = Convert.ToInt32(key.GetValue("HotkeyKey", DefaultHotkeyKey));
                    settings.GoogleApiKey = key.GetValue("GoogleApiKey", "") as string ?? "";
                    settings.AiModel = key.GetValue("AiModel", DefaultAiModel) as string ?? DefaultAiModel;
                }
            }
            catch
            {
            }

            return settings;
        }

        public void Save()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
            {
                key.SetValue("HotkeyModifiers", HotkeyModifiers, RegistryValueKind.DWord);
                key.SetValue("HotkeyKey", HotkeyKey, RegistryValueKind.DWord);
                key.SetValue("GoogleApiKey", GoogleApiKey ?? "", RegistryValueKind.String);
                key.SetValue("AiModel", NormalizeModelName(AiModel), RegistryValueKind.String);
            }
        }

        public static string NormalizeModelName(string model)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                return DefaultAiModel;
            }

            model = model.Trim();
            if (!model.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
            {
                model = "models/" + model;
            }

            return model;
        }

        public static string FormatHotkey(AiOcrSettings settings)
        {
            var sb = new StringBuilder();
            if ((settings.HotkeyModifiers & ModControl) != 0)
            {
                sb.Append("Ctrl+");
            }

            if ((settings.HotkeyModifiers & ModAlt) != 0)
            {
                sb.Append("Alt+");
            }

            if ((settings.HotkeyModifiers & ModShift) != 0)
            {
                sb.Append("Shift+");
            }

            if ((settings.HotkeyModifiers & ModWin) != 0)
            {
                sb.Append("Win+");
            }

            sb.Append(((Keys)settings.HotkeyKey).ToString());
            return sb.ToString();
        }

        public bool IsValidHotkey()
        {
            if (HotkeyKey == 0 || HotkeyModifiers == 0)
            {
                return false;
            }

            var key = (Keys)HotkeyKey;
            return key != Keys.ControlKey
                && key != Keys.Menu
                && key != Keys.ShiftKey
                && key != Keys.LWin
                && key != Keys.RWin;
        }
    }
}
