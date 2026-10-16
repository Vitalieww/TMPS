using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NutriSense.Application.Services;
using NutriSense.Application.Strategies;
using NutriSense.Application.Validation;
using NutriSense.Domain.Repositories;
using NutriSense.Domain.Strategies;
using NutriSense.Infrastructure.Persistence;
using NutriSense.Infrastructure.Reporting;

namespace NutriSense.App;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var services = new ServiceCollection();
        var defaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        services.AddSingleton<IFoodRepository>(_ => new InMemoryFoodRepository(defaultUserId));
        services.AddSingleton<IReactionRepository>(_ => new InMemoryReactionRepository(defaultUserId));

        services.AddSingleton<FoodEntryValidator>();
        services.AddSingleton<ReactionEntryValidator>();

        services.AddSingleton<IReactionAnalysisStrategy, FrequencyAnalysisStrategy>();
        services.AddSingleton<IReactionAnalysisStrategy, SeverityWeightedStrategy>();
        services.AddSingleton<IReactionAnalysisStrategy, IngredientIntoleranceStrategy>();

        services.AddSingleton<CorrelationEngine>();
        services.AddSingleton<NutriSenseTrackerService>();

        services.AddSingleton<ConsoleReportFormatter>();
        services.AddSingleton<HtmlChartReportFormatter>();

        var provider = services.BuildServiceProvider();

        var tracker = provider.GetRequiredService<NutriSenseTrackerService>();
        var consoleFormatter = provider.GetRequiredService<ConsoleReportFormatter>();
        var htmlFormatter = provider.GetRequiredService<HtmlChartReportFormatter>();

        PrintBanner();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n[ MAIN MENU - NUTRISENSE ]");
            Console.ResetColor();
            Console.WriteLine(" 1. View Meal Log & Adverse Reaction History");
            Console.WriteLine(" 2. Run All Correlation Strategies");
            Console.WriteLine(" 3. Log a New Meal & Adverse Reaction");
            Console.WriteLine(" 4. Generate & Open Web Dashboard with Interactive Charts");
            Console.WriteLine(" 0. Exit");
            Console.Write("\nSelect an option [0-4]: ");

            var choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await DisplayHistoryAsync(tracker, defaultUserId);
                    break;
                case "2":
                    await RunStrategiesAsync(tracker, consoleFormatter, defaultUserId);
                    break;
                case "3":
                    await LogNewEntryAsync(tracker, defaultUserId);
                    break;
                case "4":
                    await GenerateWebDashboardAsync(tracker, htmlFormatter, defaultUserId);
                    break;
                case "0":
                    Console.WriteLine("Goodbye! Thank you for using NutriSense.");
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid option. Please enter a number between 0 and 4.");
                    Console.ResetColor();
                    break;
            }
        }
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════════════════════╗
║               NUTRISENSE • FOOD & REACTION INTOLERANCE TRACKER               ║
╚══════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine(" Automated pattern recognition and trigger correlation analysis for diet tracking.");
    }

    private static async Task DisplayHistoryAsync(NutriSenseTrackerService tracker, Guid userId)
    {
        var foods = await tracker.GetFoodHistoryAsync(userId);
        var reactions = await tracker.GetReactionHistoryAsync(userId);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n--- RECENT MEALS CONSUMED ({foods.Count} entries) ---");
        Console.ResetColor();
        Console.WriteLine($"{"DATE & TIME (UTC)",-20} | {"FOOD ITEM",-26} | {"CATEGORY",-14} | {"RATING",-8} | {"INGREDIENTS"}");
        Console.WriteLine(new string('-', 95));

        foreach (var food in foods.Take(8))
        {
            var stars = new string('★', food.Rating) + new string('☆', 5 - food.Rating);
            var ing = string.Join(", ", food.Ingredients);
            Console.WriteLine($"{food.ConsumedAtUtc:yyyy-MM-dd HH:mm} | {food.FoodName,-26} | {food.Category,-14} | {stars,-8} | {ing}");
        }

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n--- RECORDED ADVERSE REACTIONS ({reactions.Count} entries) ---");
        Console.ResetColor();
        Console.WriteLine($"{"DATE & TIME (UTC)",-20} | {"SYMPTOM / REACTION",-28} | {"SEVERITY",-10} | {"DURATION",-10} | {"NOTES"}");
        Console.WriteLine(new string('-', 95));

        foreach (var r in reactions)
        {
            var sevBar = $"{r.Severity}/10";
            var dur = r.DurationMinutes.HasValue ? $"{r.DurationMinutes}m" : "N/A";
            Console.WriteLine($"{r.OccurredAtUtc:yyyy-MM-dd HH:mm} | {r.SymptomType,-28} | {sevBar,-10} | {dur,-10} | {r.Notes}");
        }
    }

    private static async Task RunStrategiesAsync(
        NutriSenseTrackerService tracker,
        ConsoleReportFormatter formatter,
        Guid userId)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Executing correlation analysis across time window (default: 8 hours)...");
        Console.ResetColor();

        var results = await tracker.AnalyzeTriggersAsync(userId, TimeSpan.FromHours(8));

        foreach (var (strategyName, insights) in results)
        {
            var output = formatter.FormatInsights(strategyName, insights);
            Console.WriteLine(output);
        }
    }

    private static async Task LogNewEntryAsync(NutriSenseTrackerService tracker, Guid userId)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[ LOG A NEW MEAL ]");
        Console.ResetColor();

        Console.Write("Enter Food Name (e.g., Ice Cream, Sourdough Bread): ");
        var name = Console.ReadLine() ?? "";

        Console.Write("Enter Category (e.g., Dairy, Bakery, Fast Food): ");
        var category = Console.ReadLine() ?? "General";

        Console.Write("Enter Ingredients separated by comma (e.g., milk, sugar, cream): ");
        var ingInput = Console.ReadLine() ?? "";
        var ingredients = ingInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Console.Write("Rating (1 to 5): ");
        _ = int.TryParse(Console.ReadLine(), out var rating);

        var foodResult = await tracker.LogFoodAsync(userId, name, category, ingredients, rating, DateTime.UtcNow, "Logged from CLI");
        if (foodResult.IsFailure)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Validation Failed: {foodResult.Error.Message}");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Food entry logged successfully! ID: {foodResult.Value}");
        Console.ResetColor();

        Console.Write("\nDid you experience a symptom afterwards? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
        {
            Console.Write("Symptom Type (e.g., Heartburn, Acne, Bloating, Headache): ");
            var symptom = Console.ReadLine() ?? "";

            Console.Write("Severity (1 = mild, 10 = extreme): ");
            _ = int.TryParse(Console.ReadLine(), out var severity);

            var reactionResult = await tracker.LogReactionAsync(userId, symptom, severity, DateTime.UtcNow.AddHours(2), 60, "Occurred after meal");
            if (reactionResult.IsFailure)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Validation Failed: {reactionResult.Error.Message}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Reaction logged successfully! Re-running strategies will now reflect this new data.");
                Console.ResetColor();
            }
        }
    }

    private static async Task GenerateWebDashboardAsync(
        NutriSenseTrackerService tracker,
        HtmlChartReportFormatter htmlFormatter,
        Guid userId)
    {
        var foods = await tracker.GetFoodHistoryAsync(userId);
        var reactions = await tracker.GetReactionHistoryAsync(userId);
        var strategyResults = await tracker.AnalyzeTriggersAsync(userId, TimeSpan.FromHours(10));

        var html = htmlFormatter.GenerateHtmlDashboard(foods, reactions, strategyResults);
        var filePath = Path.Combine(AppContext.BaseDirectory, "nutrisense_dashboard.html");
        await File.WriteAllTextAsync(filePath, html);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ Interactive Dashboard generated successfully at:\n  {filePath}");
        Console.ResetColor();

        try
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo(filePath) { UseShellExecute = true }
            };
            p.Start();
            Console.WriteLine("Opening dashboard in your default browser...");
        }
        catch
        {
            Console.WriteLine($"You can open the HTML file manually in any browser: {filePath}");
        }
    }
}
