using System;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Args;
using Newtonsoft.Json;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using TG_BOT_Privat.Models;

namespace TG_BOT_Privat
{
    public class Easy_Privat_Bot
    {
        private string apiAddress = "https://privatapi.herokuapp.com";
        //private string apiAddress = "https://localhost:7207";
        TelegramBotClient botClient = new TelegramBotClient("5427717344:AAESHYHqG70UV9t11M5jZNut5gSmjmTZqow");
        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
        public string City = "";
        public string DateTime_ = "";

        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await botClient.GetMeAsync();
            Console.WriteLine($"Бот {botMe.Username} почав працювати!");
            Console.ReadKey();

        }

        private Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Помилка в телеграм бот АПІ: \n{apiRequestException.ErrorCode}" +
                $"\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message.Text != null)
            {
                await HandlerMessageAsync(botClient, update.Message);
            }
            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandlerCallBackQuerry(botClient, update.CallbackQuery);
            }
        }

        public async Task<List<ExchangeRateModel>> GetCurrencyfromAPIAsync(HttpClient _client)
        {
            //var _client = new HttpClient();
            _client.BaseAddress = new Uri(apiAddress);
            var result = await _client.GetAsync("/ExchangeRate");
            result.EnsureSuccessStatusCode();
            var content = result.Content.ReadAsStringAsync().Result;

            var rates = JsonConvert.DeserializeObject<List<ExchangeRateModel>>(content);
            return rates;
        }

        public async Task<currencyArchive> GetArchive(string Date)
        {
            var _client = new HttpClient();
            _client.BaseAddress = new Uri(apiAddress);
            var result = await _client.GetAsync($"/CurrencyArchive?date={Date}");
            result.EnsureSuccessStatusCode();
            var content = result.Content.ReadAsStringAsync().Result;
            var Archive = JsonConvert.DeserializeObject<currencyArchive>(content);
            return Archive;
        }

        private async Task HandlerCallBackQuerry(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var _client = new HttpClient();
            var response = await GetCurrencyfromAPIAsync(_client);
            List<ExchangeRateModel> result = response;
            if (callbackQuery.Data.StartsWith("Today"))
            {
                foreach (var n in result)
                    await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"{n.ccy}: \nкупівля {n.buy}" +
                            $"\nпродаж {n.sale}\n");
                InlineKeyboardMarkup KeyboardMarkup = new
                (
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Так", $"yes"),
                            InlineKeyboardButton.WithCallbackData("Скасувати", $"no"),
                        }
                    }
                );
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Додати цей курс в архів? ", replyMarkup: KeyboardMarkup);
                return;
            }

            if (callbackQuery.Data.StartsWith("yes"))
            {
                var dataresult = result
                .Select(x => new ExchangeRateModel()
                {
                    ID = callbackQuery.Message.Chat.Id.ToString(),
                    Time = DateTime.UtcNow.ToString(),
                    base_ccy = x.base_ccy,
                    buy = x.buy,
                    ccy = x.ccy,
                    sale = x.sale
                })
                .ToList();
                for (int i = 1; i < 3; i++)
                    dataresult[i].Time = $"{dataresult[i].Time}:0{i}";
                foreach (ExchangeRateModel n in dataresult)
                {
                    var json = JsonConvert.SerializeObject(n);
                    var data = new StringContent(json, Encoding.UTF8, "application/json");
                    var post = await _client.PostAsync("OwnArchive", data);
                    var postcontent = post.Content.ReadAsStringAsync().Result;
                }
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Курс додано! ");
                return;
            }

            if (callbackQuery.Data.StartsWith("no"))
            {
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Скасовано!");
            }

            if (callbackQuery.Data.StartsWith("Date"))
            {
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Введіть дату у форматі " +
                    "дд.мм.рррр", replyMarkup: new ForceReplyMarkup { Selective = true });
                return;
            }

            if (callbackQuery.Data.StartsWith("change"))
            {
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Введіть дату та час " +
                    "запису у форматі дд.мм.рррр чч:хх:сс", replyMarkup: new ForceReplyMarkup { Selective = true });
                return;
            }
            return;
        }




        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == "/start" || message.Text == "/menu")
            {
                ReplyKeyboardMarkup replyKeyboard = new
                    (
                        new[]
                        {
                            new KeyboardButton[] { "Курс валют", "Власний архів" },
                            new KeyboardButton[] { "Знайти відділення", "Видалити власний архів" },
                        }
                    )
                {
                    ResizeKeyboard = true
                };
                await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть пункт меню:", replyMarkup: replyKeyboard);
                return;
            }
            else


            if (message.Text == "Курс валют")
            {

                InlineKeyboardMarkup KeyboardMarkup = new
                (
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("На сьогодні", $"Today"),
                            InlineKeyboardButton.WithCallbackData("За датою", $"Date"),
                        }
                    }
                );
                await botClient.SendTextMessageAsync(message.Chat.Id, "Дізнатися курс:", replyMarkup: KeyboardMarkup);
                return;
            }
            else


            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введіть дату у форматі дд.мм.рррр"))
            {

                string Date = message.Text;
                if (Regex.IsMatch(Date, @"[0-9]{2}.[0-9]{2}.[0-9]{4}"))
                {
                    var data = await GetArchive(Date);
                    List<exchangeRate> exchangeRates = data.exchangeRate;
                    try
                    {
                        foreach (var currency in exchangeRates)
                        {
                            if (currency.saleRate != null && currency.purchaseRate != null)
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"{currency.baseCurrency}\n{currency.currency}" +
                                    $"\nКупівля: {currency.saleRate}\nПродаж: {currency.purchaseRate}");
                        }
                    }
                    catch (HttpRequestException)
                    {
                        return;
                    }

                    await botClient.SendTextMessageAsync(message.Chat.Id, "Відкрити /menu");
                    return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Неправильний формат дати\nВідкрити /menu");
                    return;
                }
            }
            else


            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введіть дату та час запису у форматі дд.мм.рррр чч:хх:сс"))
            {
                DateTime_ = message.Text;
                if (Regex.IsMatch(DateTime_, @"[0-9]{2}.[0-9]{2}.[0-9]{4} [0-9]{2}:[0-9]{2}:[0-9]{2}") || Regex.IsMatch(DateTime_, @"[0-9]{2}.[0-9]{2}.[0-9]{4} [0-9]{2}:[0-9]{2}:[0-9]{2}:[0-9]{2}"))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть через пробіл купівлю та продаж", replyMarkup: new ForceReplyMarkup { Selective = true });
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Неправильно введено дату чи час!");
                }
                return;
            }
            else


            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введіть через пробіл купівлю та продаж"))
            {
                try
                {
                    string[] BuySale = message.Text.Split(' ');
                    var _client = new HttpClient();
                    ExchangeRateModel exchange = new ExchangeRateModel();
                    exchange.ID = message.Chat.Id.ToString();
                    exchange.Time = DateTime_;
                    try
                    {
                        exchange.buy = BuySale[0];
                        exchange.sale = BuySale[1];
                    }
                    catch (Exception e)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Сталася помилка");
                        return;
                    }
                    var json = JsonConvert.SerializeObject(exchange);
                    var data = new StringContent(json, Encoding.UTF8, "application/json");
                    var text = message.Text;
                    _client.BaseAddress = new Uri(apiAddress);
                    var put = await _client.PutAsync($"/OwnArchive?ID={exchange.ID}&Time={exchange.Time}&NewBuy={exchange.buy}&NewSale={exchange.sale}", data);

                    var putcontent = put.Content.ReadAsStringAsync().Result;
                    put.EnsureSuccessStatusCode();
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Змінено! /menu");
                }
                catch (HttpRequestException)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Сталася помилка");
                    return;
                }
                return;

            }
            else


            if (message.Text == "Власний архів")
            {
                var _client = new HttpClient();
                _client.BaseAddress = new Uri(apiAddress);
                var result_ = await _client.GetAsync("/OwnArchive");
                try
                {
                    result_.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Архів порожній");
                    return;
                }
                var content = result_.Content.ReadAsStringAsync().Result;
                var rates = JsonConvert.DeserializeObject<List<ExchangeRateModel>>(content);
                if (rates[0].sale != null)
                {
                    foreach (var n in rates)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"{n.Time}\n{n.ccy}: \nкупівля {n.buy}" +
                            $"\nпродаж {n.sale}\n");
                    }
                }
                InlineKeyboardMarkup KeyboardMarkup = new
                 (
                     new[]
                     {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Так", $"change"),
                            InlineKeyboardButton.WithCallbackData("Скасувати", $"no"),
                        }
                     }
                 );
                await botClient.SendTextMessageAsync(message.Chat.Id, "Змінити запис?", replyMarkup: KeyboardMarkup);
                return;
            }


            else
            if (message.Text == "Видалити власний архів")
            {
                var _client = new HttpClient();
                _client.BaseAddress = new Uri(apiAddress);
                var result_ = await _client.DeleteAsync("/OwnArchive");
                try
                {
                    result_.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Архів порожній");
                    return;
                }

            }


            if (message.Text == "Знайти відділення")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву міста (російською)", replyMarkup: new ForceReplyMarkup { Selective = true });

                return;
            }
            else


            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введіть назву міста (російською)"))
            {
                City = message.Text;
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву вулиці (російською)", replyMarkup: new ForceReplyMarkup { Selective = true });
            }


            if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Введіть назву вулиці (російською)"))
            {
                var _client = new HttpClient();
                _client.BaseAddress = new Uri(apiAddress);
                var result = await _client.GetAsync($"/Department?city={City}&address={message.Text}");
                result.EnsureSuccessStatusCode();
                try
                {
                    result.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Відділення не знайдено");
                    return;
                }
                var content = result.Content.ReadAsStringAsync().Result;
                var departments = JsonConvert.DeserializeObject<List<Department>>(content);
                if (departments.Count != 0)
                {
                    foreach (var n in departments)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Відділення: {n.name}\nМісто: {n.city} \nТелефон: {n.phone}" +
                            $"\nАдреса: {n.address}\n");
                    }
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"/menu");
                    return;
                }
                else
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Відділення не знайдено /menu");
            }

        }
    }
}