using System;
using System.Drawing;
using System.Windows.Forms;

namespace w7junpro
{
    public partial class Dashboard : Form
    {
        private Panel cInv = null!, cTok = null!, cKwh = null!, cCo2 = null!, cWater = null!;
        private Panel cPhone = null!, cBottle = null!, cLed = null!;

        public Dashboard()
        {
            InitializeComponent();
            InisialisasiLofiDashboard();
            this.FormClosed += (s, e) => Application.Exit();
            MuatRingkasan();
        }

        private void InisialisasiLofiDashboard()
        {
            // 1. Pengaturan Dasar Form
            this.Text = "AI Environmental Telemetry & Impact Portal";
            this.ClientSize = new Size(950, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // 2. Header Area
            Panel headerPanel = new Panel
            {
                Location = new Point(20, 15),
                Size = new Size(910, 60),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblTitle = new Label
            {
                Text = "AI Environmental Telemetry & Impact Portal",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(15, 10),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Department of Electrical Engineering & Information Technology - FT UGM",
                ForeColor = Color.DimGray,
                Location = new Point(15, 34),
                AutoSize = true
            };

            Button btnExport = new Button
            {
                Text = "Export CSV",
                Location = new Point(800, 15),
                Size = new Size(95, 30),
                BackColor = Color.White
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);
            headerPanel.Controls.Add(btnExport);
            this.Controls.Add(headerPanel);

            // 3. Section Title 1
            Label lblSec1 = new Label
            {
                Text = "PRIMARY ENVIRONMENTAL TELEMETRY",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.SlateGray,
                Location = new Point(20, 90),
                AutoSize = true
            };
            this.Controls.Add(lblSec1);

            // 4. Baris Kartu Metrik Utama (Telemetry Cards)
            int cardW = 174;
            int cardH = 90;
            int startX = 20;
            int gap = 10;

            this.Controls.Add(cInv = BuatMetricCard("TOTAL INVOCATIONS", "0", "Real-time requests", startX + 0 * (cardW + gap), 110, cardW, cardH));
            this.Controls.Add(cTok = BuatMetricCard("TOKENS PROCESSED", "0", "Prompt & payload", startX + 1 * (cardW + gap), 110, cardW, cardH));
            this.Controls.Add(cKwh = BuatMetricCard("GRID ELECTRICITY", "0.00 kWh", "Baseline estimation", startX + 2 * (cardW + gap), 110, cardW, cardH));
            this.Controls.Add(cCo2 = BuatMetricCard("CARBON FOOTPRINT", "0.00 g", "CO2 eq factor", startX + 3 * (cardW + gap), 110, cardW, cardH));
            this.Controls.Add(cWater = BuatMetricCard("DIRECT WATER USAGE", "0.00 mL", "Data-center WUE", startX + 4 * (cardW + gap), 110, cardW, cardH));

            // 5. Section Title 2
            Label lblSec2 = new Label
            {
                Text = "PHYSICAL IMPACT EQUIVALENCIES",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.SlateGray,
                Location = new Point(20, 220),
                AutoSize = true
            };
            this.Controls.Add(lblSec2);

            // 6. Kartu Dampak Fisik
            int eqW = 296;
            int eqH = 75;
            this.Controls.Add(cPhone = BuatMetricCard("Smartphone Full Charge", "0.00 charges", "Standard 15 Wh battery", startX + 0 * (eqW + gap), 240, eqW, eqH));
            this.Controls.Add(cBottle = BuatMetricCard("Freshwater Bottle Ratio", "0.0000 bottles", "Fraction of 600 mL unit", startX + 1 * (eqW + gap), 240, eqW, eqH));
            this.Controls.Add(cLed = BuatMetricCard("10W LED Duration", "0.00 hours", "Domestic LED bulb operation", startX + 2 * (eqW + gap), 240, eqW, eqH));

            // 7. Area Bawah Kiri: Console Simulasi
            GroupBox gbConsole = new GroupBox
            {
                Text = "Live Telemetry Audit Console",
                Location = new Point(20, 335),
                Size = new Size(445, 300),
                BackColor = Color.White
            };

            Label lblModel = new Label { Text = "Target Upstream Model:", Location = new Point(15, 30), AutoSize = true };
            ComboBox cbModel = new ComboBox { Location = new Point(15, 55), Size = new Size(410, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cbModel.Items.AddRange(new string[] { "Gemini 2.5 Flash", "GPT-4o Mini", "Claude 3.5 Haiku" });
            cbModel.SelectedIndex = 0;

            Label lblPrompt = new Label { Text = "Test Prompt Payload:", Location = new Point(15, 95), AutoSize = true };
            TextBox txtPrompt = new TextBox
            {
                Text = "Evaluate the carbon intensity of distributed renewable microgrids.",
                Location = new Point(15, 120),
                Size = new Size(410, 110),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            Button btnSimulate = new Button
            {
                Text = "Run Simulation",
                Location = new Point(15, 245),
                Size = new Size(410, 35),
                BackColor = Color.LightSkyBlue
            };

            btnSimulate.Click += (s, e) => JalankanSimulasi(cbModel.SelectedItem?.ToString() ?? "", txtPrompt.Text);

            gbConsole.Controls.Add(lblModel);
            gbConsole.Controls.Add(cbModel);
            gbConsole.Controls.Add(lblPrompt);
            gbConsole.Controls.Add(txtPrompt);
            gbConsole.Controls.Add(btnSimulate);
            this.Controls.Add(gbConsole);

            // 8. Area Bawah Kanan: Placeholder Grafik / Trajectory
            GroupBox gbChart = new GroupBox
            {
                Text = "Cumulative Environmental Impact Trajectory",
                Location = new Point(485, 335),
                Size = new Size(445, 300),
                BackColor = Color.White
            };

            Panel chartArea = new Panel
            {
                Location = new Point(15, 35),
                Size = new Size(415, 245),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            Label lblWireframeChart = new Label
            {
                Text = "[ Grafik Emisi & Penggunaan Daya (Wireframe Placeholder) ]",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray
            };

            chartArea.Controls.Add(lblWireframeChart);
            gbChart.Controls.Add(chartArea);
            this.Controls.Add(gbChart);
        }

        private void JalankanSimulasi(string modelName, string prompt)
        {
            try
            {
                AIModel model = AIModel.Create(modelName); // polymorphism
                ImpactResult result = model.CalculateImpact(prompt);
                Database.SaveUsage(model, prompt, result);
                MuatRingkasan();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan ke PostgreSQL: " + ex.Message);
            }
        }

        private void MuatRingkasan()
        {
            try
            {
                Database.Initialize();
                UsageSummary u = Database.GetSummary();
                SetNilai(cInv, u.Invocations.ToString());
                SetNilai(cTok, u.Tokens.ToString());
                SetNilai(cKwh, u.EnergyKWh.ToString("0.000000") + " kWh");
                SetNilai(cCo2, u.CarbonG.ToString("0.0000") + " g");
                SetNilai(cWater, u.WaterMl.ToString("0.0000") + " mL");
                SetNilai(cPhone, (u.EnergyKWh * 1000 / 15.0).ToString("0.0000") + " charges");
                SetNilai(cBottle, (u.WaterMl / 600.0).ToString("0.0000") + " bottles");
                SetNilai(cLed, (u.EnergyKWh * 1000 / 10.0).ToString("0.0000") + " hours");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Koneksi PostgreSQL gagal: " + ex.Message);
            }
        }

        private static void SetNilai(Panel card, string text) => card.Controls[1].Text = text;

        private Panel BuatMetricCard(string judul, string nilai, string catatan, int x, int y, int w, int h)
        {
            Panel card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblJudul = new Label
            {
                Text = judul,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.Gray,
                Location = new Point(8, 8),
                AutoSize = true
            };

            Label lblNilai = new Label
            {
                Text = nilai,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                Location = new Point(8, 28),
                AutoSize = true
            };

            Label lblNote = new Label
            {
                Text = catatan,
                Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                ForeColor = Color.DarkGray,
                Location = new Point(8, 58),
                AutoSize = true
            };

            card.Controls.Add(lblJudul);
            card.Controls.Add(lblNilai);
            card.Controls.Add(lblNote);
            return card;
        }
    }
}