using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace CaptureImage
{
    public class CaptureForm : Form
    {
        private Point _startPoint;
        private Rectangle _selectionRectangle;

        public CaptureForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Bounds = SystemInformation.VirtualScreen;
            this.BackColor = Color.Black; // Changed to Black for a better dimming effect
            this.Opacity = 0.3;
            this.Cursor = Cursors.Cross;
            this.ShowInTaskbar = false;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            // Set the initial region of the form to the entire screen
            this.Region = new Region(this.ClientRectangle);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            // If the user presses the Escape key, close the form to cancel.
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _startPoint = e.Location;
                _selectionRectangle = new Rectangle(e.Location, Size.Empty);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left)
            {
                int x = Math.Min(_startPoint.X, e.X);
                int y = Math.Min(_startPoint.Y, e.Y);
                int width = Math.Abs(_startPoint.X - e.X);
                int height = Math.Abs(_startPoint.Y - e.Y);
                _selectionRectangle = new Rectangle(x, y, width, height);

                // Create the "cut-out" region
                Region region = new Region(this.ClientRectangle);
                region.Exclude(_selectionRectangle);
                this.Region = region;

                // Invalidate to redraw the border
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _selectionRectangle.Width > 0 && _selectionRectangle.Height > 0)
            {
                // Reset the region and hide the form before capturing
                this.Region = new Region(this.ClientRectangle);
                this.Hide();

                CaptureScreen(_selectionRectangle);
                this.Close();
            }
            else
            {
                this.Close();
            }
        }

        private void CaptureScreen(Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;

            try
            {
                // A small delay to ensure the form is fully hidden
                System.Threading.Thread.Sleep(150);

                using (var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(rect.Location, Point.Empty, rect.Size, CopyPixelOperation.SourceCopy);
                    }

                    DisplayImage outputForm = new DisplayImage(bmp);
                    outputForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing screen: {ex.Message}", "Capture Error");
            }
        }
    }

}
