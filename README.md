# Tubes_Mawar — Tugas Besar IF25-21013 Strategi Algoritma

## Tim
**Nama Kelompok:** Mawar  
**Anggota:**
- (Nama Anggota 1)
- (Nama Anggota 2)
- (Nama Anggota 3)

---

## Struktur Repository

```
Tubes_Mawar/
├── src/
│   ├── main-bot/
│   │   └── CircleBot/          ← Bot utama (direkomendasikan untuk kompetisi)
│   └── alternative-bots/
│       ├── alt-bot-1/ZigZagBot/
│       ├── alt-bot-2/CenterBot/
│       └── alt-bot-3/PatrolBot/
├── doc/
│   └── laporan.pdf
└── README.md
```

---

## Deskripsi Singkat Strategi Greedy Setiap Bot

### 1. CircleBot (Bot Utama)
**Strategi:** Orbit Optimal  
Bot selalu memilih musuh dengan energi terendah sebagai target, lalu berputar mengelilingi musuh pada jarak optimal ±200 piksel. Setiap turn, bot secara greedy memutuskan daya tembak berdasarkan jarak (dekat=3.0, sedang=2.0, jauh=1.0) untuk memaksimalkan *Bullet Damage* sekaligus tetap hidup melalui gerakan melingkar yang sulit ditembak.

**Heuristik:** Jarak ke musuh → daya tembak optimal, arah gun langsung ke posisi musuh (head-on targeting), gerakan orbit untuk survival.

---

### 2. ZigZagBot (Alt-1)
**Strategi:** Zigzag Agresif ke Musuh Terlemah  
Bot melacak semua musuh yang pernah dipindai dalam dictionary, lalu secara greedy selalu memprioritaskan musuh dengan energi terendah. Bot bergerak zigzag saat mendekati untuk mempersulit bidikan musuh. Daya tembak ditingkatkan saat energi musuh mendekati nol untuk memastikan kill dan mendapatkan *Bullet Damage Bonus*.

**Heuristik:** Energi musuh → prioritas target, gerakan zigzag untuk evasion.

---

### 3. CenterBot (Alt-2)
**Strategi:** Dominasi Titik Tengah Arena  
Bot secara greedy selalu bergerak menuju pusat arena (posisi paling strategis — jarak rata-rata ke semua musuh minimal). Setelah di tengah, radar berputar dan tembakan diarahkan ke musuh terdekat. Daya tembak disesuaikan jarak dan sisa energi musuh.

**Heuristik:** Jarak ke pusat arena → keputusan bergerak, jarak ke musuh terdekat → daya tembak.

---

### 4. PatrolBot (Alt-3)
**Strategi:** Patroli Persegi + Manajemen Energi Greedy  
Bot berpatroli mengikuti jalur persegi di dalam arena (4 titik waypoint dengan margin dari dinding). Saat memindai musuh, daya tembak dipilih secara greedy: maksimalkan net energy gain (setiap energi yang ditembakkan menghasilkan 3x jika mengenai) namun dibatasi agar energi sendiri tidak turun di bawah 20 (survival threshold).

**Heuristik:** Net energy gain formula + jarak musuh → daya tembak, jaga energi sendiri > 20.

---

## Requirements

- **.NET 6.0 SDK** atau lebih baru  
  Download: https://dotnet.microsoft.com/en-us/download/dotnet/6.0
- **Game Engine Robocode Tank Royale** (versi yang sudah dimodifikasi asisten)
- OS: Windows, macOS, atau Linux

---

## Cara Menjalankan Bot

### Langkah 1: Clone atau ekstrak repository

```bash
git clone <url-repository>
cd Tubes_Mawar
```

### Langkah 2: Jalankan game engine (server)

Pastikan server Robocode Tank Royale sudah berjalan terlebih dahulu.

### Langkah 3: Jalankan bot

Masuk ke folder bot yang ingin dijalankan, lalu ketik `dotnet run`:

```bash
# Bot Utama (CircleBot)
cd src/main-bot/CircleBot
dotnet run

# Bot Alternatif 1 (ZigZagBot)
cd src/alternative-bots/alt-bot-1/ZigZagBot
dotnet run

# Bot Alternatif 2 (CenterBot)
cd src/alternative-bots/alt-bot-2/CenterBot
dotnet run

# Bot Alternatif 3 (PatrolBot)
cd src/alternative-bots/alt-bot-3/PatrolBot
dotnet run
```

### Catatan
- Setiap bot harus dijalankan dari dalam direktorinya masing-masing (bukan dari root project) agar file `.json` konfigurasi terbaca dengan benar.
- Bot akan otomatis terhubung ke server pada `ws://localhost:7654` secara default.
- Jika server di host/port berbeda, set environment variable: `SERVER_URL=ws://host:port`

---

## Compile / Build (opsional, tanpa langsung run)

```bash
cd src/main-bot/CircleBot
dotnet build
```

---

## Author

Tim Mawar — Institut Teknologi Sumatera  
Tugas Besar IF25-21013 Strategi Algoritma, Semester Genap 2026/2027
