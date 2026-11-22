# Panduan SolarScope

| | |
| --- | --- |
| Nama aplikasi | SolarScope |
| Versi | 1.0.0 |
| Bentuk | Aplikasi desktop Windows (WinForms, C#) |
| Berkas instalasi | `SolarScope-Setup.exe` |

## Ringkasan

SolarScope memberi estimasi awal potensi energi panel surya. Pengguna memasukkan nama simulasi, lokasi, dan kapasitas sistem (kWp). Aplikasi mengambil rata-rata radiasi matahari bulanan dari NASA POWER, menghitung estimasi energi tiap bulan dan total satu tahun, lalu menyimpan hasilnya ke database PostgreSQL (Supabase) agar dapat dibuka kembali, dibandingkan dengan kapasitas lain, atau dihapus.

Rumus estimasi:

```text
Energi bulanan (kWh) = kapasitas (kWp) × radiasi harian (kWh/m²/hari) × jumlah hari × performance ratio (0,75)
Energi tahunan (kWh) = jumlah energi 12 bulan
```

## Fitur

| Fitur | Keterangan |
| --- | --- |
| Simulasi | Input nama, lokasi (manual atau dari peta), dan kapasitas kWp |
| Hasil | Total tahunan, tabel 12 bulan, bulan tertinggi dan terendah |
| Riwayat | Simpan, buka kembali, dan hapus simulasi di database |
| Perbandingan | Bandingkan 1 atau 2 kapasitas alternatif memakai data radiasi yang sama |
| Peta (nilai tambah) | Pilih titik di peta, nama wilayah terisi otomatis |
| Panduan | Penjelasan koordinat, kWp dan kWh, serta batasan estimasi |

## Arsitektur Singkat

| Komponen | Project | Tanggung jawab | Terhubung ke |
| --- | --- | --- | --- |
| Form (Beranda, Simulasi, Hasil, Riwayat, Perbandingan, Panduan) | SolarScope.Desktop | Tampilan dan input pengguna | `SimulationManager`, `ISimulationRepository` |
| `SimulationManager` | SolarScope.Core | Mengatur alur simulasi dan perbandingan | `NasaPowerClient`, `EnergyCalculator` |
| `NasaPowerClient` | SolarScope.Core | Mengambil radiasi matahari bulanan | NASA POWER REST API |
| `GeoapifyReverseGeocodingClient` | SolarScope.Core | Mengubah koordinat menjadi nama wilayah | Geoapify REST API |
| `EnergyCalculator` | SolarScope.Core | Menghitung estimasi energi | Tidak ada (perhitungan lokal) |
| `PostgresSimulationRepository` | SolarScope.Core | Simpan, baca, dan hapus riwayat | PostgreSQL (Supabase) |

## Daftar Panduan

1. [Panduan Instalasi dan Konfigurasi](01-instalasi-konfigurasi.md)
2. [Panduan Pengguna](02-panduan-pengguna.md)
3. [Integrasi Web API](03-integrasi-web-api.md)

## Berkas Rilis

| Berkas | Isi |
| --- | --- |
| `SolarScope-Setup.exe` | Installer, dibuat dengan `installer/build.ps1`. Sudah berisi konfigurasi database dan peta |
| [`database/schema.sql`](../../database/schema.sql) | Skrip pembuatan tabel bila ingin memakai database sendiri |
