# 1. Panduan Instalasi dan Konfigurasi

## Kebutuhan Sistem

- Windows 10 versi 2004 (build 19041) atau lebih baru, 64-bit.
- Ruang disk kosong sekitar 300 MB.
- Koneksi internet (data radiasi NASA POWER, peta Geoapify, dan database Supabase).
- Tidak perlu memasang .NET. Runtime sudah dikemas di dalam installer.
- Tidak perlu hak administrator.

## Instalasi (Cara Cepat)

1. Jalankan `SolarScope-Setup.exe`.
2. Jika muncul peringatan **Windows protected your PC** (SmartScreen), klik **More info** lalu **Run anyway**. Peringatan ini muncul karena installer belum memiliki tanda tangan digital berbayar.
3. Ikuti wizard: **Next**, centang **Create a desktop shortcut** bila perlu, lalu **Install**.
4. Biarkan **Launch SolarScope** tercentang, lalu klik **Finish**.

Aplikasi terpasang di `%LocalAppData%\Programs\SolarScope` dan dapat dibuka dari Start Menu atau shortcut desktop.

**Tidak ada konfigurasi tambahan.** Installer sudah membawa berkas `solarscope.settings.json` yang berisi koneksi database tim dan API key peta, beserta sertifikat SSL database di folder `certs`.

## Uninstall

Buka **Settings > Apps > Installed apps**, cari **SolarScope**, lalu pilih **Uninstall**. Bisa juga melalui Start Menu > SolarScope > **Uninstall SolarScope**.

## Berkas Konfigurasi

Lokasi: `%LocalAppData%\Programs\SolarScope\solarscope.settings.json`

```json
{
  "DatabaseConnection": "Host=<host>;Port=5432;Database=postgres;Username=<user>;Password=<password>;SSL Mode=VerifyFull;Root Certificate=certs\\supabase-root.crt",
  "GeoapifyApiKey": "<api key Geoapify>"
}
```

| Kunci | Fungsi | Jika kosong |
| --- | --- | --- |
| `DatabaseConnection` | Connection string PostgreSQL (format Npgsql) untuk Simpan dan Riwayat | Simulasi tetap bisa dihitung, tetapi Simpan dan Riwayat menampilkan pesan database belum diatur |
| `GeoapifyApiKey` | API key untuk peta dan nama wilayah | Tombol **Pilih di peta** menampilkan pesan, lokasi diisi manual |

Catatan:
- Path `Root Certificate` boleh relatif. Aplikasi mencarinya dari folder instalasi.
- Setelah mengubah berkas, tutup lalu buka kembali SolarScope.
- Untuk pengembang, nilai yang kosong otomatis diambil dari environment variable `SOLARSCOPE_DB_CONNECTION` dan `SOLARSCOPE_GEOAPIFY_API_KEY`.

## Memakai Database Sendiri (Opsional)

Langkah ini hanya diperlukan bila tidak ingin memakai database tim.

1. Buat akun dan project baru di [supabase.com](https://supabase.com) (paket Free cukup). Catat password database yang dibuat.
2. Buka **SQL Editor**, tempel isi `database/schema.sql`, lalu klik **Run**. Dua tabel terbentuk: `simulations` dan `monthly_productions`.
3. Buka **Connect** (atau **Project Settings > Database**), pilih **Session pooler**, lalu salin Host, Port, Database, dan User.
4. Unduh **SSL certificate** dari halaman yang sama, simpan sebagai `certs\supabase-root.crt` di folder instalasi (timpa berkas lama).
5. Ubah `DatabaseConnection` di `solarscope.settings.json`:

   ```text
   Host=<host pooler>;Port=5432;Database=postgres;Username=<user pooler>;Password=<password>;SSL Mode=VerifyFull;Root Certificate=certs\supabase-root.crt
   ```

6. Buka SolarScope, buat satu simulasi, klik **Simpan**, lalu cek di menu **Riwayat**.

## Memakai API Key Geoapify Sendiri (Opsional)

1. Daftar di [myprojects.geoapify.com](https://myprojects.geoapify.com), buat project baru, salin API key.
2. Isi `GeoapifyApiKey` di `solarscope.settings.json`, lalu buka ulang aplikasi.

Paket gratis Geoapify memberi kuota sekitar 3.000 request per hari, cukup untuk pemakaian dan pengujian.

## Pemecahan Masalah

| Gejala | Penyebab | Solusi |
| --- | --- | --- |
| "Data NASA POWER gagal dimuat" atau "Tidak dapat terhubung ke NASA POWER" | Tidak ada internet atau layanan NASA sedang lambat | Periksa koneksi, klik **Hitung estimasi** lagi |
| Riwayat gagal dimuat dengan kode `XX000` | Project Supabase Free di-pause setelah sekitar 7 hari tidak aktif | Login ke dashboard Supabase, klik **Restore project**, tunggu beberapa menit |
| "Koneksi database belum diatur" | `DatabaseConnection` kosong | Isi berkas konfigurasi sesuai bagian di atas |
| "Konfigurasi database tidak valid" | Format connection string salah | Periksa tanda `=` dan `;` pada connection string |
| "Peta belum dikonfigurasi" | `GeoapifyApiKey` kosong | Isi API key atau masukkan koordinat secara manual |
| Peta tampil kosong atau "Peta gagal dimuat" | API key salah atau kuota habis | Klik **Muat ulang peta** atau ganti API key |
| Installer diblokir SmartScreen | Installer tanpa tanda tangan digital | **More info > Run anyway** |
