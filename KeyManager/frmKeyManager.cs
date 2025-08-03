using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyManager
{
    // Đảm bảo rằng ứng dụng không hiển thị cửa sổ chính
    public partial class frmKeyManager : Form
    {
        // Khai báo các hằng số và phương thức từ User32.dll
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

            // Ẩn cửa sổ chính của form ngay khi khởi chạy
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;

            // Thiết lập NotifyIcon để hiển thị ở System Tray
            SetupNotifyIcon();

            // Đăng ký phím tắt Alt + X
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, (int)Keys.X);
        }

        private void SetupNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Application; // Sử dụng biểu tượng mặc định
            notifyIcon.Text = "KeyManager - Alt + X để chụp màn hình";
            notifyIcon.Visible = true;

            // Tạo menu chuột phải cho biểu tượng
            var contextMenu = new ContextMenu();
            var exitMenuItem = new MenuItem("Exit");
            exitMenuItem.Click += (sender, e) => this.Close();
            contextMenu.MenuItems.Add(exitMenuItem);
            notifyIcon.ContextMenu = contextMenu;
        }

        protected override void WndProc(ref Message m)
        {
            // Kiểm tra thông điệp từ phím tắt
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == HOTKEY_ID)
            {
                // Khi phím tắt Alt + X được nhấn
                LaunchCaptureImage();
            }
            base.WndProc(ref m);
        }

        private void LaunchCaptureImage()
        {
            try
            {
                // Tìm đường dẫn tới file .exe của KeyManager
                string exePath = Application.ExecutablePath;
                string exeDirectory = Path.GetDirectoryName(exePath);

                // Tạo đường dẫn đầy đủ đến CaptureImage.exe
                string captureImagePath = Path.Combine(exeDirectory, @"D:\SRC\ocr_generativelanguage\CaptureImage\bin\Debug\CaptureImage.exe");

                // Kiểm tra xem file có tồn tại không
                if (File.Exists(captureImagePath))
                {
                    // Chạy ứng dụng CaptureImage.exe
                    Process.Start(captureImagePath);
                }
                else
                {
                    notifyIcon.ShowBalloonTip(3000, "Lỗi", "Không tìm thấy CaptureImage.exe cùng thư mục.", ToolTipIcon.Error);
                }
            }
            catch (Exception ex)
            {
                notifyIcon.ShowBalloonTip(3000, "Lỗi khởi chạy", ex.Message, ToolTipIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Hủy đăng ký phím tắt khi ứng dụng đóng
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            base.OnFormClosing(e);
        }
    }
}

// Đây là phần code do Visual Studio tạo, bạn không cần thay đổi.
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
