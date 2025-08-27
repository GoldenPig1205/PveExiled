using Exiled.API.Features.Pickups;
using Exiled.API.Features.Items;
using MEC;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using System.Linq;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem;
using InventorySystem.Items;
using Exiled.API.Features;
using System.Collections.Generic;
using SemanticVersioning;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using Exiled.API.Features.Roles;
using NetworkManagerUtils.Dummies;
using Exiled.Events.Handlers;
using Exiled.API.Enums;
using CustomPlayerEffects;

namespace Enemies
{
    public class ClassD : Enemy
    {
        float range = 20;
        float fireRate = 0.5f;
        float updateDuration = 0.1f;
        bool canShoot = true;
        float moveBackMinDist = 4f;

        CoroutineHandle followootine;
        CoroutineHandle targetSetRootine;
        CoroutineHandle enemyRootine;

        DummyAction? shootAction;

        InventorySystem.Items.Firearms.Firearm firearm;
        InventorySystem.Items.Firearms.Modules.MagazineModule magModule;
        public ClassD(string enemyName, Vector3 spawnPos, int id, Dictionary<int, Enemy> container, int mulCount) : base(enemyName, spawnPos, id, container, mulCount)
        {
            selfPlayer.Role.Set(PlayerRoles.RoleTypeId.ClassD);
            selfPlayer.EnableEffect<MovementBoost>(30, -1, false);
            selfPlayer.EnableEffect<SilentWalk>(200, -1, false);
            selfPlayer.EnableEffect<SpawnProtected>(5, true);
            selfPlayer.ClearInventory();
            selfPlayer.MaxHealth = 80 + mulCount*5;//35명 -> 255HP
            selfPlayer.Health = 80 + mulCount * 5;
            fpc = selfPlayer.RoleManager.CurrentRole as IFpcRole;

            ItemBase item = selfPlayer.Inventory.ServerAddItem(ItemType.GunCOM18, ItemAddReason.AdminCommand);
            selfPlayer.Inventory.ServerSelectItem(item.ItemSerial);
            firearm = item as InventorySystem.Items.Firearms.Firearm;
            firearm.TryGetModule<MagazineModule>(out magModule);

            selfPlayer.Position = spawnPos;
            Timing.CallDelayed(0.5f, () => {
                if (removed) return;
                foreach (DummyAction a in DummyActionCollector.ServerGetActions(hub))
                {
                    if (a.Name.EndsWith("Shoot->Click")) { shootAction = a; break; }
                }
                targetSetRootine = Timing.RunCoroutine(RerollTarget());
                followootine = Timing.RunCoroutine(FollowLoop());
                enemyRootine = Timing.RunCoroutine(EnemyFunction());
            });
        }
        public override void RemoveEnemy()
        {
            if (removed) return;
            removed = true;
            Timing.KillCoroutines(targetSetRootine);
            Timing.KillCoroutines(followootine);
            Timing.KillCoroutines(enemyRootine);

            base.RemoveEnemy();//인벤클리어&이벤트끊기&리스트제거
        }

        private IEnumerator<float> EnemyFunction()
        {
            while (!removed)
            {
                yield return Timing.WaitForSeconds(updateDuration);
                FollowAndLook();
                if (!canShoot) continue;
                if (targetPlayer == null) continue;

                Vector3 lookDirection = targetPlayer.Position - selfPlayer.Position;
                if (lookDirection.magnitude > 0)
                {
                    if (lookDirection.magnitude > range) continue;
                    bool shootCast = Physics.Raycast(selfPlayer.Position, lookDirection.normalized, out RaycastHit hitInfo, maxDistance: lookDirection.magnitude, layerMask: LayerMask.GetMask("Default", "Door"), queryTriggerInteraction: QueryTriggerInteraction.Ignore);
                    if (shootCast) continue;

                    canShoot = false;

                    //Server.ExecuteCommand($"/dummy action {selfPlayer.Id}. GunCOM18_(#{firearm.ItemSerial}) Shoot->Click");
                    if (!shootAction.HasValue)
                    {
                        foreach (var a in DummyActionCollector.ServerGetActions(hub))
                        {
                            if (a.Name.EndsWith("Shoot->Click")) { shootAction = a; break; }
                        }
                        continue;
                    }
                    shootAction.Value.Action();

                    magModule.ServerModifyAmmo(1);
                    Timing.CallDelayed(fireRate, () => canShoot = true);
                    if (followEnabled && lookDirection.magnitude < moveBackMinDist)
                    {
                        followEnabled = false;
                        Timing.CallDelayed(1, () => { if (removed) return; followEnabled = true; });
                    }
                }
            }
        }
    }
}
