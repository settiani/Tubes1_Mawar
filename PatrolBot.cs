// PatrolBot - Bot Alternatif 3 Tim Mawar
// Strategi Greedy: Patroli Persegi + Tembak Musuh Mana Saja yang Terdekat
//
// Heuristik: Bot berpatroli mengikuti jalur persegi di sekitar arena
// (tidak menempel dinding, sedikit ke dalam), dan radar selalu berputar.
// Saat memindai musuh, secara greedy langsung tembak dengan daya
// yang dipilih berdasarkan seberapa besar KEUNTUNGAN energi yang didapat
// (energy_gain = firePower * 3, energy_cost = firePower).
// Net gain maksimum = firePower terbesar yang masih aman digunakan
// (tidak habiskan energi hingga tersisa < 20).
//
// Fungsi objektif: Maksimalkan total Bullet Damage + Survival Score
// dengan menjaga energi sendiri agar tidak mati (greedy energy management).

using System;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class PatrolBot : Bot
{
    // Waypoint persegi untuk patrol
    // Akan diinisialisasi setelah tahu ukuran arena
    private double[][] _waypoints = Array.Empty<double[]>();
    private int _waypointIndex = 0;
    private bool _waypointsReady = false;
    private const double PATROL_MARGIN = 120.0; // Jarak dari dinding

    // Data musuh terakhir dipindai
    private int _lastScannedId = -1;
    private double _lastScannedX;
    private double _lastScannedY;
    private double _lastScannedEnergy;
    private bool _hasScanned = false;

    static void Main(string[] args)
    {
        new PatrolBot().Start();
    }

    PatrolBot() : base(BotInfo.FromFile("PatrolBot.json")) { }

    public override void Run()
    {
        _waypointsReady = false;
        _waypointIndex = 0;
        _hasScanned = false;
        _lastScannedId = -1;

        BodyColor = System.Drawing.Color.Green;
        TurretColor = System.Drawing.Color.Pink;
        RadarColor = System.Drawing.Color.OrangeRed;
        BulletColor = System.Drawing.Color.Navy;
        ScanColor = System.Drawing.Color.Orange;
        TracksColor = System.Drawing.Color.DarkGray;
        GunColor = System.Drawing.Color.Red;

        while (IsRunning)
        {
            // Inisialisasi waypoint setelah tahu ukuran arena
            if (!_waypointsReady)
            {
                InitWaypoints();
                _waypointsReady = true;
            }

            // Jika ada musuh yang dipindai, arahkan tembakan terlebih dahulu
            if (_hasScanned)
            {
                AimAndFireAtLastScanned();
            }

            // Patroli ke waypoint berikutnya (radar diputar di dalam fungsi patrol)
            PatrolToNextWaypoint();
        }
    }

    // Inisialisasi 4 titik patrol membentuk persegi di dalam arena
    private void InitWaypoints()
    {
        double w = ArenaWidth;
        double h = ArenaHeight;
        double m = PATROL_MARGIN;

        // 4 sudut arena (dengan margin dari dinding)
        _waypoints = new double[][]
        {
            new double[] { m, m },           // Sudut kiri bawah
            new double[] { w - m, m },       // Sudut kanan bawah
            new double[] { w - m, h - m },   // Sudut kanan atas
            new double[] { m, h - m }        // Sudut kiri atas
        };
    }

    // Gerak ke waypoint patrol berikutnya
    private void PatrolToNextWaypoint()
    {
        if (_waypoints.Length == 0) return;

        double[] wp = _waypoints[_waypointIndex];
        double wpX = wp[0];
        double wpY = wp[1];
        double distToWp = DistanceTo(wpX, wpY);

        if (distToWp < 50)
        {
            // Sampai di waypoint, pindah ke berikutnya
            _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
            wp = _waypoints[_waypointIndex];
            wpX = wp[0];
            wpY = wp[1];
        }

        // Gerak ke waypoint
        double bearing = BearingTo(wpX, wpY);
        TurnRight(bearing);
        // Putar radar 360 sambil bergerak untuk selalu memindai musuh
        TurnRadarRight(360);
        Forward(Math.Min(distToWp, 100));
    }

    // Arahkan gun ke musuh terakhir dipindai dan tembak
    private void AimAndFireAtLastScanned()
    {
        double gunBearing = GunBearingTo(_lastScannedX, _lastScannedY);
        TurnGunRight(gunBearing);

        if (GunHeat == 0 && Math.Abs(GunTurnRemaining) < 15)
        {
            double distToEnemy = DistanceTo(_lastScannedX, _lastScannedY);
            double firePower = CalculateGreedyFirepower(distToEnemy);
            Fire(firePower);
        }
    }

    // Greedy heuristik: pilih daya tembak yang memaksimalkan net energy gain
    // Net gain = (firePower * 3) - firePower = firePower * 2 (jika mengenai)
    // Namun dibatasi agar energi sendiri tidak turun terlalu rendah
    private double CalculateGreedyFirepower(double distance)
    {
        // Batas aman: jangan gunakan lebih dari setengah energi kita
        double safeEnergy = Math.Max(0, Energy - 20.0);
        double maxAffordable = Math.Min(3.0, safeEnergy);

        // Berdasarkan jarak: jauh = peluru lebih lambat, kemungkinan meleset lebih besar
        double distancePower;
        if (distance < 150)
            distancePower = 3.0;
        else if (distance < 300)
            distancePower = 2.0;
        else if (distance < 600)
            distancePower = 1.5;
        else
            distancePower = 1.0;

        // Ambil minimum dari yang mampu dan yang sesuai jarak
        return Math.Max(0.1, Math.Min(maxAffordable, distancePower));
    }

    public override void OnScannedBot(ScannedBotEvent evt)
    {
        // Simpan musuh yang dipindai
        _lastScannedId = evt.ScannedBotId;
        _lastScannedX = evt.X;
        _lastScannedY = evt.Y;
        _lastScannedEnergy = evt.Energy;
        _hasScanned = true;

        // Greedy: langsung tembak saat memindai
        double dist = DistanceTo(evt.X, evt.Y);
        double fp = CalculateGreedyFirepower(dist);
        double gunBearing = GunBearingTo(evt.X, evt.Y);

        SetTurnGunRight(gunBearing);
        if (GunHeat == 0 && Math.Abs(gunBearing) < 10)
        {
            SetFire(fp);
        }
    }

    public override void OnHitByBullet(HitByBulletEvent evt)
    {
        // Bergerak menghindari peluru berikutnya
        double bearing = CalcBearing(evt.Bullet.Direction);
        SetTurnRight(90 - bearing);
        SetForward(80);
    }

    public override void OnHitBot(HitBotEvent evt)
    {
        // Tembak dan mundur
        if (GunHeat == 0)
            SetFire(2.0);
        SetBack(50);
        // Lanjutkan ke waypoint berikutnya
        _waypointIndex = (_waypointIndex + 1) % Math.Max(1, _waypoints.Length);
    }

    public override void OnHitWall(HitWallEvent evt)
    {
        // Mundur dan gerak ke waypoint berikutnya
        SetBack(40);
        _waypointIndex = (_waypointIndex + 1) % Math.Max(1, _waypoints.Length);
    }

    public override void OnBulletHit(BulletHitBotEvent evt)
    {
        // Update energi musuh
        if (evt.VictimId == _lastScannedId)
        {
            _lastScannedEnergy = evt.Energy;
        }
    }

    public override void OnBotDeath(BotDeathEvent evt)
    {
        // Jika target kita mati, reset
        if (evt.VictimId == _lastScannedId)
        {
            _hasScanned = false;
            _lastScannedId = -1;
        }
    }
}
