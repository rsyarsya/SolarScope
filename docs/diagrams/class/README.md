# Class Diagram SolarScope

## Class Diagram Sesuai Implementasi

Diagram ini mengikuti kode di `src/SolarScope.Core` dan menjadi acuan utama. GitHub merender blok Mermaid secara langsung; untuk mengedit, ubah teks di bawah.

```mermaid
classDiagram
    direction LR

    class Simulation {
        +int SimulationId
        +string ScenarioName
        +DateTime CreatedAt
        +Location Location
        +SystemParameter SystemParameter
        +SimulationSummary? Summary
        +Validate() (bool, string?)
    }
    class Location {
        +double Latitude
        +double Longitude
        +string LocationName
        +Validate() (bool, string?)
    }
    class SystemParameter {
        +double SystemCapacityKwp
        +double PerformanceRatio = 0.75
        +Validate() (bool, string?)
    }
    class SimulationSummary {
        +double TotalAnnualEnergyKwh
        +string PeakProductionMonth
        +string LowestProductionMonth
        +List~MonthlyProduction~ MonthlyProductions
    }
    class MonthlyProduction {
        +int MonthIndex
        +string MonthName
        +double SolarRadiationDailyAverage
        +double EstimatedEnergyKwh
    }
    class CapacityEstimate {
        <<record>>
        +double CapacityKwp
        +SimulationSummary Summary
    }
    class ServiceResult~T~ {
        +bool IsSuccess
        +T? Data
        +string? ErrorMessage
        +Ok(T data)$ ServiceResult~T~
        +Fail(string err)$ ServiceResult~T~
    }

    class NasaPowerClient {
        -HttpClient httpClient
        +GetMonthlyRadiationAsync(Location, CancellationToken) Task~ServiceResult~IReadOnlyList~double~~~
    }
    class GeoapifyReverseGeocodingClient {
        -HttpClient httpClient
        -string apiKey
        +GetRegionAsync(double, double, CancellationToken) Task~ServiceResult~string~~
    }
    class EnergyCalculator {
        <<abstract>>
        +Calculate(SystemParameter, IReadOnlyList~double~) ServiceResult~SimulationSummary~
        #GetDaysInMonth(int month)* int
    }
    class ClimatologyEnergyCalculator {
        #GetDaysInMonth(int month) int
    }
    class YearlyEnergyCalculator {
        +int Year
        #GetDaysInMonth(int month) int
    }
    class SimulationManager {
        -NasaPowerClient nasaPower
        -ClimatologyEnergyCalculator calculator
        +CalculateAsync(string, Location, SystemParameter, CancellationToken) Task~ServiceResult~Simulation~~
        +CompareCapacities(Simulation, IReadOnlyList~decimal~) ServiceResult~IReadOnlyList~CapacityEstimate~~
    }
    class ISimulationRepository {
        <<interface>>
        +InsertSimulationAsync(Simulation) Task~ServiceResult~int~~
        +GetAllSimulationsAsync() Task~ServiceResult~List~Simulation~~~
        +GetSimulationByIdAsync(int) Task~ServiceResult~Simulation~~
        +DeleteSimulationAsync(int) Task~ServiceResult~bool~~
    }
    class PostgresSimulationRepository {
        +FromEnvironment()$ PostgresSimulationRepository
    }

    Simulation *-- Location
    Simulation *-- SystemParameter
    Simulation o-- SimulationSummary
    SimulationSummary *-- "12" MonthlyProduction
    CapacityEstimate --> SimulationSummary

    EnergyCalculator <|-- ClimatologyEnergyCalculator
    EnergyCalculator <|-- YearlyEnergyCalculator
    ISimulationRepository <|.. PostgresSimulationRepository

    SimulationManager --> NasaPowerClient
    SimulationManager --> ClimatologyEnergyCalculator
    SimulationManager ..> Simulation
    SimulationManager ..> CapacityEstimate
    PostgresSimulationRepository ..> Simulation
    EnergyCalculator ..> SimulationSummary
```

Catatan:

- `Simulation` memiliki `Location` dan `SystemParameter` secara komposisi; `Summary` bernilai `null` sampai perhitungan selesai.
- `EnergyCalculator` memakai Template Method: alur 12 bulan sama, sedangkan jumlah hari per bulan ditentukan subclass.
- Semua operasi yang dapat gagal mengembalikan `ServiceResult<T>` sehingga UI tidak menerima exception mentah.
- Implementasi `ISimulationRepository` lain di project Desktop (`UnavailableSimulationRepository`) dan test (`TestSimulationRepository`) tidak digambar.

## Arsip Rancangan Minggu 2

Gambar berikut adalah rancangan awal sebelum implementasi dan dipertahankan sebagai arsip.

### Versi sederhana

![Class diagram sederhana](class-diagram-simple.png)

Menampilkan `Simulation`, `Location`, `SystemParameter`, `SolarRadiationData`, `SimulationSummary`, dan `MonthlyProduction`.

### Versi dengan method

![Class diagram dengan method](class-diagram-with-methods.jpeg)

Menampilkan `Simulasi`, `Lokasi`, `SistemPanelSurya`, `HasilEstimasi`, `NasaPowerApiClient`, `SolarRadianceData`, `AnalyticsEngine`, dan `RiwayatSimulasi`.

### Pemetaan rancangan ke implementasi

| Rancangan Minggu 2 | Implementasi |
| :--- | :--- |
| `Simulasi` (ID `string`) | `Simulation` (ID `int`, identity PostgreSQL) |
| `Lokasi` | `Location` |
| `SistemPanelSurya` | `SystemParameter` |
| `HasilEstimasi` | `SimulationSummary` + `MonthlyProduction` |
| `SolarRadiationData` / `SolarRadianceData` | Tidak dibuat sebagai class; radiasi disimpan di `MonthlyProduction.SolarRadiationDailyAverage` |
| `NasaPowerApiClient` | `NasaPowerClient` |
| `AnalyticsEngine` | `EnergyCalculator` dan turunannya |
| `RiwayatSimulasi` | `ISimulationRepository` / `PostgresSimulationRepository` |
