using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Newtonsoft.Json;

namespace DiscordBot
{
    /// <summary>
    /// Описывает логику взаимодействия с интерфейсом приложения
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Ссылка на единственный экземпляр
        /// </summary>
        public static MainWindow instance { get; private set; }
        /// <summary>
        /// Для аудиовещания в голосовой канал
        /// </summary>
        public MusicPlayer MusicPlayer { get; private set; }
        public DiscordSocketClient Client { get; private set; }
        private CommandService commands;
        private IServiceProvider services;
        private StringBuilder saveData;
        private string saveFilePath;
        private int currentToken;

        public MainWindow()
        {
            InitializeComponent();

            if (!(instance == null))
            { return; }
            instance = this;
            this.SizeChanged += OnWindowSizeChanged;
            MiniGame.LaunchGame();
            MusicPlayer = new MusicPlayer();

            SavingsOperations();
        }

        #region Инициализация бота

        /// <summary>
        /// Запускает бота в фоновом режиме
        /// </summary>
        public async Task RunBotAsync()
        {
            Client = new DiscordSocketClient();
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton(commands)
                .BuildServiceProvider();

            // Токен Discord бота
            string token = "здесь должен быть ваш токен дискорд бота";

            Client.Log += clientLog;

            await RegisterCommandsAsync();

            await Client.LoginAsync(TokenType.Bot, token);

            await Client.StartAsync();

            await Task.Delay(-1);
        }

        /// <summary>
        /// Выводит дополнительную информацию
        /// </summary>
        private Task clientLog(LogMessage arg)
        {
            Print(arg.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Ссчитывает пришедшие в чат сообщения в фоновом режиме
        /// </summary>
        public async Task RegisterCommandsAsync()
        {
            Client.MessageReceived += HandleCommandAsync;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        /// <summary>
        /// Позволяет распознать сообщение - командой, при вводе спецсимвола
        /// </summary>
        /// <param name="arg">Сообщение</param>
        public async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(Client, message);
            if (message.Author.IsBot)
            { return; }

            int argPos = 0;
            if (message.HasCharPrefix('\\', ref argPos))
            {
                var result = await commands.ExecuteAsync(context, argPos, services);
                if (!result.IsSuccess)
                { Print(result.ErrorReason); }
            }
        }

        #endregion

        /// <summary>
        /// Выводит информацию в "консоль" приложения
        /// </summary>
        /// <param name="value">Текст для вывода</param>
        public static void Print(string value)
        {
            instance.Dispatcher.Invoke(() => instance.CreateTextBlock(value));
        }

        /// <summary>
        /// Загружает и сохраняет некоторые пользовательские настройки при старте приложения
        /// </summary>
        private void SavingsOperations()
        {
            var startDateTime = JsonConvert.SerializeObject(DateTime.Now);
            saveData = new StringBuilder(startDateTime);
            currentToken = new Random().Next(100000);
            saveData.Append("\n" + currentToken);
            saveFilePath = "SaveData.json";
            if (File.Exists(saveFilePath))
            {
                var windowSize = File.ReadAllLines(saveFilePath)[2].Split(' ');
                ChangeSize(Convert.ToDouble(windowSize[0]), Convert.ToDouble(windowSize[1]));

                DateTime oldStartDateTime = (DateTime)JsonConvert.DeserializeObject(File.ReadAllLines(saveFilePath)[0]);
                int oldToken = Convert.ToInt32(File.ReadAllLines(saveFilePath)[1]);
                DateTime oldCloseTime = (DateTime)JsonConvert.DeserializeObject(File.ReadAllLines(saveFilePath)[3]);

                Print(
                    $"Предыдущее время запуска программы: {oldStartDateTime}, " +
                    $"предыдущее время закрытия: {oldCloseTime}, " +
                    $"ваш прошлый токен {oldToken}, " +
                    $"ваш текущий токен {currentToken}");
            }
        }

        /// <summary>
        /// Создает новый блок с текстом внутри
        /// </summary>
        /// <param name="value">Текст</param>
        private void CreateTextBlock(string value)
        {
            var t = new TextBlock();
            t.Style = (Style)FindResource("TextBlocks");
            t.Text = value;
            TextStack.Children.Add(t);
        }

        /// <summary>
        /// Сохраняет размер приложения и дату закрытия приложения
        /// </summary>
        private void Save()
        {
            var windowSize = $"{Height} {Width}";
            var closeDateTime = JsonConvert.SerializeObject(DateTime.Now);
            saveData.Append("\n" + windowSize);
            saveData.Append("\n" + closeDateTime);
            File.WriteAllText(saveFilePath, saveData.ToString());
        }

        #region События

        /// <summary>
        /// При нажатии на кнопку "Start"
        /// </summary>
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Print("IntializeBot");
            await Task.Run(() => RunBotAsync());//.GetAwaiter().GetResult());
        }

        /// <summary>
        /// При нажатии на кнопку "X"
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
            Close();
        }

        /// <summary>
        /// Событие связанное с тем, что пользователь изменил размер приложения
        /// </summary>
        protected async void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double newWindowHeight = e.NewSize.Height;
            double newWindowWidth = e.NewSize.Width;
            double prevWindowHeight = e.PreviousSize.Height;
            double prevWindowWidth = e.PreviousSize.Width;

            await ChangeSize(newWindowHeight, newWindowWidth);
        }

        /// <summary>
        /// Изменяет размер внутренних окон от размера приложения
        /// </summary>
        /// <param name="height">Высота</param>
        /// <param name="width">Ширина</param>
        private Task ChangeSize(double height, double width)
        {
            if (height < 60) {
                return Task.Delay(1);
            }
            MyGrid.Height = height;
            MyGrid.Width = width;
            height -= 30;
            ScrollView.Height = height;
            ScrollView.Width = width;
            return Task.Delay(1);
        }

        /// <summary>
        /// Позволяет двигать окошко при нажатии на шапку приложения
        /// </summary>
        private void GeneralWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        #endregion
    }
}