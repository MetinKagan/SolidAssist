using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SolidAssist
{
    public class MainForm : Form
    {
        // Mil
        private TextBox txtDiameter;
        private TextBox txtLength;

        // Kama
        private CheckBox chkKeyway;
        private Label lblKeyInfo;
        private TextBox txtEdge;
        private PictureBox picKeyway;

        // Kılavuz
        private CheckBox chkThread;
        private CheckBox chkStart;
        private CheckBox chkEnd;
        private ComboBox cboMetric;

        // Alt
        private Button btnCreate;
        private Label lblStatus;

        private KeywayStandard.Dimensions keyDims;

        public MainForm()
        {
            Text = "SolidAssist — Mil Tasarımı";
            Size = new Size(760, 668);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            // ── Mil Parametreleri ──────────────────────────────────────────────
            var grpMil = new GroupBox { Text = "Mil Parametreleri", Left = 10, Top = 8, Width = 724, Height = 75 };
            var lblD = new Label { Text = "Çap (mm):", Left = 12, Top = 30, Width = 80, AutoSize = false };
            txtDiameter = new TextBox { Left = 97, Top = 27, Width = 100, Text = "50" };
            var lblL = new Label { Text = "Uzunluk (mm):", Left = 225, Top = 30, Width = 100, AutoSize = false };
            txtLength = new TextBox { Left = 330, Top = 27, Width = 100, Text = "200" };
            grpMil.Controls.AddRange(new Control[] { lblD, txtDiameter, lblL, txtLength });

            txtDiameter.TextChanged += OnShaftParamsChanged;
            txtLength.TextChanged   += OnShaftParamsChanged;

            // ── Kama Kanalı ───────────────────────────────────────────────────
            var grpKey = new GroupBox { Text = "Kama Kanalı (DIN 6885 Form A)", Left = 10, Top = 90, Width = 724, Height = 318 };

            chkKeyway = new CheckBox { Text = "Kama kanalı ekle", Left = 12, Top = 22, Width = 190, Checked = false };
            chkKeyway.CheckedChanged += (s, e) => { UpdateKeywayEnabled(); RedrawPreview(); };

            lblKeyInfo = new Label { Left = 12, Top = 48, Width = 700, Height = 36, Text = "—" };

            picKeyway = new PictureBox
            {
                Left = 12, Top = 88, Width = 700, Height = 170,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White
            };

            var lblE = new Label { Left = 12, Top = 272, Width = 155, Text = "Kenar mesafesi (mm):", AutoSize = false };
            txtEdge = new TextBox { Left = 170, Top = 269, Width = 80, Text = "10", Enabled = false };
            txtEdge.TextChanged += (s, e) => RedrawPreview();

            grpKey.Controls.AddRange(new Control[] { chkKeyway, lblKeyInfo, picKeyway, lblE, txtEdge });

            // ── Kılavuz ───────────────────────────────────────────────────────
            var grpThread = new GroupBox { Text = "Kılavuz", Left = 10, Top = 416, Width = 724, Height = 108 };

            chkThread = new CheckBox { Text = "Kılavuz ekle", Left = 12, Top = 22, Width = 145, Checked = false };
            chkThread.CheckedChanged += (s, e) => UpdateThreadEnabled();

            chkStart = new CheckBox { Text = "Başlangıç ucu (Z=0)", Left = 175, Top = 22, Width = 185, Checked = true,  Enabled = false };
            chkEnd   = new CheckBox { Text = "Bitiş ucu",            Left = 375, Top = 22, Width = 130, Checked = false, Enabled = false };

            var lblM = new Label { Left = 12, Top = 60, Width = 60, Text = "Metrik:", AutoSize = false };
            cboMetric = new ComboBox { Left = 76, Top = 57, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };

            grpThread.Controls.AddRange(new Control[] { chkThread, chkStart, chkEnd, lblM, cboMetric });

            // ── Oluştur ───────────────────────────────────────────────────────
            btnCreate = new Button
            {
                Text = "Oluştur", Left = 10, Top = 532, Width = 724, Height = 38,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnCreate.Click += BtnCreate_Click;

            lblStatus = new Label { Left = 10, Top = 578, Width = 724, Height = 44, ForeColor = Color.DarkBlue };

            Controls.AddRange(new Control[] { grpMil, grpKey, grpThread, btnCreate, lblStatus });

            RefreshKeyway();
            RefreshThread();
        }

        // ── Olay: şaft parametreleri değişti ──────────────────────────────────

        private void OnShaftParamsChanged(object sender, EventArgs e)
        {
            RefreshKeyway();
            RefreshThread();
        }

        // ── Yardımcı: metin kutularından çift değer al ────────────────────────

        private double GetDiameter()
        {
            double.TryParse(txtDiameter.Text, out double d);
            return d;
        }

        private double GetLength()
        {
            double.TryParse(txtLength.Text, out double l);
            return l;
        }

        // ── Kama standard güncellemesi ────────────────────────────────────────

        private void RefreshKeyway()
        {
            double d = GetDiameter();
            double l = GetLength();
            if (d > 0 && l > 0)
            {
                try
                {
                    keyDims = KeywayStandard.GetForShaft(d, l);
                    lblKeyInfo.Text =
                        $"Otomatik seçilen kama:  genişlik {keyDims.WidthMm} mm  ·  " +
                        $"derinlik {keyDims.DepthMm} mm  ·  uzunluk {keyDims.LengthMm} mm";
                }
                catch
                {
                    keyDims = null;
                    lblKeyInfo.Text = "Çap DIN 6885 tablosu dışında (6–130 mm).";
                }
            }
            else
            {
                keyDims = null;
                lblKeyInfo.Text = "—";
            }
            RedrawPreview();
        }

        // ── Kılavuz combo güncellemesi ────────────────────────────────────────

        private void RefreshThread()
        {
            double d = GetDiameter();
            cboMetric.Items.Clear();

            if (d > 0)
            {
                foreach (var s in ThreadStandard.Specs)
                    if (s.MinShaftMm <= d)
                        cboMetric.Items.Add($"{s.Name}  (ön delik Ø{s.TapDrillMm} mm, derinlik {s.DepthMm} mm)");

                if (cboMetric.Items.Count == 0)
                {
                    cboMetric.Items.Add("(uygun standart yok)");
                }
                else
                {
                    var pick = ThreadStandard.PickForShaft(d);
                    if (pick != null)
                    {
                        for (int i = 0; i < cboMetric.Items.Count; i++)
                            if (cboMetric.Items[i].ToString().StartsWith(pick.Name + " "))
                            { cboMetric.SelectedIndex = i; break; }
                    }
                    if (cboMetric.SelectedIndex < 0 && cboMetric.Items.Count > 0)
                        cboMetric.SelectedIndex = 0;
                }
            }

            UpdateThreadEnabled();
        }

        // ── Enable/disable grupları ────────────────────────────────────────────

        private void UpdateKeywayEnabled()
        {
            txtEdge.Enabled = chkKeyway.Checked;
        }

        private void UpdateThreadEnabled()
        {
            bool on = chkThread.Checked;
            chkStart.Enabled = on;
            chkEnd.Enabled   = on;
            bool hasSpecs = cboMetric.Items.Count > 0 &&
                            !cboMetric.Items[0].ToString().StartsWith("(uygun");
            cboMetric.Enabled = on && hasSpecs;
        }

        // ── Kama önizleme çizimi ───────────────────────────────────────────────

        private void RedrawPreview()
        {
            double d = GetDiameter();
            double l = GetLength();

            if (keyDims == null || d <= 0 || l <= 0)
            {
                var old2 = picKeyway.Image;
                picKeyway.Image = null;
                old2?.Dispose();
                return;
            }

            double edge = 0;
            double.TryParse(txtEdge.Text, out edge);
            if (edge < 0) edge = 0;

            var bmp = new Bitmap(picKeyway.Width, picKeyway.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                int margin = 30;
                int drawW  = picKeyway.Width - 2 * margin;
                int drawH  = 80;
                int x0     = margin;
                int y0     = (picKeyway.Height - drawH) / 2 - 10;

                double pxPerMm = drawW / l;

                // Şaft gövdesi
                using (var b = new SolidBrush(Color.FromArgb(225, 225, 230)))
                using (var p = new Pen(Color.Black, 2))
                {
                    g.FillRectangle(b, x0, y0, drawW, drawH);
                    g.DrawRectangle(p, x0, y0, drawW, drawH);
                }

                // Eksen çizgisi
                using (var pc = new Pen(Color.Gray, 1) { DashStyle = DashStyle.DashDot })
                    g.DrawLine(pc, x0 - 15, y0 + drawH / 2, x0 + drawW + 15, y0 + drawH / 2);

                // Kama kanalı
                double keyLenPx  = keyDims.LengthMm * pxPerMm;
                double keyWidPx  = Math.Max(10, keyDims.WidthMm * pxPerMm * 0.85);
                double keyEdgePx = edge * pxPerMm;

                if (keyEdgePx + keyLenPx > drawW)
                    keyEdgePx = Math.Max(0, drawW - keyLenPx);

                float kx = (float)(x0 + keyEdgePx);
                float kw = (float)keyLenPx;
                float kh = (float)keyWidPx;
                float ky = y0 + (drawH - kh) / 2f;

                if (chkKeyway.Checked && kw > kh)
                {
                    using (var kb   = new SolidBrush(Color.FromArgb(150, 180, 220)))
                    using (var kp   = new Pen(Color.FromArgb(40, 60, 100), 2))
                    using (var path = new GraphicsPath())
                    {
                        path.AddArc(kx, ky, kh, kh, 90, 180);
                        path.AddArc(kx + kw - kh, ky, kh, kh, 270, 180);
                        path.CloseFigure();
                        g.FillPath(kb, path);
                        g.DrawPath(kp, path);
                    }
                }

                // Kenar mesafesi kotu
                using (var dp = new Pen(Color.DarkRed, 1.6f))
                using (var db = new SolidBrush(Color.DarkRed))
                using (var f  = new Font("Segoe UI", 9, FontStyle.Bold))
                {
                    int dimY = y0 + drawH + 18;
                    g.DrawLine(dp, x0, y0 + drawH + 3, x0, dimY + 8);
                    g.DrawLine(dp, kx, ky + kh + 3,    kx, dimY + 8);
                    if (kx > x0 + 1)
                    {
                        g.DrawLine(dp, x0, dimY, kx, dimY);
                        g.FillPolygon(db, new[] { new PointF(x0,     dimY), new PointF(x0 + 8, dimY - 4), new PointF(x0 + 8, dimY + 4) });
                        g.FillPolygon(db, new[] { new PointF(kx,     dimY), new PointF(kx - 8, dimY - 4), new PointF(kx - 8, dimY + 4) });
                        g.DrawString($"{edge:0.#} mm", f, db, (x0 + kx) / 2f - 18, dimY + 9);
                    }
                }
            }

            var old = picKeyway.Image;
            picKeyway.Image = bmp;
            old?.Dispose();
        }

        // ── Oluştur butonu ────────────────────────────────────────────────────

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            // Mil doğrulama
            if (!double.TryParse(txtDiameter.Text, out double dMm) || dMm <= 0)
            {
                SetStatus("Geçerli bir çap girin.", Color.DarkRed);
                return;
            }
            if (!double.TryParse(txtLength.Text, out double lMm) || lMm <= 0)
            {
                SetStatus("Geçerli bir uzunluk girin.", Color.DarkRed);
                return;
            }

            // Kama doğrulama
            double edge = 0;
            if (chkKeyway.Checked)
            {
                if (keyDims == null)
                {
                    SetStatus("Kama standardı belirlenemedi. Çap aralığını kontrol edin.", Color.DarkRed);
                    return;
                }
                if (!double.TryParse(txtEdge.Text, out edge) || edge < 0)
                {
                    SetStatus("Geçerli bir kenar mesafesi girin.", Color.DarkRed);
                    return;
                }
                if (edge + keyDims.LengthMm > lMm)
                {
                    SetStatus($"Kanal sığmıyor: {edge} + {keyDims.LengthMm} = {edge + keyDims.LengthMm} > {lMm} mm.", Color.DarkRed);
                    return;
                }
            }

            // Kılavuz doğrulama
            ThreadStandard.Spec threadSpec = null;
            if (chkThread.Checked)
            {
                if (!chkStart.Checked && !chkEnd.Checked)
                {
                    SetStatus("En az bir kılavuz ucu seçin.", Color.DarkRed);
                    return;
                }
                if (!cboMetric.Enabled || cboMetric.SelectedIndex < 0)
                {
                    SetStatus("Metrik seçin.", Color.DarkRed);
                    return;
                }
                string mName = cboMetric.SelectedItem.ToString().Split(' ')[0];
                threadSpec = ThreadStandard.Specs.Find(x => x.Name == mName);
            }

            btnCreate.Enabled = false;

            try
            {
                SetStatus("Mil oluşturuluyor…", Color.DarkBlue);
                ShaftBuilder.Create(dMm, lMm);

                if (chkKeyway.Checked)
                {
                    SetStatus("Kama kanalı oluşturuluyor…", Color.DarkBlue);
                    KeywayBuilder.AddKeyway(dMm, lMm, edge, keyDims);
                }

                if (chkThread.Checked)
                {
                    SetStatus("Kılavuz açılıyor…", Color.DarkBlue);
                    if (chkStart.Checked) ThreadBuilder.AddTappedHole(dMm, lMm, true,  threadSpec);
                    if (chkEnd.Checked)   ThreadBuilder.AddTappedHole(dMm, lMm, false, threadSpec);
                }

                SetStatus(BuildSuccessMessage(dMm, lMm, edge, threadSpec), Color.DarkGreen);
            }
            catch (Exception ex)
            {
                SetStatus("Hata: " + ex.Message, Color.DarkRed);
            }
            finally
            {
                btnCreate.Enabled = true;
            }
        }

        private void SetStatus(string text, Color color)
        {
            lblStatus.ForeColor = color;
            lblStatus.Text = text;
            Application.DoEvents();
        }

        private string BuildSuccessMessage(double d, double l, double edge, ThreadStandard.Spec spec)
        {
            var parts = new List<string>();
            parts.Add($"Ø{d} × {l} mm mil");
            if (chkKeyway.Checked)
                parts.Add($"kama kanalı ({keyDims.WidthMm}×{keyDims.DepthMm}×{keyDims.LengthMm} mm, kenar: {edge} mm)");
            if (chkThread.Checked)
            {
                var ends = new List<string>();
                if (chkStart.Checked) ends.Add("başlangıç");
                if (chkEnd.Checked)   ends.Add("bitiş");
                parts.Add($"{spec.Name} kılavuz ({string.Join("+", ends)} ucu)");
            }
            return "Tamamlandı: " + string.Join("  +  ", parts);
        }
    }
}
