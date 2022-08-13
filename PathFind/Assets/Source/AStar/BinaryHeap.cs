using System.Collections.Generic;

public class BinaryHeap
{
	private readonly List<AsGrid> m_GridList = new List<AsGrid>();
	public int Length = 0;

	private void TryDownAt(int index)
	{
		if (index < 0)
			return;
		int childIndex = index * 2 + 1;
		if (childIndex >= Length)
		{
			return;
		}

		if (childIndex + 1 < Length
		    && m_GridList[(childIndex + 1)].Score < m_GridList
			    [(childIndex)].Score)
		{
			if (m_GridList[(index)].Score > m_GridList[(childIndex + 1)].Score)
			{
				SwapAt(index, childIndex + 1);
				TryDownAt(childIndex + 1);

			}
		}
		else
		{

			if (m_GridList[(index)].Score > m_GridList[(childIndex)].Score)
			{
				SwapAt(index, childIndex);
				TryDownAt(childIndex);

			}
		}
	}

	private void TryUpAt(int index)
	{
		if (index <= 0)
			return;
		int headIndex = (index - 1) / 2;
		if (m_GridList[(index)].Score < m_GridList[(headIndex)].Score)
		{
			SwapAt(index, headIndex);
			TryUpAt(headIndex);
		}

	}

	public void Push(AsGrid val)
	{
		m_GridList.Add(val);
		Length = m_GridList.Count;
		TryUpAt(Length - 1);
	}

	public void Clear()
	{
		m_GridList.Clear();
		Length = 0;
	}

	public AsGrid PopMix()
	{
		AsGrid val = m_GridList[0];
		SwapAt(0, Length - 1);
		m_GridList.RemoveAt(m_GridList.Count - 1);
		Length = m_GridList.Count;
		TryDownAt(0);
		return val;

	}

	public void Updata(AsGrid grid, int newScore)
	{
		int index = -1;
		for (int i = 0; i < Length; i++)
		{
			if (m_GridList[(i)] == grid)
			{
				index = i;
				break;
			}
		}

		if (grid.Score > newScore)
		{
			grid.Score = newScore;
			TryDownAt(index);
		}
		else if (grid.Score < newScore)
		{
			grid.Score = newScore;
			TryUpAt(index);
		}
	}

	private void SwapAt(int pos1, int pos2)
	{
		AsGrid c = m_GridList[(pos1)];
		m_GridList[pos1] = m_GridList[(pos2)];
		m_GridList[pos2] = c;

	}
}