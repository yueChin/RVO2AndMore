/*
 * Simulator.cs
 * RVO2 Library C#
 *
 * Copyright 2008 University of North Carolina at Chapel Hill
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Please send all bug reports to <geom@cs.unc.edu>.
 *
 * The authors may be contacted via:
 *
 * Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha
 * Dept. of Computer Science
 * 201 S. Columbia St.
 * Frederick P. Brooks, Jr. Computer Science Bldg.
 * Chapel Hill, N.C. 27599-3175
 * United States of America
 *
 * <http://gamma.cs.unc.edu/RVO2/>
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;

namespace RVO
{
    /**
     * <summary>Defines the simulation.</summary>
     */
    public sealed class Simulator
    {
        /**
         * <summary>Defines a worker.</summary>
         */
        private class Worker
        {
            private readonly ManualResetEvent m_DoneEvent;
            private readonly int m_End;
            private readonly int m_Start;

            /**
             * <summary>Constructs and initializes a worker.</summary>
             *
             * <param name="start">Start.</param>
             * <param name="end">End.</param>
             * <param name="doneEvent">Done event.</param>
             */
            internal Worker(int start, int end, ManualResetEvent doneEvent)
            {
                m_Start = start;
                m_End = end;
                m_DoneEvent = doneEvent;
            }

            /**
             * <summary>Performs a simulation step.</summary>
             *
             * <param name="obj">Unused.</param>
             */
            internal void Step(object obj)
            {
                for (int agentNo = m_Start; agentNo < m_End; ++agentNo)
                {
                    Simulator.Instance.Agents[agentNo].ComputeNeighbors();
                    Simulator.Instance.Agents[agentNo].computeNewVelocity();
                }

                m_DoneEvent.Set();
            }

            /**
             * <summary>updates the two-dimensional position and
             * two-dimensional velocity of each agent.</summary>
             *
             * <param name="obj">Unused.</param>
             */
            internal void Update(object obj)
            {
                for (int agentNo = m_Start; agentNo < m_End; ++agentNo)
                {
                    Simulator.Instance.Agents[agentNo].Update();
                }

                m_DoneEvent.Set();
            }
        }

        internal IList<Agent> Agents;
        internal IList<Obstacle> Obstacles;
        internal KdTree KdTree;
        internal float TimeStep;

        private static Simulator s_Instance = new Simulator();

        private Agent m_DefaultAgent;
        private ManualResetEvent[] m_DoneEvents;
        private Worker[] m_Workers;
        private int m_NumWorkers;
        private float m_GlobalTime;

        private HashSet<int> m_IDSet;
        private IList<Agent> m_WorldAgentList;
        public static Simulator Instance => s_Instance;

        /**
         * <summary>Adds a new agent with default properties to the simulation.
         * </summary>
         *
         * <returns>The number of the agent, or -1 when the agent defaults have
         * not been set.</returns>
         *
         * <param name="position">The two-dimensional starting position of this
         * agent.</param>
         */
        public int GetDefaultAgent(Vector2 position,bool isAddToAgents = true)
        {
            if (m_DefaultAgent == null)
            {
                return -1;
            }

            Agent agent = new Agent();
            //目前的id是初始化的时候按加入的列表数量，需要替换掉
            //agents_
            agent.ID = m_WorldAgentList.Count;
            agent.MAXNeighbors = m_DefaultAgent.MAXNeighbors;
            agent.MAXSpeed = m_DefaultAgent.MAXSpeed;
            agent.NeighborDist = m_DefaultAgent.NeighborDist;
            agent.Position = position;
            agent.Radius = m_DefaultAgent.Radius;
            agent.TimeHorizon = m_DefaultAgent.TimeHorizon;
            agent.TimeHorizonObst = m_DefaultAgent.TimeHorizonObst;
            agent.Velocity = m_DefaultAgent.Velocity;
            //agents_
            m_WorldAgentList.Add(agent);
            if (isAddToAgents)
            {
                Agents.Add(agent);
            }
            return agent.ID;
        }

        public void AddAgent(Agent agent)
        {
            Agents.Add(agent);
        }
        
        public Agent GetWorldAgent(int id)
        {
            int idx = -1;
            Agent agent = null;
            for (int i = 0; i < m_WorldAgentList.Count; i++)
            {
                if (m_WorldAgentList[i].ID == id)
                {
                    idx = i;
                    break;
                }
            }

            if (idx >= 0)
            {
                agent = m_WorldAgentList[idx];
            }

            return agent;
        }
        
        public void AddGroup(Group group)
        {
            Agents.Add(group);
        }

        public void RemoveGroup(Group group)
        {
            Agent[] agents = group.Clear();
            Agents.AddRange(agents);
        }
        
        public void AddAgentToGroup(int id,Group group)
        {
            int idx = -1;
            Agent agent = null;
            for (int i = 0; i < Agents.Count; i++)
            {
                if (Agents[i].ID == id)
                {
                    idx = i;
                    break;
                }
            }

            if (idx >= 0)
            {
                agent = Agents[idx];
                Agents.RemoveAt(idx);
                group.AddChild(agent);
            }
        }

        public void ReturnAgentFromGroup(int id,Group group)
        {
            Agent agent = group.RemoveChild(id);
            if (agent != null)
            {
                Agents.Add(agent);
            }
        }
        
        /**
         * <summary>Adds a new agent to the simulation.</summary>
         *
         * <returns>The number of the agent.</returns>
         *
         * <param name="position">The two-dimensional starting position of this
         * agent.</param>
         * <param name="neighborDist">The maximum distance (center point to
         * center point) to other agents this agent takes into account in the
         * navigation. The larger this number, the longer the running time of
         * the simulation. If the number is too low, the simulation will not be
         * safe. Must be non-negative.</param>
         * <param name="maxNeighbors">The maximum number of other agents this
         * agent takes into account in the navigation. The larger this number,
         * the longer the running time of the simulation. If the number is too
         * low, the simulation will not be safe.</param>
         * <param name="timeHorizon">The minimal amount of time for which this
         * agent's velocities that are computed by the simulation are safe with
         * respect to other agents. The larger this number, the sooner this
         * agent will respond to the presence of other agents, but the less
         * freedom this agent has in choosing its velocities. Must be positive.
         * </param>
         * <param name="timeHorizonObst">The minimal amount of time for which
         * this agent's velocities that are computed by the simulation are safe
         * with respect to obstacles. The larger this number, the sooner this
         * agent will respond to the presence of obstacles, but the less freedom
         * this agent has in choosing its velocities. Must be positive.</param>
         * <param name="radius">The radius of this agent. Must be non-negative.
         * </param>
         * <param name="maxSpeed">The maximum speed of this agent. Must be
         * non-negative.</param>
         * <param name="velocity">The initial two-dimensional linear velocity of
         * this agent.</param>
         */
        public int AddAgent(Vector2 position, float neighborDist, int maxNeighbors, float timeHorizon, float timeHorizonObst, float radius, float maxSpeed, Vector2 velocity)
        {
            Agent agent = new Agent();
            agent.ID = Agents.Count;
            agent.MAXNeighbors = maxNeighbors;
            agent.MAXSpeed = maxSpeed;
            agent.NeighborDist = neighborDist;
            agent.Position = position;
            agent.Radius = radius;
            agent.TimeHorizon = timeHorizon;
            agent.TimeHorizonObst = timeHorizonObst;
            agent.Velocity = velocity;
            Agents.Add(agent);

            return agent.ID;
        }

        /**
         * <summary>Adds a new obstacle to the simulation.</summary>
         *
         * <returns>The number of the first vertex of the obstacle, or -1 when
         * the number of vertices is less than two.</returns>
         *
         * <param name="vertices">List of the vertices of the polygonal obstacle
         * in counterclockwise order.</param>
         *
         * <remarks>To add a "negative" obstacle, e.g. a bounding polygon around
         * the environment, the vertices should be listed in clockwise order.
         * </remarks>
         */
        public int AddObstacle(IList<Vector2> vertices)
        {
            if (vertices.Count < 2)
            {
                return -1;
            }

            int obstacleNo = Obstacles.Count;

            for (int i = 0; i < vertices.Count; ++i)
            {
                Obstacle obstacle = new Obstacle();
                obstacle.Point = vertices[i];

                if (i != 0)
                {
                    obstacle.Previous = Obstacles[Obstacles.Count - 1];
                    obstacle.Previous.Next = obstacle;
                }

                if (i == vertices.Count - 1)
                {
                    obstacle.Next = Obstacles[obstacleNo];
                    obstacle.Next.Previous = obstacle;
                }

                obstacle.Direction = RVOMath.Normalize(vertices[(i == vertices.Count - 1 ? 0 : i + 1)] - vertices[i]);

                if (vertices.Count == 2)
                {
                    obstacle.Convex = true;
                }
                else
                {
                    obstacle.Convex = (RVOMath.LeftOf(vertices[(i == 0 ? vertices.Count - 1 : i - 1)], vertices[i], vertices[(i == vertices.Count - 1 ? 0 : i + 1)]) >= 0.0f);
                }

                obstacle.ID = Obstacles.Count;
                Obstacles.Add(obstacle);
            }

            return obstacleNo;
        }

        /**
         * <summary>Clears the simulation.</summary>
         */
        public void Clear()
        {
            Agents = new List<Agent>();
            m_DefaultAgent = null;
            KdTree = new KdTree();
            Obstacles = new List<Obstacle>();
            m_GlobalTime = 0.0f;
            TimeStep = 0.1f;

            m_WorldAgentList = new List<Agent>();
            SetNumWorkers(0);
        }

        /**
         * <summary>Performs a simulation step and updates the two-dimensional
         * position and two-dimensional velocity of each agent.</summary>
         *
         * <returns>The global time after the simulation step.</returns>
         */
        public float DoStep()
        {
            if (m_Workers == null)
            {
                m_Workers = new Worker[m_NumWorkers];
                m_DoneEvents = new ManualResetEvent[m_Workers.Length];

                for (int block = 0; block < m_Workers.Length; ++block)
                {
                    m_DoneEvents[block] = new ManualResetEvent(false);
                    m_Workers[block] = new Worker(block * GetNumAgents() / m_Workers.Length, (block + 1) * GetNumAgents() / m_Workers.Length, m_DoneEvents[block]);
                }
            }

            KdTree.buildAgentTree();

            for (int block = 0; block < m_Workers.Length; ++block)
            {
                m_DoneEvents[block].Reset();
                ThreadPool.QueueUserWorkItem(m_Workers[block].Step);
            }

            WaitHandle.WaitAll(m_DoneEvents);

            for (int block = 0; block < m_Workers.Length; ++block)
            {
                m_DoneEvents[block].Reset();
                ThreadPool.QueueUserWorkItem(m_Workers[block].Update);
            }

            WaitHandle.WaitAll(m_DoneEvents);

            m_GlobalTime += TimeStep;

            return m_GlobalTime;
        }

        /**
         * <summary>Returns the specified agent neighbor of the specified agent.
         * </summary>
         *
         * <returns>The number of the neighboring agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose agent neighbor is
         * to be retrieved.</param>
         * <param name="neighborNo">The number of the agent neighbor to be
         * retrieved.</param>
         */
        public int getAgentAgentNeighbor(int agentNo, int neighborNo)
        {
            return Agents[agentNo].AgentNeighborList[neighborNo].Value.ID;
        }

        /**
         * <summary>Returns the maximum neighbor count of a specified agent.
         * </summary>
         *
         * <returns>The present maximum neighbor count of the agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose maximum neighbor
         * count is to be retrieved.</param>
         */
        public int getAgentMaxNeighbors(int agentNo)
        {
            return Agents[agentNo].MAXNeighbors;
        }

        /**
         * <summary>Returns the maximum speed of a specified agent.</summary>
         *
         * <returns>The present maximum speed of the agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose maximum speed is
         * to be retrieved.</param>
         */
        public float getAgentMaxSpeed(int agentNo)
        {
            return Agents[agentNo].MAXSpeed;
        }

        /**
         * <summary>Returns the maximum neighbor distance of a specified agent.
         * </summary>
         *
         * <returns>The present maximum neighbor distance of the agent.
         * </returns>
         *
         * <param name="agentNo">The number of the agent whose maximum neighbor
         * distance is to be retrieved.</param>
         */
        public float getAgentNeighborDist(int agentNo)
        {
            return Agents[agentNo].NeighborDist;
        }

        /**
         * <summary>Returns the count of agent neighbors taken into account to
         * compute the current velocity for the specified agent.</summary>
         *
         * <returns>The count of agent neighbors taken into account to compute
         * the current velocity for the specified agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose count of agent
         * neighbors is to be retrieved.</param>
         */
        public int getAgentNumAgentNeighbors(int agentNo)
        {
            return Agents[agentNo].AgentNeighborList.Count;
        }

        /**
         * <summary>Returns the count of obstacle neighbors taken into account
         * to compute the current velocity for the specified agent.</summary>
         *
         * <returns>The count of obstacle neighbors taken into account to
         * compute the current velocity for the specified agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose count of obstacle
         * neighbors is to be retrieved.</param>
         */
        public int GetAgentNumObstacleNeighbors(int agentNo)
        {
            return Agents[agentNo].ObstacleNeighborList.Count;
        }

        /**
         * <summary>Returns the specified obstacle neighbor of the specified
         * agent.</summary>
         *
         * <returns>The number of the first vertex of the neighboring obstacle
         * edge.</returns>
         *
         * <param name="agentNo">The number of the agent whose obstacle neighbor
         * is to be retrieved.</param>
         * <param name="neighborNo">The number of the obstacle neighbor to be
         * retrieved.</param>
         */
        public int GetAgentObstacleNeighbor(int agentNo, int neighborNo)
        {
            return Agents[agentNo].ObstacleNeighborList[neighborNo].Value.ID;
        }

        /**
         * <summary>Returns the ORCA constraints of the specified agent.
         * </summary>
         *
         * <returns>A list of lines representing the ORCA constraints.</returns>
         *
         * <param name="agentNo">The number of the agent whose ORCA constraints
         * are to be retrieved.</param>
         *
         * <remarks>The halfplane to the left of each line is the region of
         * permissible velocities with respect to that ORCA constraint.
         * </remarks>
         */
        public IList<Line> GetAgentOrcaLines(int agentNo)
        {
            return Agents[agentNo].OrcaLines;
        }

        /**
         * <summary>Returns the two-dimensional position of a specified agent.
         * </summary>
         *
         * <returns>The present two-dimensional position of the (center of the)
         * agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * position is to be retrieved.</param>
         */
        public Vector2 GetAgentPosition(int agentNo)
        {
            return Agents[agentNo].Position;
        }

        public Vector2 GetWorldAgentPosition(int id)
        {
            return m_WorldAgentList[id].Position;
        }
        
        /**
         * <summary>Returns the two-dimensional preferred velocity of a
         * specified agent.</summary>
         *
         * <returns>The present two-dimensional preferred velocity of the agent.
         * </returns>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * preferred velocity is to be retrieved.</param>
         */
        public Vector2 getAgentPrefVelocity(int agentNo)
        {
            return Agents[agentNo].PrefVelocity;
        }

        /**
         * <summary>Returns the radius of a specified agent.</summary>
         *
         * <returns>The present radius of the agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose radius is to be
         * retrieved.</param>
         */
        public float GetAgentRadius(int agentNo)
        {
            return Agents[agentNo].Radius;
        }

        /**
         * <summary>Returns the time horizon of a specified agent.</summary>
         *
         * <returns>The present time horizon of the agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose time horizon is
         * to be retrieved.</param>
         */
        public float GetAgentTimeHorizon(int agentNo)
        {
            return Agents[agentNo].TimeHorizon;
        }

        /**
         * <summary>Returns the time horizon with respect to obstacles of a
         * specified agent.</summary>
         *
         * <returns>The present time horizon with respect to obstacles of the
         * agent.</returns>
         *
         * <param name="agentNo">The number of the agent whose time horizon with
         * respect to obstacles is to be retrieved.</param>
         */
        public float GetAgentTimeHorizonObst(int agentNo)
        {
            return Agents[agentNo].TimeHorizonObst;
        }

        /**
         * <summary>Returns the two-dimensional linear velocity of a specified
         * agent.</summary>
         *
         * <returns>The present two-dimensional linear velocity of the agent.
         * </returns>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * linear velocity is to be retrieved.</param>
         */
        public Vector2 GetAgentVelocity(int agentNo)
        {
            return Agents[agentNo].Velocity;
        }

        /**
         * <summary>Returns the global time of the simulation.</summary>
         *
         * <returns>The present global time of the simulation (zero initially).
         * </returns>
         */
        public float GetGlobalTime()
        {
            return m_GlobalTime;
        }

        /**
         * <summary>Returns the count of agents in the simulation.</summary>
         *
         * <returns>The count of agents in the simulation.</returns>
         */
        public int GetNumAgents()
        {
            return Agents.Count;
        }

        public int GetWorldNumAgents()
        {
            return m_WorldAgentList.Count;
        }
        
        /**
         * <summary>Returns the count of obstacle vertices in the simulation.
         * </summary>
         *
         * <returns>The count of obstacle vertices in the simulation.</returns>
         */
        public int GetNumObstacleVertices()
        {
            return Obstacles.Count;
        }

        /**
         * <summary>Returns the count of workers.</summary>
         *
         * <returns>The count of workers.</returns>
         */
        public int GetNumWorkers()
        {
            return m_NumWorkers;
        }

        /**
         * <summary>Returns the two-dimensional position of a specified obstacle
         * vertex.</summary>
         *
         * <returns>The two-dimensional position of the specified obstacle
         * vertex.</returns>
         *
         * <param name="vertexNo">The number of the obstacle vertex to be
         * retrieved.</param>
         */
        public Vector2 GetObstacleVertex(int vertexNo)
        {
            return Obstacles[vertexNo].Point;
        }

        /**
         * <summary>Returns the number of the obstacle vertex succeeding the
         * specified obstacle vertex in its polygon.</summary>
         *
         * <returns>The number of the obstacle vertex succeeding the specified
         * obstacle vertex in its polygon.</returns>
         *
         * <param name="vertexNo">The number of the obstacle vertex whose
         * successor is to be retrieved.</param>
         */
        public int GetNextObstacleVertexNo(int vertexNo)
        {
            return Obstacles[vertexNo].Next.ID;
        }

        /**
         * <summary>Returns the number of the obstacle vertex preceding the
         * specified obstacle vertex in its polygon.</summary>
         *
         * <returns>The number of the obstacle vertex preceding the specified
         * obstacle vertex in its polygon.</returns>
         *
         * <param name="vertexNo">The number of the obstacle vertex whose
         * predecessor is to be retrieved.</param>
         */
        public int GetPrevObstacleVertexNo(int vertexNo)
        {
            return Obstacles[vertexNo].Previous.ID;
        }

        /**
         * <summary>Returns the time step of the simulation.</summary>
         *
         * <returns>The present time step of the simulation.</returns>
         */
        public float GetTimeStep()
        {
            return TimeStep;
        }

        /**
         * <summary>Processes the obstacles that have been added so that they
         * are accounted for in the simulation.</summary>
         *
         * <remarks>Obstacles added to the simulation after this function has
         * been called are not accounted for in the simulation.</remarks>
         */
        public void ProcessObstacles()
        {
            KdTree.BuildObstacleTree();
        }

        /**
         * <summary>Performs a visibility query between the two specified points
         * with respect to the obstacles.</summary>
         *
         * <returns>A boolean specifying whether the two points are mutually
         * visible. Returns true when the obstacles have not been processed.
         * </returns>
         *
         * <param name="point1">The first point of the query.</param>
         * <param name="point2">The second point of the query.</param>
         * <param name="radius">The minimal distance between the line connecting
         * the two points and the obstacles in order for the points to be
         * mutually visible (optional). Must be non-negative.</param>
         */
        public bool QueryVisibility(Vector2 point1, Vector2 point2, float radius)
        {
            return KdTree.QueryVisibility(point1, point2, radius);
        }

        /**
         * <summary>Sets the default properties for any new agent that is added.
         * </summary>
         *
         * <param name="neighborDist">The default maximum distance (center point
         * to center point) to other agents a new agent takes into account in
         * the navigation. The larger this number, the longer he running time of
         * the simulation. If the number is too low, the simulation will not be
         * safe. Must be non-negative.</param>
         * <param name="maxNeighbors">The default maximum number of other agents
         * a new agent takes into account in the navigation. The larger this
         * number, the longer the running time of the simulation. If the number
         * is too low, the simulation will not be safe.</param>
         * <param name="timeHorizon">The default minimal amount of time for
         * which a new agent's velocities that are computed by the simulation
         * are safe with respect to other agents. The larger this number, the
         * sooner an agent will respond to the presence of other agents, but the
         * less freedom the agent has in choosing its velocities. Must be
         * positive.</param>
         * <param name="timeHorizonObst">The default minimal amount of time for
         * which a new agent's velocities that are computed by the simulation
         * are safe with respect to obstacles. The larger this number, the
         * sooner an agent will respond to the presence of obstacles, but the
         * less freedom the agent has in choosing its velocities. Must be
         * positive.</param>
         * <param name="radius">The default radius of a new agent. Must be
         * non-negative.</param>
         * <param name="maxSpeed">The default maximum speed of a new agent. Must
         * be non-negative.</param>
         * <param name="velocity">The default initial two-dimensional linear
         * velocity of a new agent.</param>
         */
        public void SetAgentDefaults(float neighborDist, int maxNeighbors, float timeHorizon, float timeHorizonObst, float radius, float maxSpeed, Vector2 velocity)
        {
            if (m_DefaultAgent == null)
            {
                m_DefaultAgent = new Agent();
            }

            m_DefaultAgent.MAXNeighbors = maxNeighbors;
            m_DefaultAgent.MAXSpeed = maxSpeed;
            m_DefaultAgent.NeighborDist = neighborDist;
            m_DefaultAgent.Radius = radius;
            m_DefaultAgent.TimeHorizon = timeHorizon;
            m_DefaultAgent.TimeHorizonObst = timeHorizonObst;
            m_DefaultAgent.Velocity = velocity;
        }

        /**
         * <summary>Sets the maximum neighbor count of a specified agent.
         * </summary>
         *
         * <param name="agentNo">The number of the agent whose maximum neighbor
         * count is to be modified.</param>
         * <param name="maxNeighbors">The replacement maximum neighbor count.
         * </param>
         */
        public void SetAgentMaxNeighbors(int agentNo, int maxNeighbors)
        {
            Agents[agentNo].MAXNeighbors = maxNeighbors;
        }

        /**
         * <summary>Sets the maximum speed of a specified agent.</summary>
         *
         * <param name="agentNo">The number of the agent whose maximum speed is
         * to be modified.</param>
         * <param name="maxSpeed">The replacement maximum speed. Must be
         * non-negative.</param>
         */
        public void SetAgentMaxSpeed(int agentNo, float maxSpeed)
        {
            Agents[agentNo].MAXSpeed = maxSpeed;
        }

        /**
         * <summary>Sets the maximum neighbor distance of a specified agent.
         * </summary>
         *
         * <param name="agentNo">The number of the agent whose maximum neighbor
         * distance is to be modified.</param>
         * <param name="neighborDist">The replacement maximum neighbor distance.
         * Must be non-negative.</param>
         */
        public void SetAgentNeighborDist(int agentNo, float neighborDist)
        {
            Agents[agentNo].NeighborDist = neighborDist;
        }

        /**
         * <summary>Sets the two-dimensional position of a specified agent.
         * </summary>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * position is to be modified.</param>
         * <param name="position">The replacement of the two-dimensional
         * position.</param>
         */
        public void SetAgentPosition(int agentNo, Vector2 position)
        {
            Agents[agentNo].Position = position;
        }

        /**
         * <summary>Sets the two-dimensional preferred velocity of a specified
         * agent.</summary>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * preferred velocity is to be modified.</param>
         * <param name="prefVelocity">The replacement of the two-dimensional
         * preferred velocity.</param>
         */
        public void SetAgentPrefVelocity(int agentNo, Vector2 prefVelocity)
        {
            Agents[agentNo].PrefVelocity = prefVelocity;
            if (Agents[agentNo] is Group group)
            {
                group.PrefVelocity = prefVelocity;
            }
        }

        /**
         * <summary>Sets the radius of a specified agent.</summary>
         *
         * <param name="agentNo">The number of the agent whose radius is to be
         * modified.</param>
         * <param name="radius">The replacement radius. Must be non-negative.
         * </param>
         */
        public void SetAgentRadius(int agentNo, float radius)
        {
            Agents[agentNo].Radius = radius;
        }

        public void SetWorldAgentMass(int agentNo, float mass) {
            m_WorldAgentList[agentNo].Mass = mass;
        }

        /**
         * <summary>Sets the time horizon of a specified agent with respect to
         * other agents.</summary>
         *
         * <param name="agentNo">The number of the agent whose time horizon is
         * to be modified.</param>
         * <param name="timeHorizon">The replacement time horizon with respect
         * to other agents. Must be positive.</param>
         */
        public void SetAgentTimeHorizon(int agentNo, float timeHorizon)
        {
            Agents[agentNo].TimeHorizon = timeHorizon;
        }

        /**
         * <summary>Sets the time horizon of a specified agent with respect to
         * obstacles.</summary>
         *
         * <param name="agentNo">The number of the agent whose time horizon with
         * respect to obstacles is to be modified.</param>
         * <param name="timeHorizonObst">The replacement time horizon with
         * respect to obstacles. Must be positive.</param>
         */
        public void SetAgentTimeHorizonObst(int agentNo, float timeHorizonObst)
        {
            Agents[agentNo].TimeHorizonObst = timeHorizonObst;
        }

        /**
         * <summary>Sets the two-dimensional linear velocity of a specified
         * agent.</summary>
         *
         * <param name="agentNo">The number of the agent whose two-dimensional
         * linear velocity is to be modified.</param>
         * <param name="velocity">The replacement two-dimensional linear
         * velocity.</param>
         */
        public void SetAgentVelocity(int agentNo, Vector2 velocity)
        {
            Agents[agentNo].Velocity = velocity;
        }

        /**
         * <summary>Sets the global time of the simulation.</summary>
         *
         * <param name="globalTime">The global time of the simulation.</param>
         */
        public void SetGlobalTime(float globalTime)
        {
            m_GlobalTime = globalTime;
        }

        /**
         * <summary>Sets the number of workers.</summary>
         *
         * <param name="numWorkers">The number of workers.</param>
         */
        public void SetNumWorkers(int numWorkers)
        {
            m_NumWorkers = numWorkers;

            if (m_NumWorkers <= 0)
            {
                int completionPorts;
                ThreadPool.GetMinThreads(out m_NumWorkers, out completionPorts);
            }
            m_Workers = null;
        }

        /**
         * <summary>Sets the time step of the simulation.</summary>
         *
         * <param name="timeStep">The time step of the simulation. Must be
         * positive.</param>
         */
        public void SetTimeStep(float timeStep)
        {
            TimeStep = timeStep;
        }

        /**
         * <summary>Constructs and initializes a simulation.</summary>
         */
        private Simulator()
        {
            Clear();
        }
    }
}
