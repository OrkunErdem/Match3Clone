using System;
using MatchGame.Scripts.Cubes;
using MatchGame.Scripts.Managers.Map;
using UnityEngine;
using UnityEngine.Serialization;

namespace MatchGame.Scripts.Managers
{
    public class BlastController : MonoBehaviour
    {
        private Vector2 swipeStartPos;
        private Vector2 swipeEndPos;
        private GridManager _gridManager;
        private GridManager GridManager => _gridManager ? _gridManager : (_gridManager = GridManager.Instance);
        private Camera Camera => Camera.main;
        private readonly float minSwipe = 0.1f;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                swipeStartPos = Camera.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0))
            {
                swipeEndPos = Camera.ScreenToWorldPoint(Input.mousePosition);
                DetectSwipe();
            }
        }

        private void DetectSwipe()
        {
            Vector2 swipeDirection = swipeEndPos - swipeStartPos;

            if (swipeDirection.magnitude > minSwipe)
            {
                swipeDirection.Normalize();

                if (Mathf.Abs(swipeDirection.x) > Mathf.Abs(swipeDirection.y))
                {
                    if (swipeDirection.x > 0)
                    {
                        HandleSwipe(Vector2.up);
                    }
                    else
                    {
                        HandleSwipe(Vector2.down);
                    }
                }
                else
                {
                    if (swipeDirection.y > 0)
                    {
                        HandleSwipe(Vector2.right);
                    }
                    else
                    {
                        HandleSwipe(Vector2.left);
                    }
                }
            }
        }


        private void HandleSwipe(Vector2 swipeDirection)
        {
            Vector2 mousePosition = swipeStartPos;
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                GameObject hitObject = hit.collider.gameObject;
                if (hitObject.TryGetComponent(out BasicNode node))
                {
                    if (node.transform.childCount > 0)
                    {
                        Vector2Int newGridPos = CalculateNewGridPosition(node.gridPos, swipeDirection);
                        if (newGridPos.x < 0 || newGridPos.x >= GridManager.MapX ||
                            newGridPos.y < 0 || newGridPos.y >= GridManager.MapY) return;
                        GridManager.SwapAndCheck(node.gridPos, newGridPos);
                    }
                }
            }
        }

        private Vector2Int CalculateNewGridPosition(Vector2Int currentPos, Vector2 swipeDirection)
        {
            Vector2Int newGridPos = currentPos +
                                    new Vector2Int(Mathf.RoundToInt(swipeDirection.x),
                                        Mathf.RoundToInt(swipeDirection.y));
            return newGridPos;
        }
    }
}