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
        private Label lblStatus;

        public MainForm()
        {
            Text = "SolidAssist — Mil Tasarımı";
            Size = new Size(360, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            var lblD = new Label { Text = "Çap (mm):", Left = 20, Top = 25, Width = 90 };
            txtDiameter = new TextBox { Left = 120, Top = 22, Width = 200, Text = "50" };

            var lblL = new Label { Text = "Uzunluk (mm):", Left = 20, Top = 60, Width = 90 };
            txtLength = new TextBox { Left = 120, Top = 57, Width = 200, Text = "200" };

            btnCreate = new Button { Text = "Mil Oluştur", Left = 120, Top = 95, Width = 200, Height = 32 };
            btnCreate.Click += BtnCreate_Click;

            lblStatus = new Label { Left = 20, Top = 140, Width = 310, Height = 30, ForeColor = Color.DarkBlue };

            Controls.AddRange(new Control[] { lblD, txtDiameter, lblL, txtLength, btnCreate, lblStatus });
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
            lblStatus.ForeColor = Color.DarkBlue;
            lblStatus.Text = "Oluşturuluyor…";
            Application.DoEvents();

            try
            {
                ShaftBuilder.Create(dMm, lMm);
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
    }
}
