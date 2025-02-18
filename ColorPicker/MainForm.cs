using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ColorPicker
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        // Windows' hotkey event
        private static int WM_HOTKEY = 0x312;

        private byte r = 0, g = 0, b = 0;
        private string hex;
        private Random randomizer = new Random();

        // Set up the hotkey
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_HOTKEY)
            {
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);

                if (key == Keys.F1)
                {
                    Point cursor = new Point();
                    GetCursorPos(ref cursor);

                    r = GetColorAt(cursor).R;
                    b = GetColorAt(cursor).B;
                    g = GetColorAt(cursor).G;

                    rDisplay.Text = r.ToString();
                    gDisplay.Text = g.ToString();
                    bDisplay.Text = b.ToString();

                    rSlider.Value = r;
                    gSlider.Value = g;
                    bSlider.Value = b;

                    convertRGBToHex();
                }
            }
        }

        public MainForm()
        {
            InitializeComponent();
            RegisterHotKey(this.Handle, (int)Keys.F1, 0, (uint)Keys.F1);

            generateRandomColor();
        }

        #region Textboxes
        private void hexDisplay_TextChanged(object sender, EventArgs e)
        {
            hex = hexDisplay.Text;

            //prevents crash on launch from method being called as soon as the random color is created
            if (hexDisplay.Focused)
            {
                convertHexToRGB();
            }
        }

        private void rDisplay_TextChanged(object sender, EventArgs e)
        {
            byte.TryParse(rDisplay.Text, out r);
            rSlider.Value = r;
            convertRGBToHex();
        }

        private void gDisplay_TextChanged(object sender, EventArgs e)
        {
            byte.TryParse(gDisplay.Text, out g);
            gSlider.Value = g;
            convertRGBToHex();
        }

        private void bDisplay_TextChanged(object sender, EventArgs e)
        {
            byte.TryParse(bDisplay.Text, out b);
            bSlider.Value = b;
            convertRGBToHex();
        }
        #endregion

        #region Buttons
        private void randomColor_Click(object sender, EventArgs e)
        {
            generateRandomColor();
        }

        private void openColorDialog_Click(object sender, EventArgs e)
        {
            colorDialog.Color = Color.FromArgb(r, g, b);

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                colorPreview.BackColor = colorDialog.Color;

                r = colorDialog.Color.R;
                g = colorDialog.Color.G;
                b = colorDialog.Color.B;
                rDisplay.Text = colorDialog.Color.R.ToString();
                gDisplay.Text = colorDialog.Color.G.ToString();
                bDisplay.Text = colorDialog.Color.B.ToString();
                rSlider.Value = r;
                gSlider.Value = g;
                bSlider.Value = b;

                convertRGBToHex();
            }
        }

        private void saveColor_Click(object sender, EventArgs e)
        {
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Saved Colors.txt";

            if (File.Exists(filepath))
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine("R: " + r);
                    sw.WriteLine("G: " + g);
                    sw.WriteLine("B: " + b);
                    sw.WriteLine("Hex: #" + hex);
                    sw.WriteLine("================");
                    sw.Close();
                }
            }
            else
            {
                var savefile = File.Create(filepath);
                savefile.Close();

                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine("R: " + r);
                    sw.WriteLine("G: " + g);
                    sw.WriteLine("B: " + b);
                    sw.WriteLine("Hex: #" + hex);
                    sw.WriteLine("================");
                    sw.Close();
                }
            }
        }
        #endregion

        #region Logic
        //converts the RGB values to hex, updates the textbox if it's not focused, and displays the color
        public void convertRGBToHex()
        {
            hex = string.Format("{0:X}{1:X}{2:X}", r, g, b);

            if (!hexDisplay.Focused)
            {
                hexDisplay.Text = hex;
            }

            colorPreview.BackColor = Color.FromArgb(r, g, b);
        }

        //converts hex to RGB, updates the textboxes if they aren't focused, and displays the color
        public void convertHexToRGB()
        {
            try
            {
                int num = (int)long.Parse(hex, NumberStyles.HexNumber);
                r = Convert.ToByte((num & 0xFF0000) >> 16);
                g = Convert.ToByte((num & 0xFF00) >> 8);
                b = Convert.ToByte(num & 0xFF);
            }
            catch (FormatException ex)
            {
                Console.WriteLine(ex);
            }

            if (!rDisplay.Focused || !gDisplay.Focused || !bDisplay.Focused)
            {
                rDisplay.Text = r.ToString();
                gDisplay.Text = g.ToString();
                bDisplay.Text = b.ToString();
            }

            colorPreview.BackColor = Color.FromArgb(r, g, b);
        }

        //generates a random color
        public void generateRandomColor()
        {
            r = Convert.ToByte(randomizer.Next(255));
            g = Convert.ToByte(randomizer.Next(255));
            b = Convert.ToByte(randomizer.Next(255));

            rDisplay.Text = r.ToString();
            gDisplay.Text = g.ToString();
            bDisplay.Text = b.ToString();

            rSlider.Value = r;
            gSlider.Value = g;
            bSlider.Value = b;

            convertRGBToHex();
        }

        public Color GetColorAt(Point location)
        {
            Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);

            //take an image of the screen and wherever the cursor is, save the color
            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            //return the color of whatever pixel the cursor was at (the bitmap is 1x1 so 0,0)
            return screenPixel.GetPixel(0, 0);
        }
        #endregion

        #region Value Sliders
        private void rSlider_Scroll(object sender, EventArgs e)
        {
            rDisplay.Text = rSlider.Value.ToString();
        }

        private void gSlider_Scroll(object sender, EventArgs e)
        {
            gDisplay.Text = gSlider.Value.ToString();
        }

        private void bSlider_Scroll(object sender, EventArgs e)
        {
            bDisplay.Text = bSlider.Value.ToString();
        }
        #endregion

        private void MouseMoveTimer_Tick(object sender, EventArgs e)
        {
            Point cursor = new Point();
            GetCursorPos(ref cursor);

            var c = GetColorAt(cursor);
            this.BackColor = c;
        }
    }
}