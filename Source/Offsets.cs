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
        public const uint PlayerBase_Physical = 0x4C8;
        public const uint PlayerBase_MovementContext = 0x40;
        public const uint PlayerBase_IsLocalPlayer = 0x7FB;
        public static readonly uint[] PlayerBase_HealthController = new uint[] { 0x4F0, 0x50, Offsets.UnityDictBase };
        public static readonly uint[] PlayerBase_PlayerTransformInternal = new uint[] { 0xA8, 0x28, 0x28, 0x10, 0x20, 0x10 };

        public const uint MovementContext_Direction = 0x22C;

        public const uint PlayerTransformInternal_PlayerTransfPMatrix = 0x38;
        public const uint PlayerTransformInternal_Index = 0x40;

        public const uint PlayerTransfPMatrix_PlayerTransformDependencyIndexTableBase = 0x20;
        public const uint Stamina = 0x38;
        public const uint HandsStamina = 0x40;
        public const uint Oxygen = 0x48;
        public const uint Stamina_Value = 0x48;
        public const uint HealthEntry = 0x10;
        public const uint HealthEntry_Value = 0x10;

        public const uint PlayerProfile_PlayerId = 0x10;
        public const uint PlayerProfile_PlayerInfo = 0x28;
        public const uint PlayerProfile_Skills = 0x60;
        public const uint WeaponBuffs = 0x10;// (类型 : System.Collections.Generic.Dictionary<System.Type, System.Collections.Generic.Dictionary<EFT.EBuffId,>>)
        public const uint AttentionLootSpeed = 0x150;//增加搜索速度0.02f
        public const uint AttentionExamine = 0x158;//增加找到更好战利品的几率+检视速度0.02f
        public const uint AttentionEliteLuckySearch = 0x160;//有几率瞬间完成搜索0.5elite
        public const uint AttentionEliteExtraLootExp = 0x168;//双倍搜索经验bool
        public const uint MagDrillsLoadSpeed = 0x170;//提高弹药装填速度0.6
        public const uint MagDrillsUnloadSpeed = 0x178;//提高弹药的卸载速度0.6
        public const uint MagDrillsInventoryCheckSpeed = 0x180;//提高使用菜单检查弹匣的速度0.8
        public const uint MagDrillsInventoryCheckAccuracy = 0x188;//提高使用菜单检查弹匣的准确性
        public const uint MagDrillsInstantCheck = 0x190;//移动到您的库存后立即检查弹匣bool
        public const uint MagDrillsLoadProgression = 0x198;//bool
        public const uint StrengthBuffJumpHeightInc = 0x58;// 0.6 no fall damage
        public const uint StrengthBuffThrowDistanceInc = 0x68; // 0.6 perfect
        public const uint currentmovementcontext = 0xB8;
        public const uint FreefallTime = 0x1F8;
        public const uint Buff_Value = 0x28;

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
