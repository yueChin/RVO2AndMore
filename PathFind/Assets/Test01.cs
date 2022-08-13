using RVO;
using System;
using System.Collections.Generic;
using UnityEngine;
using Vector2 = RVO.Vector2;

public class Test01 : MonoBehaviour
{

    //-----------AStar
    public int row = 222;
    public int col = 222;
    private byte[][] m_MapData;
    public LayerMask m_Layer;
    private bool m_IsNeedToNav = false;
    private int m_HalfRow;
    private int m_HalfCol;
    
    //------------RVO
    public GameObject mSpherePrefab01;
    public GameObject mSpherePrefab02;
    public float Speed = 3f;
    public float Space = 1.1f;
    public int N = 20;//方阵长宽
    private IList<AstarAgent> m_AstarAgentList;
    System.Random m_Random;
    public static List<Sphere> m_SphereList = new List<Sphere>();


    void Start()
    {
        //-------------------------Astar
        m_HalfRow = Mathf.FloorToInt(row / 2);
        m_HalfCol = Mathf.FloorToInt(col / 2);
        m_MapData = new byte[col][];
        for (int i = 0; i < col; i++)
        {
            m_MapData[i] = new byte[row];
            for (int j = 0; j < row; j++)
            {
                bool canWalk = !Physics.CheckSphere(new Vector3(i - m_HalfCol,0,j - m_HalfRow), 0.2f, m_Layer);
                m_MapData[i][j] = canWalk ? (byte)0 : (byte)1;
                if (!canWalk)
                {
                    Debug.LogError($"can {canWalk }           i:{i} j:{j}");
                }
            }
        }

        m_AstarAgentList = new List<AstarAgent>();
        
        //-------------------------RVO
        m_Random = new System.Random();

        // 创建静态阻挡
        GameObject[] obj = FindObjectsOfType(typeof(GameObject)) as GameObject[];
        foreach (GameObject g in obj)
        {
            if (g.tag.Equals("obstacle"))
            {
                Vector3 scale = g.transform.lossyScale;
                Vector3 position = g.transform.position;

                IList<RVO.Vector2> obstacle = new List<RVO.Vector2>();//这里是四个顶点，应该是为了不同形状的遮挡物做了点列表
                obstacle.Add(new RVO.Vector2(position.x + scale.x / 2, position.z + scale.z / 2));
                obstacle.Add(new RVO.Vector2(position.x - scale.x / 2, position.z + scale.z / 2));
                obstacle.Add(new RVO.Vector2(position.x - scale.x / 2, position.z - scale.z / 2));
                obstacle.Add(new RVO.Vector2(position.x + scale.x / 2, position.z - scale.z / 2));
                Simulator.Instance.AddObstacle(obstacle);
            }
        }

        Simulator.Instance.ProcessObstacles();

        // 创建小球
        Simulator.Instance.SetAgentDefaults(10.0f, 10, 1f, 1.0f, 0.5f, Speed, new RVO.Vector2(0.0f, 0.0f));
        //CreateSquad(new Vector3(-30, 0, 0), mSpherePrefab01, 1f);
        //CreateSquad(new Vector3(30, 0, 0), mSpherePrefab02, 1f);

        //CreateGroup(new Vector3(0, 0, -20), mSpherePrefab02, 1f);
        
        // 创建大球
        //CreateGameObject(new Vector3(0, 0, 60), mSpherePrefab02, 1F);
        //CreateGameObject(new Vector3(0, 0, 61), mSpherePrefab02, 1F);
        //CreateGameObject(new Vector3(0, 0, 20), mSpherePrefab02, 30f);
        CreateGameObject(new Vector3(0, 0, 50), mSpherePrefab02, 1F);
        

    }

    // 方阵
    void CreateSquad(Vector3 position, GameObject spherePrefab, float mass)
    {
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                // orca
                RVO.Vector2 p = new RVO.Vector2(i * Space + position.x, j * Space + position.z);
                int id = Simulator.Instance.GetDefaultAgent(p);
                Simulator.Instance.SetWorldAgentMass(id, mass);
                // 物体
                GameObject g = GameObject.Instantiate(spherePrefab);
                AstarAgent aa = g.AddComponent<AstarAgent>();
                m_AstarAgentList.Add(aa);
                g.transform.localScale = g.transform.localScale * 0.5f;
                m_SphereList.Add(g.AddComponent<Sphere>());
            }
        }
    }

    // 大球
    void CreateGameObject(Vector3 position, GameObject spherePrefab, float diamV2)
    {
        Simulator.Instance.SetAgentDefaults(60, 10, 1f, 1.0f, diamV2 / 2f, Speed, new RVO.Vector2(0.0f, 0.0f));
        // orca
        RVO.Vector2 p = new RVO.Vector2(position.x, position.z);
        int id = Simulator.Instance.GetDefaultAgent(p);
        Simulator.Instance.SetWorldAgentMass(id, 2.5f);
        // 物体
        GameObject g = GameObject.Instantiate(mSpherePrefab01);
        AstarAgent aa = g.AddComponent<AstarAgent>();
        m_AstarAgentList.Add(aa);
        g.transform.localScale = new Vector3(diamV2, diamV2, diamV2);
        m_SphereList.Add(g.AddComponent<Sphere>());
    }

    void CreateGroup(Vector3 position, GameObject spherePrefab, float mass)
    {
        Group group = new Group();
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                // orca
                RVO.Vector2 p = new RVO.Vector2(i * Space + position.x, j * Space + position.z);
                int id = Simulator.Instance.GetDefaultAgent(p,false);
                Simulator.Instance.SetWorldAgentMass(id, mass);
                Agent agent = Simulator.Instance.GetWorldAgent(id);
                if (agent == null)
                {
                    Debug.LogError(id);
                }
                agent.MAXNeighbors = 10;
                group.AddChild(agent);
                // 目标点
                // 物体
                GameObject g = GameObject.Instantiate(spherePrefab);
                AstarAgent aa = g.AddComponent<AstarAgent>();
                g.transform.localScale = g.transform.localScale * 0.5f;
                m_SphereList.Add(g.AddComponent<Sphere>());
            }
        }
        Simulator.Instance.AddAgent(group);
    }
    
    int key = 0;
    Vector3 hitPoint01;

    void Update()
    {
        Simulator.Instance.SetTimeStep(Time.deltaTime);
        SetPreferredVelocities();
        Simulator.Instance.DoStep();

        for (int i = 0; i < Simulator.Instance.GetWorldNumAgents(); ++i)
        {
            RVO.Vector2 p = Simulator.Instance.GetWorldAgentPosition(i);
            m_AstarAgentList[i].transform.position = new Vector3(p.X(), 0, p.Y());//GO赋值
        }

        if (Input.GetKey(KeyCode.Q))
        {
            key = 1;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            key = 2;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            key = 3;
        }
        else if (Input.GetKey(KeyCode.R))
        {
            key = 4;
        }
        // 鼠标点击Plane，设置目标点
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Input.GetMouseButton(0))
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
                if (hitInfo.collider.name == "Plane")
                {
                    hitPoint01 = hitInfo.point;
                    Vector3 position = hitInfo.point;
                    int index = 0;
                    if (key == 1)
                    {
                        index = 0;
                    }
                    else if (key == 2)
                    {
                        index = N * N;//n行n列
                    }
                    if (key == 3)
                    {
                        m_IsNeedToNav = true;
                        m_AstarAgentList[^1].SetPath(m_MapData,col,row,position); 
                    }
                    else if (key == 1 || key == 2)
                    {
                        m_IsNeedToNav = true;
                        for (int i = 0; i < N; i++)
                        {
                            for (int j = 0; j < N; j++)
                            {
                                Vector3 target = new Vector3(i * Space + position.x,0, j * Space + position.z);
                                m_AstarAgentList[index++].SetPath(m_MapData,col,row,target);
                            }
                        }
                    }
                    else if (key == 3)
                    {
                        m_IsNeedToNav = true;
                        //?
                    }
                }
            }
        }

        Debug.DrawLine(ray.origin, hitPoint01, Color.green);
    }

    void SetPreferredVelocities()
    {
        if (!m_IsNeedToNav)
            return;
        for (int i = 0; i < Simulator.Instance.GetNumAgents(); ++i)
        {
            if (m_AstarAgentList[i].PosList != null && m_AstarAgentList[i].PosList.Count > 0)
            {
                Vector2 next = m_AstarAgentList[i].GetNextPosInAstar(new Vector2(m_HalfCol, m_HalfRow));
                RVO.Vector2 goalVector =  next - Simulator.Instance.GetAgentPosition(i);
                //Debug.LogError($"{m_AstarAgentList[i].transform.position} 当前位置   +  agent位置 {Simulator.Instance.GetAgentPosition(i)}");
                //Debug.LogError(goalVector + "向量                     下一个坐标 " +  next);
                if (RVOMath.ABSSq(goalVector) > 1.0f)//新坐标点和当前位置有插值才会给向量
                {
                    goalVector = RVOMath.Normalize(goalVector) * Speed;
                }
                Simulator.Instance.SetAgentPrefVelocity(i, goalVector);

                float angle = (float)m_Random.NextDouble() * 2.0f * (float)Math.PI;
                float dist = (float)m_Random.NextDouble() * 0.0001f;

                Simulator.Instance.SetAgentPrefVelocity(i, Simulator.Instance.getAgentPrefVelocity(i) + dist * new RVO.Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));
            }
        }
    }


    
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(col, 1, row));

        if (m_MapData != null)
        {
            for (int i = 0; i < col; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    bool isCanWalk = m_MapData[i][j] == 0;
                    Gizmos.color = isCanWalk? Color.white : Color.red;
                    Gizmos.DrawCube(new Vector3(i - m_HalfCol,0,j - m_HalfRow), Vector3.one * (0.6f - 0.1f));
                }
            }
        }

        if (m_AstarAgentList != null && m_IsNeedToNav)
        {
            foreach (AstarAgent astarAgent in m_AstarAgentList)
            {
                if (astarAgent.PosList == null)
                {
                    continue;
                }
                foreach (AStarPosVo vo in astarAgent.PosList)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(new Vector3(vo.X- m_HalfCol,0,vo.Y- m_HalfRow), Vector3.one * (0.6f - 0.1f));
                }
            }
        }
    }
    
}