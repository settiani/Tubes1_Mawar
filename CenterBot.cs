// CenterBot - Bot Alternatif 2 Tim Mawar
// Strategi Greedy: Dominasi Titik Tengah Arena
//
// Heuristik: Secara greedy selalu bergerak menuju titik tengah arena.
// Pusat arena adalah posisi paling strategis karena:
//   - Jarak rata-rata ke semua musuh lebih pendek
//   - Lebih mudah menjangkau musuh dari segala arah
//   - Menghindari sudut (corner trap)
//
// Setiap turn: greedy memilih tembakan ke musuh TERDEKAT dari posisi saat ini,
// dengan daya tembak yang disesuaikan jarak.
//
// Fungsi objektif: Maksimalkan Bullet Damage dari berbagai musuh
// dengan posisi sentral yang memudahkan akses ke semua musuh.

using System;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class CenterBot : Bot
{
    // Posisi pusat arena (diperbarui dari ArenaWidth/ArenaHeight)
    private double _centerX = 400.0;
    private double _centerY = 400.0;
    private bool _arenaKnown = false;

    // Data musuh
    private Dictionary<int, (double X, double Y, double Energy, int TurnSeen)> _enemyData
        = new Dictionary<int, (double, double, double, int)>();

    // Target terdekat
    private int _nearestId = -1;
    private double _nearestX;
    private double _nearestY;
    private double _nearestEnergy;
    private double _nearestDistance = double.MaxValue;

    // Fase gerakan: apakah sedang menuju tengah atau bertarung
    private bool _atCenter = false;
    private const double CENTER_THRESHOLD = 80.0; // Radius "sudah di tengah"

    // Radar lock: sudut ke target untuk lock radar
    private bool _radarLocked = false;

    static void Main(string[] args)
    {
        new CenterBot().Start();
    }

    CenterBot() : base(BotInfo.FromFile("CenterBot.json")) { }

    public override void Run()
    {
        // Cari tahu ukuran arena
        _arenaKnown = false;
        _enemyData.Clear();
        _nearestId = -1;
        _atCenter = false;
        _radarLocked = false;

        BodyColor = System.Drawing.Color.Green;
        TurretColor = System.Drawing.Color.Pink;
        RadarColor = System.Drawing.Color.OrangeRed;
        BulletColor = System.Drawing.Color.Navy;
        ScanColor = System.Drawing.Color.Orange;
        TracksColor = System.Drawing.Color.DarkGray;
        GunColor = System.Drawing.Color.Red;

        while (IsRunning)
        {
            // Inisialisasi pusat arena saat pertama kali
            if (!_arenaKnown)
            {
                // Gunakan default atau dari ArenaWidth/ArenaHeight jika tersedia
                _centerX = ArenaWidth / 2.0;
                _centerY = ArenaHeight / 2.0;
                _arenaKnown = true;
            }

            // Hitung jarak ke pusat
            double distToCenter = DistanceTo(_centerX, _centerY);
            _atCenter = distToCenter < CENTER_THRESHOLD;

            if (!_atCenter)
            {
                // Greedy: gerak ke pusat arena (posisi terbaik)
                MoveToCenter();
            }
            else
            {
                // Sudah di tengah: putar radar penuh dan tembak musuh terdekat
                TurnRadarRight(45);

                // Tembak musuh terdekat jika ada
                if (_nearestId != -1)
                {
                    AimAndFire();
                }
            }
        }
    }

    // Gerak ke pusat arena
    private void MoveToCenter()
    {
        double bearingToCenter = BearingTo(_centerX, _centerY);
        TurnRight(bearingToCenter);
        Forward(100);

        // Sambil bergerak, tetap putar radar
        TurnRadarRight(360);
    }

    // Arahkan gun ke musuh terdekat dan tembak
    private void AimAndFire()
    {
        double distanceToNearest = DistanceTo(_nearestX, _nearestY);
        double gunBearing = GunBearingTo(_nearestX, _nearestY);
        double firePower = CalculateFirepower(distanceToNearest, _nearestEnergy);

        TurnGunRight(gunBearing);

        if (GunHeat == 0 && Math.Abs(GunTurnRemaining) < 10)
        {
            Fire(firePower);
        }

        // Lock radar ke musuh terdekat
        double radarBearing = RadarBearingTo(_nearestX, _nearestY);
        TurnRadarRight(radarBearing * 2.0); // Overscan untuk lock
    }

    // Greedy heuristik: daya tembak berdasarkan jarak dan energi musuh
    // Prioritas: musuh dekat dan berenergi rendah = tembak kuat
    private double CalculateFirepower(double distance, double enemyEnergy)
    {
        // Jika musuh hampir mati, tembak kuat untuk kill bonus
        if (enemyEnergy < 20)
            return Math.Min(3.0, enemyEnergy / 3.0 + 1.0);

        // Berdasarkan jarak
        if (distance < 150)
            return 3.0;
        else if (distance < 300)
            return 2.0;
        else if (distance < 500)
            return 1.5;
        else
            return 1.0;
    }

    // Greedy: pilih musuh terdekat sebagai target
    private void SelectNearestEnemy()
    {
        _nearestId = -1;
        _nearestDistance = double.MaxValue;

        foreach (var entry in _enemyData)
        {
            double dist = Math.Sqrt(
                Math.Pow(entry.Value.X - X, 2) +
                Math.Pow(entry.Value.Y - Y, 2)
            );

            if (dist < _nearestDistance)
            {
                _nearestDistance = dist;
                _nearestId = entry.Key;
                _nearestX = entry.Value.X;
                _nearestY = entry.Value.Y;
                _nearestEnergy = entry.Value.Energy;
            }
        }
    }

    public override void OnScannedBot(ScannedBotEvent evt)
    {
        // Simpan/update data musuh dengan turn saat ini
        _enemyData[evt.ScannedBotId] = (evt.X, evt.Y, evt.Energy, TurnNumber);

        // Greedy: perbarui target terdekat
        SelectNearestEnemy();

        // Tembak langsung saat memindai jika ini target terdekat
        if (evt.ScannedBotId == _nearestId)
        {
            double dist = DistanceTo(evt.X, evt.Y);
            double fp = CalculateFirepower(dist, evt.Energy);
            double gunBearing = GunBearingTo(evt.X, evt.Y);

            SetTurnGunRight(gunBearing);
            if (GunHeat == 0 && Math.Abs(gunBearing) < 15)
            {
                SetFire(fp);
            }
        }
    }

    public override void OnHitByBullet(HitByBulletEvent evt)
    {
        // Greedy saat terkena: kembali ke tengah (posisi aman)
        double bearing = CalcBearing(evt.Bullet.Direction);
        SetTurnRight(90 - bearing);
        SetForward(50);
    }

    public override void OnHitBot(HitBotEvent evt)
    {
        // Tembak lalu mundur
        if (GunHeat == 0)
            SetFire(2.0);
        SetBack(60);
    }

    public override void OnHitWall(HitWallEvent evt)
    {
        // Balik ke tengah saat menabrak dinding
        SetBack(50);
        double bearingToCenter = BearingTo(_centerX, _centerY);
        SetTurnRight(bearingToCenter);
    }

    public override void OnBulletHit(BulletHitBotEvent evt)
    {
        // Update energi musuh
        if (_enemyData.ContainsKey(evt.VictimId))
        {
            var old = _enemyData[evt.VictimId];
            _enemyData[evt.VictimId] = (old.X, old.Y, evt.Energy, old.TurnSeen);
        }
        SelectNearestEnemy();
    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        _enemyData.Remove(evt.VictimId);
        if (evt.VictimId == _nearestId)
        {
            SelectNearestEnemy();
        }
    }
}
