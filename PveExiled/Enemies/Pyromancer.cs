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
    public class Pyromancer : Enemy
    {
        float range = 15;
        //float fireRate = 0.04f;//margin
        float updateDuration = 0.1f;
        float moveBackMinDist = 5f;
        float absShotRange = 10;

        bool shooting = false;

        CoroutineHandle followootine;
        CoroutineHandle targetSetRootine;
        CoroutineHandle enemyRootine;

        DummyAction? holdAction;
        DummyAction? releaseAction;
        DummyAction? reloadAction;

        InventorySystem.Items.MicroHID.MicroHIDItem hid;
        public Pyromancer(string enemyName, Vector3 spawnPos, int id, Dictionary<int, Enemy> container, int mulCount) : base(enemyName, spawnPos, id, container, mulCount)
        {
            selfPlayer.RoleManager.ServerSetRole(PlayerRoles.RoleTypeId.ChaosRepressor, PlayerRoles.RoleChangeReason.RemoteAdmin);
            selfPlayer.EnableEffect<SpawnProtected>(5, true);
            selfPlayer.ClearInventory();
            selfPlayer.MaxHealth = 250 + mulCount*12;//35명 -> 670HP
            selfPlayer.Health = 250 + mulCount * 12;
            fpc = selfPlayer.Role.Base as IFpcRole;

            selfPlayer.Inventory.ServerAddItem(ItemType.ArmorHeavy, ItemAddReason.AdminCommand);
            ItemBase item = selfPlayer.Inventory.ServerAddItem(ItemType.MicroHID, ItemAddReason.AdminCommand);
            selfPlayer.Inventory.ServerSelectItem(item.ItemSerial);
            hid = item as InventorySystem.Items.MicroHID.MicroHIDItem;

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
        }
        public void ReleaseTrigger()
        {
            if (shooting)
            {
                absShotRange -= 2;
                shooting = false;
                releaseAction.Value.Action();
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
                if (lookDirection.magnitude > 0)
                {
                    if (lookDirection.magnitude > range)//사거리 밖
                    {
                        ReleaseTrigger();
                        continue;
                    }
                    bool shootCast = Physics.Raycast(selfPlayer.Position, lookDirection.normalized, out RaycastHit hitInfo, maxDistance: lookDirection.magnitude, layerMask: LayerMask.GetMask("Default", "Door"), queryTriggerInteraction: QueryTriggerInteraction.Ignore);
                    if (shootCast && lookDirection.magnitude>absShotRange)
                    {
                        ReleaseTrigger();
                        continue;
                    }
                    hid.EnergyManager.ServerSetEnergy(hid.ItemSerial, 100);
                    if (followEnabled && lookDirection.magnitude < moveBackMinDist)
                    {
                        followEnabled = false;
                        Timing.CallDelayed(1, () => { if (removed) return; followEnabled = true; });
                    }

                    if (shooting) continue;

                    shooting = true;
                    absShotRange += 2;
                    NullActionCheck();
                    holdAction.Value.Action();
                }
            }
        }
    }
}
