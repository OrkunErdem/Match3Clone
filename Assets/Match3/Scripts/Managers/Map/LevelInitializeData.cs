using System;
using System.Collections.Generic;

namespace MatchGame.Scripts.Managers.Map
{
    [Serializable]
    public class LevelInitializeData
    {
        public int row;
        public int col;
        public float cellSize;
        public List<int> allowedColumns;
    }
}