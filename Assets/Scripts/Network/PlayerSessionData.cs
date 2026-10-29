using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerSessionData : INetworkSerializable, IEquatable<PlayerSessionData>
{
    public ulong clientId;
    public int playerIndex;
    public FixedString32Bytes playerName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerIndex);
        serializer.SerializeValue(ref playerName);
    }

    public bool Equals(PlayerSessionData other)
    {
        return clientId == other.clientId && playerIndex == other.playerIndex && playerName.Equals(other.playerName);
    }
}
