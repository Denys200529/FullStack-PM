using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    // Твій ключ API
    static readonly string apiKey = "AIzaSyBSWTwcy_EBWAF8kPcSYF4fRa2u3Gr6zDk";
    static readonly string dbFile = "youtube_db.json";

    static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        bool running = true;

        Console.WriteLine("=== YouTube Search App ===");

        while (running)
        {
            Console.WriteLine("\nВведіть пошуковий запит для YouTube (або N для виходу):");
            string query = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(query) || query.ToUpper() == "N")
            {
                running = false;
                break;
            }

            await SearchYouTube(query);

            Console.Write("\nБажаєте продовжити роботу? (Y/N): ");
            string cont = Console.ReadLine()?.ToUpper();
            if (cont != "Y") running = false;
        }

        Console.WriteLine("Програму завершено.");
    }

    static async Task SearchYouTube(string query)
    {
        string url = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={Uri.EscapeDataString(query)}&type=video&maxResults=5&key={apiKey}";

        using HttpClient client = new HttpClient();

        try
        {
            string response = await client.GetStringAsync(url);

            var jsonDoc = JsonDocument.Parse(response);
            var items = jsonDoc.RootElement.GetProperty("items");

            List<VideoInfo> videos = new List<VideoInfo>();

            foreach (var item in items.EnumerateArray())
            {
                var snippet = item.GetProperty("snippet");
                videos.Add(new VideoInfo
                {
                    Title = snippet.GetProperty("title").GetString(),
                    ChannelTitle = snippet.GetProperty("channelTitle").GetString(),
                    VideoId = item.GetProperty("id").GetProperty("videoId").GetString(),
                    PublishedAt = snippet.GetProperty("publishedAt").GetString()
                });
            }

            // Вивід результатів
            Console.WriteLine("\n=== Результати пошуку ===");
            foreach (var v in videos)
            {
                Console.WriteLine($"🎬 {v.Title}");
                Console.WriteLine($"📺 Канал: {v.ChannelTitle}");
                Console.WriteLine($"📅 Дата публікації: {v.PublishedAt}");
                Console.WriteLine($"🔗 https://www.youtube.com/watch?v={v.VideoId}");
                Console.WriteLine(new string('-', 50));
            }

            // Збереження в локальну "БД" (JSON)
            var record = new YoutubeRecord
            {
                Query = query,
                Date = DateTime.Now,
                Videos = videos
            };

            List<YoutubeRecord> db = LoadDatabase();
            db.Add(record);
            SaveDatabase(db);

            // Перегляд історії
            Console.Write("\nВивести історію запитів з БД? (Y/N): ");
            string choice = Console.ReadLine()?.ToUpper();
            if (choice == "Y") PrintDatabase(db);

            // Видалення записів
            Console.Write("\nХочете видалити запис з БД? (Y/N): ");
            string del = Console.ReadLine()?.ToUpper();
            if (del == "Y")
            {
                PrintDatabase(db);
                Console.Write("Введіть номер запису для видалення (або ALL для очищення БД): ");
                string input = Console.ReadLine();
                if (input.ToUpper() == "ALL")
                {
                    db.Clear();
                    SaveDatabase(db);
                    Console.WriteLine("🗑 Всі записи видалено.");
                }
                else if (int.TryParse(input, out int idx))
                {
                    if (idx > 0 && idx <= db.Count)
                    {
                        db.RemoveAt(idx - 1);
                        SaveDatabase(db);
                        Console.WriteLine("✅ Запис видалено.");
                    }
                    else Console.WriteLine("❌ Невірний номер.");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"❌ Помилка при запиті до YouTube API: {ex.Message}");
        }
    }

    static List<YoutubeRecord> LoadDatabase()
    {
        if (!File.Exists(dbFile)) return new List<YoutubeRecord>();
        string json = File.ReadAllText(dbFile);
        return JsonSerializer.Deserialize<List<YoutubeRecord>>(json) ?? new List<YoutubeRecord>();
    }

    static void SaveDatabase(List<YoutubeRecord> db)
    {
        string json = JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dbFile, json);
    }

    static void PrintDatabase(List<YoutubeRecord> db)
    {
        Console.WriteLine("\n=== Історія запитів ===");
        for (int i = 0; i < db.Count; i++)
        {
            var r = db[i];
            Console.WriteLine($"{i + 1}. {r.Date}: \"{r.Query}\" ({r.Videos.Count} відео)");
        }
    }
}

class YoutubeRecord
{
    public string Query { get; set; }
    public DateTime Date { get; set; }
    public List<VideoInfo> Videos { get; set; }
}

class VideoInfo
{
    public string Title { get; set; }
    public string ChannelTitle { get; set; }
    public string VideoId { get; set; }
    public string PublishedAt { get; set; }
}


/*using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    static readonly string apiKey = "eecd82c44710aa71b801e63ca120f6e4"; // твій ключ
    static readonly string dbFile = "weather_db.json";

    static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Write("Введіть назву міста: ");
        string city = Console.ReadLine();

        string url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric&lang=ua";

        using HttpClient client = new HttpClient();

        try
        {
            string response = await client.GetStringAsync(url);

            // Парсимо JSON
            var jsonDoc = JsonDocument.Parse(response);
            double temp = jsonDoc.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
            string description = jsonDoc.RootElement.GetProperty("weather")[0].GetProperty("description").GetString();
            double feelsLike = jsonDoc.RootElement.GetProperty("main").GetProperty("feels_like").GetDouble();
            int humidity = jsonDoc.RootElement.GetProperty("main").GetProperty("humidity").GetInt32();
            double wind = jsonDoc.RootElement.GetProperty("wind").GetProperty("speed").GetDouble();

            Console.WriteLine($"\n=== Погода у місті {city} ===");
            Console.WriteLine($"🌡 Температура: {temp} °C (відчувається як {feelsLike} °C)");
            Console.WriteLine($"☁️ Опис: {description}");
            Console.WriteLine($"💧 Вологість: {humidity}%");
            Console.WriteLine($"💨 Вітер: {wind} м/с");

            // Зберігаємо у "базу даних"
            var weatherRecord = new WeatherRecord
            {
                City = city,
                Date = DateTime.Now,
                Temperature = temp,
                FeelsLike = feelsLike,
                Description = description,
                Humidity = humidity,
                Wind = wind
            };

            List<WeatherRecord> db = LoadDatabase();
            db.Add(weatherRecord);
            SaveDatabase(db);

            // Запит користувача на вивід з БД
            Console.Write("\nВивести дані з локальної БД? (Y/N): ");
            string choice = Console.ReadLine().ToUpper();

            if (choice == "Y")
            {
                PrintDatabase(db);
            }

            // Запит на видалення
            Console.Write("\nХочете видалити запис з БД? (Y/N): ");
            string deleteChoice = Console.ReadLine().ToUpper();

            if (deleteChoice == "Y")
            {
                PrintDatabase(db);

                Console.Write("\nВведіть номер запису для видалення: ");
                if (int.TryParse(Console.ReadLine(), out int index))
                {
                    if (index > 0 && index <= db.Count)
                    {
                        db.RemoveAt(index - 1);
                        SaveDatabase(db);
                        Console.WriteLine("✅ Запис успішно видалено.");
                    }
                    else
                    {
                        Console.WriteLine("❌ Неправильний номер запису.");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Введено некоректне число.");
                }
            }

            Console.WriteLine("\nПрограму завершено.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"❌ Помилка при запиті до API: {ex.Message}");
        }
    }

    static void PrintDatabase(List<WeatherRecord> db)
    {
        Console.WriteLine("\n=== Дані з локальної БД ===");
        for (int i = 0; i < db.Count; i++)
        {
            var rec = db[i];
            Console.WriteLine($"{i + 1}. {rec.Date}: {rec.City} | 🌡 {rec.Temperature} °C (як {rec.FeelsLike} °C) | {rec.Description} | 💧 {rec.Humidity}% | 💨 {rec.Wind} м/с");
        }
    }

    static List<WeatherRecord> LoadDatabase()
    {
        if (!File.Exists(dbFile)) return new List<WeatherRecord>();
        string json = File.ReadAllText(dbFile);
        return JsonSerializer.Deserialize<List<WeatherRecord>>(json) ?? new List<WeatherRecord>();
    }

    static void SaveDatabase(List<WeatherRecord> db)
    {
        string json = JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dbFile, json);
    }
}

class WeatherRecord
{
    public string City { get; set; }
    public DateTime Date { get; set; }
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public string Description { get; set; }
    public int Humidity { get; set; }
    public double Wind { get; set; }
}
*/