using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace eft_dma_radar
{
    /// <summary>
    /// Class containing Game Player Data. Use lock() when accessing instances of this class.
    /// </summary>
    public class Player
    {
        private static int _currentPlayerGroupID = 0; // ToDo
        public readonly string Name;
        public readonly PlayerType Type;
        public readonly int GroupID; // ToDo not working
        private ulong _playerBase;
        private ulong _playerProfile;
        private ulong _playerInfo;
        private ulong _healthController;
        private ulong[] _bodyParts;
        private ulong _movementContext;
        private ulong _playerTransform;
        public int Health = -1;
        public bool IsAlive = true;
        public bool HasExfild = false;
        public Vector3 Position = new Vector3(0, 0, 0);
        public float Direction = 0f;

        public Player(ulong playerBase, ulong playerProfile)
        {
            try
            {
                _playerBase = playerBase;
                _playerProfile = playerProfile;
                _playerInfo = Memory.ReadPtr(playerProfile + 0x28);
                _healthController = Memory.ReadPtrChain(_playerBase, new uint[] { 0x4F0, 0x50, 0x18 });
                _bodyParts = new ulong[7];
                for (uint i = 0; i < 7; i++)
                {
                    _bodyParts[i] = Memory.ReadPtrChain(_healthController, new uint[] { 0x30 + (i * 0x18), 0x10 });
                }
                _movementContext = Memory.ReadPtr(_playerBase + 0x40);
                _playerTransform = Memory.ReadPtrChain(_playerBase, new uint[] { 0xA8, 0x28, 0x28, 0x10, 0x20 });
                GroupID = Memory.ReadInt(_playerInfo + 0x18);
                var playerNickname = Memory.ReadPtr(_playerInfo + 0x10);
                Name = Memory.ReadUnityString(playerNickname);
                var isLocalPlayer = Memory.ReadBool(_playerBase + 0x7FB);
                if (isLocalPlayer)
                {
                    Type = PlayerType.CurrentPlayer;
                    _currentPlayerGroupID = GroupID;
                }
                else
                {
                    var playerSide = Memory.ReadInt(_playerInfo + 0x58); // Scav, PMC, etc.
                    if (playerSide == 0x4)
                    {
                        var regDate = Memory.ReadInt(_playerInfo + 0x5C); // Bots wont have 'reg date'
                        if (regDate == 0) Type = PlayerType.AIScav;
                        else Type = PlayerType.PlayerScav;
                    }
                    //else if (GroupID == _currentPlayerGroupID) Type = PlayerType.Teammate;
                    else if (playerSide == 0x1 || playerSide == 0x2) Type = PlayerType.PMC;
                    else Type = PlayerType.Default;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR during Player constructor for base addr 0x{playerBase.ToString("X")}: {ex}");
                throw;
            }
        }

        /// <summary>
        ///  Update Player Information (only call from Memory Thread)
        /// </summary>
        public void Update()
        {
            try
            {
                // ToDo - check player death
                // ToDo - check player exfil
                if (IsAlive && !HasExfild) // Only update if alive/in-raid
                {

                    Position = GetPosition();
                    Direction = GetDirection();
                    Health = GetHealth();
                }
            } 
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR updating player '{Name}': {ex}");
            }
        }

        /// <summary>
        /// Get current player health.
        /// </summary>
        private int GetHealth()
        {
            float totalHealth = 0;
            for (uint i = 0; i < _bodyParts.Length; i++)
            {
                var health = Memory.ReadFloat(_bodyParts[i] + 0x10);
                totalHealth += health;
            }
            return (int)Math.Round(totalHealth);
        }

        private float GetDirection()
        {
            float deg = Memory.ReadFloat(_movementContext + 0x22C);
            if (deg < 0)
            {
                return 360f + deg;
            }
            return deg;
        }

        /// <summary>
        /// Converts player transform to X,Y,Z coordinates (Vector3)
        /// </summary>
        private unsafe Vector3 GetPosition()
        {
            ulong pMatricesBuf = 0;
            ulong pIndicesBuf = 0;
            int index = 0;

            var offsets = GetPositionOffset(_playerTransform);
            pMatricesBuf = offsets.Item1;
            pIndicesBuf = offsets.Item2;
            index = offsets.Item3;

            Vector4 result = *(Vector4*)((UInt64)pMatricesBuf + 0x30 * (UInt64)index);
            int index_relation = *(int*)((UInt64)pIndicesBuf + 0x4 * (UInt64)index);

            Vector4 xmmword_1410D1340 = new Vector4(-2.0f, 2.0f, -2.0f, 0.0f);
            Vector4 xmmword_1410D1350 = new Vector4(2.0f, -2.0f, -2.0f, 0.0f);
            Vector4 xmmword_1410D1360 = new Vector4(-2.0f, -2.0f, 2.0f, 0.0f);

            while (index_relation >= 0)
            {
                Matrix34 matrix34 = *(Matrix34*)((UInt64)pMatricesBuf + 0x30 * (UInt64)index_relation);

                Vector4 v10 = matrix34.vec2 * result;
                Vector4 v11 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(0)));
                Vector4 v12 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(85)));
                Vector4 v13 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(-114)));
                Vector4 v14 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(-37)));
                Vector4 v15 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(-86)));
                Vector4 v16 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(113)));
                result = (((((((v11 * xmmword_1410D1350) * v13) - ((v12 * xmmword_1410D1360) * v14)) * Shuffle(v10, (ShuffleSel)(-86))) +
                    ((((v15 * xmmword_1410D1360) * v14) - ((v11 * xmmword_1410D1340) * v16)) * Shuffle(v10, (ShuffleSel)(85)))) +
                    (((((v12 * xmmword_1410D1340) * v16) - ((v15 * xmmword_1410D1350) * v13)) * Shuffle(v10, (ShuffleSel)(0))) + v10)) + matrix34.vec0);
                index_relation = *(int*)((UInt64)pIndicesBuf + 0x4 * (UInt64)index_relation);
            }

            // Free mem
            Recycler.Pointers.Add(new IntPtr((long)pMatricesBuf));
            Recycler.Pointers.Add(new IntPtr((long)pIndicesBuf));

            return new Vector3(result.X, result.Z, result.Y);
        }

        /// <summary>
        /// Helper method for GetPosition()
        /// </summary>
        private unsafe Tuple<ulong, ulong, int> GetPositionOffset(ulong transform)
        {
            var transform_internal = Memory.ReadPtr(transform + 0x10);

            var pMatrix = Memory.ReadPtr(transform_internal + 0x38);
            int index = Memory.ReadInt(transform_internal + 0x40);

            var matrix_list_base = Memory.ReadPtr(pMatrix + 0x18);

            var dependency_index_table_base = Memory.ReadPtr(pMatrix + 0x20);

            IntPtr pMatricesBufPtr = new IntPtr(Marshal.AllocHGlobal(sizeof(Matrix34) * index + sizeof(Matrix34)).ToInt64()); // sizeof(Matrix34) == 48
            void* pMatricesBuf = pMatricesBufPtr.ToPointer();
            Memory.ReadBuffer(matrix_list_base, pMatricesBufPtr, sizeof(Matrix34) * index + sizeof(Matrix34));

            IntPtr pIndicesBufPtr = new IntPtr(Marshal.AllocHGlobal(sizeof(int) * index + sizeof(int)).ToInt64());
            void* pIndicesBuf = pIndicesBufPtr.ToPointer();
            Memory.ReadBuffer(dependency_index_table_base, pIndicesBufPtr, sizeof(int) * index + sizeof(int));

            return Tuple.Create((UInt64)pMatricesBuf, (UInt64)pIndicesBuf, index);
        }

        private static unsafe Vector4 Shuffle(Vector4 v1, ShuffleSel sel)
        {
            var ptr = (float*)&v1;
            var idx = (int)sel;
            return new Vector4(*(ptr + ((idx >> 0) & 0x3)), *(ptr + ((idx >> 2) & 0x3)), *(ptr + ((idx >> 4) & 0x3)),
                *(ptr + ((idx >> 6) & 0x3)));
        }
    }
}
