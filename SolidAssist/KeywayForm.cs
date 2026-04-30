using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SolidAssist
{
    public class KeywayForm : Form
    {
        private readonly double shaftDiameterMm;
        private readonly double shaftLengthMm;
        private readonly KeywayStandard.Dimensions keyDims;

        private TextBox txtEdge;
        private Label lblInfo;
        private Label lblStatus;
        private Button btnCreate;
        private PictureBox pic;

        public KeywayForm(double diameterMm, double lengthMm)
        {
            shaftDiameterMm = diameterMm;
            shaftLengthMm = lengthMm;
            keyDims = KeywayStandard.GetForShaft(diameterMm, lengthMm);

            Text = "Kama Kanalı (DIN 6885 Form A)";
            Size = new Size(720, 460);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            lblInfo = new Label
            {
                Left = 20,
                Top = 15,
                Width = 670,
                Height = 40,
                Text = $"Şaft: Ø{shaftDiameterMm} × {shaftLengthMm} mm   |   " +
                       $"Kama (otomatik): genişlik {keyDims.WidthMm} mm  ·  derinlik {keyDims.DepthMm} mm  ·  uzunluk {keyDims.LengthMm} mm",
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            pic = new PictureBox
            {
                Left = 20,
                Top = 60,
                Width = 670,
                Height = 250,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            var lblE = new Label { Left = 20, Top = 325, Width = 150, Text = "Kenar mesafesi (mm):" };
            txtEdge = new TextBox { Left = 170, Top = 322, Width = 100, Text = "10" };
            txtEdge.TextChanged += (s, e) => RedrawPreview();

            btnCreate = new Button { Left = 290, Top = 320, Width = 200, Height = 28, Text = "Kanalı Oluştur" };
            btnCreate.Click += BtnCreate_Click;

            lblStatus = new Label { Left = 20, Top = 365, Width = 670, Height = 40, ForeColor = Color.DarkBlue };

            Controls.AddRange(new Control[] { lblInfo, pic, lblE, txtEdge, btnCreate, lblStatus });

            RedrawPreview();
        }

        private void RedrawPreview()
        {
            double edge = 0;
            double.TryParse(txtEdge.Text, out edge);
            if (edge < 0) edge = 0;

            var bmp = new Bitmap(pic.Width, pic.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                int margin = 30;
                int drawW = pic.Width - 2 * margin;
                int drawH = 110;
                int x0 = margin;
                int y0 = (pic.Height - drawH) / 2 - 10;

                double pxPerMm = drawW / shaftLengthMm;

                // Shaft
                using (var b = new SolidBrush(Color.FromArgb(225, 225, 230)))
                using (var p = new Pen(Color.Black, 2))
                {
                    g.FillRectangle(b, x0, y0, drawW, drawH);
                    g.DrawRectangle(p, x0, y0, drawW, drawH);
                }

                // Centerline
                using (var pc = new Pen(Color.Gray, 1) { DashStyle = DashStyle.DashDot })
                    g.DrawLine(pc, x0 - 15, y0 + drawH / 2, x0 + drawW + 15, y0 + drawH / 2);

                // Keyway dimensions in pixels
                double keyLenPx = keyDims.LengthMm * pxPerMm;
                double keyWidPx = Math.Max(14, keyDims.WidthMm * pxPerMm * 0.9); // visually exaggerated min
                double keyEdgePx = edge * pxPerMm;

                if (keyEdgePx + keyLenPx > drawW) keyEdgePx = Math.Max(0, drawW - keyLenPx);

                float kx = (float)(x0 + keyEdgePx);
                float kw = (float)keyLenPx;
                float kh = (float)keyWidPx;
                float ky = y0 + (drawH - kh) / 2f;

                using (var kb = new SolidBrush(Color.FromArgb(150, 180, 220)))
                using (var kp = new Pen(Color.FromArgb(40, 60, 100), 2))
                using (var path = new GraphicsPath())
                {
                    if (kw > kh)
                    {
                        path.AddArc(kx, ky, kh, kh, 90, 180);
                        path.AddArc(kx + kw - kh, ky, kh, kh, 270, 180);
                        path.CloseFigure();
                        g.FillPath(kb, path);
                        g.DrawPath(kp, path);
                    }
                }

                // Edge distance dimension
                using (var dp = new Pen(Color.DarkRed, 1.8f))
                using (var db = new SolidBrush(Color.DarkRed))
                using (var f = new Font("Segoe UI", 10, FontStyle.Bold))
                {
                    int dimY = y0 + drawH + 25;
                    g.DrawLine(dp, x0, y0 + drawH + 4, x0, dimY + 8);
                    g.DrawLine(dp, kx, ky + kh + 4, kx, dimY + 8);
                    if (kx > x0)
                    {
                        g.DrawLine(dp, x0, dimY, kx, dimY);
                        g.FillPolygon(db, new[] { new PointF(x0, dimY), new PointF(x0 + 8, dimY - 4), new PointF(x0 + 8, dimY + 4) });
                        g.FillPolygon(db, new[] { new PointF(kx, dimY), new PointF(kx - 8, dimY - 4), new PointF(kx - 8, dimY + 4) });
                    }
                    g.DrawString("Kenar mesafesi", f, db, (x0 + kx) / 2f - 50, dimY + 8);
                }
            }

            var old = pic.Image;
            pic.Image = bmp;
            old?.Dispose();
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(txtEdge.Text, out double edge) || edge < 0)
            {
                lblStatus.ForeColor = Color.DarkRed;
                lblStatus.Text = "Geçerli bir kenar mesafesi girin.";
                return;
            }
            if (edge + keyDims.LengthMm > shaftLengthMm)
            {
                lblStatus.ForeColor = Color.DarkRed;
                lblStatus.Text = $"Kanal sığmıyor: {edge} + {keyDims.LengthMm} = {edge + keyDims.LengthMm} > şaft {shaftLengthMm} mm.";
                return;
            }

            btnCreate.Enabled = false;
            lblStatus.ForeColor = Color.DarkBlue;
            lblStatus.Text = "Oluşturuluyor…";
            Application.DoEvents();

            try
            {
                KeywayBuilder.AddKeyway(shaftDiameterMm, shaftLengthMm, edge, keyDims);
                lblStatus.ForeColor = Color.DarkGreen;
                lblStatus.Text = "Kama kanalı oluşturuldu.";
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.DarkRed;
                lblStatus.Text = "Hata: " + ex.Message;
            }
            finally
            {
                btnCreate.Enabled = true;
            }
        }
    }
}
