 


using System;
using System.Collections.Generic;
using UnityEngine;

public class AStar
{
	public static AStar Instance = new AStar();
	private int mycount = 0;
	private readonly BinaryHeap m_OpenList = new BinaryHeap();
	private readonly Dictionary<int, AsGrid> m_GridMap = new Dictionary<int, AsGrid>();
	private byte[][] mapdt;
	private int m_Col;
	private int m_Row;
	private int m_EndX;
	private int m_EndY;
	private int m_LimitR;

	private void Open(int x, int y, int cost, AsGrid parent, bool create)
	{
		AsGrid grid;

		if (create)
		{
			grid = new AsGrid();
			grid.X = x;
			grid.Y = y;
			grid.Cost = cost;
			grid.Parent = parent;
			grid.Closed = false;
			grid.Score = 10000;
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

		mycount++;

	}

	private void Check(int new_x, int new_y, AsGrid grid, int cost)
	{

		if (Mathf.Abs(new_x - m_EndX) > m_LimitR || Mathf.Abs(new_y - m_EndY) > m_LimitR)
			return;
		if (IsBlock(new_x, new_y))
		{
			return;
		}

		AsGrid newGrid = null;


		if (!m_GridMap.TryGetValue((new_y << 16) + new_x, out newGrid))
		{
			Open(new_x, new_y, grid.Cost + cost, grid, true);
		}
		else if (!newGrid.Closed && newGrid.Cost > grid.Cost + cost)
		{
			Open(new_x, new_y, grid.Cost + cost, grid, false);
		}

	}

	private bool IsBlock(int x, int y)
	{
		try
		{
			return (y >= m_Row || x >= m_Col) || (x < 0 || y < 0) || (mapdt[y][x] & 0x1) != 0;
		}
		catch (Exception e)
		{
			Debug.LogError(x + "," + y);
			return true;
		}


	}

	private List<AStarPosVo> GetRst(AsGrid grid)
	{
		List<AStarPosVo> return_dt = new List<AStarPosVo>();
		do
		{
			AStarPosVo pos = new AStarPosVo();
			pos.X = grid.X;
			pos.Y = grid.Y;
			grid = grid.Parent;
			return_dt.Insert(0, pos);
		} while (grid != null);

		return (return_dt.Count > 2 ? return_dt : null);
	}

	public List<AStarPosVo> Find(byte[][] mapdata, int row, int col, int startX, int startY, int endX, int endY, int limitR)
	{

		mapdt = mapdata;
		this.m_LimitR = limitR;

		if (startX == endX && startY == endY)
			return null;
		m_Row = row;
		m_Col = col;
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

			AsGrid c_grid = m_OpenList.PopMix();

			if (c_grid.X == endX && c_grid.Y == endY)
			{

				rst = GetRst(c_grid);
				break;
			}

			c_grid.Closed = true;
			int x = c_grid.X;
			int y = c_grid.Y;

			Check(x, y - 1, c_grid, 10);
			Check(x - 1, y, c_grid, 10);
			Check(x + 1, y, c_grid, 10);
			Check(x, y + 1, c_grid, 10);
			Check(x - 1, y - 1, c_grid, 14);
			Check(x + 1, y - 1, c_grid, 14);
			Check(x - 1, y + 1, c_grid, 14);
			Check(x + 1, y + 1, c_grid, 14);
		}

		m_GridMap.Clear();
		m_OpenList.Clear();
		return rst;
	}
}
