using SR2MP.Packets.FX;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.PlayerFX)]
public sealed class PlayerFXHandler : BaseClientPacketHandler<PlayerFXPacket>
{
    public PlayerFXHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(PlayerFXPacket packet)
    {
        handlingPacket = true;
        try
        {
            if (!IsPlayerSoundDictionary[packet.FX])
            {
                if (fxManager.PlayerFXMap.TryGetValue(packet.FX, out var fxPrefab) && fxPrefab)
                    FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
                handlingPacket = false;
                return;
            }

            var cue = fxManager.PlayerAudioCueMap[packet.FX];

            if (ShouldPlayerSoundBeTransientDictionary[packet.FX])
            {
                RemoteFXManager.PlayTransientAudio(cue, playerObjects[packet.Player].transform.position,
                    PlayerSoundVolumeDictionary[packet.FX]);
            }
            else
            {
                var playerAudio = playerObjects[packet.Player].GetComponent<SECTR_PointSource>();

                playerAudio.Cue = cue;
                playerAudio.Loop = DoesPlayerSoundLoopDictionary[packet.FX];

                playerAudio.instance.Volume = PlayerSoundVolumeDictionary[packet.FX];
                playerAudio.Play();
            }
        }
        catch
        {
            // SrLogger.LogWarning($"This \"error\" is NOT serious, DO NOT REPORT IT!\n{ex}");
        }
        handlingPacket = false;
    }
}