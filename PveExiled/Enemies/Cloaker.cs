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
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using Exiled.API.Features.Roles;
using NetworkManagerUtils.Dummies;
using Exiled.Events.Handlers;
using Exiled.API.Enums;
using CustomPlayerEffects;

namespace Enemies
{
    public class Cloaker : Enemy
    {
        float visibleRange = 10;
        float range = 1.5f;
        //float fireRate = 0.04f;//margin
        float updateDuration = 0.1f;

        bool shooting = false;
        bool invisible = true;

        CoroutineHandle followootine;
        CoroutineHandle targetSetRootine;
        CoroutineHandle enemyRootine;

        DummyAction? holdAction;
        DummyAction? releaseAction;
        DummyAction? reloadAction;

        InventorySystem.Items.Jailbird.JailbirdItem firearm;
        public Cloaker(string enemyName, Vector3 spawnPos, int id, Dictionary<int, Enemy> container, int mulCount) : base(enemyName, spawnPos, id, container, mulCount)
        {
            selfPlayer.Role.Set(PlayerRoles.RoleTypeId.ChaosConscript);
            selfPlayer.EnableEffect<SpawnProtected>(5, true);
            selfPlayer.EnableEffect<Invisible>(-1, false);
            selfPlayer.ClearInventory();
            selfPlayer.MaxHealth = 100;
            selfPlayer.Health = 100;//항상100
            fpc = selfPlayer.RoleManager.CurrentRole as IFpcRole;
            pathCompCheckTime = 0.1f;

            selfPlayer.Inventory.ServerAddItem(ItemType.ArmorHeavy, ItemAddReason.AdminCommand);
            ItemBase item = selfPlayer.Inventory.ServerAddItem(ItemType.Jailbird, ItemAddReason.AdminCommand);
            selfPlayer.Inventory.ServerSelectItem(item.ItemSerial);
            firearm = item as InventorySystem.Items.Jailbird.JailbirdItem;

            selfPlayer.Position = spawnPos;
            Timing.CallDelayed(0.5f, () => {
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
                if (invisible)
                {
                    if (lookDirection.magnitude > visibleRange)//사거리 밖이면 continue
                    {
                        continue;
                    }
                    selfPlayer.DisableEffect<Invisible>();//아니면 진행
                    selfPlayer.EnableEffect<MovementBoost>(150, -1, false);
                    invisible = false;
                }
                else
                {
                    if (lookDirection.magnitude > visibleRange+2)//사거리 밖이면 invisible하고 continue
                    {
                        selfPlayer.EnableEffect<Invisible>(-1, false);
                        selfPlayer.DisableEffect<MovementBoost>();
                        invisible = true;
                        ReleaseTrigger();
                        continue;
                    }
                }

                if (lookDirection.magnitude > 0)
                {
                    if (lookDirection.magnitude > range)//사거리 밖
                    {
                        ReleaseTrigger();
                        continue;
                    }
                    firearm.ServerReset();

                    if (shooting) continue;
                    shooting = true;
                    NullActionCheck();
                    holdAction.Value.Action();
                }
            }
        }
    }
}
