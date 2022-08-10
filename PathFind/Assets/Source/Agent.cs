﻿/*
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
    internal class Agent {
        static bool msDirectionOpt = false;

        internal IList<KeyValuePair<float, Agent>> agentNeighbors_ = new List<KeyValuePair<float, Agent>>();
        internal IList<KeyValuePair<float, Obstacle>> obstacleNeighbors_ = new List<KeyValuePair<float, Obstacle>>();
        internal IList<Line> orcaLines_ = new List<Line>();
        internal Vector2 position_;
        internal Vector2 prefVelocity_;
        internal Vector2 velocity_;
        internal int id_ = 0;
        internal int maxNeighbors_ = 0;
        internal float maxSpeed_ = 0.0f;
        internal float neighborDist_ = 0.0f;
        internal float radius_ = 0.0f;
        internal float timeHorizon_ = 0.0f;
        internal float timeHorizonObst_ = 0.0f;

        internal float mass_ = 1.0f;

        private Vector2 newVelocity_;

        /**
         * <summary>Computes the neighbors of this agent.</summary>
         */
        internal void computeNeighbors() {
            obstacleNeighbors_.Clear();
            float rangeSq = RVOMath.sqr(timeHorizonObst_ * maxSpeed_ + radius_);
            Simulator.Instance.kdTree_.computeObstacleNeighbors(this, rangeSq);

            agentNeighbors_.Clear();

            if (maxNeighbors_ > 0) {
                rangeSq = RVOMath.sqr(neighborDist_);
                Simulator.Instance.kdTree_.computeAgentNeighbors(this, ref rangeSq);
            }
        }

        internal struct ObstacleTemp
        {
            public Vector2 Point;
            public Vector2 Direction;
            public Vector2 PrevDirection;
            public bool Convex;
        }

        internal void computeObstacleNeighbors(ObstacleTemp obstacle1, ObstacleTemp obstacle2, bool isEqual)
        {
            float invTimeHorizonObst = 1.0f / timeHorizonObst_;
            
            Vector2 relativePosition1 = obstacle1.Point - position_;
            Vector2 relativePosition2 = obstacle2.Point - position_;

            /*
             * Check if velocity obstacle of obstacle is already taken care
             * of by previously constructed obstacle ORCA lines.
             */
            bool alreadyCovered = false;

            for (int j = 0; j < orcaLines_.Count; ++j) {
                if (RVOMath.det(invTimeHorizonObst * relativePosition1 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -RVOMath.RVO_EPSILON && RVOMath.det(invTimeHorizonObst * relativePosition2 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -RVOMath.RVO_EPSILON) {
                    alreadyCovered = true;

                    break;
                }
            }

            if (alreadyCovered) {
                return;
            }

            /* Not yet covered. Check for collisions. */
            float distSq1 = RVOMath.absSq(relativePosition1);
            float distSq2 = RVOMath.absSq(relativePosition2);

            float radiusSq = RVOMath.sqr(radius_);

            Vector2 obstacleVector = obstacle2.Point - obstacle1.Point;
            float s = (-relativePosition1 * obstacleVector) / RVOMath.absSq(obstacleVector);
            float distSqLine = RVOMath.absSq(-relativePosition1 - s * obstacleVector);

            Line line;

            if (s < 0.0f && distSq1 <= radiusSq) {
                /* Collision with left vertex. Ignore if non-convex. */
                if (obstacle1.Convex) {
                    line.point = new Vector2(0.0f, 0.0f);
                    line.direction = RVOMath.normalize(new Vector2(-relativePosition1.y(), relativePosition1.x()));
                    orcaLines_.Add(line);
                }

                return;
            } 
            else if (s > 1.0f && distSq2 <= radiusSq) {
                /*
                 * Collision with right vertex. Ignore if non-convex or if
                 * it will be taken care of by neighboring obstacle.
                 */
                if (obstacle2.Convex && RVOMath.det(relativePosition2, obstacle2.Direction) >= 0.0f) {
                    line.point = new Vector2(0.0f, 0.0f);
                    line.direction = RVOMath.normalize(new Vector2(-relativePosition2.y(), relativePosition2.x()));
                    orcaLines_.Add(line);
                }

                return;
            } 
            else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq) {
                /* Collision with obstacle segment. */
                line.point = new Vector2(0.0f, 0.0f);
                line.direction = -obstacle1.Direction;
                orcaLines_.Add(line);

                return;
            }

            /*
             * No collision. Compute legs. When obliquely viewed, both legs
             * can come from a single vertex. Legs extend cut-off line when
             * non-convex vertex.
             */

            Vector2 leftLegDirection, rightLegDirection;

            if (s < 0.0f && distSqLine <= radiusSq) {
                /*
                 * Obstacle viewed obliquely so that left vertex
                 * defines velocity obstacle.
                 */
                if (!obstacle1.Convex) {
                    /* Ignore obstacle. */
                    return;
                }

                obstacle2 = obstacle1;

                float leg1 = RVOMath.sqrt(distSq1 - radiusSq);
                leftLegDirection = new Vector2(relativePosition1.x() * leg1 - relativePosition1.y() * radius_, relativePosition1.x() * radius_ + relativePosition1.y() * leg1) / distSq1;
                rightLegDirection = new Vector2(relativePosition1.x() * leg1 + relativePosition1.y() * radius_, -relativePosition1.x() * radius_ + relativePosition1.y() * leg1) / distSq1;
            } 
            else if (s > 1.0f && distSqLine <= radiusSq) {
                /*
                 * Obstacle viewed obliquely so that
                 * right vertex defines velocity obstacle.
                 */
                if (!obstacle2.Convex) {
                    /* Ignore obstacle. */
                    return;
                }

                obstacle1 = obstacle2;

                float leg2 = RVOMath.sqrt(distSq2 - radiusSq);
                leftLegDirection = new Vector2(relativePosition2.x() * leg2 - relativePosition2.y() * radius_, relativePosition2.x() * radius_ + relativePosition2.y() * leg2) / distSq2;
                rightLegDirection = new Vector2(relativePosition2.x() * leg2 + relativePosition2.y() * radius_, -relativePosition2.x() * radius_ + relativePosition2.y() * leg2) / distSq2;
            } 
            else 
            {
                /* Usual situation. */
                if (obstacle1.Convex) {
                    float leg1 = RVOMath.sqrt(distSq1 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition1.x() * leg1 - relativePosition1.y() * radius_, relativePosition1.x() * radius_ + relativePosition1.y() * leg1) / distSq1;
                } else {
                    /* Left vertex non-convex; left leg extends cut-off line. */
                    leftLegDirection = -obstacle1.Direction;
                }

                if (obstacle2.Convex) {
                    float leg2 = RVOMath.sqrt(distSq2 - radiusSq);
                    rightLegDirection = new Vector2(relativePosition2.x() * leg2 + relativePosition2.y() * radius_, -relativePosition2.x() * radius_ + relativePosition2.y() * leg2) / distSq2;
                } else {
                    /* Right vertex non-convex; right leg extends cut-off line. */
                    rightLegDirection = obstacle1.Direction;
                }
            }

            /*
             * Legs can never point into neighboring edge when convex
             * vertex, take cutoff-line of neighboring edge instead. If
             * velocity projected on "foreign" leg, no constraint is added.
             */

            //Obstacle leftNeighbor = obstacle1.previous_;
            
            bool isLeftLegForeign = false;
            bool isRightLegForeign = false;

            if (obstacle1.Convex && RVOMath.det(leftLegDirection, -obstacle1.PrevDirection) >= 0.0f) {
                /* Left leg points into obstacle. */
                leftLegDirection = -obstacle1.PrevDirection;
                isLeftLegForeign = true;
            }

            if (obstacle2.Convex && RVOMath.det(rightLegDirection, obstacle2.Direction) <= 0.0f) {
                /* Right leg points into obstacle. */
                rightLegDirection = obstacle2.Direction;
                isRightLegForeign = true;
            }

            /* Compute cut-off centers. */
            Vector2 leftCutOff = invTimeHorizonObst * (obstacle1.Point - position_);
            Vector2 rightCutOff = invTimeHorizonObst * (obstacle2.Point - position_);
            Vector2 cutOffVector = rightCutOff - leftCutOff;

            /* Project current velocity on velocity obstacle. */

            /* Check if current velocity is projected on cutoff circles. */
            float t = isEqual ? 0.5f : ((velocity_ - leftCutOff) * cutOffVector) / RVOMath.absSq(cutOffVector);
            float tLeft = (velocity_ - leftCutOff) * leftLegDirection;
            float tRight = (velocity_ - rightCutOff) * rightLegDirection;

            if ((t < 0.0f && tLeft < 0.0f) || (isEqual && tLeft < 0.0f && tRight < 0.0f)) {
                /* Project on left cut-off circle. */
                Vector2 unitW = RVOMath.normalize(velocity_ - leftCutOff);

                line.direction = new Vector2(unitW.y(), -unitW.x());
                line.point = leftCutOff + radius_ * invTimeHorizonObst * unitW;
                orcaLines_.Add(line);

                return;
            } 
            else if (t > 1.0f && tRight < 0.0f) {
                /* Project on right cut-off circle. */
                Vector2 unitW = RVOMath.normalize(velocity_ - rightCutOff);

                line.direction = new Vector2(unitW.y(), -unitW.x());
                line.point = rightCutOff + radius_ * invTimeHorizonObst * unitW;
                orcaLines_.Add(line);

                return;
            }

            /*
             * Project on left leg, right leg, or cut-off line, whichever is
             * closest to velocity.
             */
            float distSqCutoff = (t < 0.0f || t > 1.0f || isEqual) ? float.PositiveInfinity : RVOMath.absSq(velocity_ - (leftCutOff + t * cutOffVector));
            float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : RVOMath.absSq(velocity_ - (leftCutOff + tLeft * leftLegDirection));
            float distSqRight = tRight < 0.0f ? float.PositiveInfinity : RVOMath.absSq(velocity_ - (rightCutOff + tRight * rightLegDirection));

            if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight) {
                /* Project on cut-off line. */
                line.direction = -obstacle1.Direction;
                line.point = leftCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.y(), line.direction.x());
                orcaLines_.Add(line);

                return;
            }

            if (distSqLeft <= distSqRight) {
                /* Project on left leg. */
                if (isLeftLegForeign) {
                    return;
                }

                line.direction = leftLegDirection;
                line.point = leftCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.y(), line.direction.x());
                orcaLines_.Add(line);

                return;
            }

            /* Project on right leg. */
            if (isRightLegForeign) {
                return;
            }

            line.direction = -rightLegDirection;
            line.point = rightCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.y(), line.direction.x());
            orcaLines_.Add(line);
        }


        internal void computeAgentNeighbors(Agent other)
        {
            float invTimeHorizon = 1.0f / timeHorizon_;

            
            Vector2 relativePosition = other.position_ - position_;

            // mass
            float massRatio = (other.mass_ / (mass_ + other.mass_));
            float neighborMassRatio = (mass_ / (mass_ + other.mass_));
            //massRatio = 0.5f;
            //neighborMassRatio = 0.5f;
            Vector2 velocityOpt = (massRatio >= 0.5f ? (velocity_ - massRatio * velocity_) * 2 : prefVelocity_ + (velocity_ - prefVelocity_) * massRatio * 2);
            Vector2 neighborVelocityOpt = (neighborMassRatio >= 0.5f ? 2 * other.velocity_ * (1 - neighborMassRatio) : other.prefVelocity_ + (other.velocity_ - other.prefVelocity_) * neighborMassRatio * 2); ;

            //massRatio = 0.5f;
            //velocityOpt = velocity_;
            //neighborVelocityOpt = other.velocity_;

            Vector2 relativeVelocity = velocityOpt - neighborVelocityOpt;
            float distSq = RVOMath.absSq(relativePosition);
            float combinedRadius = radius_ + other.radius_;
            if (mass_ != other.mass_) {
                //combinedRadius = combinedRadius * 0.45f;
            }
            float combinedRadiusSq = RVOMath.sqr(combinedRadius);

            Line line;
            Vector2 u;

            if (distSq > combinedRadiusSq) {
                /* No collision. */
                Vector2 w = relativeVelocity - invTimeHorizon * relativePosition;

                /* Vector from cutoff center to relative velocity. */
                float wLengthSq = RVOMath.absSq(w);
                float dotProduct1 = w * relativePosition;

                if (dotProduct1 < 0.0f && RVOMath.sqr(dotProduct1) > combinedRadiusSq * wLengthSq) {
                    /* Project on cut-off circle. */
                    float wLength = RVOMath.sqrt(wLengthSq);
                    Vector2 unitW = w / wLength;

                    line.direction = new Vector2(unitW.y(), -unitW.x());
                    u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                } 
                else 
                {
                    /* Project on legs. */
                    float leg = RVOMath.sqrt(distSq - combinedRadiusSq);

                    if (RVOMath.det(relativePosition, w) > 0.0f) {
                        /* Project on left leg. */
                        line.direction = new Vector2(relativePosition.x() * leg - relativePosition.y() * combinedRadius, relativePosition.x() * combinedRadius + relativePosition.y() * leg) / distSq;
                    } else {
                        /* Project on right leg. */
                        line.direction = -new Vector2(relativePosition.x() * leg + relativePosition.y() * combinedRadius, -relativePosition.x() * combinedRadius + relativePosition.y() * leg) / distSq;
                    }

                    float dotProduct2 = relativeVelocity * line.direction;
                    u = dotProduct2 * line.direction - relativeVelocity;
                }
            } 
            else 
            {
                /* Collision. Project on cut-off circle of time timeStep. */
                float invTimeStep = 1.0f / Simulator.Instance.timeStep_;

                /* Vector from cutoff center to relative velocity. */
                Vector2 w = relativeVelocity - invTimeStep * relativePosition;

                float wLength = RVOMath.abs(w);
                Vector2 unitW = w / wLength;

                line.direction = new Vector2(unitW.y(), -unitW.x());
                u = (combinedRadius * invTimeStep - wLength) * unitW;
            }

            //line.point = velocityOpt + 0.5f * u;
            line.point = velocityOpt + massRatio * u;
            orcaLines_.Add(line);

            ////////////////////////////////////////////////////////////////////////////////////////////////
            Test01.mSphereScritps[id_].msGizmosLines.Add(line);
            ////////////////////////////////////////////////////////////////////////////////////////////////
        }
        
        /**
         * <summary>Computes the new velocity of this agent.</summary>
         */
        internal void computeNewVelocity() {
            orcaLines_.Clear();


            /* Create obstacle ORCA lines. */
            for (int i = 0; i < obstacleNeighbors_.Count; ++i) 
            {
                Obstacle obstacle1 = obstacleNeighbors_[i].Value;
                Obstacle obstacle2 = obstacle1.next_;
                ObstacleTemp temp1 = new ObstacleTemp()
                {
                    Point = obstacle1.point_,
                    Direction = obstacle1.direction_,
                    Convex = obstacle1.convex_,
                    PrevDirection = obstacle1.previous_.direction_
                };
                ObstacleTemp temp2 = new ObstacleTemp()
                {
                    Point = obstacle2.point_,
                    Direction = obstacle2.direction_,
                    Convex = obstacle2.convex_,
                    PrevDirection = obstacle2.previous_.direction_
                };
                computeObstacleNeighbors(temp1,temp2,obstacle1 == obstacle2);

            }

            int numObstLines = orcaLines_.Count;

            if (prefVelocity_.x() >= 0.01 || prefVelocity_.y() >= 0.01)
            {
                /* Create agent ORCA lines. */
                for (int i = 0; i < agentNeighbors_.Count; ++i) 
                {
                    Agent other = agentNeighbors_[i].Value;
              
                    computeAgentNeighbors(other);
                    continue;
                    if (other.prefVelocity_.x() < 0.01f && other.prefVelocity_.y() < 0.01f)
                    {
                        ObstacleTemp temp1 = new ObstacleTemp()
                        {
                            Point = position_,
                            Direction = prefVelocity_,
                            Convex = false,
                            PrevDirection = Vector2.zero
                        };
                        ObstacleTemp temp2 = new ObstacleTemp()
                        {
                            Point = position_,
                            Direction = prefVelocity_,
                            Convex = false,
                            PrevDirection = Vector2.zero
                        };
                        computeObstacleNeighbors(temp1, temp2, false);
                    }
                    else
                    {
                        computeAgentNeighbors(other);
                    }
                }
            }

            if (mass_ != 1) {
                int i = 1;
            }

            int lineFail = linearProgram2(orcaLines_, maxSpeed_, prefVelocity_, msDirectionOpt, ref newVelocity_);
            //Debug.LogError((lineFail < orcaLines_.Count) + "            "+   lineFail + "         "  + orcaLines_.Count);
            if (lineFail < orcaLines_.Count) {

                if (mass_ != 1) {
                    int i = 1;
                }

                linearProgram3(orcaLines_, numObstLines, lineFail, maxSpeed_, ref newVelocity_);
            }
        }

        /**
         * <summary>Inserts an agent neighbor into the set of neighbors of this
         * agent.</summary>
         *
         * <param name="agent">A pointer to the agent to be inserted.</param>
         * <param name="rangeSq">The squared range around this agent.</param>
         */
        internal void insertAgentNeighbor(Agent agent, ref float rangeSq) {
            if (this != agent) {
                float distSq = RVOMath.absSq(position_ - agent.position_);

                if (distSq < rangeSq) {
                    if (agentNeighbors_.Count < maxNeighbors_) {
                        agentNeighbors_.Add(new KeyValuePair<float, Agent>(distSq, agent));
                    }

                    int i = agentNeighbors_.Count - 1;

                    while (i != 0 && distSq < agentNeighbors_[i - 1].Key) {
                        agentNeighbors_[i] = agentNeighbors_[i - 1];
                        --i;
                    }

                    agentNeighbors_[i] = new KeyValuePair<float, Agent>(distSq, agent);

                    if (agentNeighbors_.Count == maxNeighbors_) {
                        rangeSq = agentNeighbors_[agentNeighbors_.Count - 1].Key;
                    }
                }
            }
        }

        /**
         * <summary>Inserts a static obstacle neighbor into the set of neighbors
         * of this agent.</summary>
         *
         * <param name="obstacle">The number of the static obstacle to be
         * inserted.</param>
         * <param name="rangeSq">The squared range around this agent.</param>
         */
        internal void insertObstacleNeighbor(Obstacle obstacle, float rangeSq) {
            Obstacle nextObstacle = obstacle.next_;

            float distSq = RVOMath.distSqPointLineSegment(obstacle.point_, nextObstacle.point_, position_);

            if (distSq < rangeSq) {
                obstacleNeighbors_.Add(new KeyValuePair<float, Obstacle>(distSq, obstacle));

                int i = obstacleNeighbors_.Count - 1;

                while (i != 0 && distSq < obstacleNeighbors_[i - 1].Key) {
                    obstacleNeighbors_[i] = obstacleNeighbors_[i - 1];
                    --i;
                }
                obstacleNeighbors_[i] = new KeyValuePair<float, Obstacle>(distSq, obstacle);
            }
        }

        /**
         * <summary>Updates the two-dimensional position and two-dimensional
         * velocity of this agent.</summary>
         */
        internal void update() {
            velocity_ = newVelocity_;
            position_ += velocity_ * Simulator.Instance.timeStep_;

            ////////////////////////////////////////////////////////////////////////////////////////////////
            Test01.mSphereScritps[id_].msVelocity = velocity_;
            ////////////////////////////////////////////////////////////////////////////////////////////////
        }

        /**
         * <summary>Solves a one-dimensional linear program on a specified line
         * subject to linear constraints defined by lines and a circular
         * constraint.</summary>
         *
         * <returns>True if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="lineNo">The specified line constraint.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private bool linearProgram1(IList<Line> lines, int lineNo, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result) {
            float dotProduct = lines[lineNo].point * lines[lineNo].direction;
            float discriminant = RVOMath.sqr(dotProduct) + RVOMath.sqr(radius) - RVOMath.absSq(lines[lineNo].point);

            if (discriminant < 0.0f) {
                /* Max speed circle fully invalidates line lineNo. */
                return false;
            }

            float sqrtDiscriminant = RVOMath.sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i) {
                float denominator = RVOMath.det(lines[lineNo].direction, lines[i].direction);
                float numerator = RVOMath.det(lines[i].direction, lines[lineNo].point - lines[i].point);

                if (RVOMath.fabs(denominator) <= RVOMath.RVO_EPSILON) {
                    /* Lines lineNo and i are (almost) parallel. */
                    if (numerator < 0.0f) {
                        return false;
                    }

                    continue;
                }

                float t = numerator / denominator;

                if (denominator >= 0.0f) {
                    /* Line i bounds line lineNo on the right. */
                    tRight = Math.Min(tRight, t);
                } else {
                    /* Line i bounds line lineNo on the left. */
                    tLeft = Math.Max(tLeft, t);
                }

                if (tLeft > tRight) {
                    return false;
                }
            }

            if (directionOpt) {
                /* Optimize direction. */
                if (optVelocity * lines[lineNo].direction > 0.0f) {
                    /* Take right extreme. */
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                } else {
                    /* Take left extreme. */
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
            } else {
                /* Optimize closest point. */
                float t = lines[lineNo].direction * (optVelocity - lines[lineNo].point);

                if (t < tLeft) {
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                } else if (t > tRight) {
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                } else {
                    result = lines[lineNo].point + t * lines[lineNo].direction;
                }
            }

            return true;
        }

        /**
         * <summary>Solves a two-dimensional linear program subject to linear
         * constraints defined by lines and a circular constraint.</summary>
         *
         * <returns>The number of the line it fails on, and the number of lines
         * if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private int linearProgram2(IList<Line> lines, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result) {
            // directionOpt 第一次为false，第二次为true，directionOpt主要用在 linearProgram1 里面
            if (directionOpt) {
                /*
                 * Optimize direction. Note that the optimization velocity is of
                 * unit length in this case.
                 */
                // 1.这个其实没什么用，只是因为velocity是归一化的所以直接乘 radius
                result = optVelocity * radius;
            } else if (RVOMath.absSq(optVelocity) > RVOMath.sqr(radius)) {
                /* Optimize closest point and outside circle. */
                // 2.当 optVelocity 太大时，先归一化optVelocity，再乘 radius
                result = RVOMath.normalize(optVelocity) * radius;
            } else {
                /* Optimize closest point and inside circle. */
                // 3.当 optVelocity 小于maxSpeed时
                result = optVelocity;
            }

            for (int i = 0; i < lines.Count; ++i) {
                if (RVOMath.det(lines[i].direction, lines[i].point - result) > 0.0f) {
                    /* Result does not satisfy constraint i. Compute new optimal result. */
                    Vector2 tempResult = result;
                    if (!linearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                    {
                        result = tempResult;
                        return i;
                    }
                }
            }

            return lines.Count;
        }

        /**
         * <summary>Solves a two-dimensional linear program subject to linear
         * constraints defined by lines and a circular constraint.</summary>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="numObstLines">Count of obstacle lines.</param>
         * <param name="beginLine">The line on which the 2-d linear program
         * failed.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private void linearProgram3(IList<Line> lines, int numObstLines, int beginLine, float radius, ref Vector2 result) {

            if (mass_ != 1) {
                Debug.Log("linearProgram3 beginLine:"+ beginLine);
            }

            float distance = 0.0f;
            // 遍历所有剩余ORCA线
            for (int i = beginLine; i < lines.Count; ++i) {
                // 每一条 ORCA 线都需要精确的做出处理，distance 为 最大违规的速度
                if (RVOMath.det(lines[i].direction, lines[i].point - result) > distance) {
                    /* Result does not satisfy constraint of line i. */
                    IList<Line> projLines = new List<Line>();
                    // 1.静态阻挡的orca线直接加到projLines中
                    for (int ii = 0; ii < numObstLines; ++ii) {
                        projLines.Add(lines[ii]);
                    }
                    // 2.动态阻挡的orca线需要重新计算line，从第一个非静态阻挡到当前的orca线
                    for (int j = numObstLines; j < i; ++j) {
                        Line line;

                        float determinant = RVOMath.det(lines[i].direction, lines[j].direction);

                        if (RVOMath.fabs(determinant) <= RVOMath.RVO_EPSILON) {
                            /* Line i and line j are parallel. */
                            if (lines[i].direction * lines[j].direction > 0.0f) {
                                /* Line i and line j point in the same direction. */
                                // 2-1 两条线平行且同向
                                continue;
                            } else {
                                /* Line i and line j point in opposite direction. */
                                // 2-2 两条线平行且反向
                                line.point = 0.5f * (lines[i].point + lines[j].point);
                            }
                        } else {
                            // 2-3 两条线不平行
                            line.point = lines[i].point + (RVOMath.det(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                        }
                        // 计算ORCA线的方向
                        line.direction = RVOMath.normalize(lines[j].direction - lines[i].direction);
                        projLines.Add(line);
                    }
                    // 3.再次计算最优速度
                    Vector2 tempResult = result;
                    // 注意这里的 new Vector2(-lines[i].direction.y(), lines[i].direction.x()) 是方向向量
                    if (linearProgram2(projLines, radius, new Vector2(-lines[i].direction.y(), lines[i].direction.x()), true, ref result) < projLines.Count) {
                        /*
                         * This should in principle not happen. The result is by
                         * definition already in the feasible region of this
                         * linear program. If it fails, it is due to small
                         * floating point error, and the current result is kept.
                         */
                        result = tempResult;
                    }

                    distance = RVOMath.det(lines[i].direction, lines[i].point - result);
                }
            }
        }
    }
}