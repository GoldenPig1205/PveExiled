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
    public class Scout : Enemy
    {
        float range = 24;
        //float fireRate = 0.04f;//margin
        float updateDuration = 0.1f;
        float moveBackMinDist = 5f;

        bool shooting = false;
        bool reloading = false;

        CoroutineHandle followootine;
        CoroutineHandle targetSetRootine;
        CoroutineHandle enemyRootine;

        DummyAction? holdAction;
        DummyAction? releaseAction;
        DummyAction? reloadAction;

        InventorySystem.Items.Firearms.Firearm firearm;
        InventorySystem.Items.Firearms.Modules.MagazineModule magModule;
        public Scout(string enemyName, Vector3 spawnPos, int id, Dictionary<int, Enemy> container, int mulCount) : base(enemyName, spawnPos, id, container, mulCount)
        {
            selfPlayer.Role.Set(PlayerRoles.RoleTypeId.ChaosConscript);
            selfPlayer.EnableEffect<SpawnProtected>(5, true);
            selfPlayer.ClearInventory();
            selfPlayer.MaxHealth = 100 + mulCount*5;//35명 -> 275HP
            selfPlayer.Health = 100 + mulCount * 5;
            fpc = selfPlayer.RoleManager.CurrentRole as IFpcRole;

            ItemBase item = selfPlayer.Inventory.ServerAddItem(ItemType.GunCrossvec, ItemAddReason.AdminCommand);
            selfPlayer.Inventory.ServerSelectItem(item.ItemSerial);
            firearm = item as InventorySystem.Items.Firearms.Firearm;
            firearm.TryGetModule<MagazineModule>(out magModule);

            selfPlayer.Position = spawnPos;
            Timing.CallDelayed(0.5f, () => {
                if (removed) return;
                NullActionCheck();
                targetSetRootine = Timing.RunCoroutine(RerollTarget());
                followootine = Timing.RunCoroutine(FollowLoop());
                enemyRootine = Timing.RunCoroutine(EnemyFunction());
                magModule.ServerModifyAmmo(50);
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
        public void NullActionCheck()
        {
            if (!holdAction.HasValue)
            {
                foreach (DummyAction a in DummyActionCollector.ServerGetActions(hub))
                {
                    if (a.Name.EndsWith("Shoot->Hold")) { holdAction = a; break; }
                }
            }
            if (!releaseAction.HasValue)
            {
                foreach (DummyAction a in DummyActionCollector.ServerGetActions(hub))
                {
                    if (a.Name.EndsWith("Shoot->Click")) { releaseAction = a; break; }
                }
            }
            if (!reloadAction.HasValue)
            {
                foreach (DummyAction a in DummyActionCollector.ServerGetActions(hub))
                {
                    if (a.Name.EndsWith("Reload->Click")) { reloadAction = a; break; }
                }
            }
        }
        public void ReleaseTrigger()
        {
            if (shooting)
            {
                shooting = false;
                releaseAction.Value.Action();
            }
            if (magModule.AmmoStored < 10)
            {
                Reload();
            }
        }
        public void Reload()
        {
            shooting = false;
            reloading = true;
            releaseAction.Value.Action();
            Timing.CallDelayed(0.1f, () =>
            {
                if (removed) return;
                selfPlayer.AddAmmo(AmmoType.Nato9, 1);
                reloadAction.Value.Action();
                Timing.CallDelayed(4, () => { if (removed) return; reloading = false; magModule.ServerModifyAmmo(50); }); ;
            });
        }

        private IEnumerator<float> EnemyFunction()
        {
            while (!removed)
            {
                yield return Timing.WaitForSeconds(updateDuration);
                FollowAndLook();
                if (targetPlayer == null) continue;

                //Shoot판단
                Vector3 lookDirection = targetPlayer.Position - selfPlayer.Position;
                if (lookDirection.magnitude > 0)
                {
                    if (lookDirection.magnitude > range)//사거리 밖
                    {
                        ReleaseTrigger();
                        continue;
                    }
                    bool shootCast = Physics.Raycast(selfPlayer.Position, lookDirection.normalized, out RaycastHit hitInfo, maxDistance: lookDirection.magnitude, layerMask: LayerMask.GetMask("Default", "Door"), queryTriggerInteraction: QueryTriggerInteraction.Ignore);
                    if (shootCast)
                    {
                        ReleaseTrigger();
                        continue;
                    }
                    if (followEnabled && lookDirection.magnitude < moveBackMinDist)
                    {
                        followEnabled = false;
                        Timing.CallDelayed(1, () => { if (removed) return; followEnabled = true; });
                    }

                    if (reloading) continue;
                    if (magModule.AmmoStored <= 0)
                    {
                        Reload();
                        continue;
                    }
                    if (shooting) continue;

                    shooting = true;
                    NullActionCheck();
                    holdAction.Value.Action();
                }
            }
        }
    }
}
