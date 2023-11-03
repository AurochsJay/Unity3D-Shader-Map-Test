using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Graphs;


public static class Prim 
{
    public class Edge : Graphs.Edge
    {
        public float Distance { get; private set; }

        public Edge(Vertex u, Vertex v) : base(u, v)
        {
            Distance = Vector3.Distance(u.Position, v.Position);
        }

        public static bool operator == (Edge left, Edge right)
        {
            return (left.U == right.U && left.V == right.V)
                || (left.U == right.V && left.V == right.U);
        }

        public static bool operator != (Edge left, Edge right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if(obj is Edge e)
            {
                return this == e;
            }

            return false;
        }

        public bool Equals(Edge e)
        {
            return this == e;
        }

        public override int GetHashCode()
        {
            return U.GetHashCode() ^ V.GetHashCode();
        }
    }

    public static List<Edge> MinimumSpanningTree(List<Edge> edges, Vertex start)
    {
        /*
         HashSet이란?
        HashSet은 C#에서 제공하는 컬렉션 타입 중 하나로, 중복 요소를 허용하지 않고, 
        순서를 보장하지 않는 집합(Set)을 나타냅니다. 
        즉, HashSet은 고유한 요소들을 저장하며, 요소들은 순서 없이 저장됩니다.

        HashSet은 특정 요소의 존재 여부를 빠르게 확인할 수 있고, 중복된 요소를 자동으로 제거해줍니다. 
        이를 통해 데이터의 일관성을 유지하고 중복 요소로 인한 문제를 방지할 수 있습니다.

        ex)
        HashSet<int> numbers = new HashSet<int>();
        numbers.Add(1);
        numbers.Add(2);
        numbers.Add(3);

        // 중복된 요소는 자동으로 제거됨
        numbers.Add(1);

        // numbers에는 중복되지 않은 1, 2, 3이 포함됨

         */
        HashSet<Vertex> openSet = new HashSet<Vertex>();
        HashSet<Vertex> closedSet = new HashSet<Vertex>();

        foreach (var edge in edges)
        {
            openSet.Add(edge.U);
            openSet.Add(edge.V);
        }

        closedSet.Add(start);

        List<Edge> results = new List<Edge>();

        while (openSet.Count > 0)
        {
            bool chosen = false;
            Edge chosenEdge = null;
            float minWeight = float.PositiveInfinity;

            foreach (var edge in edges)
            {
                int closedVertices = 0;
                if (!closedSet.Contains(edge.U)) closedVertices++;
                if (!closedSet.Contains(edge.V)) closedVertices++;
                if (closedVertices != 1) continue;
                /*
                 위 코드에서 closedVertices 변수는 현재 간선 edge의 양 끝 정점 중 MST에 이미 추가된 정점의 개수를 나타냅니다. 
                closedSet에 포함되지 않은 정점이 두 개 이상이거나(둘 다 포함되지 않은 경우) 한 개도 포함되지 않은 경우(closedVertices가 0인 경우)에는 continue 문이 실행됩니다.

                즉, MST의 정점으로 선택될 수 있는 간선은 반드시 한 쪽 끝 정점만이 MST에 속해야 합니다. 
                만약 양 끝 정점이 모두 MST에 속하거나 아무 정점도 속하지 않으면 해당 간선은 continue 문으로 무시되고, 다음 간선을 검사하는 반복문으로 넘어가게 됩니다.
                */

                if(edge.Distance < minWeight)
                {
                    chosenEdge = edge;
                    chosen = true;
                    minWeight = edge.Distance;
                }
            }

            if (!chosen) break;
            results.Add(chosenEdge);
            openSet.Remove(chosenEdge.U);
            openSet.Remove(chosenEdge.V);
            closedSet.Add(chosenEdge.U);
            closedSet.Add(chosenEdge.V);

        }

        return results;
    }
}
