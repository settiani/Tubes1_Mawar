// ZigZagBot - Bot Alternatif 1 Tim Mawar
// Strategi Greedy: Zigzag Agresif ke Musuh Terlemah
//
// Heuristik: Setiap giliran, secara greedy pilih musuh dengan energi TERENDAH
// sebagai target prioritas (mudah dibunuh = Bullet Damage Bonus lebih cepat didapat).
// Bot bergerak dalam pola zigzag menuju musuh untuk menghindari tembakan,
// sambil terus menembak saat radar mengunci target.
//
// Fungsi objektif: Maksimalkan Bullet Damage Bonus dengan fokus membunuh
// musuh berenergi rendah terlebih dahulu, serta Survival Score dari tetap hidup.

using System;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class ZigZagBot : Bot
{
    // Dictionary menyimpan data musuh yang pernah dipindai
    // Key: botId, Value: (X, Y, Energy)
    private Dictionary<int, (double X, double Y, double Energy)> _enemyData
        = new Dictionary<int, (double, double, double)>();

    // Target saat ini (bot dengan energi terendah)
    private int _targetId = -1;
    private double _targetX;
    private double _targetY;
    private double _targetEnergy = double.MaxValue;

    // Kontrol zigzag: hitung langkah untuk ganti arah
    private int _zigzagStep = 0;
    private const int ZIGZAG_INTERVAL = 3; // Ganti arah tiap N maju
    private int _zigzagDirection = 1; // 1 = kanan, -1 = kiri

    static void Main(string[] args)
    {
        new ZigZagBot().Start();
    }

    ZigZagBot() : base(BotInfo.FromFile("ZigZagBot.json")) { }

    public override void Run()
    {
        // Reset per ronde
        _enemyData.Clear();
        _targetId = -1;
        _targetEnergy = double.MaxValue;
        _zigzagStep = 0;
        _zigzagDirection = 1;

        BodyColor = System.Drawing.Color.Green;
        TurretColor = System.Drawing.Color.Pink;
        RadarColor = System.Drawing.Color.OrangeRed;
        BulletColor = System.Drawing.Color.Navy;
        ScanColor = System.Drawing.Color.Orange;
        TracksColor = System.Drawing.Color.DarkGray;
        GunColor = System.Drawing.Color.Red;

        while (IsRunning)
        {
            // Putar radar terus untuk memindai semua musuh
            TurnRadarRight(360);

            // Jika ada target, gerak zigzag ke arahnya
            if (_targetId != -1 && _enemyData.ContainsKey(_targetId))
            {
                var target = _enemyData[_targetId];
                _targetX = target.X;
                _targetY = target.Y;
                ExecuteZigzagApproach();
            }
        }
    }

    // Gerak zigzag menuju musuh
    private void ExecuteZigzagApproach()
    {
        // Hitung bearing ke musuh
        double bearingToTarget = BearingTo(_targetX, _targetY);
        double distanceToTarget = DistanceTo(_targetX, _targetY);

        // Greedy: arahkan gun ke musuh dan tembak
        double gunBearing = GunBearingTo(_targetX, _targetY);
        TurnGunRight(gunBearing);

        // Greedy: daya tembak berdasarkan energi musuh
        // Musuh hampir mati = tembak sekuat mungkin untuk kill bonus
        double firePower = CalculateFirepowerByEnemyEnergy(_targetEnergy, distanceToTarget);

        if (GunHeat == 0 && Math.Abs(GunTurnRemaining) < 15)
        {
            Fire(firePower);
        }

        // Gerak zigzag: belok kanan/kiri secara bergantian saat mendekati
        _zigzagStep++;
        if (_zigzagStep >= ZIGZAG_INTERVAL)
        {
            _zigzagStep = 0;
            _zigzagDirection *= -1; // Balik arah zigzag
        }

        // Gerak maju ke arah musuh dengan offset zigzag
        double zigzagOffset = 30 * _zigzagDirection;
        TurnRight(bearingToTarget + zigzagOffset);
        Forward(80);
    }

    // Greedy heuristik: pilih daya tembak berdasarkan sisa energi musuh
    // Jika musuh hampir mati, tembak kuat untuk pastikan kill dan raih bonus
    private double CalculateFirepowerByEnemyEnergy(double enemyEnergy, double distance)
    {
        if (enemyEnergy <= 16)
        {
            // Musuh hampir mati: tembak kuat untuk kill bonus (20% dari total damage)
            return Math.Min(3.0, enemyEnergy / 4.0 + 0.5);
        }
        else if (distance < 200)
        {
            // Dekat: tembak kuat
            return 2.5;
        }
        else if (distance < 400)
        {
            return 1.5;
        }
        else
        {
            // Jauh: tembak ringan agar peluru tidak meleset terlalu lama
            return 1.0;
        }
    }

    // Greedy: pilih target baru dengan energi terendah dari semua musuh yang diketahui
    private void SelectBestTarget()
    {
        _targetId = -1;
        _targetEnergy = double.MaxValue;

        foreach (var entry in _enemyData)
        {
            if (entry.Value.Energy < _targetEnergy)
            {
                _targetEnergy = entry.Value.Energy;
                _targetId = entry.Key;
                _targetX = entry.Value.X;
                _targetY = entry.Value.Y;
            }
        }
    }

    public override void OnScannedBot(ScannedBotEvent evt)
    {
        // Simpan/update data musuh
        _enemyData[evt.ScannedBotId] = (evt.X, evt.Y, evt.Energy);

        // Greedy: pilih ulang target terbaik setiap kali scan
        SelectBestTarget();

        // Jika ini target kita, langsung arahkan tembakan
        if (evt.ScannedBotId == _targetId)
        {
            double distance = DistanceTo(evt.X, evt.Y);
            double firePower = CalculateFirepowerByEnemyEnergy(evt.Energy, distance);
            double gunBearing = GunBearingTo(evt.X, evt.Y);

            SetTurnGunRight(gunBearing);
            if (GunHeat == 0 && Math.Abs(gunBearing) < 10)
            {
                SetFire(firePower);
            }
        }
    }

    public override void OnHitByBullet(HitByBulletEvent evt)
    {
        // Greedy: hindari peluru - belok tegak lurus dari arah datangnya peluru
        double bearing = CalcBearing(evt.Bullet.Direction);
        SetTurnRight(90 - bearing);
        // Ganti arah zigzag saat terkena peluru
        _zigzagDirection *= -1;
    }

    public override void OnHitBot(HitBotEvent evt)
    {
        // Saat menabrak, tembak kuat dan mundur
        if (GunHeat == 0)
        {
            SetFire(3.0);
        }
        SetBack(40);
        SetTurnRight(30);
    }

    public override void OnHitWall(HitWallEvent evt)
    {
        // Menabrak dinding: mundur dan belok balik
        SetBack(30);
        SetTurnRight(45 * _zigzagDirection);
    }

    public override void OnBulletHit(BulletHitBotEvent evt)
    {
        // Update energi musuh yang tertembak
        if (_enemyData.ContainsKey(evt.VictimId))
        {
            var old = _enemyData[evt.VictimId];
            _enemyData[evt.VictimId] = (old.X, old.Y, evt.Energy);
        }

        // Jika energi musuh sangat rendah, perbarui target agar bisa segera dibunuh
        SelectBestTarget();
    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        // Hapus musuh yang sudah mati dari data
        _enemyData.Remove(evt.VictimId);

        // Jika target kita mati, pilih target baru
        if (evt.VictimId == _targetId)
        {
            SelectBestTarget();
        }
    }
}
