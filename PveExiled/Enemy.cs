using CentralAuth;
using CustomPlayerEffects;
using Mirror;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Exiled;
using Exiled.API.Features;
using Exiled.API.Enums;
using MEC;
using RelativePositioning;
using PlayerRoles.FirstPersonControl;

public class Enemy
{
    public int Id { get; }
    public Dictionary<int, Enemy>  Container { get; }

    protected bool removed = false;
    protected ReferenceHub hub;
    protected Player selfPlayer;
    protected bool finished = false;
    protected bool followEnabled = true;
    protected IFpcRole fpc;

    protected Player targetPlayer;
    protected Vector3 targetPosition = Vector3.zero;

    //props
    protected bool hiddenDetect = false;
    protected float pathCompCheckTime = 0.5f;
    public Enemy(string enemyName, Vector3 spawnPos, int id, Dictionary<int, Enemy> container, int mulCount)//hub, selfPlayer지정
    {
        hub = DummyUtils.SpawnDummy(enemyName);
        hub.authManager.syncMode = (SyncMode)ClientInstanceMode.Dummy;
        hub.playerStats.OnThisPlayerDied += RemoveEnemy;
        selfPlayer = Exiled.API.Features.Player.Get(hub);
        this.Id = id;
        this.Container = container;
    }
    public virtual void RemoveEnemy(DamageHandlerBase _) => RemoveEnemy();
    public virtual void RemoveEnemy()//이벤트끊기&리스트에서 Enemy삭제
    {
        selfPlayer.ClearInventory();
        hub.playerStats.OnThisPlayerDied -= RemoveEnemy;
        if (Container != null && Container.TryGetValue(Id, out Enemy self) && self != null) Container.Remove(Id);
    }
    protected void FollowAndLook()
    {
        Vector3 direction = targetPosition - selfPlayer.Position;
        direction.y = 0;
        if (fpc == null)
        {
        }
        if (direction.magnitude > 0)
        {
            if (followEnabled) fpc.FpcModule.Motor.ReceivedPosition = new RelativePosition(selfPlayer.Position + direction.normalized);
            else fpc.FpcModule.Motor.ReceivedPosition = new RelativePosition(selfPlayer.Position - hub.transform.forward);
        }
        if (direction.magnitude < 0.5 || direction.magnitude > 10)
        {
            finished = true;
        }

        if (targetPlayer == null) return;
        Vector3 lookDirection = targetPlayer.Position - selfPlayer.Position;
        if (lookDirection.magnitude > 0)
        {
            fpc.FpcModule.MouseLook.LookAtDirection(lookDirection.normalized);
        }
    }
    protected IEnumerator<float> RerollTarget()
    {
        while (!removed)
        {
            targetPlayer = GetClosestPlayer();
            yield return Timing.WaitForSeconds(3f);//3초마다 목표물 갱신
        }
    }
    protected IEnumerator<float> FollowLoop()
    {
        while (true)
        {
            if (targetPlayer == null)
            {
                yield return Timing.WaitForSeconds(0.5f);
                continue;
            }
            Vector3[] paths = GetPath(targetPlayer.Position);
            if (paths == null)
            {
                targetPosition = targetPlayer.Position;
                yield return Timing.WaitForSeconds(0.5f);
                continue;
            }
            bool c = true;
            float trackStartTimeStamp = Time.time;
            foreach (Vector3 pathPosition in paths)
            {
                if (c) { c = false; continue; }
                if (Time.time - trackStartTimeStamp > 3) break;
                targetPosition = pathPosition+new Vector3(UnityEngine.Random.value*0.05f-0.025f,0,UnityEngine.Random.value*0.05f-0.025f);
                float timeStamp = Time.time;
                finished = false;
                while (!finished && Time.time - timeStamp <= 4) { if (!followEnabled) { timeStamp = Time.time; } yield return Timing.WaitForSeconds(pathCompCheckTime); }
                if (Time.time - timeStamp > 4)
                {
                    selfPlayer.Position = selfPlayer.CurrentRoom.Position + Vector3.up;
                    break;
                }
            }
            continue;
        }
    }
    protected Player GetClosestPlayer()
    {
        if (removed) return null;
        float bestDistance = float.MaxValue;
        Player best = null;
        foreach (Player player in Player.List)
        {
            if (!IsTargetablePlayer(player)) continue;
            float distance = (player.Position - hub.transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = player;
            }
        }
        return best;
    }

    protected Vector3[] GetPath(Vector3 target)
    {
        if (!NavMesh.SamplePosition(hub.transform.position, out var startHit, 25f, NavMesh.AllAreas))
        {
            return null;
        }
        if (!NavMesh.SamplePosition(target, out var targetHit, 25f, NavMesh.AllAreas))
        {
            return null;
        }

        int areaMask = NavMesh.AllAreas;
        var path = new NavMeshPath();
        if (NavMesh.CalculatePath(startHit.position, targetHit.position, areaMask, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete) return path.corners; // 경로 지점들 반환
        }
        selfPlayer.Position = selfPlayer.CurrentRoom.Position + Vector3.up;//낌 방지
        return null;//아님말고
    }
    protected bool IsTargetablePlayer(Player player)
    {
        if (player == null) return false;
        if (player.UserId == "ID_Dedicated" || player.UserId == "ID_Dummy" || player.IsNPC) return false;
        if (player.Role.Type != RoleTypeId.NtfSergeant) return false;
        if (!hiddenDetect && player.IsEffectActive<Invisible>()) return false;
        return true;
    }
}