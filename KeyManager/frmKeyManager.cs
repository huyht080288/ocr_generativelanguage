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
        const int MOD_ALT = 0x0001; // Alt key

        private NotifyIcon notifyIcon;

        public frmKeyManager()
        {
            InitializeComponent();

            // Hides the main form window on startup.
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;

            // Sets up the NotifyIcon to display in the System Tray.
            SetupNotifyIcon();

            // Registers the Alt + X hotkey.
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, (int)Keys.X);
        }

        private void SetupNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Application; // Use the default application icon.
            notifyIcon.Text = "KeyManager - Press Alt + X to capture screen";
            notifyIcon.Visible = true;

            // Creates the right-click context menu for the icon.
            var contextMenu = new ContextMenu();
            var exitMenuItem = new MenuItem("Exit");
            exitMenuItem.Click += (sender, e) => this.Close();
            contextMenu.MenuItems.Add(exitMenuItem);
            notifyIcon.ContextMenu = contextMenu;
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
