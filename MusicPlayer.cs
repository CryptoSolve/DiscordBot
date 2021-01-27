using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using VideoLibrary;

namespace DiscordBot
{
    public class MusicPlayer
    {
        /// <summary>
        /// Класс содержащий информацию о подключеннии к голосовому каналу
        /// </summary>
        private class ChannelINFO
        {
            public IVoiceChannel VoiceChannel { get; private set; }
            public IAudioClient AudioClient { get; private set; }
            /// <summary>
            /// Транслируется ли аудио в голосовой канал прямо сейчас
            /// </summary>
            public bool CurrentlyTranslate { get; private set; }
            public string SongName { get; private set; }
            /// <summary>
            /// Процесс приложения FFmpeg
            /// </summary>
            public Process FFmpeg { get; private set; }
            //public Stream CurrentStream { get; private set; }
            /// <summary>
            /// Выходной аудиопоток с FFmpeg
            /// </summary>
            public Stream FFmpegOutput { get; private set; }

            public ChannelINFO(IVoiceChannel voiceChannel, IAudioClient audioClient)
            {
                this.VoiceChannel = voiceChannel;
                this.AudioClient = audioClient;
                FFmpeg = null;
                //CurrentStream = null;
                FFmpegOutput = null;
            }

            public void SetVoiceChannel(IVoiceChannel value)
            {
                VoiceChannel = value;
            }
            public void SetAudioClient(IAudioClient value)
            {
                AudioClient = value;
            }
            public void SetSongName(string value)
            {
                SongName = value;
            }
            public void SetCurrentlyTranslate(bool value)
            {
                CurrentlyTranslate = value;
            }
            public void SetFFmpegProcess(Process value)
            {
                FFmpeg = value;
            }
            public void SetFFmpegOutput(Stream value)
            {
                FFmpegOutput = value;
            }
        }
        /// <summary>
        /// Список серверов, к голосовым каналам которых подключен бот в данный момент
        /// </summary>
        private readonly ConcurrentDictionary<ulong, ChannelINFO> ConnectedChannels = new ConcurrentDictionary<ulong, ChannelINFO>();
        /// <summary>
        /// Путь к папке под YouTube видео
        /// </summary>
        private string musicFolder = @"YouTube";

        public MusicPlayer() { }

        /// <summary>
        /// Присоединяет бота к голосовому чату
        /// </summary>
        /// <param name="guild">Discord cервер</param>
        /// <param name="voiceChannel">Голосовой канал</param>
        /// <param name="messageChannel">Чат-канал</param>
        /// <returns></returns>
        public async Task JoinAudio(IGuild guild, IVoiceChannel voiceChannel, IMessageChannel messageChannel)
        {
            if (voiceChannel == null) { return; }
            ChannelINFO client;
            if (voiceChannel.Guild.Id != guild.Id)
            {
                return;
            }
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                if (client.VoiceChannel.Id == voiceChannel.Id)
                {
                    await messageChannel.SendMessageAsync($"Бот уже на канале {voiceChannel.Name}");
                    return;
                }
                await messageChannel.SendMessageAsync($"Бот прибыл с канала {client.VoiceChannel.Name} на {voiceChannel.Name}");
                client.SetVoiceChannel(voiceChannel);
                client.SetAudioClient(await client.VoiceChannel.ConnectAsync());
            }
            else
            {
                await messageChannel.SendMessageAsync($"Бот прибыл на канал {voiceChannel.Name}");

                MainWindow.Print("Подключаю аудиоклиент");
                var audioClient = await voiceChannel.ConnectAsync();

                MainWindow.Print("Добавляю аудиоклиент в список");
                ConnectedChannels.TryAdd(guild.Id, new ChannelINFO(voiceChannel, audioClient));
            }
        }

        /// <summary>
        /// Заставляет бота выйти с голосового канала
        /// </summary>
        /// <param name="guild">Discord cервер</param>
        public async Task LeaveAudio(IGuild guild)
        {
            ChannelINFO client;
            if (ConnectedChannels.TryRemove(guild.Id, out client))
            {
                MainWindow.Print($"Удален аудиоканал с {guild.Name}");
                await client.AudioClient.StopAsync();
                MainWindow.Print($"Аудиоканал остановлен");
            }
        }

        /// <summary>
        /// Воспроизводит в голосовой канал, загруженное по ссылке с YouTube, аудио
        /// </summary>
        /// <param name="guild">Discord Сервер на котором запущена процедура</param>
        /// <param name="voiceChannel">Голосовой канал</param>
        /// <param name="messageChannel">Чат-канал</param>
        /// <param name="videoURL">Ссылка в YouTube для скачивания</param>
        public async Task SetSong(IGuild guild, IVoiceChannel voiceChannel, IMessageChannel messageChannel, string videoURL)
        {
            if (!ConnectedChannels.ContainsKey(guild.Id)) { await JoinAudio(guild, voiceChannel, messageChannel); }
            ChannelINFO client;
            ConnectedChannels.TryGetValue(guild.Id, out client);
            if (client.CurrentlyTranslate)
            {
                await messageChannel.SendMessageAsync("В данный момент уже проигрывается песня " + client.SongName);
                return;
            }
            client.SetCurrentlyTranslate(true);

            MainWindow.Print("Пытаюсь проиграть: " + videoURL);
            //currentStream = ReadVideo(url, channel);
            //await SendAsync(guild);
            var currentSong = SaveMP4(musicFolder, videoURL, guild, messageChannel);
            await SendAsync(guild, messageChannel, currentSong);
        }

        /// <summary>
        /// (НЕ РАБОТАЕТ)Должно считывать аудиопоток в режиме реального времени для дальнейшего перекодирования и транслирования
        /// </summary>
        /// <param name="videoURL">Ссылка в YouTube для скачивания</param>
        /// <param name="messageChannel">Чат-канал</param>
        /// <returns>Поток mp3</returns>
        private Stream ReadVideo(string videoURL, IMessageChannel messageChannel)
        {
            //var ff = Xabe.FFmpeg.FFmpeg.Conversions.New();

            var outStream = Stream.Null;

            return outStream;
        }

        /// <summary>
        /// Загружает видео с YouTube
        /// </summary>
        /// <param name="saveToFolder">Путь к папке под видео</param>
        /// <param name="videoURL">Ссылка в YouTube для скачивания</param>
        /// <param name="guild">Discord сервер на котором запущена процедура</param>
        /// <param name="messageChannel">Чат-канал</param>
        /// <returns>Путь к загруженному видео</returns>
        private string SaveMP4(string saveToFolder, string videoURL, IGuild guild, IMessageChannel messageChannel)
        {
            var youtube = YouTube.Default;
            messageChannel.SendMessageAsync($"Поиск {videoURL}".Replace('p', 'р'));
            var vid = youtube.GetVideo(videoURL);
            if (!File.Exists(saveToFolder + vid.FullName))
            {
                messageChannel.SendMessageAsync($"Загружаю видео {vid.FullName.Replace(".mp4", string.Empty)}...");
                File.WriteAllBytes(saveToFolder + vid.FullName, vid.GetBytes());
                messageChannel.SendMessageAsync($"Видео загружено");
            }
            
            ChannelINFO client;
            ConnectedChannels.TryGetValue(guild.Id, out client);
            client.SetSongName(vid.FullName.Replace(".mp4", string.Empty));

            return saveToFolder + vid.FullName;
        }

        /// <summary>
        /// Перекодирует загруженное видео с YouTube в формат .s16le и транслирует в голосовой канал дискорда
        /// </summary>
        /// <param name="guild">Discord cервер на котором запущена процедура</param>
        /// <param name="messageChannel">Чат-канал</param>
        /// <param name="path">Путь к загруженному видео</param>
        private async Task SendAsync(IGuild guild, IMessageChannel messageChannel, string path)
        {
            MainWindow.Print("Получаю аудиоклиент");
            ChannelINFO client = ConnectedChannels[guild.Id];
            MainWindow.Print("Запускаю FFmpeg");
            client.SetFFmpegProcess(CreateFFmpegProcess(path));
            MainWindow.Print("Процесс FFmpeg запущен");
            
            using (client.FFmpeg)
            {
                MainWindow.Print("Регаю выходной поток");
                client.SetFFmpegOutput(client.FFmpeg.StandardOutput.BaseStream);
                using (client.FFmpegOutput)
                {
                    MainWindow.Print("Выходной поток зареган");
                    using (var discord = client.AudioClient.CreatePCMStream(AudioApplication.Music))
                    {
                        MainWindow.Print("PCM поток создан");
                        try
                        {
                            MainWindow.Print("Процесс чтения");
                            await client.FFmpegOutput.CopyToAsync(discord);
                        }
                        finally
                        {
                            MainWindow.Print("Процесс очистки");
                            await discord.FlushAsync();
                        }
                    }
                }
            }

            client.FFmpeg.Kill();
            client.SetCurrentlyTranslate(false);
        }

        /// <summary>
        /// Запускает процесс/программу ffmpeg
        /// </summary>
        /// <param name="path">Путь к загруженному видео</param>
        /// <returns></returns>
        private Process CreateFFmpegProcess(string path)
        {
            try
            {
                MainWindow.Print("Записываю предустановки");
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = @"ffmpeg\bin\ffmpeg.exe",
                    Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                };
                MainWindow.Print("Запускаю процесс");
                Process process = Process.Start(processStartInfo);

                return process;
            }
            catch (Exception ex)
            {
                MainWindow.Print(ex.ToString());
                return null;
            }
        }
    }
}