# SolarScope - Backend Architecture & Mental Model Blueprint

Dokumen arsitektur backend SolarScope untuk panduan implementasi, integrasi antarmuka, dan kesiapan asesmen teknis (PBO).

---

## 1. Mental Model & Roadmap Pengerjaan
Pembangunan backend dibagi menjadi 5 tahapan berurutan:

```
[Step 1: Domain Models (Class Diagram Alignment)] 
         ↓
[Step 2: External Client (NASA API)] 
         ↓
[Step 3: Calculation Engine (Formula Solar)] 
         ↓
[Step 4: Persistence Layer (PostgreSQL / Supabase)] 
         ↓
[Step 5: Facade / Service Result (Titik Temu UI)]
```

| Tahapan | Komponen | Tanggung Jawab Utama | Konsep PBO yang Ditonjolkan |
| :--- | :--- | :--- | :--- |
| **Step 1** | **Domain Models** | Objek domain (`Simulation`, `Location`, dll.) | Encapsulation & Composition |
| **Step 2** | **NasaPowerClient** | Mengambil data radiasi matahari via HTTP | Single Responsibility & Abstraksi Eksternal |
| **Step 3** | **EnergyCalculator** | Menghitung estimasi energi bulanan & tahunan | Encapsulation, Inheritance, Polymorphism |
| **Step 4** | **ISimulationRepository** + **PostgresSimulationRepository** | Kontrak dan operasi CRUD riwayat ke PostgreSQL (Supabase) via Npgsql | Data Persistence, Repository Pattern, Abstraction |
| **Step 5** | **SimulationManager** | Orkestrator simulasi (`CalculateAsync`) dan perbandingan kapasitas (`CompareCapacities`) + Result Pattern | Facade Pattern & Graceful Error Handling |
| Pendukung | **GeoapifyReverseGeocodingClient** | Mengubah koordinat dari peta menjadi nama wilayah (opsional, tidak memengaruhi perhitungan) | Single Responsibility |

---

## 2. Struktur Proyek (Solution Structure)
Arsitektur decoupled 2-project:

```text
SolarScope/
├── docs/
│   ├── SolarScope_Project_Context.md
│   ├── backend-architecture.md
│   └── ...
├── src/
│   ├── SolarScope.Core/             <-- Backend (Class Library .NET 8)
│   │   ├── Models/
│   │   │   ├── Location.cs
│   │   │   ├── SystemParameter.cs
│   │   │   ├── MonthlyProduction.cs
│   │   │   ├── SimulationSummary.cs
│   │   │   ├── Simulation.cs
│   │   │   ├── CapacityEstimate.cs             <-- hasil per kapasitas (perbandingan)
│   │   │   └── ServiceResult.cs
│   │   ├── Services/
│   │   │   ├── NasaPowerClient.cs
│   │   │   ├── GeoapifyReverseGeocodingClient.cs  <-- nama wilayah dari koordinat peta
│   │   │   ├── EnergyCalculator.cs             <-- abstract, formula + alur 12 bulan
│   │   │   ├── ClimatologyEnergyCalculator.cs  <-- tahun tipikal (365 hari), dipakai aplikasi
│   │   │   ├── YearlyEnergyCalculator.cs       <-- tahun tertentu (ikut kabisat), contoh polymorphism
│   │   │   ├── ISimulationRepository.cs        <-- kontrak penyimpanan riwayat
│   │   │   ├── PostgresSimulationRepository.cs
│   │   │   └── SimulationManager.cs            <-- Facade untuk UI
│   │   └── SolarScope.Core.csproj
│   │
│   └── SolarScope.Desktop/          <-- Frontend Desktop (WinForms .NET 9)
│       ├── AppController.cs         <-- composition root: membuat HttpClient, SimulationManager, repository
│       ├── AppSettings.cs           <-- baca solarscope.settings.json, fallback env var
│       └── UnavailableSimulationRepository.cs  <-- pengganti repository saat koneksi database belum diatur
├── tests/
│   ├── SolarScope.Core.Verification/     <-- Verifikasi kalkulator, NASA POWER, Geoapify, SimulationManager, database
│   └── SolarScope.Desktop.Verification/  <-- Verifikasi alur UI
├── tools/
│   └── SolarScope.ConnectionCheck/       <-- Cek koneksi database Supabase
├── AGENTS.md
└── README.md
```

### Mengapa struktur ini dipilih? (Alasan untuk Kuis/Ujian Dosen):
1. **Separation of Concerns (SoC):** Logika backend tidak bercampur dengan kode tombol atau form UI.
2. **Loose Coupling:** Jika UI berganti dari Windows Forms ke WPF, Web, atau CLI, seluruh kode `SolarScope.Core` tidak perlu diubah satu baris pun.
3. **Testability:** Komponen backend dapat diuji secara terisolasi tanpa perlu menjalankan antarmuka grafis.

---

## 3. Spesifikasi Teknis Komponen

### A. Formula Kalkulasi Energi
```text
E_bulan (kWh) = P_peak (kWp) × G_harian (kWh/m²/hari) × Hari_dalam_bulan × PR
E_tahun (kWh) = Total dari 12 bulan E_bulan
```
- `P_peak`: Kapasitas sistem panel surya yang diinput pengguna (kWp).
- `G_harian`: Radiasi matahari harian rata-rata dari parameter NASA POWER `ALLSKY_SFC_SW_DWN`.
- `Hari_dalam_bulan`: Jumlah hari sesuai bulan (Januari: 31, Februari: 28, dst.).
- `PR` (Performance Ratio): Faktor efisiensi sistem (default = `0.75`).

### B. Sumber Data Eksternal (NASA POWER Climatology)
- **Endpoint:**
  `https://power.larc.nasa.gov/api/temporal/climatology/point?parameters=ALLSKY_SFC_SW_DWN&community=RE&longitude={lon}&latitude={lat}&format=JSON`
- **Autentikasi:** Tanpa API Key (bebas akses, publik, reliabel).
- **Output:** Nilai radiasi bulanan (`JAN` s/d `DEC`) dan tahunan (`ANN`).
- **Implementasi (`NasaPowerClient.GetMonthlyRadiationAsync`):** memvalidasi koordinat sebelum request, memakai `CultureInfo.InvariantCulture` agar desimal tetap memakai titik, timeout 15 detik, dan menolak respons dengan satuan selain `kW-hr/m^2/day`, bulan hilang, nilai negatif (fill value `-999`), atau JSON rusak. Hasilnya 12 nilai Januari sampai Desember dalam `ServiceResult<IReadOnlyList<double>>`.
- **Cache di memori:** hasil yang berhasil disimpan per URL request (koordinat yang sama) selama aplikasi berjalan. Simulasi ulang di lokasi yang sama tidak memanggil NASA POWER lagi sehingga lebih cepat dan tetap jalan saat koneksi lambat. Respons gagal tidak disimpan, jadi percobaan berikutnya tetap memanggil API. Data climatology adalah rata-rata jangka panjang, sehingga nilainya tidak berubah selama sesi. Trade-off: cache hilang saat aplikasi ditutup dan tidak dibagi antarpengguna.
- **Uji langsung (opsional):** `dotnet run --project tests/SolarScope.Core.Verification -- --nasa-live` memanggil API asli untuk Yogyakarta.

### B2. Reverse Geocoding (Geoapify, pendukung peta)
- **Kelas:** `GeoapifyReverseGeocodingClient.GetRegionAsync(lat, lon)` mengubah koordinat hasil pilihan peta menjadi nama wilayah, misalnya `Kecamatan, Kota`.
- **Endpoint:** `https://api.geoapify.com/v1/geocode/reverse?lat={lat}&lon={lon}&format=json&lang=id&limit=1&apiKey={key}`. Tile peta di UI memakai `https://maps.geoapify.com/v1/tile/osm-carto`.
- **API key:** dibaca oleh `AppSettings` dari `GeoapifyApiKey` di `solarscope.settings.json`, atau dari environment variable `SOLARSCOPE_GEOAPIFY_API_KEY` bila kosong. Jika keduanya kosong, tombol peta menampilkan pemberitahuan dan pengguna mengisi koordinat secara manual.
- **Peran:** hanya membantu pengisian nama lokasi. Kegagalan Geoapify tidak menghentikan simulasi karena perhitungan tetap memakai koordinat dan data NASA POWER.

### B3. Facade `SimulationManager`
- **`CalculateAsync(name, location, parameter)`:** menyalin dan merapikan input (trim), memvalidasi `Simulation`, mengambil radiasi dari `NasaPowerClient`, lalu menghitung dengan `ClimatologyEnergyCalculator`. Input tidak valid ditolak sebelum request ke NASA POWER.
- **`CompareCapacities(source, alternatives)`:** menghitung ulang 1 atau 2 kapasitas alternatif (0,01 sampai 10.000 kWp, maksimal dua desimal, tidak boleh sama) memakai radiasi bulanan dari simulasi acuan, sehingga tidak perlu memanggil API lagi. Hasilnya daftar `CapacityEstimate`.

### C. Database PostgreSQL (Supabase)
- **Server:** PostgreSQL 17 yang dikelola Supabase (Free plan), diakses melalui Supavisor pooler (`aws-0-ap-northeast-1.pooler.supabase.com:5432`, SSL `VerifyFull`).
- **Library:** `Npgsql` (driver ADO.NET resmi PostgreSQL untuk .NET), tanpa ORM.
- **Connection string:** dibaca oleh `AppSettings` dari `DatabaseConnection` di `solarscope.settings.json` (folder `.exe`), atau dari environment variable `SOLARSCOPE_DB_CONNECTION` bila kosong. Path `Root Certificate` yang relatif di-resolve terhadap folder aplikasi. Berkas ini hanya dibuat oleh `installer/build.ps1` ke folder `dist/` dan tidak pernah masuk repository. Skema siap jalan ada di `database/schema.sql`.
- **Tabel:** `simulations` (induk) dan `monthly_productions` (12 baris per simulasi). Detail kolom ada di `docs/diagrams/erd/README.md`.
- **Row Level Security:** aktif pada kedua tabel. Aplikasi terhubung sebagai role database melalui connection string, bukan melalui API publik Supabase.
- **Cek koneksi:** `dotnet run --project tools/SolarScope.ConnectionCheck` menampilkan jumlah baris kedua tabel.
- **Catatan operasional:** project Free plan otomatis di-pause setelah sekitar 7 hari tidak aktif. Gejalanya error `XX000 ... tenant/user ... not found`. Pulihkan melalui **Restore project** di dashboard Supabase.
- **Kontrak `ISimulationRepository`:** UI (`AppController`) bergantung pada interface ini, bukan langsung pada kelas PostgreSQL. Implementasinya `PostgresSimulationRepository` (produksi), `UnavailableSimulationRepository` (connection string kosong atau tidak valid, sehingga aplikasi tetap terbuka dan menampilkan pesan), serta `TestSimulationRepository` (verifikasi UI tanpa database).
- **Operasi repository (`PostgresSimulationRepository`, semua mengembalikan `ServiceResult<T>`):**
  - `InsertSimulationAsync(Simulation sim)`: Menyimpan riwayat simulasi baru beserta rincian 12 bulan dalam satu transaksi.
  - `GetAllSimulationsAsync()`: Mengambil daftar ringkasan riwayat (tanpa rincian bulanan) untuk ditampilkan di tabel UI.
  - `GetSimulationByIdAsync(int id)`: Membuka detail hasil simulasi tertentu beserta 12 bulan.
  - `DeleteSimulationAsync(int id)`: Menghapus satu riwayat simulasi (rincian bulanan ikut terhapus melalui `ON DELETE CASCADE`).
- **Verifikasi:** `dotnet run --project tests/SolarScope.Core.Verification` menjalankan simpan, baca, dan hapus terhadap Supabase. Data uji dihapus kembali, dan bagian ini dilewati jika `SOLARSCOPE_DB_CONNECTION` belum diisi.

### D. Skema Tabel Database (PostgreSQL)
Sesuai skema yang aktif di Supabase:
```sql
CREATE TABLE simulations (
    simulation_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    scenario_name text NOT NULL CHECK (btrim(scenario_name) <> ''),
    created_at timestamptz NOT NULL DEFAULT now(),
    latitude double precision NOT NULL CHECK (latitude BETWEEN -90 AND 90),
    longitude double precision NOT NULL CHECK (longitude BETWEEN -180 AND 180),
    location_name text,
    system_capacity_kwp double precision NOT NULL CHECK (system_capacity_kwp > 0),
    performance_ratio double precision NOT NULL DEFAULT 0.75 CHECK (performance_ratio BETWEEN 0.01 AND 1),
    data_source text NOT NULL DEFAULT 'NASA POWER Climatology',
    annual_average_radiation double precision NOT NULL CHECK (annual_average_radiation >= 0),
    total_annual_energy_kwh double precision NOT NULL CHECK (total_annual_energy_kwh >= 0),
    peak_production_month text NOT NULL,
    lowest_production_month text NOT NULL
);

CREATE TABLE monthly_productions (
    simulation_id integer NOT NULL REFERENCES simulations(simulation_id) ON DELETE CASCADE,
    month_index integer NOT NULL CHECK (month_index BETWEEN 1 AND 12),
    month_name text NOT NULL,
    solar_radiation_daily_average double precision NOT NULL CHECK (solar_radiation_daily_average >= 0),
    estimated_energy_kwh double precision NOT NULL CHECK (estimated_energy_kwh >= 0),
    PRIMARY KEY (simulation_id, month_index)
);
```

### E. Error Handling (Result Pattern)
Backend tidak membiarkan exception lolos langsung ke antarmuka pengguna agar aplikasi tidak pernah crash di hadapan dosen penguji. Semua proses dibungkus oleh `ServiceResult<T>`:
```csharp
public class ServiceResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static ServiceResult<T> Ok(T data) => new() { IsSuccess = true, Data = data };
    public static ServiceResult<T> Fail(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
```
