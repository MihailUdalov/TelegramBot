using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Weather
{

    class Program
    {
        private static readonly string Connection_String = @"Data Source=DESKTOP-RO5V6U2\SQLEXPRESS;Initial Catalog=TelegramBot;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        private static TelegramBotClient client;

        static void Main(string[] args)
        {

            client = new TelegramBotClient("2074923820:AAEjQVPw9-11f29mGH_LjCtEj5UmEV_62cc");
            client.OnMessage += BotOnMessageReceived;
            client.OnMessageEdited += BotOnMessageReceived;
            TimerCallback tm = new TimerCallback(DisplayingReminder);
            Timer timer = new Timer(tm, client, 0, 60000);
            client.StartReceiving();
            Console.ReadLine();
            client.StopReceiving();


        }

        private static async void DisplayingReminder(object client)
        {
            TelegramBotClient telegramBotClient = (TelegramBotClient)client;
            List<User> users = Get();
            foreach (User user in users)
            {
                int hour = 0;
                int minute = 0;
                int position = 0;
                try
                {
                    position = user.Time.IndexOf(":");
                    hour = Int32.Parse(user.Time.Substring(0, position));
                    minute = Int32.Parse(user.Time.Substring(position + 1));
                }
                catch (Exception exp)
                { }
                if (hour == DateTime.Now.Hour && minute == DateTime.Now.Minute)
                {
                    await telegramBotClient.SendTextMessageAsync(user.IDUser, CallWeather(user.City));
                }
            }
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            string command = " ";
            string city = " ";
            string time = " ";
            int position = 0;
            if (message?.Type == MessageType.Text)
            {
                try
                {
                    position = message.Text.IndexOf(" ");
                    command = message.Text.Substring(0, position);
                }
                catch
                {
                    command = message.Text;
                }
                switch (command)
                {
                    case "/start":
                        {

                            await client.SendTextMessageAsync(message.Chat.Id, Start());
                            break;
                        }
                    case "/weather":
                        {
                            try
                            {
                                position = message.Text.IndexOf(" ");
                                city = message.Text.Substring(position + 1);
                                await client.SendTextMessageAsync(message.Chat.Id, CallWeather(city));
                            }
                            catch
                            {
                                await client.SendTextMessageAsync(message.Chat.Id, Error());
                            }

                            break;
                        }
                    case "/help":
                        {
                            await client.SendTextMessageAsync(message.Chat.Id, Help());
                            break;
                        }
                    case "/reminder":
                        {

                            try
                            {
                                User user = new User();
                                position = message.Text.IndexOf(" ");
                                city = message.Text.Substring(position + 1);
                                message.Text = city;
                                position = message.Text.IndexOf(" ");
                                user.City = message.Text.Substring(0, position);
                                user.Time = message.Text.Substring(position + 1);
                                user.IDUser = (int)message.Chat.Id;
                                await client.SendTextMessageAsync(message.Chat.Id, Post(user));
                            }
                            catch(Exception ex)
                            {
                                await client.SendTextMessageAsync(message.Chat.Id, Error());
                            }

                            break;
                        }
                    case "/con":
                        {
                            List<User> user = new List<User>();

                            using (SqlConnection connection = new SqlConnection(Connection_String))
                            {
                                try
                                {
                                    connection.Open();

                                    using (SqlCommand com = new SqlCommand("SELECT * FROM TelegramBotUsers Order by ID", connection))
                                    {
                                        await client.SendTextMessageAsync(message.Chat.Id,"Ok");
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await client.SendTextMessageAsync(message.Chat.Id, "Error");
                                    break;
                                }
                                finally
                                {
                                    if (connection.State == ConnectionState.Open)
                                        connection.Close();
                                }
                            }
                            await client.SendTextMessageAsync(message.Chat.Id, "Error");
                            break;
                        }
                    default:
                        {
                            await client.SendTextMessageAsync(message.Chat.Id, Error());
                            break;
                        }
                }

            }

        }

        static string CallWeather(string city)
        {
            try
            {
                string url = string.Format("http://api.openweathermap.org/data/2.5/weather?q={0}&units=metric&appid=d8586230c8514cf851fb224acce09e95", city);

                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                string response;

                using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    response = streamReader.ReadToEnd();
                }

                WeatherResponse weatherResponse = JsonConvert.DeserializeObject<WeatherResponse>(response);
                return string.Format("Temperature in {0}: {1} °С", weatherResponse.Name, weatherResponse.Main.Temp.ToString());
            }
            catch (Exception exp)
            {
                return string.Format("Wrong city entered.Correct notation /weather city_name");
            }
        }

        static string Error()
        {
            return string.Format("There is no such command. Enter /help to see all available commands.");
        }

        static string Start()
        {
            return string.Format("You are greeted by a weather forecast bot. Current available commands: " +
                "/help - to call support and view all available commands /weather city_name - to determine the weather");
        }

        static string Help()
        {
            return string.Format("Current available commands: /help - to call support and view all available " +
                "commands /weather city_name - to determine the weather");
        }

        private static string Post(User user)
        {

            using (SqlConnection connection = new SqlConnection(Connection_String))
            {
                string Query = "INSERT INTO TelegramBotUsers(IDUser,Time,City) VALUES(@iduser,@time,@city);";

                using (SqlCommand command = new SqlCommand(Query, connection))
                {
                    command.Parameters.AddWithValue("@iduser", user.IDUser);
                    command.Parameters.AddWithValue("@time", user.Time);
                    command.Parameters.AddWithValue("@city", user.City);
                    try
                    {
                        connection.Open();
                        if (command.ExecuteNonQuery() > 0)
                        {
                            return "Reminder has been set";
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                            connection.Close();
                    }
                }
            }
            return "Error";


        }

        private static List<User> Get()
        {
            List<User> user = new List<User>();

            using (SqlConnection connection = new SqlConnection(Connection_String))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("SELECT * FROM TelegramBotUsers Order by ID", connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.HasRows && reader.Read())
                                user.Add(new User(reader));
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            return user;
        }
    }
}
