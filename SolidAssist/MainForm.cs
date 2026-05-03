using System;
using System.Drawing;
using System.Windows.Forms;

namespace SolidAssist
{
    public class MainForm : Form
    {
        private TextBox txtDiameter;
        private TextBox txtLength;
        private Button btnCreate;
        private Button btnKeyway;
        private Button btnThread;
        private Label lblStatus;

        // Last successfully created shaft (for follow-up features)
        private double? lastDiameter;
        private double? lastLength;

        public MainForm()
        {
            Text = "SolidAssist — Mil Tasarımı";
            Size = new Size(380, 310);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            var lblD = new Label { Text = "Çap (mm):", Left = 20, Top = 25, Width = 90 };
            txtDiameter = new TextBox { Left = 120, Top = 22, Width = 220, Text = "50" };

            var lblL = new Label { Text = "Uzunluk (mm):", Left = 20, Top = 60, Width = 90 };
            txtLength = new TextBox { Left = 120, Top = 57, Width = 220, Text = "200" };

            btnCreate = new Button { Text = "Mil Oluştur", Left = 120, Top = 95, Width = 220, Height = 32 };
            btnCreate.Click += BtnCreate_Click;

            btnKeyway = new Button { Text = "Kama Kanalı Ekle…", Left = 120, Top = 135, Width = 220, Height = 32, Enabled = false };
            btnKeyway.Click += BtnKeyway_Click;

            btnThread = new Button { Text = "Kılavuz Ekle…", Left = 120, Top = 175, Width = 220, Height = 32, Enabled = false };
            btnThread.Click += BtnThread_Click;

            lblStatus = new Label { Left = 20, Top = 220, Width = 330, Height = 30, ForeColor = Color.DarkBlue };

            Controls.AddRange(new Control[] { lblD, txtDiameter, lblL, txtLength, btnCreate, btnKeyway, btnThread, lblStatus });
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(txtDiameter.Text, out double dMm) || dMm <= 0)
            {
                lblStatus.Text = "Geçerli bir çap girin.";
                lblStatus.ForeColor = Color.DarkRed;
                return;
            }
            if (!double.TryParse(txtLength.Text, out double lMm) || lMm <= 0)
            {
                lblStatus.Text = "Geçerli bir uzunluk girin.";
                lblStatus.ForeColor = Color.DarkRed;
                return;
            }

            btnCreate.Enabled = false;
            btnKeyway.Enabled = false;
            btnThread.Enabled = false;
            lblStatus.ForeColor = Color.DarkBlue;
            lblStatus.Text = "Oluşturuluyor…";
            Application.DoEvents();

            try
            {
                ShaftBuilder.Create(dMm, lMm);
                lastDiameter = dMm;
                lastLength = lMm;
                btnKeyway.Enabled = true;
                btnThread.Enabled = true;
                lblStatus.ForeColor = Color.DarkGreen;
                lblStatus.Text = $"Mil oluşturuldu: Ø{dMm} × {lMm} mm";
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

        private void BtnKeyway_Click(object sender, EventArgs e)
        {
            if (!lastDiameter.HasValue || !lastLength.HasValue) return;
            using (var f = new KeywayForm(lastDiameter.Value, lastLength.Value))
            {
                f.ShowDialog(this);
            }
        }

        private void BtnThread_Click(object sender, EventArgs e)
        {
            if (!lastDiameter.HasValue || !lastLength.HasValue) return;
            using (var f = new ThreadForm(lastDiameter.Value, lastLength.Value))
            {
                f.ShowDialog(this);
            }
        }
    }
}
