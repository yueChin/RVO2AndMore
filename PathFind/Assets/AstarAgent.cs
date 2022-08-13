using RVO;
using System;
using System.Collections.Generic;
using UnityEngine;
using Vector2 = RVO.Vector2;

public class AstarAgent : MonoBehaviour
{
    private int m_CurtIdxInPath = 0;
    private List<AStarPosVo> m_PosList;

    public List<AStarPosVo> PosList => m_PosList;
    public void SetPath(byte[][] mapData,int colX,int rowY,Vector3 target)
    {
        Vector3 position = this.transform.position;
        //Debug.LogError($"寻路 {(int)position.x + colX/ 2}   {(int)position.z + rowY / 2}   {(int)target.x + colX/ 2}   {(int)target.z + rowY / 2}");
        List<AStarPosVo> list = AStar.Instance.Find(mapData, colX, rowY, (int)position.x + colX/ 2, (int)position.z + rowY / 2, (int)target.x + colX/ 2, (int)target.z+ rowY / 2, 100);
        if (list != null)
        {
            m_PosList = list;
            m_CurtIdxInPath = 0;
            // foreach (AStarPosVo posVo in m_PosList)
            // {
            //     Debug.LogError($"寻路坐标 {posVo.X}  {posVo.Y}");
            // }
        }
       
    }
    
    public RVO.Vector2 GetNextPosInAstar(RVO.Vector2 offset = default)
    {
        AStarPosVo pos;
        if (m_CurtIdxInPath < m_PosList.Count)
        {
            pos = m_PosList[m_CurtIdxInPath];
            Vector3 position = transform.position;
            Vector3 target = new Vector3(pos.X - offset.X(), position.y, pos.Y - offset.Y());
            float distance = Vector3.Distance(target, position);
            //Debug.LogError(distance + "距离呢？           目标 " + target);
            if (distance >= 0 && distance < 1)
            {
                pos = m_PosList[m_CurtIdxInPath];
                m_CurtIdxInPath++;
            }
        }
        else
        {
            pos = m_PosList[^1];
        }
        RVO.Vector2 v2 = new Vector2(pos.X- offset.X(),pos.Y- offset.Y());
        //Debug.LogError(m_CurtIdxInPath + "                计算得出的下一个点位置" + v2);
        return v2;
    }    
}