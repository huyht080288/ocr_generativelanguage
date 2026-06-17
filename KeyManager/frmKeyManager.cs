using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyManager
{
    // Ensures the application does not display a main window.
    public partial class frmKeyManager : Form
    {
        // Declares constants and methods from User32.dll.
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        const int HOTKEY_ID = 1;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_APPWINDOW = 0x00040000;

        private NotifyIcon notifyIcon;
        private AiOcrSettings _settings;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                cp.ExStyle &= ~WS_EX_APPWINDOW;
                return cp;
            }
        }

        public frmKeyManager()
        {
            InitializeComponent();

            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(-2000, -2000);
            this.WindowState = FormWindowState.Normal;
            this.Visible = false;

            // Sets up the NotifyIcon to display in the System Tray.
            SetupNotifyIcon();

            _settings = AiOcrSettings.Load();
            RegisterHotkeyFromSettings();
            UpdateTrayTooltip();

            // Finds the path to the KeyManager.exe file.
            string exePath = Application.ExecutablePath;
            string exeDirectory = Path.GetDirectoryName(exePath);
            // Creates the full path to CaptureImage.exe.
            string keyManager = Path.Combine(exeDirectory, @"KeyManager.exe");
            if (File.Exists(keyManager))
            {
                AddOrUpdateStartup(keyManager);
            }
        }

        private void SetupNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Hand; // Use the default application icon.
            notifyIcon.Text = "Press Alt + X to capture screen";
            notifyIcon.Visible = true;

            // Creates the right-click context menu for the icon.
            var contextMenu = new ContextMenu();
            var settingsMenuItem = new MenuItem("Settings");
            var exitMenuItem = new MenuItem("Exit");
            var helpMenuItem = new MenuItem("Help");
            settingsMenuItem.Click += (sender, e) => OpenSettings();
            exitMenuItem.Click += (sender, e) => this.Close();
            helpMenuItem.Click += (sender, e) => 
            {
                string exeDirectory = Path.GetDirectoryName(Application.ExecutablePath);
                string userManual = Path.Combine(exeDirectory, @"UserManual.html");
                if (File.Exists(userManual))
                {
                    Process.Start(userManual);
                }
            };
            contextMenu.MenuItems.Add(helpMenuItem);
            contextMenu.MenuItems.Add(settingsMenuItem);
            contextMenu.MenuItems.Add(exitMenuItem);
            notifyIcon.ContextMenu = contextMenu;
        }

        private void RegisterHotkeyFromSettings()
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            if (!RegisterHotKey(this.Handle, HOTKEY_ID, _settings.HotkeyModifiers, _settings.HotkeyKey))
            {
                notifyIcon.ShowBalloonTip(5000, "Hotkey Error",
                    "Could not register shortcut. It may already be in use by another application.",
                    ToolTipIcon.Warning);
            }
        }

        private void UpdateTrayTooltip()
        {
            notifyIcon.Text = "Press " + AiOcrSettings.FormatHotkey(_settings) + " to capture screen";
        }

        private void OpenSettings()
        {
            using (var form = new frmSettings(_settings))
            {
                if (form.ShowDialog() == DialogResult.OK && form.SavedSettings != null)
                {
                    _settings = form.SavedSettings;
                    RegisterHotkeyFromSettings();
                    UpdateTrayTooltip();
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            // Checks for a message from the hotkey.
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == HOTKEY_ID)
            {
                // When the Alt + X hotkey is pressed.
                LaunchCaptureImage();
            }
            base.WndProc(ref m);
        }

        private void LaunchCaptureImage()
        {
            try
            {
                // Finds the path to the KeyManager.exe file.
                string exePath = Application.ExecutablePath;
                string exeDirectory = Path.GetDirectoryName(exePath);

                // Creates the full path to CaptureImage.exe.
                string captureImagePath = Path.Combine(exeDirectory, @"CaptureImage.exe");

                // Checks if the file exists.
                if (File.Exists(captureImagePath))
                {
                    // --- UPDATED CODE SECTION ---

                    // Get the process name from the file path (without .exe).
                    string processName = Path.GetFileNameWithoutExtension(captureImagePath);

                    // Find all running processes with the matching name.
                    Process[] runningProcesses = Process.GetProcessesByName(processName);

                    // If any matching processes are found.
                    if (runningProcesses.Length > 0)
                    {
                        // Loop through and kill all of them.
                        foreach (Process process in runningProcesses)
                        {
                            try
                            {
                                // Kills the process.
                                process.Kill();
                                // Wait for the process to exit completely to avoid conflicts.
                                process.WaitForExit();
                            }
                            catch (Exception ex)
                            {
                                // Can log the error here if necessary.
                                // Ignore the error if the process already terminated.
                            }
                        }
                    }

                    // --- END OF UPDATED SECTION ---

                    // Start the CaptureImage.exe application after ensuring no old processes are running.
                    Process.Start(captureImagePath);
                }
                else
                {
                    notifyIcon.ShowBalloonTip(3000, "Error", "CaptureImage.exe not found in the same directory.", ToolTipIcon.Error);
                }
            }
            catch (Exception ex)
            {
                notifyIcon.ShowBalloonTip(3000, "Launch Error", ex.Message, ToolTipIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Unregisters the hotkey when the application closes.
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            base.OnFormClosing(e);
        }

        static void AddOrUpdateStartup(string filePath)
        {
            EnableIPKeyManager();
            try
            {
                // Get the path to the StartUp list in the registry
                string startupRegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run";

                // Get the file name (without the path)
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                // Check if the file already exists in the StartUp list
                if (IsInStartup(fileName))
                {
                    // If it exists, then remove it
                    RemoveFromStartup(fileName);
                }

                // Add to the StartUp list
                Registry.SetValue(startupRegistryPath, fileName, filePath);
            }
            catch (Exception) { }
        }

        static void RemoveFromStartup(string entryName)
        {
            try
            {
                // Get the path to the StartUp list in the registry
                string startupRegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run";

                // Remove entryName from the StartUp list
                RegistryKey key = Registry.CurrentUser.OpenSubKey(startupRegistryPath, true);
                key.DeleteValue(entryName, false);
                key.Close();
            }
            catch (Exception) { }
        }

        static bool IsInStartup(string entryName)
        {
            try
            {
                // Get the path to the StartUp list in the registry
                string startupRegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run";

                // Check if entryName exists in the StartUp list
                return Registry.GetValue(startupRegistryPath, entryName, null) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void EnableIPKeyManager()
        {
            try
            {
                // Get the path to the StartUpApproved registry
                string startupApprovedRegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

                // Value to set (00 00 00 00 00 00 00 00 00 00 00 00)
                byte[] valueToSet = new byte[12];

                // Set the value for the startup entry
                Registry.SetValue(startupApprovedRegistryPath, "KeyManager", valueToSet, RegistryValueKind.Binary);

            }
            catch (Exception) { }
        }

    }
}

// This section of the code is generated by Visual Studio and does not need to be changed.
namespace KeyManager
{
    partial class frmKeyManager
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "Form1";
            this.ResumeLayout(false);
        }
    }
}
