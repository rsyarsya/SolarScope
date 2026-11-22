# 3. Integrasi Web API

Sesuai Lab 10.1 (terhubung dengan REST services), SolarScope mengonsumsi dua REST API pihak ketiga melalui `HttpClient` dan `System.Text.Json` bawaan .NET, tanpa library tambahan.

| | NASA POWER | Geoapify |
| --- | --- | --- |
| Peran | **Data inti**: radiasi matahari bulanan untuk perhitungan energi | **Nilai tambah**: pilih lokasi di peta dan nama wilayah otomatis |
| Kelas C# | `NasaPowerClient` | `GeoapifyReverseGeocodingClient` (nama wilayah) dan `MapLocationDialog` (tile peta) |
| Autentikasi | Tanpa API key | API key (`GeoapifyApiKey`) |
| Format | JSON | JSON (reverse geocoding) dan PNG (tile peta) |
| Batas waktu | 15 detik | 5 detik |
| Jika gagal | Pesan kesalahan, simulasi bisa diulang | Lokasi tetap bisa diisi manual |

## A. NASA POWER (Data Inti)

**Endpoint**

```text
GET https://power.larc.nasa.gov/api/temporal/climatology/point
    ?parameters=ALLSKY_SFC_SW_DWN&community=RE
    &longitude=110.3695&latitude=-7.7956&format=JSON
```

| Parameter | Arti |
| --- | --- |
| `parameters=ALLSKY_SFC_SW_DWN` | Radiasi matahari harian di permukaan (kWh/m²/hari) |
| `community=RE` | Komunitas Renewable Energy (satuan sesuai energi terbarukan) |
| `latitude`, `longitude` | Koordinat derajat desimal, selalu memakai titik desimal |

**Contoh respons (dipotong)**

```json
{
  "properties": {
    "parameter": {
      "ALLSKY_SFC_SW_DWN": { "JAN": 4.79, "FEB": 4.71, "...": "...", "DEC": 4.76, "ANN": 5.16 }
    }
  },
  "parameters": { "ALLSKY_SFC_SW_DWN": { "units": "kW-hr/m^2/day" } }
}
```

**Alur di aplikasi**

```text
Form1 ─► SimulationManager.CalculateAsync ─► NasaPowerClient.GetMonthlyRadiationAsync
      ─► 12 nilai radiasi ─► ClimatologyEnergyCalculator ─► hasil 12 bulan + total tahunan
```

**Penanganan kesalahan** (semua dikembalikan sebagai `ServiceResult`, aplikasi tidak crash):

| Kondisi | Pesan ke pengguna |
| --- | --- |
| Koordinat di luar rentang | Ditolak sebelum request dikirim |
| HTTP selain 2xx | "NASA POWER mengembalikan status HTTP {kode}. Coba lagi nanti." |
| Tidak ada koneksi | "Tidak dapat terhubung ke NASA POWER. Periksa koneksi internet." |
| Lebih dari 15 detik | "Waktu tunggu NASA POWER habis. Coba lagi." |
| Satuan salah, bulan hilang, nilai `-999`, JSON rusak | "Data radiasi dari NASA POWER tidak valid." |

**Cache:** hasil yang berhasil disimpan di memori per koordinat selama aplikasi terbuka. Simulasi ulang di lokasi yang sama tidak memanggil API lagi.

## B. Geoapify (Nilai Tambah)

Tanpa Geoapify, pengguna harus mencari koordinat sendiri di aplikasi lain. Dengan Geoapify, pengguna cukup mengklik titik di peta.

**1. Tile peta** (gambar latar peta di dialog **Pilih lokasi**)

```text
GET https://maps.geoapify.com/v1/tile/osm-carto/{z}/{x}/{y}.png?apiKey={key}
```

**2. Reverse geocoding** (koordinat menjadi nama wilayah)

```text
GET https://api.geoapify.com/v1/geocode/reverse
    ?lat=-7.7956&lon=110.3695&format=json&lang=id&limit=1&apiKey={key}
```

Contoh respons (dipotong, nilai ilustrasi):

```json
{
  "results": [
    { "district": "Gondokusuman", "city": "Yogyakarta", "state": "Daerah Istimewa Yogyakarta", "country": "Indonesia" }
  ]
}
```

Aplikasi menyusun nama dengan urutan `district` atau `suburb`, lalu `city` atau `county`, menjadi misalnya "Gondokusuman, Yogyakarta". Jika keduanya kosong, dipakai `state` atau `country`.

**Penanganan kesalahan**

| Kondisi | Perilaku |
| --- | --- |
| API key belum diisi | "Peta belum dikonfigurasi. Isi lokasi secara manual atau isi GeoapifyApiKey di solarscope.settings.json." |
| Tile gagal dimuat | "Peta gagal dimuat." dengan tombol **Muat ulang peta** |
| Nama wilayah tidak ditemukan | "Nama wilayah belum ditemukan. Isi nama lokasi secara manual." Koordinat tetap terisi |

## Perbandingan dengan Lab 10.1

| Lab 10.1 (RajaOngkir) | SolarScope |
| --- | --- |
| Kelas `Ongkir` dengan method statis | Kelas `NasaPowerClient` dan `GeoapifyReverseGeocodingClient` dengan `HttpClient` yang di-inject |
| RestSharp + SimpleJson | `HttpClient` + `System.Text.Json` (bawaan .NET) |
| Request sinkron | Request `async`, UI tetap responsif |
| Error dicetak ke console | Error dikembalikan sebagai `ServiceResult` dan ditampilkan ke pengguna |
| API key ditulis di kode | API key dibaca dari `solarscope.settings.json`, tidak ada di source code |
