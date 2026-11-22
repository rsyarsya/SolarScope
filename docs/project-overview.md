# SolarScope Project Overview

## Ringkasan

SolarScope adalah aplikasi desktop Windows berbasis C# untuk memberikan estimasi awal potensi produksi energi panel surya.

## Permasalahan

Calon pengguna panel surya sulit memperoleh gambaran sederhana mengenai energi yang mungkin dihasilkan pada lokasi dan kapasitas tertentu. Data radiasi matahari tersedia, tetapi bentuknya teknis dan perhitungan manual membutuhkan pemahaman satuan dan parameter.

## Solusi

Pengguna memasukkan nama skenario, lokasi (koordinat manual atau pilih di peta), dan kapasitas sistem (kWp). Aplikasi mengambil radiasi matahari bulanan dari NASA POWER, menghitung estimasi energi tiap bulan dan total tahunan, menampilkan bulan tertinggi dan terendah, lalu menyimpan hasil ke PostgreSQL (Supabase) agar dapat dibuka kembali, dibandingkan dengan kapasitas lain, atau dihapus.

## Target Pengguna

- Masyarakat umum
- Institusi pendidikan
- Usaha kecil

## Fitur Inti MVP

- Membuat simulasi
- Memasukkan lokasi dan parameter sistem
- Mengambil data radiasi dari API eksternal
- Menghitung estimasi bulanan dan tahunan
- Menyimpan dan membuka riwayat simulasi

## Di Luar Scope

- Monitoring real-time
- Integrasi IoT atau inverter
- Desain instalasi PLTS profesional
- Jaminan hasil produksi aktual

## Keputusan yang Sudah Dikunci

- Nama aplikasi: SolarScope
- Bentuk produk: aplikasi desktop Windows
- Bahasa pemrograman: C#
- Orientasi: simulasi dan edukasi awal

## Keputusan Teknis yang Sudah Diterapkan

Keputusan berikut sudah diimplementasikan dan digabungkan ke `main`. Rinciannya ada di [backend-architecture.md](backend-architecture.md).

- Framework antarmuka: Windows Forms (.NET 9)
- Arsitektur: dua project, `SolarScope.Core` (backend, .NET 8) dan `SolarScope.Desktop` (UI)
- Formula: `E_bulan = kWp × radiasi harian × hari dalam bulan × PR`, dengan PR default `0.75`
- API radiasi: NASA POWER Climatology, parameter `ALLSKY_SFC_SW_DWN`
- API pendukung peta: Geoapify (reverse geocoding dan tile peta, opsional)
- Database: PostgreSQL di Supabase, tabel `simulations` dan `monthly_productions`

## Keputusan yang Masih Terbuka

- Input performance ratio oleh pengguna (saat ini tetap `0.75`)
- Grafik produksi bulanan
- Fitur tambahan di luar alur utama