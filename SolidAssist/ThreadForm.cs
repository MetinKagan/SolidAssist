using System;
using System.Drawing;
using System.Windows.Forms;

namespace SolidAssist
{
    public class ThreadForm : Form
    {
        private readonly double shaftD;
        private readonly double shaftL;
        private CheckBox chkStart;
        private CheckBox chkEnd;
        private ComboBox cboMetric;
        private Button btnApply;
        private Label lblStatus;

        public ThreadForm(double shaftDiameterMm, double shaftLengthMm)
        {
            shaftD = shaftDiameterMm;
            shaftL = shaftLengthMm;

            Text = "Kılavuz Ekle";
            Size = new Size(420, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var lblShaft = new Label
            {
                Text = $"Şaft: Ø{shaftDiameterMm} × {shaftLengthMm} mm",
                Left = 20, Top = 15, Width = 360, Font = new Font(Font, FontStyle.Bold)
            };

            var lblFaces = new Label { Text = "Hangi uçlara kılavuz açılacak?", Left = 20, Top = 50, Width = 220 };
            chkStart = new CheckBox { Text = "Başlangıç ucu (Z=0)", Left = 20, Top = 75, Width = 200, Checked = true };
            chkEnd   = new CheckBox { Text = "Bitiş ucu",            Left = 20, Top = 100, Width = 200, Checked = false };

            var lblM = new Label { Text = "Metrik:", Left = 20, Top = 140, Width = 60 };
            cboMetric = new ComboBox { Left = 90, Top = 137, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var s in ThreadStandard.Specs)
            {
                if (s.MinShaftMm <= shaftDiameterMm)
                    cboMetric.Items.Add($"{s.Name}  (ön delik Ø{s.TapDrillMm}, derinlik {s.DepthMm})");
            }
            if (cboMetric.Items.Count == 0)
            {
                cboMetric.Items.Add("(uygun standart yok)");
                cboMetric.Enabled = false;
            }
            else
            {
                var pick = ThreadStandard.PickForShaft(shaftDiameterMm);
                if (pick != null)
                {
                    int idx = ThreadStandard.Specs.FindIndex(x => x.Name == pick.Name);
                    if (idx >= 0 && idx < cboMetric.Items.Count) cboMetric.SelectedIndex = idx;
                }
                else cboMetric.SelectedIndex = 0;
            }

            btnApply = new Button { Text = "Kılavuz Aç", Left = 240, Top = 135, Width = 140, Height = 28 };
            btnApply.Click += BtnApply_Click;

            lblStatus = new Label { Left = 20, Top = 180, Width = 370, Height = 30, ForeColor = Color.DarkBlue };

            Controls.AddRange(new Control[] { lblShaft, lblFaces, chkStart, chkEnd, lblM, cboMetric, btnApply, lblStatus });
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            if (!chkStart.Checked && !chkEnd.Checked)
            {
                lblStatus.ForeColor = Color.DarkRed;
                lblStatus.Text = "En az bir uç seçin.";
                return;
            }
            if (cboMetric.SelectedIndex < 0 || !cboMetric.Enabled)
            {
                lblStatus.ForeColor = Color.DarkRed;
                lblStatus.Text = "Metrik seçin.";
                return;
            }

            string label = cboMetric.SelectedItem.ToString();
            string mName = label.Split(' ')[0];
            ThreadStandard.Spec spec = ThreadStandard.Specs.Find(x => x.Name == mName);

            btnApply.Enabled = false;
            lblStatus.ForeColor = Color.DarkBlue;
            lblStatus.Text = "Kılavuz açılıyor…";
            Application.DoEvents();

            try
            {
                if (chkStart.Checked) ThreadBuilder.AddTappedHole(shaftD, shaftL, true,  spec);
                if (chkEnd.Checked)   ThreadBuilder.AddTappedHole(shaftD, shaftL, false, spec);
                lblStatus.ForeColor = Color.DarkGreen;
                lblStatus.Text = $"{spec.Name} kılavuz delikleri açıldı.";
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.DarkRed;
                lblStatus.Text = "Hata: " + ex.Message;
            }
            finally
            {
                btnApply.Enabled = true;
            }
        }
    }
}
