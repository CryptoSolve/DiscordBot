using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Text;

namespace DiscordBot.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        #region Команды

        /// <summary>
        /// Присылает файл обратно отправителю
        /// </summary>
        [Command("ReturnBack", RunMode = RunMode.Async)]
        [Alias("r")]
        public async Task ReturnBack()
        {
            var messageChannel = Context.Channel;

            await ReturnBack(Context.Message.Attachments, messageChannel);
        }

        /// <summary>
        /// Присоединяет бота к голосовому чату
        /// </summary>
        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinChannel(IVoiceChannel channel = null)
        {
            var user = (Context.User as IGuildUser);
            if (user == null) { return; }
            channel = user.VoiceChannel;
            var clientUser = await Context.Channel.GetUserAsync(Context.Client.CurrentUser.Id);
            if (clientUser is IGuildUser)
            {
                await MainWindow.instance.MusicPlayer.JoinAudio(Context.Guild, channel, Context.Channel);
            }
        }

        /// <summary>
        /// Заставляет бота выйти с голосового чата
        /// </summary>
        [Command("leave", RunMode = RunMode.Async)]
        public async Task LeaveCmd()
        {
            await MainWindow.instance.MusicPlayer.LeaveAudio(Context.Guild);
        }

        /// <summary>
        /// Находит видео на YouTube по ссылке
        /// </summary>
        /// <param name="songURL">Ссылка на видео с YouTube</param>
        [Command("play", RunMode = RunMode.Async)]
        public async Task Play([Remainder] string songURL)
        {
            var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;

            bool canPlay = !(voiceChannel == null) || songURL.StartsWith("https://www.youtube.com/watch?");
            if (!canPlay) { return; }


            await MainWindow.instance.MusicPlayer.SetSong(
                Context.Guild,
                voiceChannel,
                Context.Channel,
                songURL);
        }

        /// <summary>
        /// Пишет в чат канал случайную фразу
        /// </summary>
        [Command("Рулетка")]
        public async Task Roll()
        {
            await ReplyAsync(states[random.Next(states.Length)]);
        }

        /// <summary>
        /// Выводит информацию о пользователе
        /// </summary>
        [Command("userinfo")]
        [Alias("user", "whois")]
        public async Task UserInfoAsync()
        {
            var userInfo = Context.User;

            await ReplyAsync(
                $"Activity {Context.Client.Activity}\n" +
                $"Status {Context.Client.Status}\n" +
                $"CreatedAt {Context.User.CreatedAt}\n" +
                $"UserName {userInfo.Username}#{userInfo.Discriminator}");
        }

        /// <summary>
        /// Объединяет команды в группу, доступных только для администратора
        /// </summary>
        [Group("как админ")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public class AdminModule : ModuleBase<SocketCommandContext>
        {
            /// <summary>
            /// Объединяет команды в группу, доступных для бота с разрешением управления сообщениями
            /// </summary>
            [Group("удали")]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            public class CleanModule : ModuleBase<SocketCommandContext>
            {
                /// <summary>
                /// Удаляет определенное количество сообщений в чате
                /// </summary>
                /// <param name="amount">Количество</param>
                [Command("сообщения")]
                public async Task CleanAsync(uint amount)
                {
                    await ReplyAsync($"Исполняю!");
                    IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync((int)amount + 2).FlattenAsync();
                    await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
                    const int delay = 1000;
                    var m = await ReplyAsync($"Очистка завершена! Это сообщение будет уничтожено через {delay / 1000}...");
                    await Task.Delay(delay);
                    await m.DeleteAsync();
                }
            }
        }

        #endregion

        #region Игра

        /// <summary>
        /// Регистрирует игрока
        /// </summary>
        [Command("Добавь в игру", RunMode = RunMode.Async)]
        [Alias("Добавь", "Зарегай")]
        public async Task NewPlayer()
        {
            var user = Context.User;
            var messageChannel = Context.Channel;

            await MiniGame.NewPlayer(user, messageChannel);
        }

        /// <summary>
        /// Возрождает игрока
        /// </summary>
        [Command("Поставь на ноги", RunMode = RunMode.Async)]
        [Alias("Воскреси", "Респаун")]
        public async Task Resurrect()
        {
            var user = Context.User;
            var messageChannel = Context.Channel;

            await MiniGame.TryResurrect(user, messageChannel);
        }

        /// <summary>
        /// Группа команд инвентаря
        /// </summary>
        [Group("Инвентарь")]
        public class InventoryModule : ModuleBase<SocketCommandContext>
        {
            /// <summary>
            /// Показывает инвентарь
            /// </summary>
            [Command("Посмотреть", RunMode = RunMode.Async)]
            [Alias("Открыть", "Открой", "Покажи")]
            public async Task Inventory()
            {
                var user = Context.User;
                var messageChannel = Context.Channel;

                await MiniGame.ShowInventory(user, messageChannel);
            }
            /// <summary>
            /// Использует предмет по введнному названию
            /// </summary>
            /// <param name="item">Название предмета</param>
            [Command("Используй", RunMode = RunMode.Async)]
            public async Task Use([Remainder] string item)
            {
                var user = Context.User;
                var messageChannel = Context.Channel;
                await MiniGame.Use(user, item, messageChannel);
            }
        }

        /// <summary>
        /// Поиск предметов или непритностей
        /// </summary>
        [Command("Искать", RunMode = RunMode.Async)]
        [Alias("Ищи")]
        public async Task Search()
        {
            var user = Context.User;
            var messageChannel = Context.Channel;

            await MiniGame.Search(user, messageChannel);
        }

        #endregion

        #region Приватные методы

        /// <summary>
        /// Реализация
        /// </summary>
        /// <param name="iAttachments">Прикрепленные файлы к сообщению</param>
        /// <param name="messageChannel">Чат-канал</param>
        private async Task ReturnBack(IEnumerable<Attachment> iAttachments, IMessageChannel messageChannel)
        {
            var attachments = new List<Attachment>();

            foreach (var attachment in iAttachments)
            {
                attachments.Add(attachment);
            }

            if (attachments.Count <= 0)
            {
                await messageChannel.SendMessageAsync("Нет прикрепленных файлов");
                return;
            }

            for (int i = 0; i < attachments.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(attachments[i].Filename))
                {
                    return;
                }
                DownloadFile(attachments[i].Url, attachments[i].Filename);
            }

            for (int i = 0; i < attachments.Count; i++)
            {
                var sentMessage = await messageChannel.SendFileAsync(attachments[i].Filename);
                var robot = new Emoji("🤖");
                await sentMessage.AddReactionAsync(robot);
            }
        }

        /// <summary>
        /// Скачивает файл
        /// </summary>
        /// <param name="Url">Ссылка</param>
        /// <param name="filePath">По какому пути сохранить файл</param>
        private void DownloadFile(string Url, string filePath)
        {
            WebClient webClient = new WebClient();
            webClient.Encoding = Encoding.UTF8;
            Uri uri = new Uri(Url);
            webClient.DownloadFile(uri, filePath);
        }

        #endregion

        #region Данные

        private Random random = new Random();
        /// <summary>
        /// Список возможных фраз
        /// </summary>
        private string[] states = new string[]
        {
            "Ты выиграл!",
            "Ты проиграл!",
            "Попробуй еще раз",
            "В другой раз повезет",
            "Хрен тебе мешок с костями!",
            "Вычисляю... возможно...",
            "Оп-па какая удача! Тебе повезло не проиграть!",
            "You DIED!",
            "Bip Bop Bop Bip Bop",
            @"'/^((([0-9A-Za-z]{1}[-0-9A-z\.]{1,}[0-9A-Za-z]{1})|([0-9А-Яа-я]{1}[-0-9А-я\.]{1,}[0-9А-Яа-я]{1}))@([-A-Za-z]{1,}\.){1,2}[-A-Za-z]{2,})$/u'"
        };

        #endregion
    }
}