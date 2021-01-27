using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace DiscordBot
{
    public class MiniGame
    {
        #region Данные
        /// <summary>
        /// Ссылка на единственный экземпляр миниигры
        /// </summary>
        public static MiniGame instance { get; private set; }
        /// <summary>
        /// Список зарегестрированных игрков
        /// </summary>
        public Dictionary<string, Player> Players { get; private set; }
        /// <summary>
        /// Путь к файлу сохранения
        /// </summary>
        private string saveFilePath = "miniGameSave.json";
        private Random random = new Random();

        #endregion

        #region Логика

        /// <summary>
        /// Создает ссылку глобального доступа/Singleton/паттерн одиночка
        /// </summary>
        public static void LaunchGame()
        {
            if (instance != null) { return; }

            instance = new MiniGame();
        }

        public MiniGame()
        {
            MainWindow.instance.Closed += Save;
            Players = new Dictionary<string, Player>();

            Load();
        }

        /// <summary>
        /// Регистрирует нового игрока
        /// </summary>
        /// <param name="user">Пользователь > его информация</param>
        /// <param name="messageChannel">Чат-канал</param>
        public async static Task NewPlayer(SocketUser user, IMessageChannel messageChannel)
        {
            if (!instance.Players.ContainsKey(user.Username))
            {
                instance.Players.Add(user.Username, new Player(user.Username, 100));
                await messageChannel.SendMessageAsync($"{instance.Players[user.Username].UserName} Добавлен!");
            }
            else
            {
                await messageChannel.SendMessageAsync($"Ты и так уже в игре -__-");
            }
        }

        /// <summary>
        /// Возрождает игрока по желанию
        /// </summary>
        /// <param name="user">Пользователь > его информация</param>
        /// <param name="messageChannel">Чат-канал</param>
        public async static Task TryResurrect(SocketUser user, IMessageChannel messageChannel)
        {
            if (instance.Players.ContainsKey(user.Username))
            {
                if (instance.Players[user.Username].isAlive)
                {
                    await messageChannel.SendMessageAsync(
                        $"Да ты живее всех живых! У тебя {instance.Players[user.Username].Health} " +
                        $"здоровья из {instance.Players[user.Username].MaxHealth}");
                }
                else
                {
                    await messageChannel.SendMessageAsync($"Ща тебя поправим, не боись, без ноги тоже жить можно");
                    instance.Players[user.Username].Resurrect();
                    var message = await messageChannel.SendMessageAsync($"Вот и все, с новой киберногой ты такой симпотяшка!");
                    var roboLeg = new Emoji(@"\U+1F9BF");
                    await message.AddReactionAsync(roboLeg);
                }
            }
            else
            {
                await messageChannel.SendMessageAsync($"Ты для начала в игру добавься!");
            }
        }

        /// <summary>
        /// Вызывает случайное действие
        /// </summary>
        /// <param name="user">Пользователь > его информация</param>
        /// <param name="messageChannel">Чат-канал</param>
        public async static Task Search(SocketUser user, IMessageChannel messageChannel)
        {
            if (instance.Players.ContainsKey(user.Username))
            {
                if (instance.Players[user.Username].isAlive)
                {
                    int i = instance.random.Next(2);
                    switch (i)
                    {
                        case 0:
                            instance.Players[user.Username].AddToInventory("Стимулятор");
                            await messageChannel.SendMessageAsync("Оп-па! Ты нашел новенький стимулятор! Круто!");
                            break;
                        case 1:
                            var sender = new Player("Raider", 100);
                            int value = -10;
                            instance.Players[user.Username].ApplyDamage(sender, -10);
                            if (instance.Players[user.Username].isAlive)
                            {
                                await messageChannel.SendMessageAsync($"{sender.UserName} нанес урон {user.Username} в размере {value} HP.");
                            }
                            else
                            {
                                await messageChannel.SendMessageAsync($"{sender.UserName} нанес урон {user.Username} в размере {value} HP и убил его." +
                                $"Чтож, одним кожанным мешком меньше, оно даже и лучше.");
                            }
                            break;
                        default:
                            await messageChannel.SendMessageAsync("Exception!");
                            break;
                    }
                }
                else
                {
                    await messageChannel.SendMessageAsync("Но ведь мертвые не могут разговаривать...");
                }
            }
            else
            {
                await messageChannel.SendMessageAsync("Тебя нету в игре!");
            }
        }

        /// <summary>
        /// Показывает инвентарь в чате
        /// </summary>
        /// <param name="user">Пользователь > его информация</param>
        /// <param name="messageChannel">Чат-канал</param>
        public async static Task ShowInventory(SocketUser user, IMessageChannel messageChannel)
        {
            if (instance.Players.ContainsKey(user.Username))
            {
                if (instance.Players[user.Username].isAlive)
                {
                    if (instance.Players[user.Username].Inventory.Count > 0)
                    {
                        string items = string.Empty;
                        foreach (var item in instance.Players[user.Username].Inventory)
                        {
                            items += $"{item.Value.Name} x{item.Value.Count}\n";
                        }
                        await messageChannel.SendMessageAsync(items);
                    }
                    else
                    {
                        await messageChannel.SendMessageAsync("Тут пока пусто, но могла быть твоя реклама.");
                    }
                }
                else
                {
                    await messageChannel.SendMessageAsync("Жмурам инвентарь не нужен...");
                }
            }
            else
            {
                await messageChannel.SendMessageAsync("Тебя нету в игре!");
            }
        }

        /// <summary>
        /// Использует предмет в инвентаре
        /// </summary>
        /// <param name="user">пользователь > его информация</param>
        /// <param name="messageChannel">Чат-канал</param>
        public async static Task Use(SocketUser user, string item, IMessageChannel messageChannel)
        {
            if (instance.Players.ContainsKey(user.Username))
            {
                if (instance.Players[user.Username].isAlive)
                {
                    if (instance.Players[user.Username].Inventory.ContainsKey(item))
                    {
                        var player = instance.Players[user.Username];
                        player.Inventory[item].Use(player, messageChannel);
                    }
                    else
                    {
                        await messageChannel.SendMessageAsync($"Ты наверное что-то потерял. У тебя нету {item} в рюкзаке.");
                    }
                }
                else
                {
                    await messageChannel.SendMessageAsync("Боюсь мертвому тебе это уже не поможет...");
                }
            }
            else
            {
                await messageChannel.SendMessageAsync("Тебя нету в игре!");
            }
        }
        #endregion

        #region Описание Player

        public class Player
        {
            /// <summary>
            /// Никнейм игрока
            /// </summary>
            public string UserName { get; private set; }
            /// <summary>
            /// Максимальное количество очков здоровья
            /// </summary>
            public int MaxHealth { get; private set; }
            /// <summary>
            /// Текущий показатель очков жизни
            /// </summary>
            public int Health { get; private set; }
            /// <summary>
            /// Игрок живой?
            /// </summary>
            public bool isAlive => Health > 0;
            /// <summary>
            /// Инвентарь
            /// </summary>
            public Dictionary<string, Item> Inventory { get; private set; }

            public Player(string userName, int maxHealth)
            {
                this.UserName = userName;
                this.MaxHealth = maxHealth;
                this.Health = this.MaxHealth;
                this.Inventory = new Dictionary<string, Item>();
            }

            /// <summary>
            /// Добавляет предмет в инвентарь игроку
            /// </summary>
            /// <param name="name">Наименование предмета</param>
            public void AddToInventory(string name)
            {
                if (Inventory.ContainsKey(name))
                {
                    Inventory[name].IncrementItemsCount(1);
                }
                else
                {
                    Inventory.Add(name, new Item(name));
                }
            }

            /// <summary>
            /// Наносит урон игроку
            /// </summary>
            /// <param name="sender">Отправитель урона</param>
            /// <param name="value">Количество очков урона</param>
            public void ApplyDamage(Player sender, int value)
            {
                if (Health + value <= 0)
                {
                    Health = 0;
                }
                else
                {
                    Health += value;
                }
            }

            /// <summary>
            /// Восстанавливает здоровье с помощью предмета
            /// </summary>
            /// <param name="item">Предмет</param>
            public void Heal(Item item)
            {
                Health += item.HealthRestore;
            }

            /// <summary>
            /// Возрождает игрока
            /// </summary>
            public void Resurrect()
            {
                ResetHealth();
                ResetInventory();
            }

            /// <summary>
            /// Полностью восстанавливает здоровье игрока
            /// </summary>
            private void ResetHealth()
            {
                Health = MaxHealth;
            }

            /// <summary>
            /// Очищает инвентарь
            /// </summary>
            private void ResetInventory()
            {
                Inventory = new Dictionary<string, Item>();
            }
        }

        /// <summary>
        /// Представляет собой предмет
        /// </summary>
        public class Item
        {
            /// <summary>
            /// Название предмета
            /// </summary>
            public string Name { get; private set; }
            /// <summary>
            /// Количество предметов
            /// </summary>
            public int Count { get; private set; }
            /// <summary>
            /// Показатель того, сколько этот предмет восстоновит здоровья в случае использования
            /// </summary>
            public int HealthRestore { get; private set; }

            internal Item(string name)
            {
                Name = name;
                Count = 1;
                HealthRestore = 10;
            }

            public Item(string name, int count)
            {
                Name = name;
                Count = count;
                HealthRestore = 10;
            }

            /// <summary>
            /// Использует предмет
            /// </summary>
            /// <param name="owner">Владелец</param>
            /// <param name="messageChannel">Чат-канал</param>
            public void Use(Player owner, IMessageChannel messageChannel)
            {
                if (owner.Health + HealthRestore > owner.MaxHealth)
                {
                    messageChannel.SendMessageAsync("Твое здоровье переполнится");
                    return;
                }

                if (Count == 1)
                {
                    owner.Inventory.Remove(Name);
                    owner.Heal(this);
                }
                else
                {
                    Count--;
                    owner.Heal(this);
                }
            }

            public void IncrementItemsCount(int value)
            {
                Count += value;
            }
        }

        /// <summary>
        /// Загружает информацию об игроках
        /// </summary>
        private static void Load()
        {
            if (instance == null) { return; }

            if (File.Exists(instance.saveFilePath))
            {
                string savingsData = File.ReadAllText(instance.saveFilePath);
                Dictionary<string, Player> temp = JsonConvert.DeserializeObject<Dictionary<string, Player>>(savingsData);
                instance.Players = temp;
            }
        }

        /// <summary>
        /// Сохраняет информацию об игроках
        /// </summary>
        private static void Save(object sender, EventArgs args)
        {
            if (instance == null) { return; }

            string dataSave = JsonConvert.SerializeObject(instance.Players);
            File.WriteAllText(instance.saveFilePath, dataSave);
        }

        #endregion
    }
}