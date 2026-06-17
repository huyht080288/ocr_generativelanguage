using System;
using System.Drawing;
using System.Windows.Forms;

namespace KeyManager
{
    public class frmSettings : Form
    {
        private readonly AiOcrSettings _settings;
        private readonly TextBox _txtHotkey;
        private readonly TextBox _txtApiKey;
        private readonly ComboBox _cmbModel;
        private readonly Button _btnShowApiKey;
        private bool _recordingHotkey;
        private bool _apiKeyVisible;

        public AiOcrSettings SavedSettings { get; private set; }

        public frmSettings(AiOcrSettings settings)
        {
            _settings = new AiOcrSettings
            {
                HotkeyModifiers = settings.HotkeyModifiers,
                HotkeyKey = settings.HotkeyKey,
                GoogleApiKey = settings.GoogleApiKey,
                AiModel = settings.AiModel
            };

            Text = "AIOCR Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;
            ClientSize = new Size(420, 230);

            var lblHotkey = new Label { Text = "Shortcut key:", Location = new Point(12, 18), AutoSize = true };
            _txtHotkey = new TextBox
            {
                Location = new Point(120, 15),
                Width = 180,
                ReadOnly = true,
                TabStop = false
            };
            _txtHotkey.Text = AiOcrSettings.FormatHotkey(_settings);
            _txtHotkey.KeyDown += TxtHotkey_KeyDown;

            var btnRecordHotkey = new Button
            {
                Text = "Change...",
                Location = new Point(310, 13),
                Width = 90
            };
            btnRecordHotkey.Click += (sender, e) =>
            {
                _recordingHotkey = true;
                _txtHotkey.Text = "Press key combination...";
                _txtHotkey.Focus();
            };

            var lblModel = new Label { Text = "AI model:", Location = new Point(12, 58), AutoSize = true };
            _cmbModel = new ComboBox
            {
                Location = new Point(120, 55),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            _cmbModel.Items.AddRange(new object[]
            {
                "models/gemini-2.5-flash",
                "models/gemini-2.0-flash",
                "models/gemini-1.5-flash",
                "models/gemini-1.5-pro"
            });
            _cmbModel.Text = AiOcrSettings.NormalizeModelName(_settings.AiModel);

            var lblApiKey = new Label { Text = "Google API key:", Location = new Point(12, 98), AutoSize = true };
            _txtApiKey = new TextBox
            {
                Location = new Point(120, 95),
                Width = 210,
                PasswordChar = '*'
            };
            _txtApiKey.Text = _settings.GoogleApiKey ?? "";

            _btnShowApiKey = new Button
            {
                Text = "Show",
                Location = new Point(340, 93),
                Width = 60
            };
            _btnShowApiKey.Click += (sender, e) =>
            {
                _apiKeyVisible = !_apiKeyVisible;
                _txtApiKey.PasswordChar = _apiKeyVisible ? '\0' : '*';
                _btnShowApiKey.Text = _apiKeyVisible ? "Hide" : "Show";
            };

            var btnSave = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.None,
                Location = new Point(230, 150),
                Width = 80
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(320, 150),
                Width = 80
            };

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblHotkey, _txtHotkey, btnRecordHotkey,
                lblModel, _cmbModel,
                lblApiKey, _txtApiKey, _btnShowApiKey,
                btnSave, btnCancel
            });
        }

        private void TxtHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_recordingHotkey)
            {
                return;
            }

            e.SuppressKeyPress = true;

            if (e.KeyCode == Keys.Escape)
            {
                _recordingHotkey = false;
                _txtHotkey.Text = AiOcrSettings.FormatHotkey(_settings);
                return;
            }

            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.ShiftKey
                || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
            {
                return;
            }

            int modifiers = 0;
            if (e.Control)
            {
                modifiers |= AiOcrSettings.ModControl;
            }

            if (e.Alt)
            {
                modifiers |= AiOcrSettings.ModAlt;
            }

            if (e.Shift)
            {
                modifiers |= AiOcrSettings.ModShift;
            }

            if ((e.Modifiers & Keys.LWin) == Keys.LWin || (e.Modifiers & Keys.RWin) == Keys.RWin)
            {
                modifiers |= AiOcrSettings.ModWin;
            }

            if (modifiers == 0)
            {
                MessageBox.Show("Please include at least one modifier key (Ctrl, Alt, Shift, or Win).",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _settings.HotkeyModifiers = modifiers;
            _settings.HotkeyKey = (int)e.KeyCode;
            _txtHotkey.Text = AiOcrSettings.FormatHotkey(_settings);
            _recordingHotkey = false;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            _settings.GoogleApiKey = _txtApiKey.Text.Trim();
            _settings.AiModel = AiOcrSettings.NormalizeModelName(_cmbModel.Text);

            if (!_settings.IsValidHotkey())
            {
                MessageBox.Show("Please set a valid shortcut key.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_settings.GoogleApiKey))
            {
                MessageBox.Show("Google API key is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_cmbModel.Text))
            {
                MessageBox.Show("AI model is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _settings.Save();
                SavedSettings = _settings;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save settings: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
