using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Graphs
{
    public class Vertex : IEquatable<Vertex>
    {
        public Vector3 Position { get; private set; }

        public Vertex() { }

        public Vertex(Vector3 position)
        {
            Position = position;
        }

        public override bool Equals(object obj)
        {
            if(obj is Vertex v)
            {
                return Position == v.Position; // 같은 타입이고 위치가 같으면 참.
            }

            return false; // 다르면 거짓
        }

        public bool Equals(Vertex other)
        {
            return Position == other.Position;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

    }

    public class Vertex<T> : Vertex
    {
        public T Item { get; private set; }

        public Vertex(T item)
        {
            Item = item;
        }

        public Vertex(Vector3 position, T item) : base(position)
        {
            Item = item;
        }

    }

    public class Edge : IEquatable<Edge>
    {
        public Vertex U { get; set; }
        public Vertex V { get; set; }

        public Edge() { }

        public Edge(Vertex u, Vertex v)
        {
            U = u;
            V = v;
        }

        public static bool operator == (Edge left, Edge right)
        {
            return (left.U == right.U || left.U == right.V) && (left.V == right.U || left.V == right.V);
        }

        public static bool operator != (Edge left, Edge right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if(obj is Edge e)
            {
                return this == e; // 들어온 obj가 Edge와 같은 타입이고, 둘이 같다면 참
            }

            return false;
        }

        public bool Equals(Edge e)
        {
            return this == e;
        }

        public override int GetHashCode()
        {
            return U.GetHashCode() ^ V.GetHashCode(); // ^ : XOR연산자 , 입력값이 같으면 0, 입력값이 다르면 1 
                                                      // 0 ^ 0 = 0 / 0 ^ 1 = 1 / 1 ^ 0 = 1 / 1 ^ 1 = 0
        }

    }




}

