using System.Text;
using System.Text.Json;
using NutriSense.Domain.Entities;

namespace NutriSense.Infrastructure.Reporting;

/// <summary>
/// Generates interactive HTML dashboard reports with Chart.js visualization.
/// </summary>
public sealed class HtmlChartReportFormatter
{
    public string GenerateHtmlDashboard(
        IReadOnlyList<FoodEntry> foods,
        IReadOnlyList<ReactionEntry> reactions,
        Dictionary<string, IReadOnlyList<TriggerInsight>> strategyResults)
    {
        var allInsights = strategyResults.Values
            .SelectMany(x => x)
            .DistinctBy(x => $"{x.FoodName}_{x.SymptomType}")
            .OrderByDescending(x => x.RiskScore)
            .Take(10)
            .ToList();

        var triggerLabels = JsonSerializer.Serialize(allInsights.Select(i => $"{i.FoodName} → {i.SymptomType}"));
        var triggerData = JsonSerializer.Serialize(allInsights.Select(i => i.CorrelationPercentage));

        var symptomCounts = reactions.GroupBy(r => r.SymptomType)
            .Select(g => new { Symptom = g.Key, Count = g.Count() })
            .ToList();

        var symptomLabels = JsonSerializer.Serialize(symptomCounts.Select(s => s.Symptom));
        var symptomData = JsonSerializer.Serialize(symptomCounts.Select(s => s.Count));

        var sb = new StringBuilder();
        sb.Append($$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>NutriSense - Intolerance & Reaction Dashboard</title>
            <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
            <style>
                :root {
                    --bg-dark: #0f172a;
                    --card-bg: #1e293b;
                    --card-border: #334155;
                    --accent-emerald: #10b981;
                    --accent-rose: #f43f5e;
                    --accent-amber: #f59e0b;
                    --accent-sky: #0ea5e9;
                    --text-main: #f8fafc;
                    --text-muted: #94a3b8;
                }
                * { box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; }
                body { background: var(--bg-dark); color: var(--text-main); padding: 2rem; min-height: 100vh; }
                .container { max-width: 1200px; margin: 0 auto; }
                header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--card-border); padding-bottom: 1.5rem; margin-bottom: 2rem; }
                h1 { font-size: 1.8rem; font-weight: 700; color: #fff; }
                .grid-stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 1rem; margin-bottom: 2rem; }
                .stat-card { background: var(--card-bg); border: 1px solid var(--card-border); border-radius: 12px; padding: 1.25rem; }
                .stat-value { font-size: 2rem; font-weight: 700; color: var(--accent-emerald); margin-top: 0.5rem; }
                .grid-charts { display: grid; grid-template-columns: 2fr 1fr; gap: 1.5rem; margin-bottom: 2rem; }
                @media (max-width: 900px) { .grid-charts { grid-template-columns: 1fr; } }
                .chart-card { background: var(--card-bg); border: 1px solid var(--card-border); border-radius: 12px; padding: 1.5rem; }
                .chart-title { font-size: 1.1rem; font-weight: 600; margin-bottom: 1rem; display: flex; align-items: center; gap: 0.5rem; }
                table { width: 100%; border-collapse: collapse; margin-top: 1rem; font-size: 0.9rem; }
                th, td { text-align: left; padding: 0.75rem 1rem; border-bottom: 1px solid var(--card-border); }
                th { color: var(--text-muted); font-weight: 600; text-transform: uppercase; font-size: 0.75rem; }
                tr:hover { background: rgba(255, 255, 255, 0.03); }
                .tag-critical { color: var(--accent-rose); font-weight: bold; }
                .tag-high { color: var(--accent-amber); font-weight: bold; }
            </style>
        </head>
        <body>
            <div class="container">
                <header>
                    <div>
                        <h1>NutriSense &bull; Intolerance & Reaction Tracker</h1>
                        <p style="color: var(--text-muted); font-size: 0.9rem;">Automated Time-Window Pattern Analysis for Food Sensitivities</p>
                    </div>
                </header>

                <div class="grid-stats">
                    <div class="stat-card">
                        <div style="color: var(--text-muted); font-size: 0.85rem;">Total Meals Logged</div>
                        <div class="stat-value" style="color: var(--accent-sky);">{{foods.Count}}</div>
                    </div>
                    <div class="stat-card">
                        <div style="color: var(--text-muted); font-size: 0.85rem;">Adverse Reactions Recorded</div>
                        <div class="stat-value" style="color: var(--accent-rose);">{{reactions.Count}}</div>
                    </div>
                    <div class="stat-card">
                        <div style="color: var(--text-muted); font-size: 0.85rem;">Identified Trigger Suspects</div>
                        <div class="stat-value" style="color: var(--accent-amber);">{{allInsights.Count}}</div>
                    </div>
                    <div class="stat-card">
                        <div style="color: var(--text-muted); font-size: 0.85rem;">Analysis Time Window</div>
                        <div class="stat-value" style="font-size: 1.5rem; color: var(--accent-emerald);">2 - 12 Hours</div>
                    </div>
                </div>

                <div class="grid-charts">
                    <div class="chart-card">
                        <div class="chart-title">📊 Top Suspect Triggers vs Symptom Correlation (%)</div>
                        <canvas id="triggerChart" height="130"></canvas>
                    </div>
                    <div class="chart-card">
                        <div class="chart-title">🍩 Adverse Symptoms Distribution</div>
                        <canvas id="symptomChart"></canvas>
                    </div>
                </div>

                <div class="chart-card">
                    <div class="chart-title">📋 Detailed Intolerance Correlation Insights</div>
                    <table>
                        <thead>
                            <tr>
                                <th>Food / Ingredient</th>
                                <th>Correlated Reaction</th>
                                <th>Preceded Count</th>
                                <th>Correlation Rate</th>
                                <th>Risk Score</th>
                                <th>Severity Tier</th>
                                <th>Recommendation</th>
                            </tr>
                        </thead>
                        <tbody>
        """);

        foreach (var item in allInsights)
        {
            var tierClass = item.SeverityLevel is "Critical" ? "tag-critical" : item.SeverityLevel is "High" ? "tag-high" : "";
            sb.Append($$"""
                            <tr>
                                <td><strong>{{item.FoodName}}</strong></td>
                                <td>{{item.SymptomType}}</td>
                                <td>{{item.TimesConsumedBeforeSymptom}} / {{item.TotalTimesConsumed}}</td>
                                <td>{{item.CorrelationPercentage:F0}}%</td>
                                <td>{{item.RiskScore:F1}}/10</td>
                                <td class="{{tierClass}}">{{item.SeverityLevel}}</td>
                                <td style="color: var(--text-muted); font-size: 0.85rem;">{{item.SummaryRecommendation}}</td>
                            </tr>
            """);
        }

        sb.Append($$"""
                        </tbody>
                    </table>
                </div>
            </div>

            <script>
                // Trigger Correlation Bar Chart
                const ctxTrigger = document.getElementById('triggerChart').getContext('2d');
                new Chart(ctxTrigger, {
                    type: 'bar',
                    data: {
                        labels: {{triggerLabels}},
                        datasets: [{
                            label: 'Correlation Probability (%)',
                            data: {{triggerData}},
                            backgroundColor: 'rgba(244, 63, 94, 0.75)',
                            borderColor: '#f43f5e',
                            borderWidth: 1,
                            borderRadius: 6
                        }]
                    },
                    options: {
                        responsive: true,
                        plugins: { legend: { display: false } },
                        scales: {
                            y: { beginAtZero: true, max: 100, grid: { color: '#334155' }, ticks: { color: '#94a3b8' } },
                            x: { grid: { display: false }, ticks: { color: '#94a3b8', font: { size: 11 } } }
                        }
                    }
                });

                // Symptoms Donut Chart
                const ctxSymptom = document.getElementById('symptomChart').getContext('2d');
                new Chart(ctxSymptom, {
                    type: 'doughnut',
                    data: {
                        labels: {{symptomLabels}},
                        datasets: [{
                            data: {{symptomData}},
                            backgroundColor: ['#f43f5e', '#f59e0b', '#0ea5e9', '#8b5cf6', '#10b981'],
                            borderWidth: 0
                        }]
                    },
                    options: {
                        responsive: true,
                        plugins: {
                            legend: { position: 'bottom', labels: { color: '#cbd5e1', boxWidth: 12, font: { size: 11 } } }
                        }
                    }
                });
            </script>
        </body>
        </html>
        """);

        return sb.ToString();
    }
}
