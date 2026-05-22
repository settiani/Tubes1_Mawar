// CircleBot - Bot Utama Tim Mawar
// Strategi Greedy: Orbit Optimal
//
// Heuristik: Selalu pilih musuh dengan jarak terdekat sebagai target,
// lalu berputar mengelilingi musuh tersebut pada jarak optimal (~200px).
// Setiap giliran, secara greedy memutuskan: 
//   - Daya tembak optimal berdasarkan jarak (dekat = daya besar, jauh = daya kecil)
//   - Arahkan gun langsung ke posisi musuh (head-on targeting)
//   - Gerak melingkar untuk menghindari tembakan balik
//
// Fungsi objektif: Maksimalkan Bullet Damage + Bullet Damage Bonus
//   dengan tetap hidup (Survival Score) melalui gerakan melingkar.

using System;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class CircleBot : Bot
{
    // Data musuh yang dipindai terakhir kali
    private double _targetX;
    private double _targetY;
    private double _targetEnergy;
    private int _targetId = -1;
    private bool _hasTarget = false;

    // Arah orbit: 1 = searah jarum jam, -1 = berlawanan
    private int _orbitDirection = 1;

    // Jarak orbit optimal
    private const double ORBIT_DISTANCE = 200.0;

    // Jarak maksimum yang masih layak untuk tembak berat
    private const double CLOSE_RANGE = 150.0;
    private const double MID_RANGE = 300.0;

    static void Main(string[] args)
    {
        new CircleBot().Start();
    }

    // Konstruktor: membaca file konfigurasi JSON
    CircleBot() : base(BotInfo.FromFile("CircleBot.json")) { }

    // Dipanggil saat ronde baru dimulai
    public override void Run()
    {
        // Atur warna badan bot (opsional, untuk identifikasi)
        // Reset target setiap ronde
        _hasTarget = false;
        _targetId = -1;
        _orbitDirection = 1;

        BodyColor = System.Drawing.Color.Green;
        TurretColor = System.Drawing.Color.Purple;
        RadarColor = System.Drawing.Color.Pink;
        BulletColor = System.Drawing.Color.Navy;
        ScanColor = System.Drawing.Color.Pink;
        TracksColor = System.Drawing.Color.Pink;
        GunColor = System.Drawing.Color.Red;

        // Loop utama selama bot masih berjalan
        while (IsRunning)
        {
            // Putar radar penuh jika belum punya target
            if (!_hasTarget)
            {
                // Greedy: scan 360 derajat untuk cari musuh
                TurnRadarRight(360);
            }
            else
            {
                // Greedy: orbit di sekitar musuh pada jarak optimal
                ExecuteOrbit();
            }
        }
    }

    // Eksekusi gerakan orbit mengelilingi musuh
    private void ExecuteOrbit()
    {
        // Hitung bearing menuju musuh (sudut absolut ke musuh)
        double bearingToTarget = BearingTo(_targetX, _targetY);
        double distanceToTarget = DistanceTo(_targetX, _targetY);

        // Greedy: putuskan daya tembak berdasarkan jarak
        // Dekat = tembak kuat, jauh = tembak ringan (hemat energi)
        double firePower = CalculateOptimalFirepower(distanceToTarget);

        // Arahkan gun ke musuh
        double gunBearing = GunBearingTo(_targetX, _targetY);
        TurnGunRight(gunBearing);

        // Tembak jika gun sudah mengarah dan meriam tidak panas
        if (Math.Abs(GunTurnRemaining) < 5 && GunHeat == 0)
        {
            Fire(firePower);
        }

        // Greedy: tentukan gerakan orbit
        // Jika terlalu dekat -> mundur sedikit sambil orbit
        // Jika terlalu jauh -> maju mendekati
        if (distanceToTarget < ORBIT_DISTANCE - 50)
        {
            // Terlalu dekat, orbit sambil menjauh sedikit
            TurnRight(bearingToTarget + (90 * _orbitDirection));
            Back(30);
        }
        else if (distanceToTarget > ORBIT_DISTANCE + 100)
        {
            // Terlalu jauh, dekati musuh
            TurnRight(bearingToTarget);
            Forward(50);
        }
        else
        {
            // Jarak optimal, berputar mengelilingi musuh
            TurnRight(bearingToTarget + (90 * _orbitDirection));
            Forward(60);
        }

        // Putar radar agar tetap melacak musuh
        TurnRadarRight(RadarBearingTo(_targetX, _targetY) * 2);
    }

    // Greedy heuristik: hitung daya tembak optimal berdasarkan jarak
    // Semakin dekat = semakin besar daya tembak
    private double CalculateOptimalFirepower(double distance)
    {
        if (distance < CLOSE_RANGE)
        {
            // Dekat: tembak maksimum (3.0) untuk maksimalkan damage
            return 3.0;
        }
        else if (distance < MID_RANGE)
        {
            // Jarak menengah: tembak sedang (2.0)
            return 2.0;
        }
        else
        {
            // Jauh: tembak ringan (1.0) agar peluru lebih cepat dan hemat energi
            return 1.0;
        }
    }

    // Event: bot memindai musuh
    public override void OnScannedBot(ScannedBotEvent evt)
    {
        // Greedy: pilih target dengan energi terendah jika sudah ada target,
        // atau ambil target pertama yang ditemukan
        if (!_hasTarget || evt.Energy < _targetEnergy || evt.ScannedBotId == _targetId)
        {
            _targetId = evt.ScannedBotId;
            _targetX = evt.X;
            _targetY = evt.Y;
            _targetEnergy = evt.Energy;
            _hasTarget = true;
        }

        // Jika target saat ini adalah bot yang dipindai, langsung tembak
        if (evt.ScannedBotId == _targetId)
        {
            double distance = DistanceTo(evt.X, evt.Y);
            double firePower = CalculateOptimalFirepower(distance);
            double gunBearing = GunBearingTo(evt.X, evt.Y);

            // Arahkan gun ke musuh dan tembak
            SetTurnGunRight(gunBearing);
            if (GunHeat == 0 && Math.Abs(gunBearing) < 10)
            {
                SetFire(firePower);
            }
        }
    }

    // Event: bot ditembak peluru
    public override void OnHitByBullet(HitByBulletEvent evt)
    {
        // Greedy: hindari peluru dengan berbalik arah 90 derajat
        // dari arah datangnya peluru
        double bulletBearing = CalcBearing(evt.Bullet.Direction);
        SetTurnRight(90 - bulletBearing);

        // Ubah arah orbit untuk mempersulit penembak
        _orbitDirection *= -1;
    }

    // Event: bot menabrak bot lain
    public override void OnHitBot(HitBotEvent evt)
    {
        // Greedy: saat menabrak, tembak dengan daya maksimum (ram damage bonus)
        // lalu mundur untuk keluar dari tabrakan
        if (GunHeat == 0)
        {
            SetFire(3.0);
        }
        SetBack(50);
    }

    // Event: bot menabrak dinding
    public override void OnHitWall(HitWallEvent evt)
    {
        // Greedy: saat menabrak dinding, mundur dan belok
        SetBack(50);
        SetTurnRight(45);
    }

    // Event: peluru bot mengenai musuh
    public override void OnBulletHit(BulletHitBotEvent evt)
    {
        // Update energi target yang terkena
        if (evt.VictimId == _targetId)
        {
            _targetEnergy = evt.Energy;

            // Jika energi musuh rendah, tembak lebih kuat untuk kill bonus
            if (_targetEnergy < 20 && GunHeat == 0)
            {
                SetFire(3.0);
            }
        }
    }

    // Event: bot musuh mati
    public override void OnBotDeath(BotDeathEvent evt)
    {
        // Jika target kita mati, cari target baru
        if (evt.VictimId == _targetId)
        {
            _hasTarget = false;
            _targetId = -1;
        }
    }
}
