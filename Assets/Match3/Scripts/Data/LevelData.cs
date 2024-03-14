using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MatchGame.Scripts.Data
{
    [CreateAssetMenu(fileName = "LevelData_0", menuName = "ScriptableObjects/LevelData", order = 4)]
    public class LevelData : ScriptableObject
    {
        private static readonly Dictionary<string, int> TileValueMap = new Dictionary<string, int>
        {
            { "r", 0 },
            { "g", 1 },
            { "b", 2 },
            { "y", 3 },
        };

        public string Id
        {
            get => id;
            set => id = value;
        }

        public int[,] GenerateRandomMap()
        {
            var numRows = gridHeight;
            var numCols = gridWidth;
            int[,] mapData = new int[numRows, numCols];

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    mapData[i, j] = GetRandomNonConsecutiveColor(mapData, i, j);
                }
            }

            return mapData;
        }

        private int GetRandomNonConsecutiveColor(int[,] mapData, int row, int col)
        {
            int previousColor = (col > 0) ? mapData[row, col - 1] : -1;
            int aboveColor = (row > 0) ? mapData[row - 1, col] : -1;

            List<int> availableColors = new List<int> { 0, 1, 2, 3 };

            if (col > 1 && mapData[row, col - 2] == previousColor)
            {
                availableColors.Remove(previousColor);
            }

            if (row > 1 && mapData[row - 2, col] == aboveColor)
            {
                availableColors.Remove(aboveColor);
            }

            return availableColors[Random.Range(0, availableColors.Count)];
        }


        [Header("LEVEL DATA")] public int gridWidth;
        public int gridHeight;
        [SerializeField] private string id;
    }
}