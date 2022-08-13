/*
 * KdTree.cs
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

using System.Collections.Generic;
using System;

namespace RVO
{
    /**
     * <summary>Defines k-D trees for agents and static obstacles in the
     * simulation.</summary>
     */
    internal class KdTree
    {
        /**
         * <summary>Defines a node of an agent k-D tree.</summary>
         */
        private struct AgentTreeNode
        {
            internal int Begin;
            internal int End;
            internal int Left;
            internal int Right;
            internal float MaxX;
            internal float MaxY;
            internal float MinX;
            internal float MinY;
        }

        /**
         * <summary>Defines a pair of scalar values.</summary>
         */
        private struct FloatPair
        {
            private float m_A;
            private float m_B;

            /**
             * <summary>Constructs and initializes a pair of scalar
             * values.</summary>
             *
             * <param name="a">The first scalar value.</returns>
             * <param name="b">The second scalar value.</returns>
             */
            internal FloatPair(float a, float b)
            {
                m_A = a;
                m_B = b;
            }

            /**
             * <summary>Returns true if the first pair of scalar values is less
             * than the second pair of scalar values.</summary>
             *
             * <returns>True if the first pair of scalar values is less than the
             * second pair of scalar values.</returns>
             *
             * <param name="pair1">The first pair of scalar values.</param>
             * <param name="pair2">The second pair of scalar values.</param>
             */
            public static bool operator <(FloatPair pair1, FloatPair pair2)
            {
                return pair1.m_A < pair2.m_A || !(pair2.m_A < pair1.m_A) && pair1.m_B < pair2.m_B;
            }

            /**
             * <summary>Returns true if the first pair of scalar values is less
             * than or equal to the second pair of scalar values.</summary>
             *
             * <returns>True if the first pair of scalar values is less than or
             * equal to the second pair of scalar values.</returns>
             *
             * <param name="pair1">The first pair of scalar values.</param>
             * <param name="pair2">The second pair of scalar values.</param>
             */
            public static bool operator <=(FloatPair pair1, FloatPair pair2)
            {
                return (pair1.m_A == pair2.m_A && pair1.m_B == pair2.m_B) || pair1 < pair2;
            }

            /**
             * <summary>Returns true if the first pair of scalar values is
             * greater than the second pair of scalar values.</summary>
             *
             * <returns>True if the first pair of scalar values is greater than
             * the second pair of scalar values.</returns>
             *
             * <param name="pair1">The first pair of scalar values.</param>
             * <param name="pair2">The second pair of scalar values.</param>
             */
            public static bool operator >(FloatPair pair1, FloatPair pair2)
            {
                return !(pair1 <= pair2);
            }

            /**
             * <summary>Returns true if the first pair of scalar values is
             * greater than or equal to the second pair of scalar values.
             * </summary>
             *
             * <returns>True if the first pair of scalar values is greater than
             * or equal to the second pair of scalar values.</returns>
             *
             * <param name="pair1">The first pair of scalar values.</param>
             * <param name="pair2">The second pair of scalar values.</param>
             */
            public static bool operator >=(FloatPair pair1, FloatPair pair2)
            {
                return !(pair1 < pair2);
            }
        }

        /**
         * <summary>Defines a node of an obstacle k-D tree.</summary>
         */
        private class ObstacleTreeNode
        {
            internal Obstacle Obstacle;
            internal ObstacleTreeNode Left;
            internal ObstacleTreeNode Right;
        };

        /**
         * <summary>The maximum size of an agent k-D tree leaf.</summary>
         */
        private const int MaxLeafSize = 10;

        private Agent[] m_AgentArray;
        private AgentTreeNode[] m_AgentTree;
        private ObstacleTreeNode m_ObstacleTree;

        /**
         * <summary>Builds an agent k-D tree.</summary>
         */
        internal void buildAgentTree()
        {
            if (m_AgentArray == null || m_AgentArray.Length != Simulator.Instance.Agents.Count)
            {
                m_AgentArray = new Agent[Simulator.Instance.Agents.Count];

                for (int i = 0; i < m_AgentArray.Length; ++i)
                {
                    m_AgentArray[i] = Simulator.Instance.Agents[i];
                }

                m_AgentTree = new AgentTreeNode[2 * m_AgentArray.Length];

                for (int i = 0; i < m_AgentTree.Length; ++i)
                {
                    m_AgentTree[i] = new AgentTreeNode();
                }
            }

            if (m_AgentArray.Length != 0)
            {
                BuildAgentTreeRecursive(0, m_AgentArray.Length, 0);
            }
        }

        /**
         * <summary>Builds an obstacle k-D tree.</summary>
         */
        internal void BuildObstacleTree()
        {
            m_ObstacleTree = new ObstacleTreeNode();

            IList<Obstacle> obstacles = new List<Obstacle>(Simulator.Instance.Obstacles.Count);

            for (int i = 0; i < Simulator.Instance.Obstacles.Count; ++i)
            {
                obstacles.Add(Simulator.Instance.Obstacles[i]);
            }

            m_ObstacleTree = BuildObstacleTreeRecursive(obstacles);
        }

        /**
         * <summary>Computes the agent neighbors of the specified agent.
         * </summary>
         *
         * <param name="agent">The agent for which agent neighbors are to be
         * computed.</param>
         * <param name="rangeSq">The squared range around the agent.</param>
         */
        internal void ComputeAgentNeighbors(Agent agent, ref float rangeSq)
        {
            QueryAgentTreeRecursive(agent, ref rangeSq, 0);
        }

        /**
         * <summary>Computes the obstacle neighbors of the specified agent.
         * </summary>
         *
         * <param name="agent">The agent for which obstacle neighbors are to be
         * computed.</param>
         * <param name="rangeSq">The squared range around the agent.</param>
         */
        internal void ComputeObstacleNeighbors(Agent agent, float rangeSq)
        {
            QueryObstacleTreeRecursive(agent, rangeSq, m_ObstacleTree);
        }

        /**
         * <summary>Queries the visibility between two points within a specified
         * radius.</summary>
         *
         * <returns>True if q1 and q2 are mutually visible within the radius;
         * false otherwise.</returns>
         *
         * <param name="q1">The first point between which visibility is to be
         * tested.</param>
         * <param name="q2">The second point between which visibility is to be
         * tested.</param>
         * <param name="radius">The radius within which visibility is to be
         * tested.</param>
         */
        internal bool QueryVisibility(Vector2 q1, Vector2 q2, float radius)
        {
            return QueryVisibilityRecursive(q1, q2, radius, m_ObstacleTree);
        }

        /**
         * <summary>Recursive method for building an agent k-D tree.</summary>
         *
         * <param name="begin">The beginning agent k-D tree node node index.
         * </param>
         * <param name="end">The ending agent k-D tree node index.</param>
         * <param name="node">The current agent k-D tree node index.</param>
         */
        private void BuildAgentTreeRecursive(int begin, int end, int node)
        {
            m_AgentTree[node].Begin = begin;
            m_AgentTree[node].End = end;
            m_AgentTree[node].MinX = m_AgentTree[node].MaxX = m_AgentArray[begin].Position.m_X;
            m_AgentTree[node].MinY = m_AgentTree[node].MaxY = m_AgentArray[begin].Position.m_Y;

            for (int i = begin + 1; i < end; ++i)
            {
                m_AgentTree[node].MaxX = Math.Max(m_AgentTree[node].MaxX, m_AgentArray[i].Position.m_X);
                m_AgentTree[node].MinX = Math.Min(m_AgentTree[node].MinX, m_AgentArray[i].Position.m_X);
                m_AgentTree[node].MaxY = Math.Max(m_AgentTree[node].MaxY, m_AgentArray[i].Position.m_Y);
                m_AgentTree[node].MinY = Math.Min(m_AgentTree[node].MinY, m_AgentArray[i].Position.m_Y);
            }

            if (end - begin > MaxLeafSize)
            {
                /* No leaf node. */
                bool isVertical = m_AgentTree[node].MaxX - m_AgentTree[node].MinX > m_AgentTree[node].MaxY - m_AgentTree[node].MinY;
                float splitValue = 0.5f * (isVertical ? m_AgentTree[node].MaxX + m_AgentTree[node].MinX : m_AgentTree[node].MaxY + m_AgentTree[node].MinY);

                int left = begin;
                int right = end;

                while (left < right)
                {
                    while (left < right && (isVertical ? m_AgentArray[left].Position.m_X : m_AgentArray[left].Position.m_Y) < splitValue)
                    {
                        ++left;
                    }

                    while (right > left && (isVertical ? m_AgentArray[right - 1].Position.m_X : m_AgentArray[right - 1].Position.m_Y) >= splitValue)
                    {
                        --right;
                    }

                    if (left < right)
                    {
                        (m_AgentArray[left], m_AgentArray[right - 1]) = (m_AgentArray[right - 1], m_AgentArray[left]);
                        ++left;
                        --right;
                    }
                }

                int leftSize = left - begin;

                if (leftSize == 0)
                {
                    ++leftSize;
                    ++left;
                    ++right;
                }

                m_AgentTree[node].Left = node + 1;
                m_AgentTree[node].Right = node + 2 * leftSize;

                BuildAgentTreeRecursive(begin, left, m_AgentTree[node].Left);
                BuildAgentTreeRecursive(left, end, m_AgentTree[node].Right);
            }
        }

        /**
         * <summary>Recursive method for building an obstacle k-D tree.
         * </summary>
         *
         * <returns>An obstacle k-D tree node.</returns>
         *
         * <param name="obstacles">A list of obstacles.</param>
         */
        private ObstacleTreeNode BuildObstacleTreeRecursive(IList<Obstacle> obstacles)
        {
            if (obstacles.Count == 0)
            {
                return null;
            }

            ObstacleTreeNode node = new ObstacleTreeNode();

            int optimalSplit = 0;
            int minLeft = obstacles.Count;
            int minRight = obstacles.Count;

            for (int i = 0; i < obstacles.Count; ++i)
            {
                int leftSize = 0;
                int rightSize = 0;

                Obstacle obstacleI1 = obstacles[i];
                Obstacle obstacleI2 = obstacleI1.Next;

                /* Compute optimal split node. */
                for (int j = 0; j < obstacles.Count; ++j)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    Obstacle obstacleJ1 = obstacles[j];
                    Obstacle obstacleJ2 = obstacleJ1.Next;

                    float j1LeftOfI = RVOMath.LeftOf(obstacleI1.Point, obstacleI2.Point, obstacleJ1.Point);
                    float j2LeftOfI = RVOMath.LeftOf(obstacleI1.Point, obstacleI2.Point, obstacleJ2.Point);

                    if (j1LeftOfI >= -RVOMath.RVO_EPSILON && j2LeftOfI >= -RVOMath.RVO_EPSILON)
                    {
                        ++leftSize;
                    }
                    else if (j1LeftOfI <= RVOMath.RVO_EPSILON && j2LeftOfI <= RVOMath.RVO_EPSILON)
                    {
                        ++rightSize;
                    }
                    else
                    {
                        ++leftSize;
                        ++rightSize;
                    }

                    if (new FloatPair(Math.Max(leftSize, rightSize), Math.Min(leftSize, rightSize)) >= new FloatPair(Math.Max(minLeft, minRight), Math.Min(minLeft, minRight)))
                    {
                        break;
                    }
                }

                if (new FloatPair(Math.Max(leftSize, rightSize), Math.Min(leftSize, rightSize)) < new FloatPair(Math.Max(minLeft, minRight), Math.Min(minLeft, minRight)))
                {
                    minLeft = leftSize;
                    minRight = rightSize;
                    optimalSplit = i;
                }
            }

            {
                /* Build split node. */
                IList<Obstacle> leftObstacles = new List<Obstacle>(minLeft);

                for (int n = 0; n < minLeft; ++n)
                {
                    leftObstacles.Add(null);
                }

                IList<Obstacle> rightObstacles = new List<Obstacle>(minRight);

                for (int n = 0; n < minRight; ++n)
                {
                    rightObstacles.Add(null);
                }

                int leftCounter = 0;
                int rightCounter = 0;
                int i = optimalSplit;

                Obstacle obstacleI1 = obstacles[i];
                Obstacle obstacleI2 = obstacleI1.Next;

                for (int j = 0; j < obstacles.Count; ++j)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    Obstacle obstacleJ1 = obstacles[j];
                    Obstacle obstacleJ2 = obstacleJ1.Next;

                    float j1LeftOfI = RVOMath.LeftOf(obstacleI1.Point, obstacleI2.Point, obstacleJ1.Point);
                    float j2LeftOfI = RVOMath.LeftOf(obstacleI1.Point, obstacleI2.Point, obstacleJ2.Point);

                    if (j1LeftOfI >= -RVOMath.RVO_EPSILON && j2LeftOfI >= -RVOMath.RVO_EPSILON)
                    {
                        leftObstacles[leftCounter++] = obstacles[j];
                    }
                    else if (j1LeftOfI <= RVOMath.RVO_EPSILON && j2LeftOfI <= RVOMath.RVO_EPSILON)
                    {
                        rightObstacles[rightCounter++] = obstacles[j];
                    }
                    else
                    {
                        /* Split obstacle j. */
                        float t = RVOMath.Det(obstacleI2.Point - obstacleI1.Point, obstacleJ1.Point - obstacleI1.Point) / RVOMath.Det(obstacleI2.Point - obstacleI1.Point, obstacleJ1.Point - obstacleJ2.Point);

                        Vector2 splitPoint = obstacleJ1.Point + t * (obstacleJ2.Point - obstacleJ1.Point);

                        Obstacle newObstacle = new Obstacle();
                        newObstacle.Point = splitPoint;
                        newObstacle.Previous = obstacleJ1;
                        newObstacle.Next = obstacleJ2;
                        newObstacle.Convex = true;
                        newObstacle.Direction = obstacleJ1.Direction;

                        newObstacle.ID = Simulator.Instance.Obstacles.Count;

                        Simulator.Instance.Obstacles.Add(newObstacle);

                        obstacleJ1.Next = newObstacle;
                        obstacleJ2.Previous = newObstacle;

                        if (j1LeftOfI > 0.0f)
                        {
                            leftObstacles[leftCounter++] = obstacleJ1;
                            rightObstacles[rightCounter++] = newObstacle;
                        }
                        else
                        {
                            rightObstacles[rightCounter++] = obstacleJ1;
                            leftObstacles[leftCounter++] = newObstacle;
                        }
                    }
                }

                node.Obstacle = obstacleI1;
                node.Left = BuildObstacleTreeRecursive(leftObstacles);
                node.Right = BuildObstacleTreeRecursive(rightObstacles);

                return node;
            }
        }

        /**
         * <summary>Recursive method for computing the agent neighbors of the
         * specified agent.</summary>
         *
         * <param name="agent">The agent for which agent neighbors are to be
         * computed.</param>
         * <param name="rangeSq">The squared range around the agent.</param>
         * <param name="node">The current agent k-D tree node index.</param>
         */
        private void QueryAgentTreeRecursive(Agent agent, ref float rangeSq, int node)
        {
            if (m_AgentTree[node].End - m_AgentTree[node].Begin <= MaxLeafSize)
            {
                for (int i = m_AgentTree[node].Begin; i < m_AgentTree[node].End; ++i)
                {
                    agent.insertAgentNeighbor(m_AgentArray[i], ref rangeSq);
                }
            }
            else
            {
                float distSqLeft = RVOMath.Sqr(Math.Max(0.0f, m_AgentTree[m_AgentTree[node].Left].MinX - agent.Position.m_X)) + RVOMath.Sqr(Math.Max(0.0f, agent.Position.m_X - m_AgentTree[m_AgentTree[node].Left].MaxX)) + RVOMath.Sqr(Math.Max(0.0f, m_AgentTree[m_AgentTree[node].Left].MinY - agent.Position.m_Y)) + RVOMath.Sqr(Math.Max(0.0f, agent.Position.m_Y - m_AgentTree[m_AgentTree[node].Left].MaxY));
                float distSqRight = RVOMath.Sqr(Math.Max(0.0f, m_AgentTree[m_AgentTree[node].Right].MinX - agent.Position.m_X)) + RVOMath.Sqr(Math.Max(0.0f, agent.Position.m_X - m_AgentTree[m_AgentTree[node].Right].MaxX)) + RVOMath.Sqr(Math.Max(0.0f, m_AgentTree[m_AgentTree[node].Right].MinY - agent.Position.m_Y)) + RVOMath.Sqr(Math.Max(0.0f, agent.Position.m_Y - m_AgentTree[m_AgentTree[node].Right].MaxY));

                if (distSqLeft < distSqRight)
                {
                    if (distSqLeft < rangeSq)
                    {
                        QueryAgentTreeRecursive(agent, ref rangeSq, m_AgentTree[node].Left);

                        if (distSqRight < rangeSq)
                        {
                            QueryAgentTreeRecursive(agent, ref rangeSq, m_AgentTree[node].Right);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        QueryAgentTreeRecursive(agent, ref rangeSq, m_AgentTree[node].Right);

                        if (distSqLeft < rangeSq)
                        {
                            QueryAgentTreeRecursive(agent, ref rangeSq, m_AgentTree[node].Left);
                        }
                    }
                }

            }
        }

        /**
         * <summary>Recursive method for computing the obstacle neighbors of the
         * specified agent.</summary>
         *
         * <param name="agent">The agent for which obstacle neighbors are to be
         * computed.</param>
         * <param name="rangeSq">The squared range around the agent.</param>
         * <param name="node">The current obstacle k-D node.</param>
         */
        private void QueryObstacleTreeRecursive(Agent agent, float rangeSq, ObstacleTreeNode node)
        {
            if (node != null)
            {
                Obstacle obstacle1 = node.Obstacle;
                Obstacle obstacle2 = obstacle1.Next;

                float agentLeftOfLine = RVOMath.LeftOf(obstacle1.Point, obstacle2.Point, agent.Position);

                QueryObstacleTreeRecursive(agent, rangeSq, agentLeftOfLine >= 0.0f ? node.Left : node.Right);

                float distSqLine = RVOMath.Sqr(agentLeftOfLine) / RVOMath.ABSSq(obstacle2.Point - obstacle1.Point);

                if (distSqLine < rangeSq)
                {
                    if (agentLeftOfLine < 0.0f)
                    {
                        /*
                         * Try obstacle at this node only if agent is on right side of
                         * obstacle (and can see obstacle).
                         */
                        agent.insertObstacleNeighbor(node.Obstacle, rangeSq);
                    }

                    /* Try other side of line. */
                    QueryObstacleTreeRecursive(agent, rangeSq, agentLeftOfLine >= 0.0f ? node.Right : node.Left);
                }
            }
        }

        /**
         * <summary>Recursive method for querying the visibility between two
         * points within a specified radius.</summary>
         *
         * <returns>True if q1 and q2 are mutually visible within the radius;
         * false otherwise.</returns>
         *
         * <param name="q1">The first point between which visibility is to be
         * tested.</param>
         * <param name="q2">The second point between which visibility is to be
         * tested.</param>
         * <param name="radius">The radius within which visibility is to be
         * tested.</param>
         * <param name="node">The current obstacle k-D node.</param>
         */
        private bool QueryVisibilityRecursive(Vector2 q1, Vector2 q2, float radius, ObstacleTreeNode node)
        {
            if (node == null)
            {
                return true;
            }

            Obstacle obstacle1 = node.Obstacle;
            Obstacle obstacle2 = obstacle1.Next;

            float q1LeftOfI = RVOMath.LeftOf(obstacle1.Point, obstacle2.Point, q1);
            float q2LeftOfI = RVOMath.LeftOf(obstacle1.Point, obstacle2.Point, q2);
            float invLengthI = 1.0f / RVOMath.ABSSq(obstacle2.Point - obstacle1.Point);

            if (q1LeftOfI >= 0.0f && q2LeftOfI >= 0.0f)
            {
                return QueryVisibilityRecursive(q1, q2, radius, node.Left) && ((RVOMath.Sqr(q1LeftOfI) * invLengthI >= RVOMath.Sqr(radius) && RVOMath.Sqr(q2LeftOfI) * invLengthI >= RVOMath.Sqr(radius)) || QueryVisibilityRecursive(q1, q2, radius, node.Right));
            }

            if (q1LeftOfI <= 0.0f && q2LeftOfI <= 0.0f)
            {
                return QueryVisibilityRecursive(q1, q2, radius, node.Right) && ((RVOMath.Sqr(q1LeftOfI) * invLengthI >= RVOMath.Sqr(radius) && RVOMath.Sqr(q2LeftOfI) * invLengthI >= RVOMath.Sqr(radius)) || QueryVisibilityRecursive(q1, q2, radius, node.Left));
            }

            if (q1LeftOfI >= 0.0f && q2LeftOfI <= 0.0f)
            {
                /* One can see through obstacle from left to right. */
                return QueryVisibilityRecursive(q1, q2, radius, node.Left) && QueryVisibilityRecursive(q1, q2, radius, node.Right);
            }

            float point1LeftOfQ = RVOMath.LeftOf(q1, q2, obstacle1.Point);
            float point2LeftOfQ = RVOMath.LeftOf(q1, q2, obstacle2.Point);
            float invLengthQ = 1.0f / RVOMath.ABSSq(q2 - q1);

            return point1LeftOfQ * point2LeftOfQ >= 0.0f && RVOMath.Sqr(point1LeftOfQ) * invLengthQ > RVOMath.Sqr(radius) && RVOMath.Sqr(point2LeftOfQ) * invLengthQ > RVOMath.Sqr(radius) && QueryVisibilityRecursive(q1, q2, radius, node.Left) && QueryVisibilityRecursive(q1, q2, radius, node.Right);
        }
    }
}
