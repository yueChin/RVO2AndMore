using System.Collections.Generic;

public class BinaryHeap
{
	private List<AsGrid> gridlist = new List<AsGrid>();
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
		    && gridlist[(childIndex + 1)].Score < gridlist
			    [(childIndex)].Score)
		{
			if (gridlist[(index)].Score > gridlist[(childIndex + 1)].Score)
			{
				SwapAt(index, childIndex + 1);
				TryDownAt(childIndex + 1);

			}
		}
		else
		{

			if (gridlist[(index)].Score > gridlist[(childIndex)].Score)
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
		if (gridlist[(index)].Score < gridlist[(headIndex)].Score)
		{
			SwapAt(index, headIndex);
			TryUpAt(headIndex);
		}

	}

	public void Push(AsGrid val)
	{
		gridlist.Add(val);
		Length = gridlist.Count;
		TryUpAt(Length - 1);
	}

	public void Clear()
	{
		gridlist.Clear();
		Length = 0;
	}

	public AsGrid PopMix()
	{
		AsGrid val = gridlist[0];
		SwapAt(0, Length - 1);
		gridlist.RemoveAt(gridlist.Count - 1);
		Length = gridlist.Count;
		TryDownAt(0);
		return val;

	}

	public void Updata(AsGrid grid, int newScore)
	{
		int index = -1;
		for (int i = 0; i < Length; i++)
		{
			if (gridlist[(i)] == grid)
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
		AsGrid c = gridlist[(pos1)];
		gridlist[pos1] = gridlist[(pos2)];
		gridlist[pos2] = c;

	}
}