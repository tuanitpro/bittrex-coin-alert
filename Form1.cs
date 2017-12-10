﻿using AppExchangeCoinAlert.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace AppExchangeCoinAlert
{
    public partial class Form1 : Form
    {
        public const string Version = "v1.1";
        public const string BaseUrl = "https://bittrex.com/api/" + Version + "/";
        int CountItem = 0;
        List<MarketSummary> ListMarketSummaries = null;
        List<MarketSummary> ListAllMarketSummaries = null;
        Setting CurrentSetting = null;
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
            this.Height = 85;
            btnOption.FlatAppearance.BorderSize = 0;
            btnOption.TabStop = false;
            btnOption.FlatStyle =
                btnOption.FlatStyle = FlatStyle.Flat;
            GetSetting();
            GetData();

            DisplayText();
            System.Timers.Timer t = new System.Timers.Timer(5000);
            t.AutoReset = true;
            t.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            t.Start();
        }
        private async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {          
            GetData();          
        }
        string GetSourceCurrencyFromMarketName(string marketName) => marketName.Split('-').First();
         string GetTargetCurrencyFromMarketName(string marketName) => marketName.Split('-').Last();
        private async  void CallAlarm()
        {
            if (CurrentSetting.AlarmItems.Any())
            {
                var dataSumaries = ListMarketSummaries;
                if (dataSumaries!=null && dataSumaries.Any())
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

        void GetSetting()
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
        // Random random = new Random();
        async void GetData()
        {
            try
            {
                List<string> listMarkets = CurrentSetting.AlarmItems.Select(x => x.MarketName).ToList();
                if (listMarkets == null || listMarkets.Count == 0)
                    listMarkets = new List<string> {
                "USDT-ETC", "USDT-NEO", "USDT-BTC", "USDT-BCC"
            };
                ListMarketSummaries = new List<MarketSummary>();
                var response = await GetMarketSummaries();
                if (response.Success)
                {
                    ListAllMarketSummaries = response.Result.ToList();

                    ListMarketSummaries = response.Result.Where(x => listMarkets.Contains(x.MarketName)).ToList();
                    DisplayText();
                    CallAlarm();
                }
            }catch(Exception ex) { }
        }
        void DisplayText()
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
            cbbListMarket.DataSource = ListAllMarketSummaries;
            cbbListMarket.ValueMember = "MarketName";
            cbbListMarket.DisplayMember = "MarketName";
        }
        async Task<ResponseWrapper<IEnumerable<MarketSummary>>> GetMarketSummaries()
        {
            Bittrex bittrex = new Bittrex();
            var uri = BaseUrl + "public/getmarketsummaries";
            var marketSummariesResponse = await bittrex.Request<IEnumerable<MarketSummary>>(HttpMethod.Get, uri);
            return marketSummariesResponse;
        }
        System.Media.SoundPlayer player = null;
        void PlaySound()
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
            this.Height = 500;
            txtAbove.Text = txtBelow.Text = txtCurrentPrice.Text = "";
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
            this.Height = 85;
            txtAbove.Text = txtBelow.Text = txtCurrentPrice.Text = "";
        }

        private void cbbListMarket_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbbListMarket.SelectedIndex > -1)
            {
                var value = cbbListMarket.SelectedValue;
                var item = ListAllMarketSummaries.FirstOrDefault(x => x.MarketName == value.ToString());
                if(item != null)
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
                if(row != null)
                {
                    string market = row.ToString();
                    if (CurrentSetting.AlarmItems.Any())
                    {
                        var item = CurrentSetting.AlarmItems.FirstOrDefault(x => x.MarketName == market);
                        if(item != null)
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
    }
}
