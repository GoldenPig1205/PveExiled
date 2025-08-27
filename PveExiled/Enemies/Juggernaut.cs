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
using Exiled.Events.EventArgs.Player;
using Exiled.API.Features.Toys;
using LabApi.Features.Wrappers;
using CentralAuth;

namespace Enemies
{
    public class Juggernaut : Enemy
    {
        float range = 30;
        //float fireRate = 0.04f;//margin
        float updateDuration = 0.1f;
        float moveBackMinDist = 5f;

        bool shooting = false;
        bool attackStateChangeable = true;

        CoroutineHandle followootine;
        CoroutineHandle targetSetRootine;
        CoroutineHandle enemyRootine;

        DummyAction? holdAction;
        DummyAction? releaseAction;
        DummyAction? aimAction;
        DummyAction? unaimAction;

        InventorySystem.Items.Firearms.Firearm firearm;
        InventorySystem.Items.Firearms.Modules.MagazineModule magModule;
        public Juggernaut(string enemyName, Vector3 spawnPos, int id, Dictionary<int, Enemy> container, int mulCount) : base(enemyName, spawnPos, id, container, mulCount)
        {
            selfPlayer.Role.Set(PlayerRoles.RoleTypeId.ChaosConscript);
            selfPlayer.EnableEffect<Slowness>(30, -1, false);
            selfPlayer.EnableEffect<SpawnProtected>(5, true);
            selfPlayer.ClearInventory();
            selfPlayer.MaxHealth = 500 + mulCount*50;//35명 -> 2250HP
            selfPlayer.Health = 500 + mulCount * 50;
            fpc = selfPlayer.RoleManager.CurrentRole as IFpcRole;

            selfPlayer.Inventory.ServerAddItem(ItemType.ArmorHeavy, ItemAddReason.AdminCommand);
            Firearm item = Firearm.Create(FirearmType.Logicer);
            item.Give(selfPlayer);
            selfPlayer.Inventory.ServerSelectItem(item.Serial);
            firearm = item.Base;
            firearm.TryGetModule<MagazineModule>(out magModule);
            item.Inaccuracy = 0;

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
            if (!aimAction.HasValue)
            {
                foreach (DummyAction a in DummyActionCollector.ServerGetActions(hub))
                {
                    if (a.Name.EndsWith("Zoom->Hold")) { aimAction = a; break; }
                }
            }
            if (!unaimAction.HasValue)
            {
                foreach (DummyAction a in DummyActionCollector.ServerGetActions(hub))
                {
                    if (a.Name.EndsWith("Zoom->Click")) { unaimAction = a; break; }
                }
            }
        }
        public void ReleaseTrigger()
        {
            if (shooting)
            {
                shooting = false;
                releaseAction.Value.Action();
                unaimAction.Value.Action();
                attackStateChangeable = false;
                Timing.CallDelayed(4, () => attackStateChangeable = true);
            }
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
                if (followEnabled && lookDirection.magnitude < moveBackMinDist)
                {
                    followEnabled = false;
                    Timing.CallDelayed(1, () => followEnabled = true);
                }
                if (!attackStateChangeable) { magModule.ServerModifyAmmo(50); continue; }
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
                    magModule.ServerModifyAmmo(50);

                    if (shooting) continue;

                    shooting = true;
                    attackStateChangeable = false;
                    NullActionCheck();
                    aimAction.Value.Action();
                    Timing.CallDelayed(2, () => {
                        if (removed) return;
                        holdAction.Value.Action();
                        Timing.CallDelayed(10, () => { if (removed) return; attackStateChangeable = true; });
                    });
                }
            }
        }
    }
}
