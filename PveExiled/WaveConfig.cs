using Exiled.API.Features;
using System.Collections.Generic;
using PlayerEventArgs = Exiled.Events.EventArgs.Player;
using MapEventArgs = Exiled.Events.EventArgs.Map;
using ServerEventArgs = Exiled.Events.EventArgs.Server;
using Exiled.API.Features.Items;

public class WaveConfig
{
    public int MulCount { get; set; } = 0;
    public float EnemyMultiplyPerPlayers { get; } = 0.1f;//플레이어당 유닛 배율(2인 이상에서만 유효)
    public float SupplyMultiplyPerPlayers { get; } = 0.25f;//플레이어당 보급 배율(2인 이상에서만 유효)

    public void OnHurting(PlayerEventArgs.HurtingEventArgs ev)
    {
        if (ev.Attacker == null) return;
        if (!ev.Attacker.IsNPC) return;
        if (ev.Player.IsNPC) { ev.IsAllowed = false; return; }
        if (ev.Player.CurrentItem == null) return;

        switch (ev.Attacker.CurrentItem.Type)
        {
            case ItemType.Jailbird: ev.DamageHandler.Damage *= 0.5f; break;//항상 25
            case ItemType.MicroHID: ev.DamageHandler.Damage *= (0.15f + 0.01f * MulCount); break;//35명 -> X0.5
            case ItemType.GunCrossvec: ev.DamageHandler.Damage *= (0.1f + 0.02f * MulCount); break;//35명 -> X0.8
            case ItemType.GunLogicer: ev.DamageHandler.Damage *= (0.2f + 0.02f * MulCount); break;//35명 -> X0.9
            default: ev.DamageHandler.Damage *= (0.08f + 0.01f * MulCount); break;//35명 -> X0.43
        }
    }
    public void OnPickupAdded(MapEventArgs.PickupAddedEventArgs ev)
    {
        if (ev.Pickup.PreviousOwner == null) return;
        if (!ev.Pickup.PreviousOwner.IsNPC) return;
        ev.Pickup.Destroy();
    }
    public void OnPlacingBulletHole(MapEventArgs.PlacingBulletHoleEventArgs ev) { ev.IsAllowed = false; }
    public void OnRespawningTeam(ServerEventArgs.RespawningTeamEventArgs ev) { ev.IsAllowed = false; }

    public WaveInfo[] Waves { get; } = new WaveInfo[7]//웨이브 구조
    {
            new WaveInfo(
                    intermissionTime: 10,
                    bcText: "Wave 1(ClassD)",
                    supplySpawnInfos: new List<SupplySpawnInfo>
                    {
                        new SupplySpawnInfo(ItemType.Painkillers, 4),
                        new SupplySpawnInfo(ItemType.Adrenaline, 2),
                    },
                    enemySpawnInfos: new List<EnemySpawnInfo>
                    {
                        new EnemySpawnInfo("ClassD", 5),
                    },
                    supplyGiveInfos: new List<ItemType>(){ ItemType.Adrenaline, ItemType.MicroHID }
            ),
            new WaveInfo(
                    intermissionTime: 5,
                    bcText: "Wave 2(Cloaker)",
                    supplySpawnInfos: new List<SupplySpawnInfo>
                    {
                        new SupplySpawnInfo(ItemType.ArmorLight, 1),
                        new SupplySpawnInfo(ItemType.Medkit, 3),
                        new SupplySpawnInfo(ItemType.Ammo9x19, 5),
                    },
                    enemySpawnInfos: new List<EnemySpawnInfo>
                    {
                        new EnemySpawnInfo("Cloaker", 1),
                    }
            ),
            new WaveInfo(
                    intermissionTime: 5,
                    bcText: "Wave 3(Demolisher)",
                    supplySpawnInfos: new List<SupplySpawnInfo>
                    {
                        new SupplySpawnInfo(ItemType.GunCrossvec, 2),
                        new SupplySpawnInfo(ItemType.ArmorCombat, 2),
                        new SupplySpawnInfo(ItemType.Medkit, 3),
                        new SupplySpawnInfo(ItemType.Adrenaline, 3),
                        new SupplySpawnInfo(ItemType.Ammo9x19, 8),
                    },
                    enemySpawnInfos: new List<EnemySpawnInfo>
                    {
                        new EnemySpawnInfo("Demolisher", 1),
                    }
            ),
            new WaveInfo(
                    intermissionTime: 5,
                    bcText: "Wave 4(Juggernaut)",
                    supplySpawnInfos: new List<SupplySpawnInfo>
                    {
                        new SupplySpawnInfo(ItemType.Medkit, 5),
                        new SupplySpawnInfo(ItemType.Adrenaline, 3),
                        new SupplySpawnInfo(ItemType.Ammo9x19, 10),
                        new SupplySpawnInfo(ItemType.GunE11SR, 2),
                        new SupplySpawnInfo(ItemType.ArmorHeavy, 1),
                        new SupplySpawnInfo(ItemType.Ammo556x45, 5),
                    },
                    enemySpawnInfos: new List<EnemySpawnInfo>
                    {
                        new EnemySpawnInfo("Juggernaut", 1),
                    }
            ),
            new WaveInfo(
                    intermissionTime: 5,
                    bcText: "Wave 5(Pyromancer)",
                    supplySpawnInfos: new List<SupplySpawnInfo>
                    {
                        new SupplySpawnInfo(ItemType.Medkit, 3),
                        new SupplySpawnInfo(ItemType.Adrenaline, 3),
                        new SupplySpawnInfo(ItemType.SCP500, 1),
                        new SupplySpawnInfo(ItemType.SCP268, 1),
                        new SupplySpawnInfo(ItemType.Ammo9x19, 10),
                    },
                    enemySpawnInfos: new List<EnemySpawnInfo>
                    {
                        new EnemySpawnInfo("Pyromancer", 1),
                    }
            ),
            new WaveInfo(
                    intermissionTime: 5,
                    bcText: "Wave 6(Scout)",
                    supplySpawnInfos: new List<SupplySpawnInfo>
                    {
                        new SupplySpawnInfo(ItemType.Medkit, 3),
                        new SupplySpawnInfo(ItemType.Adrenaline, 3),
                        new SupplySpawnInfo(ItemType.SCP500, 1),
                        new SupplySpawnInfo(ItemType.SCP268, 1),
                        new SupplySpawnInfo(ItemType.GunLogicer, 1),
                        new SupplySpawnInfo(ItemType.ArmorHeavy, 1),
                        new SupplySpawnInfo(ItemType.Ammo556x45, 10),
                        new SupplySpawnInfo(ItemType.Ammo9x19, 10),
                        new SupplySpawnInfo(ItemType.Ammo762x39, 10),

                    },
                    enemySpawnInfos: new List<EnemySpawnInfo>
                    {
                        new EnemySpawnInfo("Scout", 1),
                    }
            ),
            new WaveInfo(
                    intermissionTime: 5,
                    bcText: "Wave 7(Tranquilizer)",
                    supplySpawnInfos: new List<SupplySpawnInfo>
                    {
                        new SupplySpawnInfo(ItemType.Medkit, 3),
                        new SupplySpawnInfo(ItemType.Adrenaline, 3),
                        new SupplySpawnInfo(ItemType.SCP500, 1),
                        new SupplySpawnInfo(ItemType.SCP268, 1),
                        new SupplySpawnInfo(ItemType.GunLogicer, 1),
                        new SupplySpawnInfo(ItemType.ArmorHeavy, 1),
                        new SupplySpawnInfo(ItemType.Ammo556x45, 10),
                        new SupplySpawnInfo(ItemType.Ammo9x19, 10),
                        new SupplySpawnInfo(ItemType.Ammo762x39, 10),

                    },
                    enemySpawnInfos: new List<EnemySpawnInfo>
                    {
                        new EnemySpawnInfo("Tranquilizer", 1),
                    }
            ),
    };
    //타입
    public class EnemySpawnInfo
    {
        public string EnemyName { get; }
        public float Amount { get; }
        public EnemySpawnInfo(string enemyType, float amount)
        {
            EnemyName = enemyType;
            Amount = amount;
        }
    }
    public class SupplySpawnInfo
    {
        public ItemType Type { get; }
        public float Amount { get; }

        public SupplySpawnInfo(ItemType type, float amount)
        {
            Type = type;
            Amount = amount;
        }
    }
    public class SupplyGiveInfo
    {
        public ItemType Type { get; }

        public SupplyGiveInfo(ItemType type)
        {
            Type = type;
        }
    }
    public class WaveInfo
    {
        public List<EnemySpawnInfo> EnemySpawnInfos { get; }
        public int IntermissionTime { get; }
        public List<SupplySpawnInfo> SupplySpawnInfos { get; }
        public List<ItemType> SupplyGiveInfos { get; }

        public string BCtext { get; }

        public WaveInfo(
            List<EnemySpawnInfo> enemySpawnInfos,
            int intermissionTime,
            List<SupplySpawnInfo> supplySpawnInfos,
            string bcText,
            List<ItemType> supplyGiveInfos = null)
        {
            IntermissionTime = intermissionTime;
            SupplySpawnInfos = supplySpawnInfos;
            EnemySpawnInfos = enemySpawnInfos;
            BCtext = bcText;
            SupplyGiveInfos = supplyGiveInfos;

        }
    }
}