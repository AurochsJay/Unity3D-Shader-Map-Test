/* Adapted from https://github.com/Bl4ckb0ne/delaunay-triangulation

Copyright (c) 2015-2019 Simon Zeni (simonzeni@gmail.com)


Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:


The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.


THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Graphs;

public class Delaunay2D
{
    public class Triangle : IEquatable<Triangle>
    {
        public Vertex A { get; set; }
        public Vertex B { get; set; }
        public Vertex C { get; set; }
        public bool IsBad { get; set; }

        public Triangle() { }

        public Triangle(Vertex a, Vertex b, Vertex c)
        {
            A = a;
            B = b;
            C = c;
        }

        public bool ContainsVertex(Vector3 v) // 정점을 가지고 있으면 true
        {
            return Vector3.Distance(v, A.Position) < 0.01f
                || Vector3.Distance(v, B.Position) < 0.01f
                || Vector3.Distance(v, C.Position) < 0.01f;
        }

        public bool CircumCircleContains(Vector3 v)
        {
            Vector3 a = A.Position;
            Vector3 b = B.Position;
            Vector3 c = C.Position;

            float ab = a.sqrMagnitude;
            float cd = b.sqrMagnitude;
            float ef = c.sqrMagnitude;

            /*
             a.sqrMagnitude는 벡터 a의 제곱 길이(squared magnitude)를 반환하는 메서드입니다. 
             제곱 길이는 원래 길이의 제곱값을 나타냅니다. 
             즉, a.sqrMagnitude는 다음과 같이 계산됩니다:

            이 값은 원래 벡터의 길이를 제곱한 것이므로 음수가 될 수 없습니다. 
            제곱 길이를 사용하면 벡터의 길이를 비교할 때 제곱근 연산을 수행하는 것보다 더 빠르게 비교할 수 있습니다. 
            때때로 벡터의 길이를 정확한 크기보다 비교할 때는 제곱 길이를 사용하는 것이 성능상의 이점이 있습니다.
             */

            float circumX = (ab * (c.y - b.y) + cd * (a.y - c.y) + ef * (b.y - a.y)) / (a.x * (c.y - b.y) + b.x * (a.y - c.y) + c.x * (b.y - a.y));
            float circumY = (ab * (c.x - b.x) + cd * (a.x - c.x) + ef * (b.x - a.x)) / (a.y * (c.x - b.x) + b.y * (a.x - c.x) + c.y * (b.x - a.x));

            /*
            계산의 결과로 나오는 circumX와 circumY는 세 점을 통과하는 원의 중심의 x와 y 좌표를 나타냅니다. 
            이렇게 구해진 중심 좌표를 사용하면 세 점을 통과하는 원을 정의할 수 있습니다.
            이걸로 세 점 = 삼각형의 중심을 구해서 Edge를 만드는 Node나 Vertex로 쓸 수 있겠구나
             */

            Vector3 circum = new Vector3(circumX / 2, circumY / 2); // z값 0
            float circumRadius = Vector3.SqrMagnitude(a - circum);
            float dist = Vector3.SqrMagnitude(v - circum);
            return dist <= circumRadius;

            /*
             dist가 circumRadius보다 작거나 같으면, 점 v는 원의 내부에 위치합니다. 
            이 조건은 원의 중심에서 점 v까지의 거리가 반지름보다 작거나 같은 경우를 검사합니다.

            결과적으로, 주어진 점 v가 세 점 a, b, c를 통과하는 원 내부에 있는지를 판단하여 true 또는 false 값을 반환합니다.
             
            변수 이름 circum은 'circumcenter'의 축약어로, 세 점을 통과하는 원의 중심을 가리키는 일반적인 용어입니다.
             */

        }

        public static bool operator == (Triangle left, Triangle right)
        {
            return (left.A == right.A || left.A == right.B || left.A == right.C)
                && (left.B == right.A || left.B == right.B || left.B == right.C)
                && (left.C == right.A || left.C == right.B || left.C == right.C);
        }

        public static bool operator != (Triangle left, Triangle right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if(obj is Triangle t)
            {
                return this == t;
            }

            return false;
        }

        public bool Equals(Triangle t)
        {
            return this == t;
        }

        public override int GetHashCode()
        {
            return A.GetHashCode() ^ B.GetHashCode() ^ C.GetHashCode();
        }
    }

    public class Edge
    {
        public Vertex U { get; set; }
        public Vertex V { get; set; }
        public bool IsBad { get; set; }

        public Edge() { }

        public Edge(Vertex u, Vertex v)
        {
            U = u;
            V = v;
        }

        public static bool operator ==(Edge left, Edge right)
        {
            return (left.U == right.U || left.U == right.V)
                && (left.V == right.U || left.V == right.V);
        }

        public static bool operator !=(Edge left, Edge right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge e)
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

        public static bool AlmostEqual(Edge left, Edge right)
        {
            return Delaunay2D.AlmostEqual(left.U, right.U) && Delaunay2D.AlmostEqual(left.V, right.V)
                || Delaunay2D.AlmostEqual(left.U, right.V) && Delaunay2D.AlmostEqual(left.V, right.U);
        }

    }

    static bool AlmostEqual(float x, float y)
    {
        return Mathf.Abs(x - y) <= float.Epsilon * Mathf.Abs(x + y) * 2
            || Mathf.Abs(x - y) < float.MinValue;
    }

    static bool AlmostEqual(Vertex left, Vertex right)
    {
        return AlmostEqual(left.Position.x, right.Position.x) && AlmostEqual(left.Position.y, right.Position.y);

    }

    public List<Vertex> Vertices { get; private set; }
    public List<Edge> Edges { get; private set; }
    public List<Triangle> Triangles { get; private set; }

    Delaunay2D()
    {
        Edges = new List<Edge>();
        Triangles = new List<Triangle>();
    }

    public static Delaunay2D Triangulate(List<Vertex> vertices)
    {
        Delaunay2D delaunay = new Delaunay2D();
        delaunay.Vertices = new List<Vertex>(vertices);
        delaunay.Triangulate();

        return delaunay;
    }

    private void Triangulate()
    {
        float minX = Vertices[0].Position.x;
        float minY = Vertices[0].Position.y;
        float maxX = minX;
        float maxY = minY;
        /*
         minX, minY, maxX, maxY 변수를 초기화하고, 
        이들을 첫 번째 Vertex 객체의 x, y 좌표 값으로 설정합니다.
         */

        foreach (var vertex in Vertices)
        {
            if (vertex.Position.x < minX) minX = vertex.Position.x;
            if (vertex.Position.x > maxX) maxX = vertex.Position.x;
            if (vertex.Position.y < minY) minY = vertex.Position.y;
            if (vertex.Position.y > maxY) maxY = vertex.Position.y;
            
        }
        /*
          minX, minY는 주어진 Vertices 리스트에 있는 모든 Vertex 객체의 x, y 좌표 중에서 가장 작은 값입니다
          maxX, maxY는 가장 큰 값이며, deltaMax는 최소 사각형을 감싸는 크기를 결정합니다.
         이러한 정보는 이후 단계에서 Delaunay 삼각화를 위해 사용됩니다.
         */

        float dx = maxX - minX;
        float dy = maxY - minY;
        float deltaMax = Mathf.Max(dx, dy) * 2;
        /*
        모든 Vertex 객체를 확인한 후, 최소 및 최대 x, y 좌표를 사용하여 dx와 dy를 계산합니다. 
        dx는 x 좌표의 범위(최대 x - 최소 x), dy는 y 좌표의 범위(최대 y - 최소 y)입니다.

        deltaMax 변수에는 dx와 dy 중에서 큰 값의 두 배를 저장합니다. 
        이 값은 최소 사각형을 감싸는 크기를 결정합니다.
        */

        Vertex p1 = new Vertex(new Vector2(minX - 1         , minY - 1          ));
        Vertex p2 = new Vertex(new Vector2(minX - 1         , maxY + deltaMax   ));
        Vertex p3 = new Vertex(new Vector2(maxX + deltaMax  , minY - 1          ));
        /*
          최소 사각형을 감싸는 세 개의 추가적인 Vertex 객체를 생성합니다. 
        이들 Vertex 객체는 minX, minY, maxX, maxY, deltaMax 값을 사용하여 생성됩니다. 
        각 Vertex 객체는 2D 공간에서의 좌표를 나타내며, 
        이러한 좌표는 최소 사각형을 완전히 포함하는 큰 삼각형의 꼭짓점으로 사용됩니다.

        p1: (minX - 1, minY - 1) 좌표를 갖는 Vertex 객체를 생성합니다. 
        이 좌표는 최소 사각형의 왼쪽 아래 꼭짓점의 왼쪽 아래에 위치한 지점입니다.

        p2: (minX - 1, maxY + deltaMax) 좌표를 갖는 Vertex 객체를 생성합니다. 
        이 좌표는 최소 사각형의 왼쪽 위 꼭짓점의 왼쪽 위에 위치한 지점입니다. 
        deltaMax를 더해줌으로써 사각형의 높이를 더 확장합니다.

        p3: (maxX + deltaMax, minY - 1) 좌표를 갖는 Vertex 객체를 생성합니다. 
        이 좌표는 최소 사각형의 오른쪽 아래 꼭짓점의 오른쪽 아래에 위치한 지점입니다. 
        deltaMax를 더해줌으로써 사각형의 너비를 더 확장합니다.

        이렇게 생성된 p1, p2, p3는 최소 사각형을 완전히 포함하는 큰 삼각형의 세 꼭짓점으로 사용됩니다. 
        Delaunay 삼각화 알고리즘에서는 이러한 큰 삼각형을 초기 삼각형으로 사용하여 시작합니다. 
        이 초기 삼각형을 기반으로 점들을 추가하면서 Delaunay 삼각화를 진행합니다.
         */

        Triangles.Add(new Triangle(p1, p2, p3));

        /*
        모든 Vertex들을 포함하는 커다란 삼각형을 만든건가?
        예, 정확히 맞습니다. p1, p2, p3로 생성된 큰 삼각형은 모든 Vertices 리스트에 있는 Vertex 객체들을 완전히 포함하는 최소 사각형을 감싸는 역할을 합니다. 이 삼각형은 주어진 점들을 통과하는 원의 외부에 위치한 삼각형입니다.

        Delaunay 삼각화 알고리즘에서는 보통 주어진 점들을 포함하는 큰 삼각형이나 사각형을 만들어서 시작합니다. 
        그런 다음, 주어진 점들을 이 큰 삼각형 안에 하나씩 추가하면서 Delaunay 삼각화를 수행합니다. 
        큰 삼각형이나 사각형을 만들어서 시작하는 이유는 모든 점들을 포함하는 초기 구조가 필요하기 때문입니다. 
        삼각화를 시작할 때 이런 초기 구조가 없으면 알고리즘이 제대로 동작하지 않을 수 있습니다.

        따라서 p1, p2, p3로 생성된 큰 삼각형은 주어진 Vertices 리스트에 있는 모든 Vertex 객체를 완전히 포함하면서 Delaunay 삼각화 알고리즘을 시작하는 데 사용됩니다.
         */

        foreach (var vertex in Vertices)
        {
            List<Edge> polygon = new List<Edge>(); // 이 리스트는 현재 vertex를 포함하는 Delaunay 삼각형의 에지들을 저장할 목적으로 사용됩니다.

            foreach (var t in Triangles)
            {
                if (t.CircumCircleContains(vertex.Position))
                /*
                 t.CircumCircleContains(vertex.Position)를 호출하여 
                 현재 t 삼각형의 외접원(circumcircle)이 vertex를 포함하는지 확인합니다.

                 만약 t.CircumCircleContains(vertex.Position)가 true를 반환하면, 
                t를 제거할 것임을 표시하기 위해 t.IsBad를 true로 설정합니다. 
                그리고 t의 세 꼭짓점을 사용하여 세 개의 에지를 생성하고, 이들 에지를 polygon 리스트에 추가합니다. 
                이 에지들은 나중에 삭제될 삼각형을 식별하는 데 사용됩니다.
                */
                {
                    t.IsBad = true; // isbad 가 true면 제거할 삼각형이나 간선
                    polygon.Add(new Edge(t.A, t.B));
                    polygon.Add(new Edge(t.B, t.C));
                    polygon.Add(new Edge(t.C, t.A));
                }
            }

            Triangles.RemoveAll((Triangle t) => t.IsBad);
            /*
             Triangles 리스트에서 IsBad가 true인 모든 Triangle 객체를 제거합니다. 
            이로써 vertex를 포함하는 Delaunay 삼각형을 제외한 모든 삼각형이 삭제됩니다.

            여기서 (Triangle t) => t.IsBad는 람다(lambda) 식입니다. 
            이 람다 식은 Triangle 객체를 입력으로 받아 해당 객체의 IsBad 속성을 확인하고, 
            IsBad가 true인 경우에 해당하는 삼각형을 제거하도록 동작합니다.

            Triangles 리스트에 있는 각 Triangle 객체 t에 대해 람다 식을 실행합니다.
람다        식에서는 t.IsBad를 확인하여 IsBad가 true이면 해당 Triangle 객체 t를 제거 대상으로 표시합니다.
모든        riangle 객체를 확인한 후에 IsBad가 true로 표시된 객체들을 모두 제거합니다.
             */

            for (int i = 0; i < polygon.Count; i++)
            {
                for (int j = i + 1; j < polygon.Count; j++) // 숙지필요. j = 0 이면 들로네에 edge가 안담김
                {
                    if(Edge.AlmostEqual(polygon[i], polygon[j]))
                    {
                        polygon[i].IsBad = true; 
                        polygon[j].IsBad = true; //제거해야할 폴리곤
                    }
                }
            }


            polygon.RemoveAll((Edge e) => e.IsBad);

            foreach (var edge in polygon)
            {
                Triangles.Add(new Triangle(edge.U, edge.V, vertex));
            }
        }

        Triangles.RemoveAll((Triangle t) => t.ContainsVertex(p1.Position) || t.ContainsVertex(p2.Position) || t.ContainsVertex(p3.Position));

        HashSet<Edge> edgeSet = new HashSet<Edge>();

        foreach (var t in Triangles)
        {
            var ab = new Edge(t.A, t.B);
            var bc = new Edge(t.B, t.C);
            var ca = new Edge(t.C, t.A);

            if (edgeSet.Add(ab))
            {
                Debug.Log("뭐든 들어왔었겠지");
                Edges.Add(ab);
            }

            if(edgeSet.Add(bc))
            {
                Edges.Add(bc);
            }

            if(edgeSet.Add(ca))
            {
                Edges.Add(ca);
            }
        }
    }
}
