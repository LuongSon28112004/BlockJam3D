using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

public enum TyoeSpot
{
    OneSpawn,
    ManySpawn,
}

public class GridSpotSpawn : MonoBehaviour
{
    [SerializeField] TyoeSpot tyoeSpot;
    [Header("Config")]
    [SerializeField] int maxPointSpawn = 1;
    //[SerializeField] int maxContainer = 1;
    [SerializeField] List<Direction> directions;
    [SerializeField] GameObject GridParent;
    [SerializeField] Dictionary<Direction, Container> containers;
    [SerializeField] BaseGridSpotAnimation baseGridSpotAnimation;
    [SerializeField] TextMeshProUGUI textCount;
    public List<Direction> Directions { get => directions; set => directions = value; }
    public BaseGridSpotAnimation BaseGridSpotAnimation { get => baseGridSpotAnimation; set => baseGridSpotAnimation = value; }
    public Dictionary<Direction, Container> Containers { get => containers; set => containers = value; }
    [SerializeField] public BoardCell JustSpawn;
    public int MaxPointSpawn
    {
        get => maxPointSpawn;
        set
        {
            maxPointSpawn = value;
            if (textCount != null)
            {
                textCount.text = maxPointSpawn.ToString();
            }
        }
    }

    public void DestroyBoardCellJustSpawn()
    {
        JustSpawn.transform.DOScale(Vector3.zero, 0.15f);
        maxPointSpawn += 1;
        LevelManager.Instance.BoardCtrl.initialTypeCounts[JustSpawn.TypeItem] -= 1;
        LevelManager.Instance.BoardCtrl.UpdateBoardCell(JustSpawn);
        Destroy(JustSpawn.transform.gameObject);
        textCount.text = maxPointSpawn.ToString();
    }


    void Reset()
    {
        GridParent = GameObject.Find("GridBoard");
        if (GridParent == null)
        {
            Debug.LogError("GridParent not found in the scene!");
        }
    }

    void Awake()
    {
        GridParent = GameObject.Find("GridBoard");
        if (GridParent == null)
        {
            Debug.LogError("GridParent not found in the scene!");
        }
        containers = new Dictionary<Direction, Container>();
    }

    public bool CheckDirection(Direction direction)
    {
        if (!directions.Contains(direction))
        {
            return false;
        }
        return true;
    }

    public void AddContainer(Container container, Direction direction)
    {
        if (containers.Count >= maxPointSpawn)
        {
            Debug.LogError("Cannot add more containers, maximum capacity reached.");
            return;
        }
        containers.Add(direction, container);
    }

    public bool CheckContainer(Container container)
    {
        return containers.ContainsValue(container);
    }

    public IEnumerator SpawnBlock(GameObject blockPrefab, Container containerr, TypeItem typeItem, Action<BoardCell> onSpawned)
    {
        if (MaxPointSpawn <= 0)
        {
            Debug.LogWarning("MaxPointSpawn reached zero, cannot spawn more blocks.");
            yield break;
        }
        if (blockPrefab == null)
        {
            Debug.LogError("Block prefab is null!");
            yield break;
        }

        GameObject obj = Instantiate(blockPrefab, transform.position, Quaternion.identity, GridParent.transform);
        BoardCell boardCell = obj.GetComponent<BoardCell>();
        JustSpawn = boardCell;

        if (boardCell == null)
        {
            Debug.LogError("BoardCell component not found on the instantiated block!");
            Destroy(obj);
            yield break;
        }



        BoardCellMovement bc = obj.GetComponent<BoardCell>().BoardCellMovement;
        if (bc == null)
        {
            Debug.LogError("BoardCellMovement component not found on the instantiated block!");
            Destroy(obj);
            yield break;
        }
        Direction direction = containers.FirstOrDefault(kv => kv.Value == containerr).Key;
        Container container = containers[direction];
        if (container == null)
        {
            Debug.LogError("Container is null!");
            yield break;
        }
        container.IsContaining = true;
        maxPointSpawn--;
        if (textCount != null)
        {
            textCount.text = maxPointSpawn.ToString();
        }
        boardCell.Pos = container.Pos;
        boardCell.IdType = Enum.GetName(typeof(TypeItem), typeItem);
        boardCell.TypeItem = typeItem;
        boardCell.Barrel.SetActive(false);
        boardCell.HasClick = true;
        boardCell.Container = containers[direction];
        boardCell.BoardCellAnimation.SetActive();
        if (tyoeSpot == TyoeSpot.OneSpawn)
        {
            baseGridSpotAnimation.SetAnimationExit();
        }
        else
        {
            // if(direction == Direction.Up)
            // {
            //     baseGridSpotAnimation.SetAnimationExit(1);
            // }
            if(direction == Direction.Down)
            {
                baseGridSpotAnimation.SetAnimationExit(3);
            }
            // else if(direction == Direction.Left)
            // {
            //     baseGridSpotAnimation.SetAnimationExit(3);
            // }
            else if(direction == Direction.Right)
            {
                baseGridSpotAnimation.SetAnimationExit(2);
            }
        }
        obj.transform.localScale = Vector3.zero;
        AudioManager.Instance.PlayOneShot("BLJ_Game_Obstacles_Pipe_Normal_04", 1f);
        onSpawned?.Invoke(boardCell);
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(bc.MovementToPos(container.Pos));
        obj.transform.DOScale(Vector3.one, 0.1f);
    }

}
