using System;
using MatchGame.Scripts.Cubes;

namespace MatchGame.Scripts.Managers.Map
{
    [Serializable]
    public struct CubeItemData
    {
        public string id;
        public CubeItem prefab;
    }
}