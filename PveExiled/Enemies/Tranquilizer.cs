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
    public class Tranquilizer : Enemy
    {
        float range = 30;
        float fireRate = 2f;
        float updateDuration = 0.1f;
        float moveBackMinDist = 20;

        bool canShoot = true;
        bool aiming = false;
        float aimStartTime = 0;

        CoroutineHandle followootine;
        CoroutineHandle targetSetRootine;
        CoroutineHandle enemyRootine;

        DummyAction? shootAction;
        DummyAction? zoomAction;
        DummyAction? unzoomAction;

        InventorySystem.Items.Firearms.Firearm firearm;
        InventorySystem.Items.Firearms.Modules.MagazineModule magModule;
        public Tranquilizer(string enemyName, Vector3 spawnPos, int id, Dictionary<int, Enemy> container, int mulCount) : base(enemyName, spawnPos, id, container, mulCount)
        {
            selfPlayer.RoleManager.ServerSetRole(PlayerRoles.RoleTypeId.ChaosRepressor, PlayerRoles.RoleChangeReason.RemoteAdmin);
            selfPlayer.EnableEffect<MovementBoost>(30, -1, false);
            selfPlayer.EnableEffect<SilentWalk>(200, -1, false);
            selfPlayer.EnableEffect<SpawnProtected>(5, true);
            selfPlayer.ClearInventory();
            selfPlayer.MaxHealth = 150 + mulCount*5;//35명 -> 325HP
            selfPlayer.Health = 150 + mulCount * 5;
            fpc = selfPlayer.Role.Base as IFpcRole;

            Firearm item = Firearm.Create(FirearmType.Com15);
            item.Give(selfPlayer);
            selfPlayer.Inventory.ServerSelectItem(item.Serial);
            firearm = item.Base;
            firearm.TryGetModule<MagazineModule>(out magModule);
            item.Damage = 0;

            selfPlayer.Position = spawnPos;
            Timing.CallDelayed(0.5f, () => {
                if (removed) return;
                NullActionCheck();
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
        private void NullActionCheck()
        {
            if (!shootAction.HasValue)
            {
                foreach (DummyAction a in DummyActionCollector.ServerGetActions(hub))
                {
                    if (a.Name.EndsWith("Shoot->Click")) { shootAction = a; break; }
                }
            }
            if (!zoomAction.HasValue)
            {
                foreach (DummyAction a in DummyActionCollector.ServerGetActions(hub))
                {
                    if (a.Name.EndsWith("Zoom->Hold")) { zoomAction = a; break; }
                }
            }
            if (!unzoomAction.HasValue)
            {
                foreach (DummyAction a in DummyActionCollector.ServerGetActions(hub))
                {
                    if (a.Name.EndsWith("Zoom->Click")) { unzoomAction = a; break; }
                }
            }
        }
        private void doUnzoom()
        {
            if (!aiming) return;
            aiming = false;
            unzoomAction.Value.Action();
        }
        private IEnumerator<float> EnemyFunction()
        {
            while (!removed)
            {
                yield return Timing.WaitForSeconds(updateDuration);
                FollowAndLook();
                if (targetPlayer == null || targetPlayer.Role.Type != PlayerRoles.RoleTypeId.NtfSergeant)
                {
                    doUnzoom();
                    continue;
                }

                Vector3 lookDirection = targetPlayer.Position - selfPlayer.Position;
                if (lookDirection.magnitude > 0)
                {
                    if (lookDirection.magnitude > range)
                    {
                        doUnzoom();
                        continue;
                    }
                    bool shootCast = Physics.Raycast(selfPlayer.Position, lookDirection.normalized, out RaycastHit hitInfo, maxDistance: lookDirection.magnitude, layerMask: LayerMask.GetMask("Default", "Door"), queryTriggerInteraction: QueryTriggerInteraction.Ignore);
                    if (shootCast)
                    {
                        doUnzoom();
                        continue;
                    }
                    if (followEnabled && lookDirection.magnitude < moveBackMinDist)
                    {
                        followEnabled = false;
                        Timing.CallDelayed(1, () => { if (removed) return; followEnabled = true; });
                    }
                    if (!canShoot) continue;
                    if (!aiming)
                    {
                        aiming = true;
                        aimStartTime = Time.time;
                        NullActionCheck();
                        zoomAction.Value.Action();

                        continue;
                    }
                    else if ( Time.time - aimStartTime < 3) continue;

                    canShoot = false;
                    aiming = false;

                    targetPlayer.EnableEffect<Sinkhole>(4, true);
                    targetPlayer.EnableEffect<Blurred>(100, 4, true);
                    targetPlayer.EnableEffect<Concussed>(8, true);

                    unzoomAction.Value.Action();
                    shootAction.Value.Action();
                    magModule.ServerModifyAmmo(1);
                    Timing.CallDelayed(fireRate, () => { if (removed) return; canShoot = true; });
                }
            }
        }
    }
}
