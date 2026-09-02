# Multiplayer Networking Design

Project Aria uses **Unity Netcode for GameObjects (NGO)** with a **server-authoritative** model. This document explains the design.

## 🎯 Goals

- 2-20 players per world
- Cheat-resistant
- Smooth on mobile networks (3G/4G)
- Drop-in / drop-out

## 🏛️ Architecture

### Server-Authoritative

The server is the source of truth for **all gameplay-affecting state**:
- Block placement / breaking
- Combat damage
- Inventory changes
- NPC behavior
- World event triggers

Clients only render + send input. The server validates and broadcasts.

```
Client (predicts locally) → Server (validates) → All Clients (apply authoritative state)
```

### Anti-Cheat Chokepoint — `ServerAuthority`

Every gameplay-affecting action goes through a `TryApprove*` method that checks:
- **Rate limit** (e.g. max 12 blocks/sec, 6 attacks/sec)
- **Distance** (action target within reach)
- **Sanity** (damage in [0, 1000], item id valid)

If `TryApprove*` returns `false`, the action is rejected. This blocks:
- Speed hacks (distance check)
- Damage hacks (sanity + max damage cap)
- Block-id spoofing (id range)
- Inventory duplication (server-only add)

### Client Prediction

To hide network latency, clients predict the result of their own actions immediately. The server confirms or rolls back.

Example — player attacks:
```
1. Client: spawn attack hitbox, apply damage locally (predicted)
2. Client → Server: SendAttack(targetId, damage)
3. Server: validate via ServerAuthority
4. Server: broadcast ApplyDamage(targetId, damage) to all clients
5. Other clients: apply authoritative damage
6. Attacker client: confirm or reconcile
```

Rollback is **not implemented in MVP** (it would require history snapshots). The hooks are in place.

## 🌐 Network Topology

- **Host mode** — one player hosts (server + client), others join
- **Client mode** — pure client
- **Server mode** (dedicated) — headless server for large worlds (future)

Default transport: **UnityTransport (UTP)** over UDP. Reliable for important events, unreliable for movement.

## 📦 Data Sync

| Data | Method | Frequency |
|---|---|---|
| Player position | NetworkVariable | 30 Hz (tick rate) |
| Player rotation | NetworkVariable | 30 Hz |
| HP / stats | NetworkVariable | On change |
| Inventory | RPC (manual sync) | On change |
| Block changes | RPC | On place/break |
| World time | NetworkVariable | 1 Hz |
| Weather | NetworkVariable | On change |
| NPC state | NetworkVariable | On change |
| Chat | ClientRpc | On send |

## 🔌 Connection Lifecycle

```
1. Host: StartHost() → binds UDP port
2. Client: StartClient(address, port)
3. Server: ConnectionApprovalCallback decides accept/reject
   - Check: player count < 20
   - Check: not banned
4. On approve: spawn player NetworkObject
5. Game state syncs via NetworkVariables
6. Player disconnects → server despawns NetworkObject
```

## 💬 Chat, Party, Trading

- **Chat** — `SendChatServerRpc` → `BroadcastChatServerRpc` to all clients
- **Party** — players in same group share XP, can teleport. Server holds the party list.
- **Trading** — both players must be within 3m. Server mediates the trade: validates item ownership on both sides, then atomically swaps.

## 🌍 World Sync

For MVP, the world is **shared** — every player sees the same blocks. When a player places/breaks a block:

```
Client: WorldManager.SetBlockWorld(pos, id)
  → if (IsServer) directly apply
  → else Send ServerRpc with pos + id
  → Server: ServerAuthority.TryApproveBlockPlace
  → if approved: WorldManager.SetBlockWorld + ClientRpc to broadcast
  → All clients: apply block change
```

For **per-player worlds** (future feature), the server holds per-player chunk states and only sends relevant diffs.

## ⚠️ Mobile Considerations

- **3G/4G latency** — 100-300ms RTT typical. Keep tick rate at 30 (not 60).
- **Battery** — WiFi-only multiplayer on mobile by default (toggle in settings)
- **Bandwidth** — under 50 KB/s per client for movement; spikes on chunk sync
- **Packet loss** — NGO uses reliable channels for important state

## 🛡️ Future Hardening

- [ ] Rollback netcode for combat
- [ ] Server-side rate limiting per client (beyond per-action)
- [ ] Encrypted transport (UTP supports TLS)
- [ ] Anti-DoS: connection rate limit per IP
- [ ] Server-side validation of all ScriptableObject effects
- [ ] Encrypted save files
- [ ] Server-side NPC AI (not client-predict)
- [ ] Lag compensation for melee attacks
- [ ] Snapshot interpolation for movement
