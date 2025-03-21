// Form1.cs
using System;
using System.IO.Ports;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace proje
{
    public partial class Form1 : Form
    {
        private SerialPort arduinoPort;
        private ComboBox cmbPorts, cmbCategory;
        private RadioButton rbAuto, rbManuel;
        private Button btnLightOn, btnLightOff, btnConnect, btnRefreshPorts;
        private Label lblStatus;

        public Form1()
        {
            // Form Özellikleri
            this.Text = "Akıllı Aydınlatma Kontrol Paneli";
            this.Size = new Size(400, 550);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Kategori ComboBox
            Label lblCategory = new Label
            {
                Text = "Kategori Seçin:",
                Location = new Point(50, 30),
                AutoSize = true
            };

            cmbCategory = new ComboBox
            {
                Location = new Point(50, 60),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCategory.Items.AddRange(new string[] { "Salon", "Yatak Odası", "Mutfak", "Bahçe", "Çalışma Odası" });

            // Mod Seçim Grubu
            GroupBox gbMode = new GroupBox
            {
                Text = "Çalışma Modu",
                Location = new Point(50, 100),
                Size = new Size(300, 100)
            };

            rbAuto = new RadioButton
            {
                Text = "Otomatik Mod",
                Location = new Point(20, 30),
                Checked = true
            };

            rbManuel = new RadioButton
            {
                Text = "Manuel Mod",
                Location = new Point(20, 60)
            };

            gbMode.Controls.Add(rbAuto);
            gbMode.Controls.Add(rbManuel);

            // Işık Kontrol Butonları
            btnLightOn = new Button
            {
                Text = "Işığı Aç",
                Location = new Point(50, 250),
                Size = new Size(130, 50),
                Enabled = false
            };

            btnLightOff = new Button
            {
                Text = "Işığı Kapat",
                Location = new Point(220, 250),
                Size = new Size(130, 50),
                Enabled = false
            };

            // Durum Etiketi
            lblStatus = new Label
            {
                Text = "Durum: Bağlantı Bekleniyor",
                Location = new Point(50, 320),
                AutoSize = true
            };

            // Seri Port Listesi ComboBox
            Label lblPort = new Label
            {
                Text = "Bağlantı Noktası Seçin:",
                Location = new Point(50, 350),
                AutoSize = true
            };

            cmbPorts = new ComboBox
            {
                Location = new Point(50, 380),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Portları Yenile Butonu
            btnRefreshPorts = new Button
            {
                Text = "Yenile",
                Location = new Point(260, 380),
                Size = new Size(90, 30)
            };

            // Bağlantı Butonu
            btnConnect = new Button
            {
                Text = "Bağlan",
                Location = new Point(50, 420),
                Size = new Size(300, 50)
            };

            // Kontrolleri Forma Ekle
            this.Controls.Add(lblCategory);
            this.Controls.Add(cmbCategory);
            this.Controls.Add(gbMode);
            this.Controls.Add(btnLightOn);
            this.Controls.Add(btnLightOff);
            this.Controls.Add(lblStatus);
            this.Controls.Add(lblPort);
            this.Controls.Add(cmbPorts);
            this.Controls.Add(btnRefreshPorts);
            this.Controls.Add(btnConnect);

            // Event Handler'ları Tanımla
            btnLightOn.Click += (sender, e) => SendCommand("ON");
            btnLightOff.Click += (sender, e) => SendCommand("OFF");
            rbAuto.CheckedChanged += (sender, e) => SendCommand("AUTO");
            rbManuel.CheckedChanged += (sender, e) => SendCommand("MANUEL");
            btnConnect.Click += ConnectToArduino;
            btnRefreshPorts.Click += (sender, e) => ListAvailablePorts();

            // Uygulama Açıldığında Portları Listele
            ListAvailablePorts();
        }

        private void ListAvailablePorts()
        {
            cmbPorts.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            cmbPorts.Items.AddRange(ports);

            if (ports.Length > 0)
                cmbPorts.SelectedIndex = 0;
            else
                lblStatus.Text = "Durum: COM Port Bulunamadı!";
        }

        private void ConnectToArduino(object sender, EventArgs e)
        {
            if (cmbPorts.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir COM portu seçin!");
                return;
            }

            try
            {
                string selectedPort = cmbPorts.SelectedItem.ToString();
                arduinoPort = new SerialPort(selectedPort, 9600);
                arduinoPort.Open();
                lblStatus.Text = $"Durum: {selectedPort} bağlı!";
                MessageBox.Show($"{selectedPort} bağlantısı başarılı!");

                // Butonları Aktif Et
                btnLightOn.Enabled = true;
                btnLightOff.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bağlantı hatası: " + ex.Message);
            }
        }

        private void SendCommand(string command)
        {
            if (arduinoPort != null && arduinoPort.IsOpen)
            {
                arduinoPort.WriteLine(command);
            }
            else
            {
                MessageBox.Show("Arduino bağlantısı yapılmadı!");
            }
        }
    }
}