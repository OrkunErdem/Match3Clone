using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MatchGame.Scripts.Cubes;
using MatchGame.Scripts.Data;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace MatchGame.Scripts.Managers.Map
{
    public class GridManager : Singleton<GridManager>
    {
        [SerializeField] private LevelContainer levelContainer;
        [SerializeField] private Transform map;
        private static GameObject[,] _cells;
        private int[,] _objectMatrix;
        [SerializeField] private List<CubeItemData> nodePrefabDataList;
        private readonly Dictionary<string, CubeItem> _prefabDictionary = new Dictionary<string, CubeItem>();
        [SerializeField] private LevelInitializeData levelInitializeData;
        public int MapX => levelInitializeData.row;
        public int MapY => levelInitializeData.col;

        #region MapGenerationPhase

        private void Start()
        {
            var x = levelContainer.Levels[PlayerPrefs.GetInt("ActiveLevel")].GenerateRandomMap();
            LevelInitialize(x);
        }

        private void LevelInitialize(int[,] mapData)
        {
            _objectMatrix = mapData;
            ReverseRows(_objectMatrix);
            levelInitializeData.row = _objectMatrix.GetLength(0);
            levelInitializeData.col = _objectMatrix.GetLength(1);
            _cells = new GameObject[levelInitializeData.row, levelInitializeData.col];
            InitializeMap();
        }

        private void InitializeMap()
        {
            for (int row = 0; row < levelInitializeData.row; row++)
            {
                for (int col = 0; col < levelInitializeData.col; col++)
                {
                    float x = col * levelInitializeData.cellSize;
                    float y = row * levelInitializeData.cellSize;
                    Vector3 position = new Vector3(x, y, 0);

                    GameObject cell = PoolingSystem.Instance.Instantiate("BasicNode", position, Quaternion.identity);
                    cell.GetComponent<BasicNode>().gridPos = new Vector2Int(row, col);
                    cell.transform.parent = map;
                    _cells[row, col] = cell;
                }
            }

            Filler();
        }

        private void Filler()
        {
            foreach (var data in nodePrefabDataList)
            {
                _prefabDictionary.Add(data.id, data.prefab);
            }

            _FillMap();
        }

        private void _FillMap()
        {
            for (int i = 0; i < levelInitializeData.row; i++)
            {
                for (int j = 0; j < levelInitializeData.col; j++)
                {
                    var node = _prefabDictionary[_objectMatrix[i, j].ToString()];
                    InstantiateObjectAtLocation(i, j, node.gameObject);
                }
            }
        }

        private void InstantiateObjectAtLocation(int row, int col, GameObject nodeObject)
        {
            GameObject cellObject = _cells[row, col];

            if (nodeObject.TryGetComponent(out IPoolable poolObject))
            {
                var o =
                    PoolingSystem.Instance.Instantiate(poolObject.PoolInstanceId, cellObject.transform.position,
                        Quaternion.identity);
                o.TryGetComponent(out CubeItem cubeItem);
                cubeItem.Health = 1;
                o.transform.parent = cellObject.transform;
            }
        }

        #endregion

        #region GamingPhase

        private void DamageCube(Vector2Int gridPosition, int damage = 1)
        {
            CubeItem cubeItem = _cells[gridPosition.x, gridPosition.y].GetComponentInChildren<CubeItem>();
            if (cubeItem == null) return;
            cubeItem.TakeDamage(() => { _objectMatrix[gridPosition.x, gridPosition.y] = -1; }, damage);
        }

        private GameObject InstantiateObject(int row, int col, GameObject nodeObject)
        {
            GameObject cellObject = _cells[row, col];

            if (nodeObject.TryGetComponent(out IPoolable poolObject))
            {
                var o =
                    PoolingSystem.Instance.Instantiate(poolObject.PoolInstanceId, cellObject.transform.position,
                        Quaternion.identity);
                o.transform.DOMoveY(levelInitializeData.row + (row * levelInitializeData.cellSize), 0);
                o.transform.parent = cellObject.transform;
                return o;
            }

            return null;
        }

        public void CheckForBlast(Vector2Int gridPosition, Action onBlasted = null)
        {
            List<Vector2Int> sameColorRowNeighbors = GetAdjacentSameColorNeighborsInRow(gridPosition);
            List<Vector2Int> sameColorColNeighbors = GetAdjacentSameColorNeighborsInColumn(gridPosition);
            if (sameColorRowNeighbors.Count <= 1 && sameColorColNeighbors.Count <= 1) return;
            onBlasted?.Invoke();
            sameColorColNeighbors.Add(gridPosition);
            sameColorRowNeighbors.Add(gridPosition);
            if (sameColorRowNeighbors.Count > 2)
            {
                foreach (var neighbor in sameColorRowNeighbors)
                {
                    DamageCube(neighbor);
                }
            }

            if (sameColorColNeighbors.Count > 2)
            {
                foreach (var neighbor in sameColorColNeighbors)
                {
                    DamageCube(neighbor);
                }
            }

            StartCoroutine(FillEmptySpace());
        }

        private IEnumerator FillEmptySpace()
        {
            for (int col = 0; col < levelInitializeData.col; col++)
            {
                int newRow = 0;

                for (int row = 0; row < levelInitializeData.row; row++)
                {
                    if (_objectMatrix[row, col] != -1)
                    {
                        _objectMatrix[newRow, col] = _objectMatrix[row, col];

                        if (newRow != row)
                        {
                            _objectMatrix[row, col] = -1;
                        }

                        GameObject cubeObject = _cells[row, col].transform.GetChild(0).gameObject;
                        cubeObject.transform.parent = _cells[newRow, col].transform;
                        cubeObject.transform.DOMove(_cells[newRow, col].transform.position, 6).SetSpeedBased(true)
                            .SetEase(Ease.Linear);

                        newRow++;
                    }
                }
            }

            yield return null;
            StartCoroutine(CheckForFilling());
        }

        private IEnumerator CheckForFilling()
        {
            for (int col = 0; col < levelInitializeData.col; col++)
            {
                for (int row = 0; row < levelInitializeData.row; row++)
                {
                    if (_objectMatrix[row, col] != -1)
                    {
                        CheckForBlast(new Vector2Int(row, col));
                    }
                }
            }

            yield return null;
            SpawnToEmpty();
        }


        private void SpawnToEmpty()
        {
            for (int col = 0; col < levelInitializeData.col; col++)
            {
                if (IsColumnAllowedToSpawn(col)) continue;
                for (int row = 0; row < levelInitializeData.row; row++)
                {
                    if (_objectMatrix[row, col] == -1)
                    {
                        CheckAroundAndSpawn(row, col);
                    }
                }
            }
        }

        private void CheckAroundAndSpawn(int row, int col)
        {
            List<int> availableColors = new List<int> { 0, 1, 2, 3 };
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(row - 1, col),
                new Vector2Int(row + 1, col),
                new Vector2Int(row, col - 1),
                new Vector2Int(row, col + 1)
            };

            foreach (var n in neighbors)
            {
                var x = n.x;
                var y = n.y;
                if (x < 0 || x >= levelInitializeData.row || y < 0 || y >= levelInitializeData.col) continue;
                if (_objectMatrix[n.x, n.y] == -1) continue;
                availableColors.Remove(_objectMatrix[n.x, n.y]);
            }

            var color = availableColors[Random.Range(0, availableColors.Count)];
            var newCube = _prefabDictionary[color.ToString()];
            var o = InstantiateObject(row, col, newCube.gameObject);
            o.transform.DOMove(_cells[row, col].transform.position, 6).SetSpeedBased(true).SetEase(Ease.Linear);
            _objectMatrix[row, col] = color;
        }


        private List<Vector2Int> GetAdjacentSameColorNeighborsInRow(Vector2Int gridPosition)
        {
            List<Vector2Int> sameColorNeighbors = new List<Vector2Int>();
            var targetColor = _objectMatrix[gridPosition.x, gridPosition.y];
            int currentCol = gridPosition.y;

            int leftMostCol = currentCol;
            int rightMostCol = currentCol;

            while (leftMostCol > 0 && _objectMatrix[gridPosition.x, leftMostCol - 1] == targetColor)
            {
                leftMostCol--;
            }

            while (rightMostCol < levelInitializeData.col - 1 &&
                   _objectMatrix[gridPosition.x, rightMostCol + 1] == targetColor)
            {
                rightMostCol++;
            }

            if (rightMostCol - leftMostCol + 1 >= 3)
            {
                for (int col = leftMostCol; col <= rightMostCol; col++)
                {
                    sameColorNeighbors.Add(new Vector2Int(gridPosition.x, col));
                }
            }

            return sameColorNeighbors;
        }


        private List<Vector2Int> GetAdjacentSameColorNeighborsInColumn(Vector2Int gridPosition)
        {
            List<Vector2Int> sameColorNeighbors = new List<Vector2Int>();
            var targetColor = _objectMatrix[gridPosition.x, gridPosition.y];
            int currentRow = gridPosition.x;

            int topMostRow = currentRow;
            int bottomMostRow = currentRow;

            while (topMostRow > 0 && _objectMatrix[topMostRow - 1, gridPosition.y] == targetColor)
            {
                topMostRow--;
            }

            while (bottomMostRow < levelInitializeData.row - 1 &&
                   _objectMatrix[bottomMostRow + 1, gridPosition.y] == targetColor)
            {
                bottomMostRow++;
            }

            if (bottomMostRow - topMostRow + 1 >= 3)
            {
                for (int row = topMostRow; row <= bottomMostRow; row++)
                {
                    sameColorNeighbors.Add(new Vector2Int(row, gridPosition.y));
                }
            }

            return sameColorNeighbors;
        }


        private bool IsColumnAllowedToSpawn(int col)
        {
            return !levelInitializeData.allowedColumns.Contains(col);
        }

        private static void ReverseRows(int[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            for (int i = 0; i < rows / 2; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    (matrix[i, j], matrix[rows - 1 - i, j]) = (matrix[rows - 1 - i, j], matrix[i, j]);
                }
            }
        }

        public void SwapAndCheck(Vector2Int gridPosition1, Vector2Int gridPosition2)
        {
            StartCoroutine(SwapAndCheckCo(gridPosition1, gridPosition2));
        }

        private IEnumerator SwapAndCheckCo(Vector2Int gridPosition1, Vector2Int gridPosition2)
        {
            bool blasted = false;
            if (_objectMatrix[gridPosition1.x, gridPosition1.y] == -1 ||
                _objectMatrix[gridPosition2.x, gridPosition2.y] == -1) yield break;
            Swap(gridPosition1, gridPosition2, () =>
            {
                CheckForBlast(gridPosition1, () => { blasted = true; });
                CheckForBlast(gridPosition2, () => { blasted = true; });
            });
            yield return new WaitForSeconds(.5f);
            if (!blasted)
            {
                Swap(gridPosition2, gridPosition1);
            }
        }

        private void Swap(Vector2Int gridPosition1, Vector2Int gridPosition2, Action onSwapped = null)
        {
            int item1Type = _objectMatrix[gridPosition1.x, gridPosition1.y];
            int item2Type = _objectMatrix[gridPosition2.x, gridPosition2.y];

            _objectMatrix[gridPosition1.x, gridPosition1.y] = item2Type;
            _objectMatrix[gridPosition2.x, gridPosition2.y] = item1Type;

            GameObject item1Object = _cells[gridPosition1.x, gridPosition1.y].transform.GetChild(0).gameObject;
            item1Object.transform.parent = _cells[gridPosition2.x, gridPosition2.y].transform;

            GameObject item2Object = _cells[gridPosition2.x, gridPosition2.y].transform.GetChild(0).gameObject;
            item2Object.transform.parent = _cells[gridPosition1.x, gridPosition1.y].transform;

            Vector3 item1Position = item1Object.transform.position;
            Vector3 item2Position = item2Object.transform.position;

            item1Object.transform.DOMove(item2Position, .4f);
            item2Object.transform.DOMove(item1Position, .4f).OnComplete(
                () => { onSwapped?.Invoke(); });
        }

        #endregion
    }
}