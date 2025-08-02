using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace CaptureImage
{
    // This class is the bridge between JavaScript and C#.
    // It must be public and have the ComVisible attribute.
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ScriptingBridge
    {
        private readonly DisplayImage _form;

        public ScriptingBridge(DisplayImage form)
        {
            _form = form;
        }

        // This public method will be callable from JavaScript
        public void PerformOcr()
        {
            // Call the main method on the form's thread
            _form.BeginInvoke(new Action(async () => await _form.ProcessOcrRequest()));
        }
    }

    public partial class DisplayImage : Form
    {
        // Store the captured image in a field to access it later
        private Image _capturedImage;

        public DisplayImage(Image capturedImage)
        {
            InitializeComponent();
            _capturedImage = capturedImage;

            // Configure the WebBrowser control
            webBrowser1.ObjectForScripting = new ScriptingBridge(this);
            webBrowser1.AllowWebBrowserDrop = false;

            this.MouseDown += me_MouseDown;
            this.MouseMove += me_MouseMove;
            this.MouseUp += me_MouseUp;
            this.DoubleClick += (sender, e) => { this.Close(); };
            
            // Generate and load the initial HTML UI
            LoadInitialHtml();
        }

        public async Task ProcessOcrRequest()
        {
            if (_capturedImage == null) return;

            CallScript("setButtonState", new object[] { true, "Converting..." });
            CallScript("updateResults", new object[] { "<i>Processing, please wait...</i>" });

            try
            {
                // **START OF NEW CODE**
                // 1. Prepare the image for the OCR API to meet the minimum size requirement.
                // This will either return the original image or a new padded image.
                using (Image imageForOcr = SetUpImageBeforeCallAPI(_capturedImage))
                {
                    // **END OF NEW CODE**

                    // 2. Convert the (potentially new) image to bytes
                    byte[] imageBytes;
                    using (var ms = new MemoryStream())
                    {
                        // Use the imageForOcr object for saving.
                        imageForOcr.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        imageBytes = ms.ToArray();
                    }

                    // 3. Call OCR service and get HTML formatted result
                    string htmlResult = await GetOcrHtmlContentAsync(imageBytes);

                    // 4. Update UI with results via JavaScript
                    CallScript("updateResults", new object[] { htmlResult });
                } // The 'using' block ensures the dummy image is properly disposed of.
            }
            catch (Exception ex)
            {
                string errorHtml = $"<p style='color: red;'><b>Failed to process image.</b><br/>Error: {ex.Message}</p>";
                CallScript("updateResults", new object[] { errorHtml });
            }
            finally
            {
                // 5. Restore button state via JavaScript
                CallScript("setButtonState", new object[] { false, "Convert to Text" });
            }
        }

        private Image SetUpImageBeforeCallAPI(Image originalImage)
        {
            // Define the minimum required dimensions.
            const int minDimension = 50;

            // Check if the image already meets the minimum size requirements.
            if (originalImage.Width >= minDimension && originalImage.Height >= minDimension)
            {
                return originalImage; // No change needed.
            }

            // Determine the final dimensions for the new dummy image.
            int newWidth = Math.Max(originalImage.Width, minDimension);
            int newHeight = Math.Max(originalImage.Height, minDimension);

            // Create a new Bitmap with the determined size and a white background.
            Bitmap dummyImage = new Bitmap(newWidth, newHeight);

            // Use a Graphics object to draw the original image onto the dummy image.
            using (Graphics g = Graphics.FromImage(dummyImage))
            {
                // Set a high-quality rendering mode for a cleaner result.
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                // Fill the background with white.
                g.Clear(Color.White);

                // Calculate the position to center the original image on the dummy image.
                int x = (newWidth - originalImage.Width) / 2;
                int y = (newHeight - originalImage.Height) / 2;

                // Draw the original image onto the dummy image at the calculated position.
                g.DrawImage(originalImage, x, y, originalImage.Width, originalImage.Height);
            }

            // Return the new, padded image.
            return dummyImage;
        }

        /// <summary>
        /// Converts the stored image to a Base64 string for embedding in HTML.
        /// </summary>
        private string GetImageAsBase64()
        {
            using (var ms = new MemoryStream())
            {
                _capturedImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// Generates the initial HTML document with the image, button, and scripts.
        /// </summary>
        private void LoadInitialHtml()
        {
            string imageBase64 = GetImageAsBase64();

            var html = new StringBuilder();
            html.Append("<!DOCTYPE html><html lang='en'><head>");
            html.Append("<meta http-equiv='X-UA-Compatible' content='IE=Edge' />");
            html.Append("<title>Image OCR</title>");

            // --- CSS STYLES ---
            html.Append("<style>");
            html.Append("html, body { margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #e9ebee; overflow: hidden; height: 100%; }");
            html.Append(".container { display: flex; flex-direction: column; height: 100%; }");
            html.Append(".image-container { flex-shrink: 0; background-color: #f0f2f5; text-align: center; padding: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            html.Append("#capturedImage { max-width: 100%; max-height: 35vh; border-radius: 8px; box-shadow: 0 4px 8px rgba(0,0,0,0.15); }");
            html.Append(".controls { flex-shrink: 0; text-align: center; padding: 15px; background-color: #f0f2f5; }");
            html.Append("#btnConvert { background-color: #4267B2; color: white; border: none; padding: 12px 24px; font-size: 16px; border-radius: 6px; cursor: pointer; transition: background-color 0.3s; }");
            html.Append("#btnConvert:disabled { background-color: #9dbdf2; cursor: not-allowed; }");
            html.Append(".results-container { flex-grow: 1; overflow-y: auto; background-color: #ffffff; padding: 20px; }");
            html.Append(".region-box { border-left: 3px solid #4267B2; padding: 10px; margin-bottom: 12px; background-color: #f7f8fa; line-height: 1.6; }");
            html.Append("</style>");

            html.Append("</head><body><div class='container'>");

            // --- HTML CONTENT ---
            html.Append($"<div class='image-container'><img id='capturedImage' src='data:image/png;base64,{imageBase64}' alt='Captured Image' /></div>");
            html.Append("<div class='controls'><button id='btnConvert' onclick='convertClick()'>Convert to Text</button></div>");
            html.Append("<div class='results-container' id='results'></div>");

            html.Append("</div>");

            // --- JAVASCRIPT ---
            html.Append("<script type='text/javascript'>");
            html.Append("function convertClick() { window.external.PerformOcr(); }");
            html.Append("function setButtonState(disabled, text) { var btn = document.getElementById('btnConvert'); btn.disabled = disabled; btn.innerText = text; }");
            html.Append("function updateResults(htmlContent) { document.getElementById('results').innerHTML = htmlContent; }");
            html.Append("</script>");

            html.Append("</body></html>");

            webBrowser1.DocumentText = html.ToString();
        }

        /// <summary>
        /// Calls the OCR service and returns the result formatted as an HTML snippet.
        /// </summary>
        private async Task<string> GetOcrHtmlContentAsync(byte[] imageBytes)
        {
            string apiKey = ConfigurationManager.AppSettings["AzureCognitiveServices.ComputerVisionKey"];
            string endpoint = ConfigurationManager.AppSettings["AzureCognitiveServices.Endpoint"];
            string ocrUrl = $"{endpoint}/vision/v3.2/ocr";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
                using (var content = new ByteArrayContent(imageBytes))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    HttpResponseMessage response = await client.PostAsync(ocrUrl, content);
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"API request failed. Status: {response.StatusCode}. Details: {jsonResponse}");
                    }

                    var textContent = new StringBuilder();
                    JObject data = JObject.Parse(jsonResponse);

                    if (data["regions"] != null && data["regions"].HasValues)
                    {
                        foreach (var region in data["regions"])
                        {
                            textContent.Append("<div class='region-box'>");
                            foreach (var line in region["lines"])
                            {
                                foreach (var word in line["words"])
                                {
                                    textContent.Append(word["text"]).Append(" ");
                                }
                                textContent.Append("<br/>");
                            }
                            textContent.Append("</div>");
                        }
                    }
                    return textContent.Length > 0 ? textContent.ToString() : "<p>No text was detected.</p>";
                }
            }
        }

        /// <summary>
        /// Safely invokes a JavaScript function in the WebBrowser control.
        /// </summary>
        public void CallScript(string functionName, object[] args)
        {
            if (webBrowser1.Document != null)
            {
                webBrowser1.Document.InvokeScript(functionName, args);
            }
        }

        #region Windows Form Designer generated code
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.WebBrowser webBrowser1;

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
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // webBrowser1
            // 
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(0, 0);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(484, 561);
            this.webBrowser1.TabIndex = 0;
            // 
            // DisplayImage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 561);
            this.Controls.Add(this.webBrowser1);
            this.Padding = new Padding(15,2,15,2);
            this.Name = "DisplayImage";
            this.Text = "Image to Text Converter";
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.LightGray; // Changed to Black for a better dimming effect
            this.ShowInTaskbar = false;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            this.ResumeLayout(false);
        }
        #endregion

        private bool isDragging = false;

        private Point dragStartPoint;
        private void me_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                // Store the starting point of the drag
                dragStartPoint = new Point(e.X, e.Y);
            }
        }

        private void me_MouseMove(object sender, MouseEventArgs e)
        {
            // If dragging is enabled
            if (isDragging)
            {
                // Get the current position of the cursor on the screen
                Point newPoint = this.PointToScreen(new Point(e.X, e.Y));
                // Offset the new point by the starting point to get the new form location
                newPoint.Offset(-dragStartPoint.X, -dragStartPoint.Y);
                // Set the form's location to the new point
                this.Location = newPoint;
            }
        }

        private void me_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Stop dragging
                isDragging = false;
            }
        }
    }
}
