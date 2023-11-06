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
        new Vector2Int(1,0), // 오른쪽
        new Vector2Int(-1, 0), // 왼쪽
        new Vector2Int(0, 1), // 위
        new Vector2Int(0, -1), // 아래
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
                grid[x, y] = new Node(new Vector2Int(x, y)); // 그리드 위치에 노드 위치, 노드 생성자는 받은 매개변수의 값을 포지션 그대로 사용
            }
        }
    }

    // 그리드 내의 모든 노드의 이전 노드와 비용을 초기화
    /*
     ResetNodes 메서드 내에서 node 변수는 grid의 각 요소를 참조하고 있습니다. 
    grid는 2D 배열로서 Node 클래스의 인스턴스를 담고 있습니다. 
    따라서 node 변수는 grid[x, y]의 레퍼런스를 가지고 있습니다.

    node.Previous와 node.Cost를 초기화하고 있는 것은 이 레퍼런스를 통해 원래 grid 배열 안의 Node 객체에 접근하여 값을 변경하고 있습니다. 
    이러한 이유로 ResetNodes 메서드를 호출하면 grid 배열 내의 모든 노드 객체의 Previous와 Cost 속성이 초기화되게 됩니다.

    메서드 내에서 node 변수가 지역 변수로 선언되었지만, 이 변수는 grid의 요소에 대한 레퍼런스를 저장하고 있으므로 node를 통해 접근한 노드 객체의 속성을 변경하면 실제로 grid 배열 내의 노드 객체에 반영됩니다. 
    이렇게 함으로써 ResetNodes 메서드는 grid 배열 내의 모든 노드 객체를 초기화할 수 있습니다.
     */
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
    /*
     Func<Node, Node, PathCost>은 델리게이트 타입으로, 두 개의 Node 객체를 입력으로 받아 PathCost 객체를 반환하는 메서드나 람다식을 나타냅니다. 
    이 델리게이트는 FindPath 메서드에서 경로 탐색 중에 두 노드 간의 비용을 계산하는 역할을 합니다.

      delegate TResult System.Func<in T1, in T2, out TResult>(T1 args, T2 args)
     */
    {
        ResetNodes();
        queue.Clear();
        closed.Clear();

        queue = new SimplePriorityQueue<Node, float>(); // 새로운 queue객체가 생성되어 queue 완전 초기화해서 다시 선언함. 매개변수로, Node와 우선순위를 가짐
        closed = new HashSet<Node>();

        grid[start].Cost = 0;
        queue.Enqueue(grid[start], 0); // 우선순위 queue에 추가할 때 a가 결정된다. a:시작노드
                                       // 앞쪽으로 먼저 넣는다 우선순위 0번, 나중에 탐색할때 가장 먼저 탐색이 됨.

        //Debug.Log("큐는 시작할때 몇개지? : " + queue.Count);
        bool isqueue = true;

        while (queue.Count > 0)
        {
            Node node = queue.Dequeue();
            closed.Add(node);

            //Debug.Log("큐는 몇번이나 돌지?" + queue.Count);

            if (node.Position == end)
            {
                //노드가 끝에 다다르면 통로의 포지션을 반환한다. 이게 Generator2D에 있는 pathfinder메서드 안에 있는 var path임
                return ReconstructPath(node);
            }


            foreach (var offset in neighbors) // neighbors는 4개의 Vector2Int를 가진 배열이므로 foreach 사용 가능
            {
                if (!grid.InBounds(node.Position + offset)) continue; //grid안에 없으면 넘어가라, 현 노드의 위치와 이웃의 위치를 더했는데 그게 grid 범위를 넘어가면 넘긴다.
                                                                      //(고려 안한단 얘기) ex) 위에 탐색했는데 범위 밖이면 버리고 다음꺼 탐색
                var neighbor = grid[node.Position + offset]; // grid 범위 안에 있다면, 임시로 neighbor 변수를 만들어서 탐색한 위치의 값을 저장한다.
                if (closed.Contains(neighbor)) continue; // 그리고 탐색한 neighbor의 위치가 이미 closed에 있다면 다음 탐색으로 간다.

                var pathCost = costFunction(node, neighbor); // 새로 탐색한 위치이므로 pathCost를 계산해본다. 
                if (!pathCost.traversable) continue; // 가중치를 따져봤을때 이동할 수 없으면 넘어가라

                float newCost = node.Cost + pathCost.cost; // 시작한 노드(a)의 코스트와 계산한 pathCost를 더하면 갱신된 Cost가 나온다.
                                                           // 시작한 node의 cost는 0이다. grid[start].Cost = 0;

                    //Debug.Log("시작점의 코스트 : " + node.Cost);
                    //Debug.Log("이웃의 코스트 : " + neighbor.Cost);
                    //Debug.Log("pathCost의 코스트 : " + pathCost.cost);
                    //Debug.Log("newCost의 코스트 : " + newCost);



                if (newCost < neighbor.Cost) // 새로만든 코스트가 이웃의 코스트보다 작다면
                {
                    neighbor.Previous = node; // 전에 노드에 지금 노드를 넣고
                    neighbor.Cost = newCost; //이웃 코스트에 새 코스트를 넣는다.

                    if (queue.TryGetPriority(node, out float existingPriority)) // 우선순위 큐에서 현재 노드의 우선순위를 가져옵니다.
                                                                                // 만약 큐에 이미 현재 노드가 존재하고 그 우선순위가 existingPriority에 저장되었다면 다음을 수행합니다.
                    {
                        queue.UpdatePriority(node, newCost); //우선순위 큐에서 현재 노드의 우선순위를 새로 계산된 비용(newCost)으로 업데이트합니다.
                    }
                    else // 만약 현재 노드가 우선순위 큐에 존재하지 않는다면 다음을 수행합니다.
                    {
                        queue.Enqueue(neighbor, neighbor.Cost); //우선순위 큐에 이웃 노드(neighbor)를 추가하고, 그 노드의 우선순위를 현재까지의 최소 비용(neighbor.Cost)으로 설정합니다.
                                                                //이는 새로운 경로를 찾은 경우 해당 노드를 우선순위 큐에 추가함을 의미합니다.
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
