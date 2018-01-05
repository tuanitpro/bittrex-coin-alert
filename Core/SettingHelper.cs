using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AppExchangeCoinAlert.Core
{
    public class AlarmItem
    {
        public string MarketName { get; set; }
        public decimal Above { get; set; }
        public decimal Below { get; set; }
    }

    public class Setting
    {
        public string FilePath { get; set; }
        public List<AlarmItem> AlarmItems { get; set; }
    }

    public class SettingHelper
    {
        public Setting Setting { get; set; }

        public SettingHelper()
        {
            Setting = new Setting
            {
                FilePath = $@"{AppDomain.CurrentDomain.BaseDirectory}data\alarm.wav",
                AlarmItems = new List<AlarmItem>()
            };
        }

        public Setting GetSetting()
        {
            var filePath = $@"{AppDomain.CurrentDomain.BaseDirectory}data\data.ini";
            string text = System.IO.File.ReadAllText(filePath);
            if (!string.IsNullOrEmpty(text))
            {
                Setting = JsonConvert.DeserializeObject<Setting>(text);
            }
            return Setting;
        }

        public static void Save(Setting setting)
        {
            string text = JsonConvert.SerializeObject(setting);
            var filePath = $@"{AppDomain.CurrentDomain.BaseDirectory}data\data.ini";
            System.IO.File.WriteAllText(filePath, text);
        }
    }
}