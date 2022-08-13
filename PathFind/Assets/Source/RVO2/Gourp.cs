/*
 * Agent.cs
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
using UnityEngine;

namespace RVO {
    /**
     * <summary>Defines an agent in the simulation.</summary>
     */
    public class Group : Agent
    {
        internal List<Agent> m_ChildList = new List<Agent>();
        internal Vector2 m_Center;
        internal Vector2 m_PadMin = Vector2.Zero, m_PadMax = Vector2.MIN;
        internal void AddChild(Agent agent)
        {
            if (agent == null)
                return;
            m_ChildList.Add(agent);

            float minX = 0, minY = 0;
            CaculCircular(agent:agent);

            Position = (m_PadMin + m_PadMax) / 2;
            m_Center = Position;//目前中心点就是包围圆的坐标点
            Mass += agent.Mass;
            Vector2 diamV2 = (m_PadMax - m_PadMin);
            Radius = Mathf.Sqrt(diamV2.m_X * diamV2.m_X + diamV2.m_Y * diamV2.m_Y) / 2 ;
        }

        internal Agent RemoveChild(int id)
        {
            int idx = -1;
            Agent agent = null;
            for (int i = 0; i < m_ChildList.Count; i++)
            {
                if (m_ChildList[i].ID == id)
                {
                    idx = i;
                    break;
                }
            }

            if (idx >= 0)
            {
                agent = m_ChildList[idx];
                Mass -= agent.Mass;
                if ((agent.Position.m_X == m_PadMin.m_X) || (agent.Position.m_X == m_PadMax.m_X)
                    ||(agent.Position.m_Y == m_PadMin.m_Y) || (agent.Position.m_Y == m_PadMax.m_Y))
                {
                    ReCaculCircular();
                }
                m_ChildList.RemoveAt(idx);
            }
            return agent;
        }

        internal void ReCaculCircular()
        {
            m_PadMin = Vector2.MAX;
            m_PadMax = Vector2.MIN;
            if (m_ChildList.Count > 0)
            {
                for (int i = 0; i < m_ChildList.Count; i++)
                {
                    Agent agent = m_ChildList[i];
                    CaculCircular(agent:agent);
                }
            }
            Position = (m_PadMin + m_PadMax) / 2;
            m_Center = Position;//目前中心点就是包围圆的坐标点
            Vector2 diamV2 = (m_PadMax - m_PadMin);
            Radius = Mathf.Sqrt(diamV2.m_X * diamV2.m_X + diamV2.m_Y * diamV2.m_Y) / 2 ;
        }

        internal void CaculCircular(Agent agent)
        {
            float minX = 0, minY = 0;
            minX = m_PadMin.m_X > agent.Position.m_X ? agent.Position.m_X : m_PadMin.m_X;
            minY = m_PadMin.m_Y > agent.Position.m_Y ? agent.Position.m_Y : m_PadMin.m_Y;
            if (minX != 0 || minY != 0)
            {
                m_PadMin = new Vector2(minX, minY);
            }
            
            float maxX = 0, maxY = 0;
            maxX = m_PadMax.m_X < agent.Position.m_X ? agent.Position.m_X : m_PadMax.m_X;
            maxY = m_PadMax.m_Y < agent.Position.m_Y ? agent.Position.m_Y : m_PadMax.m_Y;
            if (minX != 0 || minY != 0)
            {
                m_PadMax = new Vector2(maxX, maxY);
            }
        }
        
        internal Agent[] Clear()
        {
            Agent[] agents = m_ChildList.ToArray();
            m_ChildList.Clear();
            Mass = 0;
            Radius = 0;
            return agents;
        }
        
        internal override void Update()
        {
            Velocity = NewVelocity;
            Position += Velocity * Simulator.Instance.TimeStep;

            
            for (int i = 0; i < m_ChildList.Count; i++)
            {
                m_ChildList[i].Update();
            }
            
            ////////////////////////////////////////////////////////////////////////////////////////////////
            Test01.m_SphereList[ID].msVelocity = Velocity;
            ////////////////////////////////////////////////////////////////////////////////////////////////
        }
    }
}
