/*
 * Vector2.cs
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
using System.Globalization;

namespace RVO
{
    /**
     * <summary>Defines a two-dimensional vector.</summary>
     */
    public struct Vector2
    {
        public static readonly Vector2 Zero = new Vector2(0.0f, 0.0f);
        public static readonly Vector2 Left = new Vector2(-1.0f, 0.0f);
        public static readonly Vector2 Right = new Vector2(1.0f, 0.0f);
        public static readonly Vector2 Up = new Vector2(0.0f, 1.0f);
        public static readonly Vector2 Down = new Vector2(0.0f, -1.0f);
        public static readonly Vector2 MAX = new Vector2(float.MaxValue, float.MaxValue);
        public static readonly Vector2 MIN = new Vector2(float.MinValue, float.MinValue);
        internal float m_X;
        internal float m_Y;

        /**
         * <summary>Constructs and initializes a two-dimensional vector from the
         * specified xy-coordinates.</summary>
         *
         * <param name="x">The x-coordinate of the two-dimensional vector.
         * </param>
         * <param name="y">The y-coordinate of the two-dimensional vector.
         * </param>
         */
        public Vector2(float x, float y)
        {
            m_X = x;
            m_Y = y;
        }

        /**
         * <summary>Returns the string representation of this vector.</summary>
         *
         * <returns>The string representation of this vector.</returns>
         */
        public override string ToString()
        {
            return "(" + m_X.ToString(new CultureInfo("").NumberFormat) + "," + m_Y.ToString(new CultureInfo("").NumberFormat) + ")";
        }

        /**
         * <summary>Returns the x-coordinate of this two-dimensional vector.
         * </summary>
         *
         * <returns>The x-coordinate of the two-dimensional vector.</returns>
         */
        public float X()
        {
            return m_X;
        }

        /**
         * <summary>Returns the y-coordinate of this two-dimensional vector.
         * </summary>
         *
         * <returns>The y-coordinate of the two-dimensional vector.</returns>
         */
        public float Y()
        {
            return m_Y;
        }

        /**
         * <summary>Computes the dot product of the two specified
         * two-dimensional vectors.</summary>
         *
         * <returns>The dot product of the two specified two-dimensional
         * vectors.</returns>
         *
         * <param name="vector1">The first two-dimensional vector.</param>
         * <param name="vector2">The second two-dimensional vector.</param>
         */
        public static float operator *(Vector2 vector1, Vector2 vector2)
        {
            return vector1.m_X * vector2.m_X + vector1.m_Y * vector2.m_Y;
        }

        /**
         * <summary>Computes the scalar multiplication of the specified
         * two-dimensional vector with the specified scalar value.</summary>
         *
         * <returns>The scalar multiplication of the specified two-dimensional
         * vector with the specified scalar value.</returns>
         *
         * <param name="scalar">The scalar value.</param>
         * <param name="vector">The two-dimensional vector.</param>
         */
        public static Vector2 operator *(float scalar, Vector2 vector)
        {
            return vector * scalar;
        }

        /**
         * <summary>Computes the scalar multiplication of the specified
         * two-dimensional vector with the specified scalar value.</summary>
         *
         * <returns>The scalar multiplication of the specified two-dimensional
         * vector with the specified scalar value.</returns>
         *
         * <param name="vector">The two-dimensional vector.</param>
         * <param name="scalar">The scalar value.</param>
         */
        public static Vector2 operator *(Vector2 vector, float scalar)
        {
            return new Vector2(vector.m_X * scalar, vector.m_Y * scalar);
        }

        /**
         * <summary>Computes the scalar division of the specified
         * two-dimensional vector with the specified scalar value.</summary>
         *
         * <returns>The scalar division of the specified two-dimensional vector
         * with the specified scalar value.</returns>
         *
         * <param name="vector">The two-dimensional vector.</param>
         * <param name="scalar">The scalar value.</param>
         */
        public static Vector2 operator /(Vector2 vector, float scalar)
        {
            return new Vector2(vector.m_X / scalar, vector.m_Y / scalar);
        }

        /**
         * <summary>Computes the vector sum of the two specified two-dimensional
         * vectors.</summary>
         *
         * <returns>The vector sum of the two specified two-dimensional vectors.
         * </returns>
         *
         * <param name="vector1">The first two-dimensional vector.</param>
         * <param name="vector2">The second two-dimensional vector.</param>
         */
        public static Vector2 operator +(Vector2 vector1, Vector2 vector2)
        {
            return new Vector2(vector1.m_X + vector2.m_X, vector1.m_Y + vector2.m_Y);
        }

        /**
         * <summary>Computes the vector difference of the two specified
         * two-dimensional vectors</summary>
         *
         * <returns>The vector difference of the two specified two-dimensional
         * vectors.</returns>
         *
         * <param name="vector1">The first two-dimensional vector.</param>
         * <param name="vector2">The second two-dimensional vector.</param>
         */
        public static Vector2 operator -(Vector2 vector1, Vector2 vector2)
        {
            return new Vector2(vector1.m_X - vector2.m_X, vector1.m_Y - vector2.m_Y);
        }

        /**
         * <summary>Computes the negation of the specified two-dimensional
         * vector.</summary>
         *
         * <returns>The negation of the specified two-dimensional vector.
         * </returns>
         *
         * <param name="vector">The two-dimensional vector.</param>
         */
        public static Vector2 operator -(Vector2 vector)
        {
            return new Vector2(-vector.m_X, -vector.m_Y);
        }
        
        public static bool operator ==(Vector2 vectorA,Vector2 vectorB)
        {
            return Math.Abs(vectorA.X() - vectorB.X()) < 0.01f || Math.Abs(vectorA.Y() - vectorB.Y()) < 0.01f;
        }
        
        public static bool operator !=(Vector2 vectorA,Vector2 vectorB)
        {
            return Math.Abs(vectorA.X() - vectorB.X()) > 0.01f || Math.Abs(vectorA.Y() - vectorB.Y()) > 0.01f;
        }
        
        public void Normalize()
        {
            float magnitude = (float) Math.Sqrt(this.m_X * this.m_X + this.m_Y * this.m_Y);
            if ((double) magnitude > 9.99999974737875E-06)
                this /= magnitude;
            else
                this = Vector2.Zero;
        }
    }
}
