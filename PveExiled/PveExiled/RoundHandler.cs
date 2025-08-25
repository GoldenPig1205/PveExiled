using CustomPlayerEffects;
using Exiled;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Pickups;
using InventorySystem;
using MapGeneration;
using MEC;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using AdminToys;
using Mirror;

using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEventArgs = Exiled.Events.EventArgs.Server;
using LabApi;

public class RoundHandler
{
    private bool roundStarted = false;
    private WaveConfig waveConfig = new WaveConfig();

    private NavMeshDataInstance navMesh;
    private Vector3 playerSpawnPoint;
    private List<Vector3> enemySpawnPoints = new List<Vector3>();
    private Dictionary<int, Enemy> enemies = new Dictionary<int, Enemy>();

    private CoroutineHandle runningRound;
    private int cursor = 0;
    public void OnRoundStarted()
    {
        OnEndingRound();
        roundStarted = true;

        NavMesh.RemoveAllNavMeshData();

        //이벤트연결
        PlayerEvents.Hurting += waveConfig.OnHurting;
        Exiled.Events.Handlers.Map.PickupAdded += waveConfig.OnPickupAdded;

        Round.IsLocked = true;
        foreach (Door door in Door.List)//문잠금
        {
            if (door.Zone != ZoneType.Entrance) continue;
            door.Lock(10000000, DoorLockType.Regular079);

            ElevatorDoor Edoor = door as ElevatorDoor;
            BreakableDoor Bdoor = door as BreakableDoor;
            if (Edoor != null){door.IsOpen = false;continue;}
            if (Bdoor != null)
            {
                Bdoor.MaxHealth = 1000000;
                Bdoor.Health = 1000000;
            }
            door.IsOpen = (door.Name != "INTERCOM" && door.Name != "Unsecured");
        }
        foreach (Room room in Room.List)//Room for
        {
            if (room.Zone != ZoneType.Entrance) continue;
            if (room.Type == RoomType.EzGateA || room.Type == RoomType.EzGateB || room.Type == RoomType.EzSmallrooms || room.Type == RoomType.EzCollapsedTunnel || room.Type == RoomType.EzShelter)
            {
                if (room.Type == RoomType.EzCollapsedTunnel || room.Type == RoomType.EzShelter)
                {
                    enemySpawnPoints.Add(room.Doors.First().Position + Vector3.up);
                }
                else
                {
                    enemySpawnPoints.Add(room.Position + Vector3.up);
                }
                continue;
            }
            if (room.Type == RoomType.EzUpstairsPcs)
            {
                playerSpawnPoint = room.Position + Vector3.up;
                continue;
            }
            if (room.Type == RoomType.EzCheckpointHallwayA || room.Type == RoomType.EzCheckpointHallwayB)
            {
                foreach(Door door in room.Doors)
                {
                    door.IsOpen = false;
                }
            }
            /*if (room.Type == RoomType.EzTCross)
            {
                LabApi.Features.Wrappers.PrimitiveObjectToy primitive = LabApi.Features.Wrappers.PrimitiveObjectToy.Create();
                primitive.Position = room.Position;
            }*/
        }
        foreach (Pickup pickup in Pickup.List) pickup.Destroy();

        var bounds = new Bounds(Vector3.zero, new Vector3(2000, 2000, 2000));
        var markups = new List<NavMeshBuildMarkup>();
        var sources = new List<NavMeshBuildSource>();

        int maskInvCol = LayerMask.GetMask("Default", "InvisibleCollider");
        NavMeshBuilder.CollectSources(bounds, maskInvCol, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);

        var settings = NavMesh.CreateSettings();
        settings.agentRadius = .17f;
        settings.agentHeight = 1f;
        settings.agentClimb = .24f;
        settings.agentSlope = 45;

        var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
        navMesh = NavMesh.AddNavMeshData(data);

        runningRound = Timing.RunCoroutine(DoRound());
    }
    public void OnEndingRound()//라운드종료
    {
        if (!roundStarted) return;
        Map.Broadcast(message: "라종", duration: 4);
        roundStarted = false;
        Round.IsLocked = false;

        enemySpawnPoints.Clear();

        //이벤트해제
        PlayerEvents.Hurting -= waveConfig.OnHurting;
        Exiled.Events.Handlers.Map.PickupAdded -= waveConfig.OnPickupAdded;

        NavMesh.RemoveAllNavMeshData();
        Timing.KillCoroutines(runningRound);

        foreach (Enemy enemy in enemies.Values.ToList())
        {
            enemy.RemoveEnemy();
        }
        enemies.Clear();
        DummyUtils.DestroyAllDummies();
    }
    public void OnEndingRound(ServerEventArgs.EndingRoundEventArgs _) => OnEndingRound();

    private IEnumerator<float> DoRound()//라운드진행
    {
        yield return Timing.WaitForSeconds(1);
        bool won = true;
        foreach (WaveConfig.WaveInfo waveInfo in waveConfig.Waves)
        {
            DummyUtils.DestroyAllDummies();
            waveConfig.MulCount = Player.Count-1;
            int mulCount = waveConfig.MulCount;
            SpawnPlayers();

            Map.ShowHint("PlayerCount: " + (mulCount + 1), duration: 10);
            foreach (WaveConfig.SupplySpawnInfo itemInfo in waveInfo.SupplySpawnInfos)//보급품
            {
                for (int i = 0; i < (int)(itemInfo.Amount + mulCount * itemInfo.Amount * waveConfig.SupplyMultiplyPerPlayers); i++)
                {
                    Pickup pickup = Pickup.Create(itemInfo.Type);
                    pickup.Position = playerSpawnPoint;
                    pickup.Spawn();
                }
            }

            for (int i = waveInfo.IntermissionTime; i > 0 ; i--)//타이머
            {
                Map.Broadcast(message: i.ToString(), duration: 1);
                yield return Timing.WaitForSeconds(1);
            }
            Map.Broadcast(message: waveInfo.BCtext, duration: 10);

            foreach (WaveConfig.EnemySpawnInfo spawnInfo in waveInfo.EnemySpawnInfos)//적 스폰
            {
                for (int i = 0; i < (int)(spawnInfo.Amount + mulCount * spawnInfo.Amount * waveConfig.EnemyMultiplyPerPlayers); i++)
                {
                    SpawnEnemy(spawnInfo.EnemyName);
                    yield return Timing.WaitForSeconds(0.8f);
                }
            }
            while (enemies.Count > 0 && GetAlivePlayerCount() > 0) yield return Timing.WaitForSeconds(5);//ㄱㄷ
            if (GetAlivePlayerCount() <= 0) { won = false; break; }
        }
        Map.Broadcast(message: won.ToString(), duration: 4);
        OnEndingRound();
    }

    private void SpawnPlayers()
    {
        foreach(Player player in Player.List)
        {
            if (!IsValidPlayer(player)) continue;
            if (player.Role == RoleTypeId.NtfSergeant) continue;
            player.RoleManager.ServerSetRole(RoleTypeId.NtfSergeant, RoleChangeReason.RemoteAdmin);
            Timing.CallDelayed(0.5f, () =>
            {
                if (player == null || player.Role.Type != RoleTypeId.NtfSergeant) return;
                player.ClearInventory();
                player.Position = playerSpawnPoint;
                player.Inventory.ServerAddItem(ItemType.GunCOM18, InventorySystem.Items.ItemAddReason.AdminCommand);
                player.Inventory.ServerAddAmmo(ItemType.Ammo9x19, 40);
                player.EnableEffect<HeavyFooted>(255, -1, false);
            });
        }
    }
    private void SpawnEnemy(string enemyName)
    {
        Enemy enemy;
        switch (enemyName)
        {
            case "ClassD": enemy = new Enemies.ClassD(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig.MulCount); break;
            case "Scout": enemy = new Enemies.Scout(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig.MulCount); break;
            case "Cloaker": enemy = new Enemies.Cloaker(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig.MulCount); break;
            case "Pyromancer": enemy = new Enemies.Pyromancer(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig.MulCount); break;
            case "Juggernaut": enemy = new Enemies.Juggernaut(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig.MulCount); break;
            case "Demolisher": enemy = new Enemies.Demolisher(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig.MulCount); break;
            case "Tranquilizer": enemy = new Enemies.Tranquilizer(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig.MulCount); break;
            default: enemy = new Enemies.ClassD(enemyName, enemySpawnPoints.RandomItem<Vector3>(), cursor, enemies, waveConfig.MulCount); break;
        }
        enemies.Add(cursor, enemy);
        cursor++;
    }
    private int GetAlivePlayerCount()
    {
        int count = 0;
        foreach (Player player in Player.List)
        {
            if (!IsValidPlayer(player)) continue;
            if (player.Role != RoleTypeId.NtfSergeant) continue;
            count++;
        }
        return count;
    }
    private bool IsValidPlayer(Player player)
    {
        if (player == null) return false;
        if (player.UserId == "ID_Dedicated" || player.UserId == "ID_Dummy" || player.IsNPC) return false;
        return true;
    }
    private bool IsAlivePlayer(Player player)
    {
        if (player == null) return false;
        if (player.UserId == "ID_Dedicated" || player.UserId == "ID_Dummy" || player.IsNPC) return false;
        if (player.Role != RoleTypeId.NtfSergeant) return false;
        return true;
    }
}