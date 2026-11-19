# SolarScope
SolarScope adalah aplikasi desktop Windows berbasis C# untuk membantu pengguna nonspesialis memperoleh estimasi awal produksi energi panel surya berdasarkan lokasi dan kapasitas sistem. Aplikasi menggunakan data radiasi matahari dari API eksternal, menyajikan estimasi energi bulanan dan tahunan, serta menyimpan riwayat simulasi dalam database. SolarScope bukan aplikasi monitoring, bukan alat desain instalasi profesional, dan tidak menjamin hasil produksi aktual. Prioritas proyek adalah membangun alur end-to-end yang sederhana, benar, stabil, dan dapat didemonstrasikan. Arsitektur serta detail teknis belum dikunci dan tidak boleh diasumsikan tanpa keputusan tim.

## Anggota Kelompok

- **Anggota 1:** RASYADWA ARSYA IRNANTYANTO - 24/534714/TK/59283
- **Anggota 2:** RADITYA AZHAR ANANTA - 24/539913/TK/59881
- **Anggota 3:** GHAISAN RIFQI KAMIEL - 24/540091/TK/59899

## Dokumentasi

- [Gambaran Proyek](docs/project-overview.md)
- [Pembagian Peran](docs/team-roles.md)
- [Git Workflow](docs/git-workflow.md)
- [Diagram Sistem](docs/diagrams/README.md)

## Aplikasi desktop resmi

Enam layar Windows Forms resmi berada di `src/SolarScope.Desktop/`: Beranda, Input Simulasi, Hasil Estimasi, Riwayat, Perbandingan, dan Panduan. UI menggunakan C# dan .NET 9, dengan file Designer yang dapat diedit kembali.

Buka `SolarScope.sln` melalui Visual Studio 2022 dengan workload .NET desktop dan .NET 9 SDK. Aplikasi menargetkan Windows 10 versi 2004 (build 19041) atau lebih baru. Tetapkan `SolarScope.Desktop` sebagai startup project, lalu jalankan dengan Ctrl+F5.

- Build seluruh solution: `dotnet build SolarScope.sln`
- Jalankan frontend: `dotnet run --project src/SolarScope.Desktop/SolarScope.Desktop.csproj`
- Verifikasi: `dotnet run --project tests/SolarScope.Desktop.Verification/Verification.csproj`
- Backend domain models: `src/SolarScope.Core/` (.NET 8).
- Latihan login: `examples/LabLogin/`.

Di menu Simulasi, isi nama, nama lokasi, koordinat desimal, dan kapasitas, lalu pilih **Hitung Estimasi**. Contoh koordinat Yogyakarta adalah latitude `-7,7956` dan longitude `110,3695`. Aplikasi membutuhkan internet untuk mengambil data klimatologi NASA POWER.

Untuk memakai **Pilih di peta**, atur `SOLARSCOPE_GEOAPIFY_API_KEY` di lingkungan lokal sebelum menjalankan
aplikasi. Satu key [Geoapify](https://www.geoapify.com/) dipakai untuk tile peta dan pencarian nama wilayah.
Geoapify menerima koordinat pin saat kamu memilih **Gunakan lokasi ini**. Nama wilayah dan koordinat
tetap bisa diedit di form. Simpan key di luar Git.

Simpan dan Riwayat menggunakan PostgreSQL. Sebelum menjalankan SolarScope, set environment variable
`SOLARSCOPE_DB_CONNECTION` dengan koneksi PostgreSQL yang diberikan pengelola Supabase. Akun koneksi
perlu izin baca, simpan, dan hapus pada skema SolarScope. Lihat [konfigurasi dan skema](docs/backend-architecture.md).
Kredensial disimpan di luar Git. Perhitungan tetap tersedia saat database belum dikonfigurasi.
Layar Perbandingan menampilkan contoh kapasitas tetap.

Untuk menguji alur WinForms dengan data NASA POWER langsung, jalankan `dotnet run --project tests/SolarScope.Desktop.Verification -- --nasa-live`.
