﻿using System;
namespace TG_BOT_Privat.Models
{
    public class ExchangeRateModel
    {
        public string ID { get; set; }
        public string Time { get; set; }
        public string ccy { get; set; }
        public string base_ccy { get; set; }
        public string buy { get; set; }
        public string sale { get; set; }
    }
}

