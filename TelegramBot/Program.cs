using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.InputFiles;
namespace TelegramBot
{
    class Program
    {
        private static ITelegramBotClient botClient;
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task Main()
        {
            //строка підключення до тг боту
            botClient = new TelegramBotClient("");

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() 
            };

            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            // Send cancellation request to stop bot
            cts.Cancel();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message != null)
            {
                var message = update.Message;

                if (message.Text != null)
                {
                    Console.WriteLine($"Received a '{message.Text}' message in chat {message.Chat.Id}.");

                    if (message.Text.ToLower() == "/start")
                    {
                       
                        var replyKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                        new KeyboardButton[] { "Отримати факт про котів", "Отримати факт про собак", "Інша команда" }
                    })
                        {
                            ResizeKeyboard = true
                        };

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Виберіть опцію:",
                            replyMarkup: replyKeyboard,
                            cancellationToken: cancellationToken
                        );
                    }
                    else if (message.Text == "Отримати факт про котів")
                    {
                       
                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                        InlineKeyboardButton.WithCallbackData("Так, будь ласка", "catfact")
                    });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Ви хочете отримати факт про котів?",
                            replyMarkup: inlineKeyboard,
                            cancellationToken: cancellationToken
                        );
                    }
                    else if (message.Text == "Отримати факт про собак")
                    {
                        
                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                        InlineKeyboardButton.WithCallbackData("Так, будь ласка", "dogfact")
                    });

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Ви хочете отримати факт про собак?",
                            replyMarkup: inlineKeyboard,
                            cancellationToken: cancellationToken
                        );
                    }
                    else if (message.Text == "Інша команда")
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Ви вибрали іншу команду.",
                            cancellationToken: cancellationToken
                        );
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"Ви сказали: {message.Text}",
                            cancellationToken: cancellationToken
                        );
                    }
                }
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                var callbackQuery = update.CallbackQuery;

                if (callbackQuery.Data == "catfact")
                {
                    (string catFact, InputOnlineFile catImageUrl) = await GetCatFactWithImageAsync();

                    await botClient.SendPhotoAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        photo: catImageUrl,
                        caption: catFact,
                        cancellationToken: cancellationToken
                    );

                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: "Ви отримали факт про котів з фото!",
                        cancellationToken: cancellationToken
                    );
                }
                else if(callbackQuery.Data == "dogfact")
                {
                    (string catFact, InputOnlineFile catImageUrl) = await GetDogFactWithImageAsync();

                    await botClient.SendPhotoAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        photo: catImageUrl,
                        caption: catFact,
                        cancellationToken: cancellationToken
                    );

                    await botClient.AnswerCallbackQueryAsync(
                        callbackQueryId: callbackQuery.Id,
                        text: "Ви отримали факт про котів з фото!",
                        cancellationToken: cancellationToken
                    );
                }



            }
        }

        private static async Task<(string, InputOnlineFile)> GetCatFactWithImageAsync()
        {
            var factResponse = await httpClient.GetAsync("https://cat-fact.herokuapp.com/facts/random");
            factResponse.EnsureSuccessStatusCode();

            var factContent = await factResponse.Content.ReadAsStringAsync();
            var fact = JObject.Parse(factContent)["text"].ToString();

            
            string catImageUrl = $"https://cataas.com/cat?position=center&timestamp={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            InputOnlineFile photo = new InputOnlineFile(catImageUrl);

            return (fact, photo);
        }

        private static async Task<(string, InputOnlineFile)> GetDogFactWithImageAsync()
        {
            var factResponse = await httpClient.GetAsync("https://dogapi.dog/api/v2/facts");
            factResponse.EnsureSuccessStatusCode();

            var factContent = await factResponse.Content.ReadAsStringAsync();
            var fact = JObject.Parse(factContent)["data"][0]["attributes"]["body"].ToString();



            var imageUrlResponse = await httpClient.GetAsync("https://dog.ceo/api/breeds/image/random");
            imageUrlResponse.EnsureSuccessStatusCode();

            var imageUrlContent = await imageUrlResponse.Content.ReadAsStringAsync();
            var imageUrl = JObject.Parse(imageUrlContent)["message"].ToString();


                InputOnlineFile photo = new InputOnlineFile(imageUrl);
                return (fact, photo);
         
            

         
        }


        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}