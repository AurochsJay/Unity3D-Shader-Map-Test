using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlueRaja;

public class Pathfinder2D
{
    public class Node
    {
        /*
         각 노드는 위치, 이전 노드, 비용 정보를 갖는다.
         */
        public Vector2Int Position { get; private set; } 
        public Node Previous { get; set; }
        public float Cost { get; set; }

        public Node(Vector2Int position)
        {
            Position = position;
        }
    }

    public struct PathCost
    {
        public bool traversable;
        public float cost;
    }

    //주변 이웃 노드의 상대적인 위치를 나타내는 상수 배열
    static readonly Vector2Int[] neighbors =
        {
        new Vector2Int(1,0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
    };

    Grid2D<Node> grid; // 2D공간에 노드들을 배치
    SimplePriorityQueue<Node, float> queue; //우선순위 큐, A*알고리즘에서 사용됨.
    HashSet<Node> closed; // 이미 탐색한 노드들의 집합을 나타내는 HashSet<Node>
    Stack<Vector2Int> stack; // 경로를 재구성할 때 사용할 스택

    public Pathfinder2D(Vector2Int size)
    {
        grid = new Grid2D<Node>(size, Vector2Int.zero);
        queue = new SimplePriorityQueue<Node, float>();
        closed = new HashSet<Node>();
        stack = new Stack<Vector2Int>();
        /*
        인스턴스 항상 주의하자
        널값 오류가 발생하는 경우에는 stack이 선언만 되고 실제로 인스턴스화되지 않았을 때입니다.
        따라서 문제를 해결하려면 stack 변수가 올바르게 초기화되었는지 확인해야 합니다. 
        C#에서 스택을 사용할 때는 다음과 같이 스택을 선언하고 초기화해야 합니다:
         */

        for (int x = 0; x < size.x; x++)
        {
            for(int y = 0; y < size.y; y++)
            {
                grid[x, y] = new Node(new Vector2Int(x, y));
            }
        }
    }

    // 그리드 내의 모든 노드의 이전 노드와 비용을 초기화
    private void ResetNodes()
    {
        var size = grid.Size;

        for( int x= 0; x < size.x; x++)
        {
            for(int y = 0; y < size.y; y++)
            {
                var node = grid[x, y];
                node.Previous = null;
                node.Cost = float.PositiveInfinity;
            }
        }
    }

    //시작점부터 목표 지점까지의 최적 경로를 찾는 메서드. 
    //A*알고리즘을 사용하여 경로를 탐색하고, 경로를 찾으면 경로를 반환
    public List<Vector2Int> FindPath(Vector2Int start, Vector2 end, Func<Node, Node, PathCost> costFunction)
    // costFunction은 현재 노드와 이웃 노드 간의 비용을 계산하는 델리게이트
    {
        ResetNodes();
        queue.Clear();
        closed.Clear();

        queue = new SimplePriorityQueue<Node, float>();
        closed = new HashSet<Node>();

        grid[start].Cost = 0;
        queue.Enqueue(grid[start], 0);

        while (queue.Count > 0)
        {
            Node node = queue.Dequeue();
            closed.Add(node);

            if (node.Position == end)
            {
                Debug.Log("여기 노드의 위치가 마지막과 같은데가 들어왔다는거지");
                return ReconstructPath(node);
            }

            foreach (var offset in neighbors)
            {
                if (!grid.InBounds(node.Position + offset)) continue; //grid안에 없으면 넘어가라
                var neighbor = grid[node.Position + offset]; // grid안에 있으면 넣는다.
                if (closed.Contains(neighbor)) continue; // 이미 탐색했다면 넘어가라

                var pathCost = costFunction(node, neighbor);
                if (!pathCost.traversable) continue; // 가중치를 따져봤을때 이동할 수 없으면 넘어가라

                float newCost = node.Cost + pathCost.cost;

                if (newCost < neighbor.Cost) // 새로만든 코스트가 이웃의 코스트보다 작다면
                {
                    neighbor.Previous = node; // 전에 노드에 지금 노드를 넣고
                    neighbor.Cost = newCost; //이웃 코스트에 새 코스트를 넣는다.

                    if (queue.TryGetPriority(node, out float existingPriority))
                    {
                        queue.UpdatePriority(node, newCost); //BlueRaja
                    }
                    else
                    {
                        queue.Enqueue(neighbor, neighbor.Cost);
                    }
                }
            }
        }

        return null;
    }


    //최적 경로를 재구성하여 경로를 반환
    //최적 경로를 재구성하여 stack에 저장하고, 이후에 이 스택을 사용하여 최종 경로를 반환. 
    private List<Vector2Int> ReconstructPath(Node node)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        while (node != null)
        {
            //이게 왜 안담아지지? 널이 아니니까 노드의 정보를 담을수 있을텐데 왜 안담기지?
            stack.Push(node.Position);
            node = node.Previous;
        }

        while (stack.Count > 0)
        {
            result.Add(stack.Pop());
        }

        return result;
    }

}
