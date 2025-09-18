using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    
    static readonly string apiKey = "AIzaSyBSWTwcy_EBWAF8kPcSYF4fRa2u3Gr6zDk";
    static readonly string dbFile = "youtube_db.json";

    static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        bool running = true;

        Console.WriteLine("=== YouTube Search App ===");s

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

            Console.WriteLine("\n=== Результати пошуку ===");
            foreach (var v in videos)
            {
                Console.WriteLine($"🎬 {v.Title}");
                Console.WriteLine($"📺 Канал: {v.ChannelTitle}");
                Console.WriteLine($"📅 Дата публікації: {v.PublishedAt}");
                Console.WriteLine($"🔗 https://www.youtube.com/watch?v={v.VideoId}");
                Console.WriteLine(new string('-', 50));
            }

           
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


