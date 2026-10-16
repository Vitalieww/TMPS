# TMPS Laboratory Work #1 — SOLID Principles
## Application: NutriSense (Food & Reaction Intolerance Tracker)

**Domain**: Food & Adverse Symptom/Reaction Tracking with Time-Window Correlation Analysis.  
**Tech Stack**: C# / .NET 10, Clean Architecture (4 Decoupled Projects), Chart.js Dashboard.

---

## 1. Architectural Solution Structure

Following clean architecture principles, the solution is partitioned into 4 distinct projects with strictly enforced dependency directions:

```
NutriSense/
├── NutriSense.Domain/          # Pure Domain Entities, Value Objects, Repository Interfaces, Error/Result
│   ├── Common/                 # Result<T>, Error
│   ├── Entities/               # FoodEntry, ReactionEntry, TriggerInsight
│   ├── Repositories/           # IFoodRepository, IReactionRepository
│   └── Strategies/             # IReactionAnalysisStrategy (OCP abstraction)
├── NutriSense.Application/     # Use Cases, Core Logic, Validation & Pluggable Strategies
│   ├── Validation/             # FoodEntryValidator, ReactionEntryValidator (SRP)
│   ├── Strategies/             # FrequencyAnalysisStrategy, SeverityWeightedStrategy, IngredientIntoleranceStrategy (OCP)
│   └── Services/               # CorrelationEngine, NutriSenseTrackerService (DIP)
├── NutriSense.Infrastructure/  # Concrete Storage & Visualization Presenters
│   ├── Persistence/            # InMemoryFoodRepository, InMemoryReactionRepository
│   └── Reporting/              # ConsoleReportFormatter, HtmlChartReportFormatter (Chart.js)
└── NutriSense.App/             # Composition Root & Interactive Demonstration CLI
    └── Program.cs              # DI Container Registration (DIP) & Menu
```

---

## 2. The 3 Core SOLID Principles Implemented

### Principle 1: Single Responsibility Principle (SRP)
> *"A class should have one, and only one, reason to change."*

#### Where in Code:
1. **Entities vs. Validation**:
   - [`FoodEntry.cs`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Domain/Entities/FoodEntry.cs): Encapsulates only domain state and internal invariants (rating between 1–5, UTC timestamps).
   - [`FoodEntryValidator.cs`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Application/Validation/FoodEntryValidator.cs): Solely responsible for validating input data before submission (length checks, empty strings, future date constraints).
2. **Persistence vs. Logic vs. Presentation**:
   - [`InMemoryFoodRepository.cs`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Infrastructure/Persistence/InMemoryFoodRepository.cs): Solely responsible for storing and querying records. It knows nothing about correlation math or charts.
   - [`CorrelationEngine.cs`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Application/Services/CorrelationEngine.cs): Solely responsible for orchestrating mathematical correlation algorithms.
   - [`HtmlChartReportFormatter.cs`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Infrastructure/Reporting/HtmlChartReportFormatter.cs) & [`ConsoleReportFormatter.cs`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Infrastructure/Reporting/ConsoleReportFormatter.cs): Solely responsible for rendering output (HTML Chart.js and terminal tables).

#### Teacher Defence Question:
* *"What happens if we switch from Console/HTML output to exporting PDF reports?"*  
  **Answer:** We only create or edit a formatter class in `Reporting/`. Not a single line of database code, validation logic, or correlation math will change.

---

### Principle 2: Open/Closed Principle (OCP)
> *"Software entities should be open for extension, but closed for modification."*

#### Where in Code:
We abstracted correlation algorithms behind the [`IReactionAnalysisStrategy`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Domain/Strategies/IReactionAnalysisStrategy.cs) interface:
```csharp
public interface IReactionAnalysisStrategy
{
    string StrategyName { get; }
    string Description { get; }
    IReadOnlyList<TriggerInsight> Analyze(
        IReadOnlyList<FoodEntry> foods,
        IReadOnlyList<ReactionEntry> reactions,
        TimeSpan timeWindow);
}
```

We provided three concrete strategies without touching the [`CorrelationEngine`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Application/Services/CorrelationEngine.cs):
1. **`FrequencyAnalysisStrategy`**: Measures raw occurrence percentage of symptoms within a time window (e.g. 2–12h).
2. **`SeverityWeightedStrategy`**: Calculates a composite index by multiplying frequency by reaction severity (1–10), so mild bloating doesn't mask an acute migraine.
3. **`IngredientIntoleranceStrategy`**: Inspects underlying ingredients (e.g., *dairy*, *gluten*, *caffeine*) across diverse meals (pizza, cheese, milk).

#### Teacher Defence Question:
* *"How would you add machine learning or AI sentiment correlation to this system?"*  
  **Answer:** We simply create a new class `AiClusteringStrategy : IReactionAnalysisStrategy` and register it in `Program.cs`. The `CorrelationEngine` will automatically run it without modifying any existing class.

---

### Principle 3: Dependency Inversion Principle (DIP)
> *"High-level modules should not depend on low-level modules. Both should depend on abstractions."*

#### Where in Code:
1. The high-level orchestrator [`NutriSenseTrackerService.cs`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Application/Services/NutriSenseTrackerService.cs) and [`CorrelationEngine.cs`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Application/Services/CorrelationEngine.cs) do **not** instantiate concrete classes (`new InMemoryFoodRepository()`).
2. Instead, they depend on domain abstractions:
   ```csharp
   public NutriSenseTrackerService(
       IFoodRepository foodRepository,
       IReactionRepository reactionRepository,
       FoodEntryValidator foodValidator,
       ReactionEntryValidator reactionValidator,
       CorrelationEngine correlationEngine)
   ```
3. In [`Program.cs`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.App/Program.cs), dependencies are resolved via the Microsoft Dependency Injection container:
   ```csharp
   services.AddSingleton<IFoodRepository, InMemoryFoodRepository>();
   services.AddSingleton<IReactionRepository, InMemoryReactionRepository>();
   services.AddSingleton<IReactionAnalysisStrategy, FrequencyAnalysisStrategy>();
   services.AddSingleton<IReactionAnalysisStrategy, SeverityWeightedStrategy>();
   services.AddSingleton<IReactionAnalysisStrategy, IngredientIntoleranceStrategy>();
   ```

#### Teacher Defence Question:
* *"Can we swap InMemory storage for a PostgreSQL or SQLite database?"*  
  **Answer:** Yes. We write a `PostgresFoodRepository : IFoodRepository` and change one line in `Program.cs`. The core business logic in `NutriSenseTrackerService` is completely decoupled from the database.

---

## 3. Bonus Principles Implemented

* **LSP (Liskov Substitution Principle)**: Any `IReactionAnalysisStrategy` can substitute another in `CorrelationEngine` without violating contracts, throwing unexpected exceptions, or altering system behavior.
* **ISP (Interface Segregation Principle)**: Instead of a bloated `ITrackerDatabase` interface with dozens of unrelated methods, interfaces are segregated into focused contracts: [`IFoodRepository`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Domain/Repositories/IFoodRepository.cs), [`IReactionRepository`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Domain/Repositories/IReactionRepository.cs), and [`IReportFormatter`](file:///c:/Users/vital/University_folder/year_3/sem_5/TMPS/TMPS/NutriSense.Infrastructure/Reporting/IReportFormatter.cs).

---

## 4. How to Run & Demonstrate for the Mentor/Teacher

In terminal:
```powershell
cd C:\Users\vital\University_folder\year_3\sem_5\TMPS\TMPS
dotnet run --project NutriSense.App
```

### Recommended 3-Minute Demo Flow:
1. **Option 1**: Show the pre-seeded realistic meal history (pizza, espresso, curry, wings) and reaction history (heartburn, acne, migraines).
2. **Option 2**: Run all correlation strategies — demonstrate how **Frequency**, **Severity-Weighted**, and **Ingredient-Level** analyses produce distinct insights (proving **OCP**).
3. **Option 3**: Log a new food item with invalid data (e.g. rating = 7 or empty name) to show **SRP validator** rejecting bad inputs cleanly.
4. **Option 4**: Generate the **Interactive HTML Dashboard** (`nutrisense_dashboard.html`), which opens in the browser with Chart.js charts!
5. **Option 5**: Display the built-in SOLID summary cheat sheet.
