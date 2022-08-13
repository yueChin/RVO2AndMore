 


using System;
using System.Collections.Generic;
using UnityEngine;

public class AStar
{
	public static AStar Instance = new AStar();
	private int m_MyCount = 0;
	private readonly BinaryHeap m_OpenList = new BinaryHeap();
	private readonly Dictionary<int, AsGrid> m_GridMap = new Dictionary<int, AsGrid>();
	private byte[][] m_MapData;
	private int m_ColX;
	private int m_RowY;
	private int m_EndX;
	private int m_EndY;
	private int m_LimitR;

	private void Open(int x, int y, int cost, AsGrid parent, bool create)
	{
		AsGrid grid;

		if (create)
		{
			grid = new AsGrid
			{
				X = x,
				Y = y,
				Cost = cost,
				Parent = parent,
				Closed = false,
				Score = 10000
			};
			m_GridMap[(y << 16) + x] = grid;
			int dx = Mathf.Abs(x - m_EndX);
			int dy = Mathf.Abs(y - m_EndY);
			if (dx > 0 && dy > 0)
			{
				grid.Last = 14;
			}
			else
			{
				grid.Last = 10;
			}

			grid.Score = grid.Cost + grid.Last;
			m_OpenList.Push(grid);
		}
		else
		{
			grid = m_GridMap[((y << 16) + x)];
			grid.Parent = parent;
			;
			grid.Cost = cost;
			m_OpenList.Updata(grid, grid.Cost + grid.Last);
		}

		m_MyCount++;

	}

	private void Check(int newColX, int newRowY, AsGrid grid, int cost)
	{

		if (Mathf.Abs(newColX - m_EndX) > m_LimitR || Mathf.Abs(newRowY - m_EndY) > m_LimitR)
			return;
		if (IsBlock(newColX, newRowY))
		{
			return;
		}

		if (!m_GridMap.TryGetValue((newRowY << 16) + newColX, out AsGrid newGrid))
		{
			Open(newColX, newRowY, grid.Cost + cost, grid, true);
		}
		else if (!newGrid.Closed && newGrid.Cost > grid.Cost + cost)
		{
			Open(newColX, newRowY, grid.Cost + cost, grid, false);
		}

	}

	private bool IsBlock(int x, int y)
	{
		try
		{
			bool isBlock = (x >= m_ColX || y >= m_RowY ) || (x < 0 || y < 0) || (m_MapData[x][y] & 0x1) != 0;
			// if (isBlock)
			// {
			// 	Debug.LogError($"挡住了 {x} {m_ColX}  {y} {m_RowY}  {m_MapData[x][y]}  {(m_MapData[x][y] & 0x1)}");
			// }
			return isBlock;
		}
		catch (Exception e)
		{
			Debug.LogError(x + "," + y);
			return true;
		}


	}

	private List<AStarPosVo> GetRst(AsGrid grid)
	{
		List<AStarPosVo> retPath = new List<AStarPosVo>();
		do
		{
			AStarPosVo pos = new AStarPosVo
			{
				X = grid.X,
				Y = grid.Y
			};
			grid = grid.Parent;
			retPath.Insert(0, pos);
		} while (grid != null);

		return (retPath.Count > 2 ? retPath : null);
	}

	public List<AStarPosVo> Find(byte[][] mapdata, int col, int row, int startX, int startY, int endX, int endY, int limitR)
	{

		m_MapData = mapdata;
		this.m_LimitR = limitR;

		if (startX == endX && startY == endY)
			return null;
		m_ColX = col;
		m_RowY = row;
		m_EndX = endX;
		m_EndY = endY;
		if (IsBlock(startX, startY))
		{
			return null;
		}

		if (IsBlock(endX, endY))
		{
			return null;
		}

		List<AStarPosVo> rst = null;

		Open(startX, startY, 0, null, true);

		while (true)
		{
			int len = m_OpenList.Length;
			if (len == 0)
			{
				break;
			}

			AsGrid grid = m_OpenList.PopMix();

			if (grid.X == endX && grid.Y == endY)
			{

				rst = GetRst(grid);
				break;
			}

			grid.Closed = true;
			int x = grid.X;
			int y = grid.Y;
			Check(x - 1, y, grid, 10);
			Check(x, y - 1, grid, 10);
			Check(x, y + 1, grid, 10);
			Check(x + 1, y, grid, 10);
			Check(x - 1, y - 1, grid, 14);
			Check(x - 1, y + 1, grid, 14);
			Check(x + 1, y - 1, grid, 14);
			Check(x + 1, y + 1, grid, 14);
		}

		m_GridMap.Clear();
		m_OpenList.Clear();
		return rst;
	}
}
