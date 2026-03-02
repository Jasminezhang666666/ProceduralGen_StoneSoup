using NUnit.Framework;
using System.

using System.Collections.Generic;
using UnityEngine;

public class ValidateRoom : Room
{
    public bool HasUpExit = false;
    public bool HasDownExit = false;
    public bool HasLeftExit = false;
    public bool HasRightExit = false;

    public bool HasUpToDown = false;
    public bool HasUpToLeft = false;
    public bool HasUpToRight = false;
    public bool HasDownToLeft = false;
    public bool HasDownToRight = false;
    public bool HasLeftToRight = false;
    int[,] _indexGrid = new int[LevelGenerator.ROOM_WIDTH, LevelGenerator.ROOM_HEIGHT];

    public void ValidateExits()
    {
        loadData();

        if (_indexGrid[4,0] == 0)
        {
            HasUpExit = true;
        } else
        {
            HasUpExit = false;
        }

        if (_indexGrid[4, LevelGenerator.ROOM_HEIGHT - 1] == 0)
        {
            HasDownExit = true;
        } else
        {
            HasDownExit = false;
        }

        if (_indexGrid[0,3] == 0)
        {
            HasLeftExit = true;
        } else
        {
            HasLeftExit = false;
        }

        if (_indexGrid[LevelGenerator.ROOM_WIDTH - 1, 3] == 0)
        {

        } 
    }

    bool HasPath(Vector2 start, Vector2 target)
    {
        List<Vector2Int> openSet = new List<Vector2Int>();
        List<Vector2Int> closedSet = new List<Vector2Int>();

        openSet.Add(start);

        while (openSet.Count > 0)
        {
            Vector2Int currentNode = openSet[0];
            openSet.RemoveAt(0);

            if (currentNode == target)
            {
                return true;
            }

            Vector2Int upNeighbor = new Vector2Int(currentNode.x, currentNode.y - 1);
            Vector2Int downNeighbor = new Vector2Int(currentNode.x, currentNode.y + 1);
            Vector2Int leftNeighbor = new Vector2Int(currentNode.x - 1, currentNode.y);
            Vector2Int rightNeighbor = new Vector2Int(currentNode.x + 1, currentNode.y);

            if (closedSet.Contains(upNeighbor) == false && _indexGrid[upNeighbor.x, upNeighbor.y] == 0 && IsBounds(upNeighbor)
            {
                openSet.Add(upNeighbor);
            }
        }

        return false;
    }

    bool IsInBounds(Vector2Int node)
    {
        return (node.x >= 0 && node.x < LevelGenerator.ROOM_WIDTH && node.y >= 0 && node.y < LevelGenerator.ROOM_HEIGHT);
    }

    public bool MeetsConstraints(ExitConstraint constraints)
    {
        if (constraints.upExitRequired && HasUpExit == false)
            return false;
        if (constraints.downExitRequired && HasDownExit == false)
            return false;
        if (constraints.leftExitRequired && HasLeftExit == false)
            return false;
        if (constraints.rightExitRequired && HasRightExit == false)
            return false;

        if (constraints.upExitRequired && constraints.downExitRequired && HasUpToDown == false)
            return false;
        if (constraints.upExitRequired && constraints.leftExitRequired && HasUpToLeft == false)
            return false;
        if (constraints.upExitRequired && constraints.rightExitRequired && HasUpToRight == false)
            return false;
        if (constraints.downExitRequired && constraints.leftExitRequired && HasDownToLeft == false)
            return false;
        if (constraints.downExitRequired && constraints.rightExitRequired && HasDownToRight == false)
            return false;
        if (constraints.leftExitRequired && constraints.rightExitRequired && HasLeftToRight == false)
            return false;

        return true;
    }

    public virtual void loadData()
    {

        string initialGridString = designedRoomFile.text;
        string[] rows = initialGridString.Trim().Split('\n');
        int width = rows[0].Trim().Split(',').Length;
        int height = rows.Length;
        if (height != LevelGenerator.ROOM_HEIGHT)
        {
            throw new UnityException(string.Format("Error in room by {0}. Wrong height, Expected: {1}, Got: {2}", roomAuthor, LevelGenerator.ROOM_HEIGHT, height));
        }
        if (width != LevelGenerator.ROOM_WIDTH)
        {
            throw new UnityException(string.Format("Error in room by {0}. Wrong width, Expected: {1}, Got: {2}", roomAuthor, LevelGenerator.ROOM_WIDTH, width));
        }
        _indexGrid = new int[width, height];
        for (int r = 0; r < height; r++)
        {
            string row = rows[height - r - 1];
            string[] cols = row.Trim().Split(',');
            for (int c = 0; c < width; c++)
            {
                _indexGrid[c, r] = int.Parse(cols[c]);
            }
        }
    }
}
