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
         �� ���� ��ġ, ���� ���, ��� ������ ���´�.
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

    //�ֺ� �̿� ����� ������� ��ġ�� ��Ÿ���� ��� �迭
    static readonly Vector2Int[] neighbors =
        {
        new Vector2Int(1,0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
    };

    Grid2D<Node> grid; // 2D������ ������ ��ġ
    SimplePriorityQueue<Node, float> queue; //�켱���� ť, A*�˰��򿡼� ����.
    HashSet<Node> closed; // �̹� Ž���� ������ ������ ��Ÿ���� HashSet<Node>
    Stack<Vector2Int> stack; // ��θ� �籸���� �� ����� ����

    public Pathfinder2D(Vector2Int size)
    {
        grid = new Grid2D<Node>(size, Vector2Int.zero);
        queue = new SimplePriorityQueue<Node, float>();
        closed = new HashSet<Node>();
        stack = new Stack<Vector2Int>();
        /*
        �ν��Ͻ� �׻� ��������
        �ΰ� ������ �߻��ϴ� ��쿡�� stack�� ���� �ǰ� ������ �ν��Ͻ�ȭ���� �ʾ��� ���Դϴ�.
        ���� ������ �ذ��Ϸ��� stack ������ �ùٸ��� �ʱ�ȭ�Ǿ����� Ȯ���ؾ� �մϴ�. 
        C#���� ������ ����� ���� ������ ���� ������ �����ϰ� �ʱ�ȭ�ؾ� �մϴ�:
         */

        for (int x = 0; x < size.x; x++)
        {
            for(int y = 0; y < size.y; y++)
            {
                grid[x, y] = new Node(new Vector2Int(x, y));
            }
        }
    }

    // �׸��� ���� ��� ����� ���� ���� ����� �ʱ�ȭ
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

    //���������� ��ǥ ���������� ���� ��θ� ã�� �޼���. 
    //A*�˰����� ����Ͽ� ��θ� Ž���ϰ�, ��θ� ã���� ��θ� ��ȯ
    public List<Vector2Int> FindPath(Vector2Int start, Vector2 end, Func<Node, Node, PathCost> costFunction)
    // costFunction�� ���� ���� �̿� ��� ���� ����� ����ϴ� ��������Ʈ
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
                Debug.Log("���� ����� ��ġ�� �������� �������� ���Դٴ°���");
                return ReconstructPath(node);
            }

            foreach (var offset in neighbors)
            {
                if (!grid.InBounds(node.Position + offset)) continue; //grid�ȿ� ������ �Ѿ��
                var neighbor = grid[node.Position + offset]; // grid�ȿ� ������ �ִ´�.
                if (closed.Contains(neighbor)) continue; // �̹� Ž���ߴٸ� �Ѿ��

                var pathCost = costFunction(node, neighbor);
                if (!pathCost.traversable) continue; // ����ġ�� ���������� �̵��� �� ������ �Ѿ��

                float newCost = node.Cost + pathCost.cost;

                if (newCost < neighbor.Cost) // ���θ��� �ڽ�Ʈ�� �̿��� �ڽ�Ʈ���� �۴ٸ�
                {
                    neighbor.Previous = node; // ���� ��忡 ���� ��带 �ְ�
                    neighbor.Cost = newCost; //�̿� �ڽ�Ʈ�� �� �ڽ�Ʈ�� �ִ´�.

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


    //���� ��θ� �籸���Ͽ� ��θ� ��ȯ
    //���� ��θ� �籸���Ͽ� stack�� �����ϰ�, ���Ŀ� �� ������ ����Ͽ� ���� ��θ� ��ȯ. 
    private List<Vector2Int> ReconstructPath(Node node)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        while (node != null)
        {
            //�̰� �� �ȴ������? ���� �ƴϴϱ� ����� ������ ������ �����ٵ� �� �ȴ����?
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
