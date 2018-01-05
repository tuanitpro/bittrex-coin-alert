using AppExchangeCoinAlert.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace AppExchangeCoinAlert
{
    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        public const string Version = "v1.1";
        public const string BaseUrl = "https://bittrex.com/api/" + Version + "/";
        private int CountItem = 0;
        private List<MarketSummary> ListMarketSummaries = null;
        private List<MarketSummary> ListAllMarketSummaries = null;
        private Setting CurrentSetting = null;

        public Form1()
        {
            InitializeComponent();
            BuildDataGrid_Alert();
        }

        private void BuildDataGrid_Alert()
        {
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AllowUserToResizeRows = false;

            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView1.ColumnHeadersHeight = 30;

            DataGridViewColumn dataGridViewColumn = new DataGridViewTextBoxColumn
            {
                Name = "Martket",
                DataPropertyName = "MarketName",
                ReadOnly = true,
            };
            dataGridView1.Columns.Add(dataGridViewColumn);

            DataGridViewColumn dataGridViewColumnAbove = new DataGridViewTextBoxColumn
            {
                Name = "Cao hơn",
                DataPropertyName = "Above",
                ReadOnly = true
            };
            dataGridView1.Columns.Add(dataGridViewColumnAbove);
            DataGridViewColumn dataGridViewColumnBelow = new DataGridViewTextBoxColumn
            {
                Name = "Thấp hơn",
                DataPropertyName = "Below",
                ReadOnly = true
            };
            dataGridView1.Columns.Add(dataGridViewColumnBelow);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            this.Height = 40;
            this.Top = Screen.PrimaryScreen.WorkingArea.Height - 40;
            btnOption.FlatAppearance.BorderSize = 0;
            btnOption.TabStop = false;
            btnOption.FlatStyle =
                btnOption.FlatStyle = FlatStyle.Flat;
            GetSetting();
            GetData();

            System.Timers.Timer t = new System.Timers.Timer(5000);
            t.AutoReset = true;
            t.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            t.Start();
        }

        private async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            GetData();
        }

        private string GetSourceCurrencyFromMarketName(string marketName) => marketName.Split('-').First();

        private string GetTargetCurrencyFromMarketName(string marketName) => marketName.Split('-').Last();

        private async void CallAlarm()
        {
            if (CurrentSetting.AlarmItems.Any())
            {
                var dataSumaries = ListMarketSummaries;
                if (dataSumaries != null && dataSumaries.Any())
                {
                    var listMarket = CurrentSetting.AlarmItems.Select(x => x.MarketName).ToArray();
                    var listDataToCheck = dataSumaries.Where(x => listMarket.Contains(x.MarketName)).ToList();

                    foreach (var item in listDataToCheck)
                    {
                        var itemNeedCheck = CurrentSetting.AlarmItems.FirstOrDefault(x => x.MarketName == item.MarketName);
                        var coinName = GetTargetCurrencyFromMarketName(item.MarketName);
                        string text = "";
                        var last = item.Last;

                        if (itemNeedCheck.Above < last)
                        {
                            text = $"{coinName} đạt giá cao hơn {itemNeedCheck.Above} trên bittrex";
                        }
                        else if (itemNeedCheck.Below > last)
                        {
                            text = $"{coinName} đạt giá thấp hơn {itemNeedCheck.Below} on bittrex";
                        }
                        if (!string.IsNullOrEmpty(text))
                        {
                            PlaySound();

                            if (this.InvokeRequired)
                            {
                                lblText.Invoke((Action)delegate ()
                                {
                                    lblText.Text = text;
                                });
                            }
                            else
                            {
                                lblText.Text = text;
                            }
                        }
                    }
                }
            }
        }

        private void GetSetting()
        {
            SettingHelper settingHelper = new SettingHelper();
            CurrentSetting = settingHelper.GetSetting();
            if (CurrentSetting != null)
            {
                if (CurrentSetting.AlarmItems != null)
                {
                    dataGridView1.DataSource = CurrentSetting.AlarmItems;
                    foreach (DataGridViewRow item in dataGridView1.Rows)
                    {
                        item.DefaultCellStyle.ForeColor = Color.Red;
                    }
                }
                if (!string.IsNullOrEmpty(CurrentSetting.FilePath))
                {
                    cbbAudio.Text = CurrentSetting.FilePath;
                    cbbAudio.SelectedIndex = 0;
                }
            }
        }

        private async void lblText_Click(object sender, EventArgs e)
        {
            DisplayText();
            if (player != null) player.Stop();
        }
        CoinmarketcapService coinMarketCapService = new CoinmarketcapService();
        // Random random = new Random();
        private async void GetData()
        {
            try
            {
                List<string> listMarkets = CurrentSetting.AlarmItems.Select(x => x.MarketName).ToList();
                if (listMarkets == null || listMarkets.Count == 0)
                    listMarkets = new List<string> {
              "USDT-BTC",  
            };
                ListMarketSummaries = new List<MarketSummary>();
                

                //  string msg = $"{item.MarketName}: Last: {item.Last} H: {item.High} L: {item.Low} V:{item.Volume}";
                var response = await GetMarketSummaries();
                if (response.Success)
                {
                    ListAllMarketSummaries = response.Result.ToList();

                    ListMarketSummaries = response.Result.Where(x => listMarkets.Contains(x.MarketName)).ToList();

                    var coinMarketCapData = await coinMarketCapService.GetCoinmarketcapGlobal();
                    ListMarketSummaries.Insert(0, new MarketSummary
                    {
                        MarketName = "Coinmarketcap",
                        Last = coinMarketCapData.TotalMarketCapUsd,
                        High = coinMarketCapData.Total24hVolumeUsd,
                        Volume = coinMarketCapData.BitcoinPercentageOfMarketCap,
                        Low = 0
                    });

                    DisplayText();
                    CallAlarm();
                }
               
               
            }
            catch (Exception ex) { }
        }

        private void DisplayText()
        {
            var data = ListMarketSummaries;
            if (data != null && data.Any())
            {
                if (CountItem >= data.Count) CountItem = 0;
                var index = CountItem; //random.Next(data.Count);
                var item = data[index];
                string msg = $"{item.MarketName}: Last: {item.Last} H: {item.High} L: {item.Low} V:{item.Volume}";
                if (this.InvokeRequired)
                {
                    lblText.Invoke((Action)delegate ()
                    {
                        lblText.Text = msg;
                    });
                }
                else
                {
                    lblText.Text = msg;
                }
                CountItem++;
            }
        }

        private async Task<ResponseWrapper<IEnumerable<MarketSummary>>> GetMarketSummaries()
        {
            Bittrex bittrex = new Bittrex();
            var uri = BaseUrl + "public/getmarketsummaries";
            var marketSummariesResponse = await bittrex.Request<IEnumerable<MarketSummary>>(HttpMethod.Get, uri);
            return marketSummariesResponse;
        }

        private System.Media.SoundPlayer player = null;

        private void PlaySound()
        {
            try
            {
                string file = CurrentSetting.FilePath;
                if (string.IsNullOrEmpty(file))
                    file = "alarm0";
                string filePath = $@"{AppDomain.CurrentDomain.BaseDirectory}data\{file}.wav";
                player = new System.Media.SoundPlayer(filePath);
                player.PlayLooping();
            }
            catch (Exception ex) { }
        }

        private void btnOption_Click(object sender, EventArgs e)
        {
            groupBox1.Visible = true;
            this.Height = 500;
            this.Top = Screen.PrimaryScreen.WorkingArea.Height - 500;
            txtAbove.Text = txtBelow.Text = txtCurrentPrice.Text = "";

            cbbListMarket.DataSource = ListAllMarketSummaries;
            cbbListMarket.ValueMember = "MarketName";
            cbbListMarket.DisplayMember = "MarketName";
        }

        private void btnSaveChange_Click(object sender, EventArgs e)
        {
            try
            {
                var fileAudioPath = cbbAudio.Text;
                GetSetting();

                var listAlarm = CurrentSetting.AlarmItems;

                var getAbove = txtAbove.Text;
                var getBelow = txtBelow.Text;
                var marketName = btnSaveChange.Tag;
                if (!string.IsNullOrEmpty(getAbove) || !string.IsNullOrEmpty(getBelow))
                {
                    var above = ColorLife.Core.Helper.ConvertType.ToDecimal(getAbove);
                    if (above < 0) above = 0;
                    var below = ColorLife.Core.Helper.ConvertType.ToDecimal(getBelow);
                    if (below < 0) below = 0;
                    if (marketName != null)
                    {
                        var exists = listAlarm.FirstOrDefault(x => x.MarketName == marketName.ToString());
                        if (exists == null)
                        {
                            listAlarm.Add(new AlarmItem
                            {
                                MarketName = marketName.ToString(),
                                Above = above,
                                Below = below
                            });
                        }
                        else
                        {
                            exists.Above = above;
                            exists.Below = below;
                        }
                    }
                    else
                    {
                        marketName = cbbListMarket.SelectedValue;
                        var exists = listAlarm.FirstOrDefault(x => x.MarketName == marketName.ToString());
                        if (exists == null)
                        {
                            listAlarm.Add(new AlarmItem
                            {
                                MarketName = marketName.ToString(),
                                Above = above,
                                Below = below
                            });
                        }
                        else
                        {
                            exists.Above = above;
                            exists.Below = below;
                        }
                    }
                }

                CurrentSetting.FilePath = fileAudioPath;
                CurrentSetting.AlarmItems = listAlarm;
                SettingHelper.Save(CurrentSetting);
                GetSetting();

                txtAbove.Text = txtBelow.Text = txtCurrentPrice.Text = "";
                btnSaveChange.Tag = null;
            }
            catch (Exception ex)
            {
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Height = 40;
            this.Top = Screen.PrimaryScreen.WorkingArea.Height - 40;
            groupBox1.Visible = true;
            txtAbove.Text = txtBelow.Text = txtCurrentPrice.Text = "";
            btnSaveChange.Tag = null;
        }

        private void cbbListMarket_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbbListMarket.SelectedIndex > -1)
            {
                var value = cbbListMarket.SelectedValue;
                var item = ListAllMarketSummaries.FirstOrDefault(x => x.MarketName == value.ToString());
                if (item != null)
                {
                    txtCurrentPrice.Text = item.Last.ToString();
                }
            }
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                var row = dataGridView1.SelectedRows[0].Cells[0].Value;
                if (row != null)
                {
                    string market = row.ToString();
                    if (CurrentSetting.AlarmItems.Any())
                    {
                        var item = CurrentSetting.AlarmItems.FirstOrDefault(x => x.MarketName == market);
                        if (item != null)
                        {
                            CurrentSetting.AlarmItems.Remove(item);
                            SettingHelper.Save(CurrentSetting);
                            GetSetting();
                        }
                    }
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var row = dataGridView1.SelectedRows[0].Cells[0].Value;
            if (row != null)
            {
                string market = row.ToString();
                if (CurrentSetting.AlarmItems.Any())
                {
                    var item = CurrentSetting.AlarmItems.FirstOrDefault(x => x.MarketName == market);
                    if (item != null)
                    {
                        txtAbove.Text = item.Above.ToString();
                        txtBelow.Text = item.Below.ToString();
                        btnSaveChange.Tag = item.MarketName;
                        cbbListMarket.SelectedValue = item.MarketName;
                    }
                }
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}