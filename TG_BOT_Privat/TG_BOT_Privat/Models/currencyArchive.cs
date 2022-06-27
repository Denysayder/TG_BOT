using System;
namespace TG_BOT_Privat
{

    public class currencyArchive
    {
        public List<exchangeRate> exchangeRate { get; set; }
    }

    public class exchangeRate
    {
        public string baseCurrency { get; set; }
        public string currency { get; set; }
        public string saleRate { get; set; }
        public string purchaseRate { get; set; }
    }

}
