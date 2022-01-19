using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eft_dma_radar
{
    internal static class Offsets
    {
        public const uint ModuleBase_GameObjectManager = 0x17F8D28;
        public static readonly uint[] GameWorld_LocalGameWorld = new uint[] { 0x30, 0x18, 0x28 };
        
        public const uint RegisteredPlayers = 0x80;
        public const uint RegisteredPlayers_Count = 0x18;

        public const uint PlayerBase_Profile = 0x4B8;
        public const uint PlayerBase_MovementContext = 0x40;
        public const uint PlayerBase_IsLocalPlayer = 0x7FB;
        public static readonly uint[] PlayerBase_HealthController = new uint[] { 0x4F0, 0x50, Offsets.UnityDictBase };
        public static readonly uint[] PlayerBase_PlayerTransformInternal = new uint[] { 0xA8, 0x28, 0x28, 0x10, 0x20, 0x10 };

        public const uint MovementContext_Direction = 0x22C;

        public const uint PlayerTransformInternal_PlayerTransfPMatrix = 0x38;
        public const uint PlayerTransformInternal_Index = 0x40;

        public const uint PlayerTransfPMatrix_PlayerTransformDependencyIndexTableBase = 0x20;

        public const uint HealthEntry = 0x10;
        public const uint HealthEntry_Value = 0x10;

        public const uint PlayerProfile_PlayerId = 0x10;
        public const uint PlayerProfile_PlayerInfo = 0x28;

        public const uint PlayerInfo_PlayerName = 0x10;
        public const uint PlayerInfo_PlayerSide = 0x58;
        public const uint PlayerInfo_RegDate = 0x5C;

        public const uint UnityDictBase = 0x18;

        public const uint UnityListBase = 0x10;
        public const uint UnityListBase_Start = 0x20;

        public const uint UnityString_Len = 0x10;
        public const uint UnityString_Value = 0x14;

        public const uint UnityObject_Name = 0x60;
    }
}
