using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;


namespace proje
{
    public class WeatherInfo
    {
        public MainInfo Main { get; set; }
        public WeatherCondition[] Weather { get; set; }
        public Sys Sys { get; set; } // Sys sınıfını ekleyin
    }

    public class Sys
    {
        public long Sunrise { get; set; }
        public long Sunset { get; set; }
    }

    public class WeatherCondition
    {
        public string Description { get; set; }
        public string Icon { get; set; }
    }

    public class MainInfo
    {
        public float Temp { get; set; }
        public int Humidity { get; set; }
    }

    public partial class Form1 : Form
    {
        private SerialPort arduinoPort;
        private ComboBox cmbPorts, cmbCategory;
        private RadioButton rbAuto, rbManuel;
        private Button btnLightOn, btnLightOff, btnConnect, btnRefreshPorts, btnProjectInfo, btnToggleSidePanel;
        private Label lblStatus;
        private Panel pnlCategory, pnlMode, pnlStatus, pnlPort, pnlLogo, pnlSide;
        private PictureBox picBoxLogo;
        private Image backgroundImage;
        private bool isSidePanelVisible = false;
        private Timer sideAnimationTimer;
        private Panel pnlBrightness;
        private TrackBar brightnessSlider;
        private Label lblBrightness;
        private Label lblBrightnessValue;
        private bool isBrightnessPanelVisible = false;
        private Panel pnlWeather;
        private Label lblTemperature;
        private Label lblCondition;
        private Label lblHumidity;
        private PictureBox weatherIcon;
        private Button btnRefreshWeather;
        private string currentCity = "Kastamonu";
        private const string WEATHER_API_KEY = "7bb01c521059ba6ef6ff5a12afa9406b";
        private const string WEATHER_API_URL = "http://api.openweathermap.org/data/2.5/weather";
        private Panel pnlTiming;
        private DateTimePicker timePickerStart, timePickerEnd;
        private Label lblSunrise, lblSunset;
        private Timer dailyTimer;
        private bool isTimingPanelVisible = false;
        private bool isTimingEnabled = false;
        private Button btnToggleTiming;
        private DateTime sunriseTime;
        private DateTime sunsetTime;
        private bool isDaylightOffEnabled = false; // Gün ışığında LED'i kapatma özelliği
        private Button btnDaylightOff; // Gün ışığında kapat butonu
        private Panel pnlRgb;
        private Button btnColorRed, btnColorGreen, btnColorBlue, btnColorWhite, btnColorPurple, btnColorYellow;
        private TrackBar trackRed, trackGreen, trackBlue;
        private Label lblRed, lblGreen, lblBlue, lblRedValue, lblGreenValue, lblBlueValue;
        private Panel pnlColorPreview;


        public class BufferedPanel : Panel
        {
            public BufferedPanel()
            {
                this.DoubleBuffered = true;
            }
        }
        public Form1()
        {
            InitializeComponent();
            InitializeForm();
            CreatePanelsAndControls();
            SetupSidePanel();
            SetupEventHandlers();
            ListAvailablePorts();
            SetupWeatherPanel();
            SetupTimingPanel();
            InitializeTimers();
            SetupRgbPanel();

            isTimingEnabled = false;

            // Uygulama başlangıcında hava durumu bilgisini al
            _ = UpdateWeatherInfo();




        }

        private void InitializeForm()
        {
            this.DoubleBuffered = true;
            sideAnimationTimer = new Timer { Interval = 16 };
            sideAnimationTimer.Tick += SidePanel_Animation;

            this.Text = "Akıllı Aydınlatma Kontrol Paneli";
            this.Size = new Size(550, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(0, 122, 204);

            try
            {
                string backgroundPath = @"C:\Users\Burak\Desktop\proje\Fotoğrafçılık (2).jpg";
                if (File.Exists(backgroundPath))
                {
                    backgroundImage = Image.FromFile(backgroundPath);
                    this.BackgroundImage = backgroundImage;
                    this.BackgroundImageLayout = ImageLayout.Stretch;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Arka plan yüklenirken hata oluştu: {ex.Message}");
            }
        }

        private void CreatePanelsAndControls()
        {
            pnlCategory = CreatePanel(50, 40, 450, 100);
            Label lblCategory = CreateLabel("KATEGORİ SEÇİN", 25, 15);
            cmbCategory = CreateComboBox(25, 45, 400);
            cmbCategory.Items.AddRange(new string[] { "Salon", "Yatak Odası", "Mutfak", "Bahçe", "Çalışma Odası" });
            pnlCategory.Controls.AddRange(new Control[] { lblCategory, cmbCategory });

            pnlMode = CreatePanel(50, 160, 450, 120);
            Label lblMode = CreateLabel("ÇALIŞMA MODU", 25, 15);
            rbAuto = CreateRadioButton("Otomatik Mod", 30, 55);
            rbManuel = CreateRadioButton("Manuel Mod", 240, 55);
            rbAuto.Checked = true;
            pnlMode.Controls.AddRange(new Control[] { lblMode, rbAuto, rbManuel });

            btnLightOn = CreateRoundedButton("IŞIĞI AÇ", 50, 300, 215, 60, Color.FromArgb(0, 153, 255));
            btnLightOff = CreateRoundedButton("IŞIĞI KAPAT", 285, 300, 215, 60, Color.FromArgb(0, 102, 204));
            btnLightOn.Enabled = btnLightOff.Enabled = false;

            pnlStatus = CreatePanel(50, 380, 450, 60);
            lblStatus = CreateLabel("DURUM: Bağlantı Bekleniyor", 25, 20);
            pnlStatus.Controls.Add(lblStatus);

            pnlPort = CreatePanel(50, 460, 450, 140);
            Label lblPort = CreateLabel("BAĞLANTI NOKTASI SEÇİN", 25, 15);
            cmbPorts = CreateComboBox(25, 50, 300);
            btnRefreshPorts = CreateRoundedButton("YENİLE", 335, 50, 90, 35, Color.FromArgb(0, 153, 255));
            btnConnect = CreateRoundedButton("BAĞLAN", 25, 95, 400, 35, Color.FromArgb(0, 153, 255));
            pnlPort.Controls.AddRange(new Control[] { lblPort, cmbPorts, btnRefreshPorts, btnConnect });

            pnlLogo = CreatePanel(50, 620, 450, 80);
            picBoxLogo = new PictureBox
            {
                Size = new Size(450, 80),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill
            };

            try
            {
                string logoPath = @"C:\Users\Burak\Downloads\Desktop Screenshot 2025.02.08 - 13.19.53.53-fotor-bg-remover-20250208132136.png";
                if (File.Exists(logoPath))
                {
                    picBoxLogo.Image = Image.FromFile(logoPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Logo yüklenirken hata oluştu: {ex.Message}");
            }
            pnlLogo.Controls.Add(picBoxLogo);

            btnProjectInfo = CreateRoundedButton("PROJE BİLGİLERİ", 50, 710, 450, 50, Color.FromArgb(0, 102, 204));

            this.Controls.AddRange(new Control[] {
                    pnlCategory, pnlMode, btnLightOn, btnLightOff,
                    pnlStatus, pnlPort, pnlLogo, btnProjectInfo
                });
        }

        private void SetupSidePanel()
        {
            pnlSide = new Panel
            {
                Size = new Size(250, this.Height),
                Location = new Point(-250, 0),
                BackColor = Color.FromArgb(40, 40, 40),
                Visible = true,
                Dock = DockStyle.None
            };

            typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(pnlSide, true, null);

            Label lblSideTitle = new Label
            {
                Text = "Ek Kontroller",
                Font = new Font("Montserrat", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };

            Button btnSideOption1 = CreateRoundedButton("Parlaklık Ayarları", 20, 70, 210, 40, Color.FromArgb(0, 153, 255));
            Button btnSideOption2 = CreateRoundedButton("Hava Durumu", 20, 120, 210, 40, Color.FromArgb(0, 153, 255));
            Button btnSideOption3 = CreateRoundedButton("Zamanlama Ayarları", 20, 170, 210, 40, Color.FromArgb(0, 153, 255));
            Button btnSideOption4 = CreateRoundedButton("RGB Led Kontrolü", 20, 220, 210, 40, Color.FromArgb(0, 153, 255));
            btnSideOption4.Click += ToggleRgbPanel;


            btnSideOption1.Click += ToggleBrightnessPanel;
            btnSideOption2.Click += (sender, e) => ToggleWeatherPanel();
            btnSideOption3.Click += ToggleTimingPanel;

            pnlSide.Controls.AddRange(new Control[] {
                    lblSideTitle,
    btnSideOption1,
    btnSideOption2,
    btnSideOption3,
    btnSideOption4
                });

            this.Controls.Add(pnlSide);
            pnlSide.BringToFront();

            SetupBrightnessPanel();

            btnToggleSidePanel = CreateRoundedButton("→", 10, 10, 30, 30, Color.FromArgb(0, 153, 255));
            this.Controls.Add(btnToggleSidePanel);
            btnToggleSidePanel.BringToFront();
        }

        private void SetupBrightnessPanel()
        {
            pnlBrightness = new Panel
            {
                Size = new Size(400, 200),
                Location = new Point(75, 200),
                BackColor = Color.FromArgb(40, 40, 40),
                Visible = false
            };

            Label lblTitle = new Label
            {
                Text = "Parlaklık Ayarları",
                Font = new Font("Montserrat", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };

            lblBrightness = new Label
            {
                Text = "Parlaklık Seviyesi",
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                Location = new Point(20, 60),
                AutoSize = true
            };

            lblBrightnessValue = new Label
            {
                Text = "50%",
                Font = new Font("Montserrat", 10),
                ForeColor = Color.White,
                Location = new Point(320, 62),
                AutoSize = true
            };

            brightnessSlider = new TrackBar
            {
                Location = new Point(20, 90),
                Size = new Size(360, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                TickFrequency = 10,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            Button btnClose = CreateRoundedButton("X", 360, 10, 30, 30, Color.FromArgb(255, 50, 50));
            btnClose.Click += ToggleBrightnessPanel;

            brightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;

            pnlBrightness.Controls.AddRange(new Control[] {
                    lblTitle,
                    lblBrightness,
                    lblBrightnessValue,
                    brightnessSlider,
                    btnClose
                });

            this.Controls.Add(pnlBrightness);
        }

        private void ToggleBrightnessPanel(object sender, EventArgs e)
        {
            isBrightnessPanelVisible = !isBrightnessPanelVisible;
            pnlBrightness.Visible = isBrightnessPanelVisible;
            if (isBrightnessPanelVisible)
            {
                pnlBrightness.BringToFront();
            }
        }

        private void BrightnessSlider_ValueChanged(object sender, EventArgs e)
        {
            lblBrightnessValue.Text = $"{brightnessSlider.Value}%";

            if (arduinoPort != null && arduinoPort.IsOpen)
            {
                try
                {
                    arduinoPort.WriteLine($"BR{brightnessSlider.Value}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Komut gönderme hatası: {ex.Message}");
                }
            }
            else
            {
                lblStatus.Text = "DURUM: Simülasyon modunda";
            }
        }

        private void SidePanel_Animation(object sender, EventArgs e)
        {
            const int STEP_SIZE = 25;
            const int TARGET_OPEN_POSITION = 0;
            const int TARGET_CLOSED_POSITION = -250;

            if (isSidePanelVisible)
            {
                int newPosition = pnlSide.Left + STEP_SIZE;
                if (newPosition >= TARGET_OPEN_POSITION)
                {
                    pnlSide.Left = TARGET_OPEN_POSITION;
                    sideAnimationTimer.Stop();
                    btnToggleSidePanel.Text = "←";
                }
                else
                {
                    pnlSide.Left = newPosition;
                }
            }
            else
            {
                int newPosition = pnlSide.Left - STEP_SIZE;
                if (newPosition <= TARGET_CLOSED_POSITION)
                {
                    pnlSide.Left = TARGET_CLOSED_POSITION;
                    sideAnimationTimer.Stop();
                    btnToggleSidePanel.Text = "→";
                }
                else
                {
                    pnlSide.Left = newPosition;
                }
            }

            pnlSide.BringToFront();
            btnToggleSidePanel.BringToFront();
        }

        private void ToggleSidePanel(object sender, EventArgs e)
        {
            isSidePanelVisible = !isSidePanelVisible;
            sideAnimationTimer.Start();
            pnlSide.BringToFront();
            btnToggleSidePanel.BringToFront();
        }

        private Panel CreatePanel(int x, int y, int width, int height)
        {
            return new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.FromArgb(60, 60, 60, 180),
                BorderStyle = BorderStyle.None
            };
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Montserrat", 13, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
        }

        private ComboBox CreateComboBox(int x, int y, int width)
        {
            return new ComboBox
            {
                Location = new Point(x, y),
                Width = width,
                Height = 40,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Montserrat", 12),
                ForeColor = Color.FromArgb(0, 122, 204),
                BackColor = Color.White
            };
        }

        private RadioButton CreateRadioButton(string text, int x, int y)
        {
            return new RadioButton
            {
                Text = text,
                Location = new Point(x, y),
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
        }

        private Button CreateRoundedButton(string text, int x, int y, int width, int height, Color backColor)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Montserrat", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Region = new Region(GetRoundedPath(new Rectangle(0, 0, width, height), 20))
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float curveSize = radius * 2F;

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, curveSize, curveSize, 180, 90);
            path.AddArc(rect.Right - curveSize, rect.Y, curveSize, curveSize, 270, 90);
            path.AddArc(rect.Right - curveSize, rect.Bottom - curveSize, curveSize, curveSize, 0, 90);
            path.AddArc(rect.X, rect.Bottom - curveSize, curveSize, curveSize, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void ShowProjectInfo(object sender, EventArgs e)
        {
            string projectInfo =
                "PROJE BİLGİLERİ\n\n" +
                "Proje Adı: Akıllı Aydınlatma Sistemi\n\n" +
                "Öğrenci: [BERAT TEZEL]\n" +
                "Okul: [ÖZLEM BURMA MTAL]\n" +
                "Öğretmen: [ZEYNEP AYGÜN]";

            MessageBox.Show(
                projectInfo,
                "Proje Bilgileri",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void ListAvailablePorts()
        {
            cmbPorts.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            cmbPorts.Items.AddRange(ports);

            if (ports.Length > 0)
                cmbPorts.SelectedIndex = 0;
            else
                lblStatus.Text = "DURUM: COM Port Bulunamadı!";
        }

        private void ConnectToArduino(object sender, EventArgs e)
        {
            if (cmbPorts.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir COM portu seçin!");
                return;
            }

            string selectedPort = null; // selectedPort'u try bloğu dışında tanımla
            try
            {
                selectedPort = cmbPorts.SelectedItem.ToString();

                if (arduinoPort != null && arduinoPort.IsOpen)
                {
                    arduinoPort.Close();
                }

                arduinoPort = new SerialPort(selectedPort, 9600)
                {
                    DtrEnable = true,
                    RtsEnable = true
                };
                arduinoPort.Open();

                lblStatus.Text = $"DURUM: {selectedPort} bağlı!"; // Artık selectedPort burada tanımlı
                MessageBox.Show($"{selectedPort} bağlantısı başarılı!");

                btnLightOn.Enabled = true;
                btnLightOff.Enabled = true;
            }
            catch (InvalidOperationException ex) // Özel hata türü
            {
                MessageBox.Show($"Geçersiz işlem hatası: {ex.Message}");
                lblStatus.Text = "DURUM: Geçersiz işlem hatası!";
            }
            catch (Exception ex) // Genel hata türü (en sona koyuldu)
            {
                MessageBox.Show($"Bağlantı hatası: {ex.Message}");
                lblStatus.Text = "DURUM: Bağlantı hatası!";
            }
        }

        private void SetupWeatherPanel()
        {
            pnlWeather = new Panel
            {
                Size = new Size(400, 250),
                Location = new Point(75, 200),
                BackColor = Color.FromArgb(40, 40, 40),
                Visible = false
            };

            Label lblTitle = new Label
            {
                Text = "Hava Durumu",
                Font = new Font("Montserrat", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };

            weatherIcon = new PictureBox
            {
                Size = new Size(64, 64),
                Location = new Point(20, 60),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            lblTemperature = new Label
            {
                Text = "Sıcaklık: --°C",
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                Location = new Point(100, 60),
                AutoSize = true
            };

            lblCondition = new Label
            {
                Text = "Durum: ---",
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                Location = new Point(100, 90),
                AutoSize = true
            };

            lblHumidity = new Label
            {
                Text = "Nem: --%",
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                Location = new Point(100, 120),
                AutoSize = true
            };

            btnRefreshWeather = CreateRoundedButton("Güncelle", 20, 160, 360, 35, Color.FromArgb(0, 153, 255));
            btnRefreshWeather.Click += async (s, e) => await UpdateWeatherInfo();

            Button btnClose = CreateRoundedButton("X", 360, 10, 30, 30, Color.FromArgb(255, 50, 50));
            btnClose.Click += (s, e) => ToggleWeatherPanel();

            pnlWeather.Controls.AddRange(new Control[] {
                    lblTitle,
                    weatherIcon,
                    lblTemperature,
                    lblCondition,
                    lblHumidity,
                    btnRefreshWeather,
                    btnClose
                });

            this.Controls.Add(pnlWeather);
        }

        private void ToggleWeatherPanel()
        {
            bool isVisible = !pnlWeather.Visible;
            pnlWeather.Visible = isVisible;

            if (isVisible)
            {
                pnlWeather.BringToFront();
                _ = UpdateWeatherInfo();
            }
        }

        private async Task UpdateWeatherInfo()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string url = $"{WEATHER_API_URL}?q={currentCity}&appid={WEATHER_API_KEY}&units=metric&lang=tr";
                    var response = await client.GetStringAsync(url);
                    var weatherData = JsonConvert.DeserializeObject<WeatherInfo>(response);

                    lblTemperature.Text = $"Sıcaklık: {weatherData.Main.Temp:F1}°C";
                    lblHumidity.Text = $"Nem: {weatherData.Main.Humidity}%";
                    lblCondition.Text = $"Durum: {weatherData.Weather[0].Description}";

                    // Gün doğumu ve batımı saatlerini güncelle
                    sunriseTime = DateTimeOffset.FromUnixTimeSeconds(weatherData.Sys.Sunrise).DateTime.ToLocalTime();
                    sunsetTime = DateTimeOffset.FromUnixTimeSeconds(weatherData.Sys.Sunset).DateTime.ToLocalTime();

                    // Etiketleri güncelle
                    lblSunrise.Text = $"Gün Doğumu: {sunriseTime:HH:mm}";
                    lblSunset.Text = $"Gün Batımı: {sunsetTime:HH:mm}";

                    // Zamanlanmış görevleri de güncelle
                    UpdateTimingSettings();

                    string iconCode = weatherData.Weather[0].Icon;
                    string iconUrl = $"http://openweathermap.org/img/w/{iconCode}.png";
                    using (var webClient = new HttpClient())
                    {
                        var imageBytes = await webClient.GetByteArrayAsync(iconUrl);
                        using (var ms = new MemoryStream(imageBytes))
                        {
                            weatherIcon.Image = Image.FromStream(ms);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hava durumu güncellenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void UpdateTimingSettings()
        {
            if (isTimingEnabled && rbAuto.Checked)
            {
                // Sadece zamanlama etkinse ve otomatik modda ise güncelle
                CheckTimingRules(null, EventArgs.Empty);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            try
            {
                if (arduinoPort != null && arduinoPort.IsOpen)
                {
                    SendCommand("OFF");
                    arduinoPort.Close();
                    arduinoPort.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uygulama kapatılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupEventHandlers()
        {
            btnLightOn.Click += (sender, e) =>
            {
                SendCommand("ON");
                // Manuel olarak açma yapıldığında zamanlama devre dışı kalsın
                if (isTimingEnabled)
                {
                    ToggleTimingEnabled(sender, e);
                }
            };

            btnLightOff.Click += (sender, e) =>
            {
                SendCommand("OFF");
                // Manuel olarak kapatma yapıldığında zamanlama devre dışı kalsın
                if (isTimingEnabled)
                {
                    ToggleTimingEnabled(sender, e);
                }
            };

            rbAuto.CheckedChanged += (sender, e) =>
            {
                SendCommand("AUTO");
                // Otomatiğe geçildiğinde zamanlama etkinse, zamanlamayı kontrol et
                if (isTimingEnabled && rbAuto.Checked)
                {
                    CheckTimingRules(sender, e);
                }
            };

            rbManuel.CheckedChanged += (sender, e) => SendCommand("MANUEL");
            btnConnect.Click += ConnectToArduino;
            btnRefreshPorts.Click += (sender, e) => ListAvailablePorts();
            btnProjectInfo.Click += ShowProjectInfo;
            btnToggleSidePanel.Click += ToggleSidePanel;
        }

        private void SendCommand(string command)
        {
            try
            {
                if (arduinoPort != null && arduinoPort.IsOpen)
                {
                    arduinoPort.WriteLine(command);

                    // Zamanlama etkinse durum mesajında belirt
                    if (isTimingEnabled && (command == "ON" || command == "OFF"))
                    {
                        lblStatus.Text = $"DURUM: Zamanlama - {command} komutu gönderildi";
                    }
                    else
                    {
                        lblStatus.Text = $"DURUM: {command} komutu gönderildi";
                    }
                }
                else
                {
                    MessageBox.Show("Arduino bağlantısı yok!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "DURUM: Bağlantı yok!";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Komut gönderme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "DURUM: Komut gönderme hatası!";
            }
        }
        private void ToggleTimingEnabled(object sender, EventArgs e)
        {
            isTimingEnabled = !isTimingEnabled; // Zamanlama durumunu tersine çevir

            // Buton metnini güncelle
            btnToggleTiming.Text = isTimingEnabled ? "Zamanlama: Açık" : "Zamanlama: Kapalı";

            // Buton rengini güncelle (opsiyonel)
            btnToggleTiming.BackColor = isTimingEnabled ? Color.FromArgb(0, 204, 102) : Color.FromArgb(0, 153, 255);

            // Durum etiketini güncelle
            lblStatus.Text = isTimingEnabled ? "DURUM: Zamanlama etkin" : "DURUM: Zamanlama devre dışı";

            // Eğer zamanlama etkinleştirilmişse ve otomatik moddaysak hemen kontrol et
            if (isTimingEnabled && rbAuto.Checked)
            {
                CheckTimingRules(sender, e);
            }
        }

        private void SetupTimingPanel()
        {
            pnlTiming = new Panel
            {
                Size = new Size(400, 350), // Yüksekliği biraz artıralım
                Location = new Point(75, 200),
                BackColor = Color.FromArgb(40, 40, 40),
                Visible = false
            };

            Label lblTitle = new Label
            {
                Text = "Zamanlama Ayarları",
                Font = new Font("Montserrat", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };

            // Gün doğumu ve batımı bilgileri
            lblSunrise = new Label
            {
                Text = "Gün Doğumu: --:--",
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                Location = new Point(20, 60),
                AutoSize = true
            };

            lblSunset = new Label
            {
                Text = "Gün Batımı: --:--",
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                Location = new Point(20, 90),
                AutoSize = true
            };

            // Gün Işığında Kapat butonu
            btnDaylightOff = CreateRoundedButton("Gün Işığında Kapat: Kapalı", 20, 130, 360, 35, Color.FromArgb(0, 153, 255));
            btnDaylightOff.Click += ToggleDaylightOff;

            // LED Zamanlama Ayarları
            Label lblTimingTitle = new Label
            {
                Text = "LED Zamanlama Ayarları",
                Font = new Font("Montserrat", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 180), // Konumu aşağı kaydıralım
                AutoSize = true
            };

            Label lblStartTime = new Label
            {
                Text = "Başlangıç Saati:",
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                Location = new Point(20, 220), // Konumu aşağı kaydıralım
                AutoSize = true
            };

            timePickerStart = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Location = new Point(180, 220), // Konumu aşağı kaydıralım
                Size = new Size(100, 25),
                Value = DateTime.Parse("07:00")
            };

            Label lblEndTime = new Label
            {
                Text = "Bitiş Saati:",
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                Location = new Point(20, 260), // Konumu aşağı kaydıralım
                AutoSize = true
            };

            timePickerEnd = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Location = new Point(180, 260), // Konumu aşağı kaydıralım
                Size = new Size(100, 25),
                Value = DateTime.Parse("18:00")
            };

            Button btnApply = CreateRoundedButton("Uygula", 20, 300, 360, 35, Color.FromArgb(0, 153, 255));
            btnApply.Click += ApplyTimingSettings;

            Button btnClose = CreateRoundedButton("X", 360, 10, 30, 30, Color.FromArgb(255, 50, 50));
            btnClose.Click += ToggleTimingPanel;
            btnToggleTiming = CreateRoundedButton("Zamanlama: Kapalı", 20, 300, 170, 35, Color.FromArgb(0, 153, 255));
            btnToggleTiming.Click += ToggleTimingEnabled;

            // Move the Apply button to be beside it
            btnApply.Location = new Point(210, 300);
            btnApply.Size = new Size(170, 35);

            // Add the new button to the panel controls collection
            pnlTiming.Controls.Add(btnToggleTiming);

            pnlTiming.Controls.AddRange(new Control[] {
        lblTitle,
        lblSunrise,
        lblSunset,
        btnDaylightOff,
        lblTimingTitle,
        lblStartTime,
        timePickerStart,
        lblEndTime,
        timePickerEnd,
        btnApply,
        btnClose
    });

            this.Controls.Add(pnlTiming);

            // Günlük kontrol için timer ayarla
            dailyTimer = new Timer
            {
                Interval = 60000 // Her dakika kontrol et
            };
            dailyTimer.Tick += CheckTimingRules;
            dailyTimer.Start();

            // İlk güneş bilgilerini al
            UpdateSunTimes();
        }
        private void SetupRgbPanel()
        {
            pnlRgb = new Panel
            {
                Size = new Size(400, 350),
                Location = new Point(75, 200),
                BackColor = Color.FromArgb(40, 40, 40),
                Visible = false
            };

            Label lblTitle = new Label
            {
                Text = "RGB LED Kontrolü",
                Font = new Font("Montserrat", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };

            // Renk önizleme paneli
            pnlColorPreview = new Panel
            {
                Size = new Size(360, 40),
                Location = new Point(20, 60),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Hızlı renk seçimi butonları
            btnColorRed = CreateRoundedButton("Kırmızı", 20, 110, 110, 35, Color.Red);
            btnColorGreen = CreateRoundedButton("Yeşil", 140, 110, 110, 35, Color.Green);
            btnColorBlue = CreateRoundedButton("Mavi", 260, 110, 110, 35, Color.Blue);

            btnColorWhite = CreateRoundedButton("Beyaz", 20, 155, 110, 35, Color.White);
            btnColorPurple = CreateRoundedButton("Mor", 140, 155, 110, 35, Color.Purple);
            btnColorYellow = CreateRoundedButton("Sarı", 260, 155, 110, 35, Color.Yellow);

            // Hızlı renk seçim butonlarının metin rengini ayarla
            btnColorWhite.ForeColor = Color.Black;
            btnColorYellow.ForeColor = Color.Black;

            // RGB değer ayarları
            lblRed = new Label
            {
                Text = "Kırmızı (R):",
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                Location = new Point(20, 200),
                AutoSize = true
            };

            trackRed = new TrackBar
            {
                Location = new Point(20, 225),
                Size = new Size(300, 45),
                Minimum = 0,
                Maximum = 255,
                Value = 255,
                TickFrequency = 25,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            lblRedValue = new Label
            {
                Text = "255",
                Font = new Font("Montserrat", 10),
                ForeColor = Color.White,
                Location = new Point(330, 225),
                AutoSize = true
            };

            lblGreen = new Label
            {
                Text = "Yeşil (G):",
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                Location = new Point(20, 250),
                AutoSize = true
            };

            trackGreen = new TrackBar
            {
                Location = new Point(20, 275),
                Size = new Size(300, 45),
                Minimum = 0,
                Maximum = 255,
                Value = 255,
                TickFrequency = 25,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            lblGreenValue = new Label
            {
                Text = "255",
                Font = new Font("Montserrat", 10),
                ForeColor = Color.White,
                Location = new Point(330, 275),
                AutoSize = true
            };

            lblBlue = new Label
            {
                Text = "Mavi (B):",
                Font = new Font("Montserrat", 12),
                ForeColor = Color.White,
                Location = new Point(20, 300),
                AutoSize = true
            };

            trackBlue = new TrackBar
            {
                Location = new Point(20, 325),
                Size = new Size(300, 45),
                Minimum = 0,
                Maximum = 255,
                Value = 255,
                TickFrequency = 25,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            lblBlueValue = new Label
            {
                Text = "255",
                Font = new Font("Montserrat", 10),
                ForeColor = Color.White,
                Location = new Point(330, 325),
                AutoSize = true
            };

            Button btnClose = CreateRoundedButton("X", 360, 10, 30, 30, Color.FromArgb(255, 50, 50));
            btnClose.Click += ToggleRgbPanel;

            // Event handler'ları ekle
            trackRed.ValueChanged += UpdateRgbColor;
            trackGreen.ValueChanged += UpdateRgbColor;
            trackBlue.ValueChanged += UpdateRgbColor;

            btnColorRed.Click += (s, e) => SetRgbColor(255, 0, 0);
            btnColorGreen.Click += (s, e) => SetRgbColor(0, 255, 0);
            btnColorBlue.Click += (s, e) => SetRgbColor(0, 0, 255);
            btnColorWhite.Click += (s, e) => SetRgbColor(255, 255, 255);
            btnColorPurple.Click += (s, e) => SetRgbColor(128, 0, 128);
            btnColorYellow.Click += (s, e) => SetRgbColor(255, 255, 0);

            // Kontrolleri panele ekle
            pnlRgb.Controls.AddRange(new Control[] {
        lblTitle,
        pnlColorPreview,
        btnColorRed, btnColorGreen, btnColorBlue,
        btnColorWhite, btnColorPurple, btnColorYellow,
        lblRed, trackRed, lblRedValue,
        lblGreen, trackGreen, lblGreenValue,
        lblBlue, trackBlue, lblBlueValue,
        btnClose
    });

            this.Controls.Add(pnlRgb);
        }

        private void ToggleRgbPanel(object sender, EventArgs e)
        {
            bool isVisible = !pnlRgb.Visible;
            pnlRgb.Visible = isVisible;
            if (isVisible)
            {
                pnlRgb.BringToFront();
            }
        }

        private void UpdateRgbColor(object sender, EventArgs e)
        {
            int red = trackRed.Value;
            int green = trackGreen.Value;
            int blue = trackBlue.Value;

            // Label değerlerini güncelle
            lblRedValue.Text = red.ToString();
            lblGreenValue.Text = green.ToString();
            lblBlueValue.Text = blue.ToString();

            // Renk önizleme panelini güncelle
            pnlColorPreview.BackColor = Color.FromArgb(red, green, blue);

            // Arduino'ya RGB değerlerini gönder
            SendRgbCommand(red, green, blue);
        }

        private void SetRgbColor(int red, int green, int blue)
        {
            // Kaydırıcıları ayarla (bu otomatik olarak UpdateRgbColor'ı tetikleyecek)
            trackRed.Value = red;
            trackGreen.Value = green;
            trackBlue.Value = blue;
        }

        private void SendRgbCommand(int red, int green, int blue)
        {
            if (arduinoPort != null && arduinoPort.IsOpen)
            {
                try
                {
                    string rgbCommand = $"RGB,{red},{green},{blue}";
                    arduinoPort.WriteLine(rgbCommand);
                    lblStatus.Text = $"DURUM: RGB değerleri gönderildi (R:{red}, G:{green}, B:{blue})";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"RGB komut gönderme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                lblStatus.Text = "DURUM: Arduino bağlı değil, RGB komutları gönderilemiyor";
            }
        }

        private void ToggleTimingPanel(object sender, EventArgs e)
        {
            isTimingPanelVisible = !isTimingPanelVisible;
            pnlTiming.Visible = isTimingPanelVisible;
            if (isTimingPanelVisible)
            {
                pnlTiming.BringToFront();
                UpdateSunTimes(); // Güneş doğuş ve batış bilgilerini güncelle
            }
        }

        private void UpdateSunTimes()
        {
            try
            {
                // Eğer API'den veriler alındıysa, onları göster
                if (sunriseTime != default(DateTime) && sunsetTime != default(DateTime))
                {
                    lblSunrise.Text = $"Gün Doğumu: {sunriseTime:HH:mm}";
                    lblSunset.Text = $"Gün Batımı: {sunsetTime:HH:mm}";
                }
                else
                {
                    // Henüz API'den veri alınmadıysa, hava durumu bilgisini güncelleyin
                    _ = UpdateWeatherInfo();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Güneş zamanlarını güncellerken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyTimingSettings(object sender, EventArgs e)
        {
            if (timePickerStart.Value >= timePickerEnd.Value)
            {
                MessageBox.Show("Başlangıç saati bitiş saatinden önce olmalıdır!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (arduinoPort != null && arduinoPort.IsOpen)
                {
                    // Zamanlama ayarlarını gönder
                    string timeCommand = $"TIME,{timePickerStart.Value.Hour},{timePickerStart.Value.Minute}," +
                                       $"{timePickerEnd.Value.Hour},{timePickerEnd.Value.Minute}";
                    arduinoPort.WriteLine(timeCommand);

                    // Mevcut saati senkronize et
                    string syncCommand = $"SYNC,{DateTime.Now.Hour},{DateTime.Now.Minute}";
                    arduinoPort.WriteLine(syncCommand);

                    MessageBox.Show("Zamanlama ayarları kaydedildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Zamanlama aktifse, hemen yeni ayarları uygula
                    if (isTimingEnabled)
                    {
                        CheckTimingRules(sender, e);
                    }
                }
                else
                {
                    MessageBox.Show("Arduino bağlantısı yok!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Zamanlama ayarları gönderilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);



                    
            }
        }

        private void ToggleDaylightOff(object sender, EventArgs e)
        {
            isDaylightOffEnabled = !isDaylightOffEnabled;
            btnDaylightOff.Text = isDaylightOffEnabled ? "Gün Işığında Kapat: Açık" : "Gün Işığında Kapat: Kapalı";

            // Renk değişimi ekleyelim (opsiyonel)
            btnDaylightOff.BackColor = isDaylightOffEnabled ? Color.FromArgb(0, 204, 102) : Color.FromArgb(0, 153, 255);

            // Yeni ayarı hemen uygula
            if (rbAuto.Checked && arduinoPort != null && arduinoPort.IsOpen)
            {
                CheckTimingRules(sender, e);
            }

            lblStatus.Text = isDaylightOffEnabled ?
                "DURUM: Gün ışığında LED kapatma aktif" :
                "DURUM: Gün ışığında LED kapatma devre dışı";
        }
        // Her dakika saat senkronizasyonu yapmak için timer ekleyin
        private Timer syncTimer;

        private void InitializeTimers()
        {
            syncTimer = new Timer
            {
                Interval = 60000 // Her dakika
            };
            syncTimer.Tick += SyncTime;
            syncTimer.Start();
        }

        private void SyncTime(object sender, EventArgs e)
        {
            if (arduinoPort != null && arduinoPort.IsOpen)
            {
                string syncCommand = $"SYNC,{DateTime.Now.Hour},{DateTime.Now.Minute}";
                arduinoPort.WriteLine(syncCommand);
            }
        }


        private void CheckTimingRules(object sender, EventArgs e)
        {
            // Zamanlama etkin değilse veya seri port bağlı değilse işlem yapma
            if (!isTimingEnabled || arduinoPort == null || !arduinoPort.IsOpen) return;


            DateTime now = DateTime.Now;
            DateTime currentTimeOfDay = DateTime.Parse(now.ToString("HH:mm"));

            // Otomatik modda değilse hiçbir şey yapma
            if (!rbAuto.Checked) return;

            // Gün doğumu ve batımı bilgileri mevcut mu kontrol et
            if (sunriseTime != default(DateTime) && sunsetTime != default(DateTime))
            {
                DateTime sunriseTimeOfDay = DateTime.Parse(sunriseTime.ToString("HH:mm"));
                DateTime sunsetTimeOfDay = DateTime.Parse(sunsetTime.ToString("HH:mm"));

                // Şu an gün ışığı saatleri içinde miyiz? (gün doğumu ile gün batımı arasında)
                bool isDaylight = currentTimeOfDay >= sunriseTimeOfDay && currentTimeOfDay <= sunsetTimeOfDay;

                if (isDaylight && isDaylightOffEnabled)
                {
                    // Gün ışığında ve özellik aktifse, LED'i kapat
                    SendCommand("OFF");
                    lblStatus.Text = "DURUM: Gün ışığı saatleri - LED kapalı";
                }
                else if (!isDaylight)
                {
                    // Gece saatlerindeyiz, LED'i aç
                    SendCommand("ON");
                    lblStatus.Text = "DURUM: Gece saatleri - LED açık";
                }
                else
                {
                    // Gün ışığındayız ama özellik aktif değil
                    if (isDaylightOffEnabled == false)
                    {
                        SendCommand("ON");
                        lblStatus.Text = "DURUM: Gün ışığı saatleri - LED açık";
                    }
                }
            }
            else
            {
                // Güneş bilgileri yoksa, manuel zamanlama ayarlarını kullan
                DateTime startTime = timePickerStart.Value;
                DateTime endTime = timePickerEnd.Value;

                if (currentTimeOfDay >= startTime && currentTimeOfDay <= endTime)
                {
                    SendCommand("OFF"); // Belirlenen saatler arasında LED'i kapat
                    lblStatus.Text = "DURUM: Belirlenmiş zaman aralığı - LED kapalı";
                }
                else
                {
                    SendCommand("ON"); // Belirlenen saatler dışında LED'i aç
                    lblStatus.Text = "DURUM: Belirlenmiş zaman aralığı dışı - LED açık";
                }
            }
        }
    }
}