using RVO;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Test01 : MonoBehaviour
{
    public GameObject mSpherePrefab01;
    public GameObject mSpherePrefab02;
    
    public int row = 22;
    public int col = 22;
    private byte[][] m_Mapdata;
    private List<AStarPosVo> m_PosList;

    public float Speed = 3f;
    public float Space = 1.1f;
    public int N = 20;//方阵长宽
    private readonly List<GameObject> m_SphereList = new List<GameObject>();
    private IList<RVO.Vector2> m_GoalList;
    System.Random m_Random;
    private RVO.Vector2 groupGoal;
    public static List<Sphere> m_SphereScritps = new List<Sphere>();


    void Start()
    {
        m_GoalList = new List<RVO.Vector2>();
        m_Random = new System.Random();

        // 创建静态阻挡
        GameObject[] obj = FindObjectsOfType(typeof(GameObject)) as GameObject[];
        foreach (GameObject g in obj)
        {
            if (g.tag.Equals("obstacle"))
            {
                Vector3 scale = g.transform.lossyScale;
                Vector3 position = g.transform.position;

                IList<RVO.Vector2> obstacle = new List<RVO.Vector2>();
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

        CreateGroup(new Vector3(0, 0, -20), mSpherePrefab02, 1f);
        
        // 创建大球
        //CreateGameObject(new Vector3(0, 0, 60), mSpherePrefab02, 1F);
        //CreateGameObject(new Vector3(0, 0, 61), mSpherePrefab02, 1F);
        CreateGameObject(new Vector3(0, 0, 20), mSpherePrefab02, 30f);
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
                // 目标点
                m_GoalList.Add(p);
                // 物体
                GameObject g = GameObject.Instantiate(spherePrefab);
                m_SphereList.Add(g);
                g.transform.localScale = g.transform.localScale * 0.5f;
                m_SphereScritps.Add(g.AddComponent<Sphere>());
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
        // 目标点
        m_GoalList.Add(p);
        // 物体
        GameObject g = GameObject.Instantiate(mSpherePrefab01);
        g.transform.localScale = new Vector3(diamV2, diamV2, diamV2);
        m_SphereList.Add(g);
        m_SphereScritps.Add(g.AddComponent<Sphere>());
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
                m_SphereList.Add(g);
                g.transform.localScale = g.transform.localScale * 0.5f;
                m_SphereScritps.Add(g.AddComponent<Sphere>());
            }
        }
        m_GoalList.Add(group.Position);
        Simulator.Instance.AddAgent(group);
    }
    
    int key = 0;
    Vector3 hitPoint01;

    void Update()
    {
        Simulator.Instance.setTimeStep(Time.deltaTime);
        setPreferredVelocities();
        Simulator.Instance.doStep();

        for (int i = 0; i < Simulator.Instance.GetWorldNumAgents(); ++i)
        {
            RVO.Vector2 p = Simulator.Instance.GetWorldAgentPosition(i);
            m_SphereList[i].transform.position = new Vector3(p.x(), 0, p.y());//GO赋值
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
                        m_GoalList[m_GoalList.Count - 1] = new RVO.Vector2(position.x, position.z);
                    }
                    else if (key == 1 || key == 2)
                    {
                        for (int i = 0; i < N; i++)
                        {
                            for (int j = 0; j < N; j++)
                            {
                                RVO.Vector2 p = new RVO.Vector2(i * Space + position.x, j * Space + position.z);
                                m_GoalList[index++] = p;
                            }
                        }
                    }
                    else if (key == 3)
                    {
                        groupGoal = new RVO.Vector2(position.x, position.z);
                    }
                }
            }
        }

        Debug.DrawLine(ray.origin, hitPoint01, Color.green);
    }

    void setPreferredVelocities()
    {
        for (int i = 0; i < Simulator.Instance.GetNumAgents(); ++i)
        {
            RVO.Vector2 goalVector = m_GoalList[i] - Simulator.Instance.GetAgentPosition(i);

            if (RVOMath.ABSSq(goalVector) > 1.0f)
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