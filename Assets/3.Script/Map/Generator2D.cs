using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;

public class Generator2D : MonoBehaviour
{
    enum CellType
    {
        None,
        Room,
        Hallway
    }

    enum CellDirection
    {
        right,
        left,
        up,
        down,
        none
    }

    class Room
    {
        public RectInt bounds;

        public Room(Vector2Int location, Vector2Int size)
        {
            bounds = new RectInt(location, size);
        }

        public static bool Intersect(Room a, Room b)
        {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x)
                || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y));
            //a.bounds.position.x : a���� ���ʰ��, b.bounds.position.x + b.bounds.size.x : b ���� �����ʰ��.
            //a���� ���ʰ�谡 b���� ������ ��躸�� ũ�ų� ���ٸ� �� ���� ��ġ�� �ʴ´�.
        }
        /* RectInt�� position ���� -> minx�� miny�� ��´�.
          public Vector2Int position 
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2Int(m_XMin, m_YMin); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value.x; m_YMin = value.y; }
        }
         */
    }

    [SerializeField] Vector2Int size;
    [SerializeField] int roomCount;
    [SerializeField] Vector2Int roomMaxSize;
    [SerializeField] GameObject cubePrefab;
    [SerializeField] private GameObject cornerTopRightPrefab;
    [SerializeField] private GameObject cornerTopLeftPrefab;
    [SerializeField] private GameObject cornerBottomRightPrefab;
    [SerializeField] private GameObject cornerBottomLeftPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private GameObject wallTopPrefab;
    [SerializeField] private GameObject wallBottomPrefab;
    [SerializeField] private GameObject wallRightPrefab;
    [SerializeField] private GameObject wallLeftPrefab;
    [SerializeField] private GameObject hallwayXPrefab;
    [SerializeField] private GameObject hallwayYPrefab;
    [SerializeField] Material redMaterial;
    [SerializeField] Material blueMaterial;

    Random random;
    Grid2D<CellType> grid;
    List<Room> rooms;
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges;


    //�� ��ȣ
    private int count = 1;
    //��� ��ȣ
    private int countHall = 1;

    //���Ա� �� ���ſ� �浹ü
    //[SerializeField] private GameObject destroyObject;

    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        random = new Random(); // Random�� �õ尪�� �ִ� ������ ���� ��� �ٲ�� ���� ��������
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();

        //�����
        PlaceRooms();
        //��γ� �ﰢ����
        Triangulate();
        //��纹������
        CreateHallways();
        //����� ������ ����
        PathfindHallways();

    }

    private void PlaceRooms()
    {
        for(int i =0; i<roomCount;i++)
        {
            Vector2Int location = new Vector2Int(random.Next(0, size.x), random.Next(0, size.y));
            Vector2Int roomSize = new Vector2Int(random.Next(1, roomMaxSize.x + 1), random.Next(1, roomMaxSize.y + 1));

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector2Int(-1, -1), roomSize + new Vector2Int(2, 2));

            foreach(Room room in rooms)
            {
                if(Room.Intersect(room, buffer))
                {
                    Debug.Log("Intersect�� ���ͼ� add�� �ȵ�");
                    add = false;
                    break;
                }
            }

            if(newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y)
            {
                Debug.Log("�׸��� ���� ������ ���� ť�갡 �ִ���");
                add = false;
            }

            if(add)
            {
                if(newRoom.bounds.size.x > 2 && newRoom.bounds.size.y > 2)
                {
                    Debug.Log("���� �߰���");
                    rooms.Add(newRoom);
                    PlaceRoom(newRoom.bounds.position, newRoom.bounds.size);

                    foreach (var pos in newRoom.bounds.allPositionsWithin)
                    {
                        grid[pos] = CellType.Room;
                    }
                }
                
            }
            

        }
    }

    private void Triangulate()
    {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms)
        {
            vertices.Add(new Vertex<Room>((Vector2)room.bounds.position + ((Vector2)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);

        //Ȯ�ο뵵
        for (int i = 0; i < delaunay.Vertices.Count; i++)
        {
            Vector3 CheckPoint = new Vector3(delaunay.Vertices[i].Position.x, 0, delaunay.Vertices[i].Position.y);
            //Debug.DrawRay(CheckPoint, Vector3.up * 8f, Color.red, Mathf.Infinity);
            //Debug.Log(delaunay.Vertices[i].Position);
        }

        Debug.Log("������ ���°ǰ�?" + delaunay.Edges.Count);
        for (int i = 0; i < delaunay.Edges.Count; i++)
        {
            //Debug.Log("����� ����" + i);
            //Debug.Log("???? 11: " + delaunay.Edges[i].U.Position);
        }
    }

    private void CreateHallways()
    {
        List<Prim.Edge> edges = new List<Prim.Edge>();

        //Debug.Log("???? 11: " + delaunay.Edges[0].U.Position);
        foreach (var edge in delaunay.Edges)
        {

            edges.Add(new Prim.Edge(edge.U, edge.V));
            //Debug.Log("???? 11: " + edge.U.Position);
            //Debug.Log("???? 22: " + edge.V.Position);
        }

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U); //����Ʈ�� ������(0)

        selectedEdges = new HashSet<Prim.Edge>(mst);
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        //���鳢�� ����� ���� �����ֱ�
        //StartCoroutine(ShowLine(edges));

        //Debug.DrawRay(remainingEdges[0].edg, Vector3.up * 3f, Color.blue, Mathf.Infinity);

        foreach (var edge in remainingEdges)
        {
            Vector3 checkEdgePosition_U = new Vector3(edge.U.Position.x, 0, edge.U.Position.y);
            Vector3 checkEdgePosition_V = new Vector3(edge.V.Position.x, 0, edge.V.Position.y);
            
            //���� ���� ��� ����Ǿ����� Edge(U,V)
            //Debug.DrawLine(checkEdgePosition_U, checkEdgePosition_V, Color.blue, Mathf.Infinity);

            if (random.NextDouble() < 0.125) // �����ٴ� �ǰ�? Ư�� �������� ���� �� �˰ڴ�.
                                             // ������ �� ���°���? ��� ��θ� ����°� ��ȿ�����̾�
                                             // �ٵ� ������ ������ ��θ� ���ϸ� � ���� ������ �ʳ�?
            {
                selectedEdges.Add(edge);

                //if���ȿ��� ����Ǵ°Ŵϱ� �� �����ȿ� �ִ� �༮�鸸 ���ͼ� ����ǰ���.
                //Debug.DrawLine(checkEdgePosition_U, checkEdgePosition_V, Color.green, Mathf.Infinity);
                //Debug.Log("SelectedEdges : " + edge.ToString());
                //Debug.DrawRay(checkEdgePosition_U, Vector3.up * 13f, Color.black, Mathf.Infinity);
                //Debug.DrawRay(checkEdgePosition_V, Vector3.up * 15f, Color.white, Mathf.Infinity);
                
            }
        }
    }

    private void PathfindHallways()
    {
        Pathfinder2D aStar = new Pathfinder2D(size);

        foreach (var edge in selectedEdges)
        {
            /*
             C#���� as Ű����� ���� ������ �ٸ� ���� �������� ��ȯ�� �� ���˴ϴ�. 
             as Ű����� ��������� ����ȯ�� �õ��ϰ�, �����ϸ� null�� ��ȯ�մϴ�.

             edge.U�� Vertex<Room> �������� ����ȯ�Ϸ� �õ��ϰ�, 
            �����ϸ� �ش� Vertex<Room> ��ü�� Item �Ӽ��� �����Ͽ� startRoom ������ �Ҵ��մϴ�. 
            ���� edge.U�� Vertex<Room> ������ �ƴ϶�� null�� startRoom�� �Ҵ�˴ϴ�.
             */
            var startRoom = (edge.U as Vertex<Room>).Item; // Vertex U�� edge�� ������
            var endRoom = (edge.V as Vertex<Room>).Item; // Vertex V�� edge�� ������

            var startPosf = startRoom.bounds.center; //�ӽ÷� �ε��Ҽ���(float)���� �߾Ӱ� �޾ƿ�
            var endPosf = endRoom.bounds.center;
            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

            Vector3 startpositon = new Vector3(startPos.x, 1.6f, startPos.y);
            Vector3 endpositon = new Vector3(endPos.x, 1.6f, endPos.y);
            Debug.DrawLine(startpositon, endpositon, Color.red, Mathf.Infinity);
            Debug.DrawRay(startpositon, Vector3.up*3f, Color.green, Mathf.Infinity);
            Debug.DrawRay(endpositon, Vector3.up*2f, Color.blue, Mathf.Infinity);

            bool isfirst = true;
            bool issecond = true;
            bool isthird = true;

            Debug.Log("selectedEdges�� ���� : " + selectedEdges.Count);

            //b�� ���� Ž������ �̿� ��带 ��Ÿ����. a�� ���س�����
            /*
             costFunction�� FindPath �Լ����� ȣ��� ��, ���� ����� a�� �̿� ����� b�� ������ �ڵ����� ���޵˴ϴ�. 
            �̶� a�� ���� Ž�� ���� ����� ��ġ�� ��Ÿ���ϴ�. 
            ���� a.Position�� ���� Ž�� ���� ����� ��ġ�� �˴ϴ�.

            �̷��� a�� ���� ����� ��ġ�� ��Ÿ���� ���� FindPath �Լ� ���ο��� ���� ����� ������ �ʱ�ȭ�ϰ�, �켱���� ť�� �߰��� �� �����˴ϴ�. 
            ���� ����� ������ �ʱ�ȭ�� �Ŀ� �켱���� ť�� ���� ��, a.Position�� �ش��ϴ� ��ġ ������ ����Ͽ� ���� ����� ��ġ�� �����մϴ�. 
            �׷��� ���� costFunction���� a�� ���� ��带 ��Ÿ���� �˴ϴ�.
             */
            var path = aStar.FindPath(startPos, endPos, (Pathfinder2D.Node a, Pathfinder2D.Node b) =>
            {
                var pathCost = new Pathfinder2D.PathCost();

                pathCost.cost = Vector2Int.Distance(b.Position, endPos); // heuristic // b�� ��ġ�� �������?

                //if(isfirst)
                //{
                //    Debug.DrawRay(new Vector3(b.Position.x, 0, b.Position.y), Vector3.up * 10f, Color.white, Mathf.Infinity);
                //    isfirst = false;
                //}
                //else if(issecond)
                //{
                //    Debug.DrawRay(new Vector3(b.Position.x, 0, b.Position.y), Vector3.up * 10f, Color.black, Mathf.Infinity);
                //    issecond = false;
                //}
                //else if (isthird)
                //{
                //    Debug.DrawRay(new Vector3(b.Position.x, 0, b.Position.y), Vector3.up * 10f, Color.magenta, Mathf.Infinity);
                //    isthird = false;
                //}

                if (grid[b.Position] == CellType.Room)
                {
                    pathCost.cost += 10;
                }
                else if (grid[b.Position] == CellType.None)
                {
                    pathCost.cost += 5;
                }
                else if (grid[b.Position] == CellType.Hallway)
                {
                    pathCost.cost += 1;
                }

                pathCost.traversable = true;

                return pathCost;
            }
            );


            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    var current = path[i];
                    //Debug.DrawRay(new Vector3(current.x, 0, current.y), Vector3.up * 10f, Color.cyan, Mathf.Infinity);

                    if (grid[current] == CellType.None)
                    {
                        grid[current] = CellType.Hallway; // ����̸� ������ �ٲ۴�.

                        
                        if(grid[path[i-1]] == CellType.Room)
                        {
                            Debug.DrawRay(new Vector3(path[i-1].x, 0, path[i-1].y), Vector3.up * 4f, Color.black, Mathf.Infinity);
                            Debug.Log("i-1��°(ù ��ι��� ����)�� ���̴�");

                            Vector3 prevPos = new Vector3(path[i - 1].x, 0.5f, path[i - 1].y);

                            //��(Room)�� ���Ա� ����� �ٽ� �����.
                            RegenerateEntrance(prevPos);
                        }

                        if(grid[path[i+1]] == CellType.Room)
                        {
                            Debug.DrawRay(new Vector3(path[i + 1].x, 0, path[i + 1].y), Vector3.up * 6f, Color.white, Mathf.Infinity);
                            Debug.Log("i+1��°(������ ��ι��� ����)�� ���̴�");

                            Vector3 nextPos = new Vector3(path[i + 1].x, 0.5f, path[i + 1].y);

                            RegenerateEntrance(nextPos);
                        }

                    }

                    if (i > 0 && i < path.Count-1)
                    {
                        // ������ -> ����� -> ������, delta�� (1,0)�̸� ������, (-1,0)�̸� ����, (0,1)�̸� ��, (0,-1)�̸� �Ʒ�
                        var prev = path[i - 1];

                        var next = path[i + 1];

                        var delta1 = current - prev;

                        var delta2 = next - current;

                        CellDirection delta1Dir;
                        CellDirection delta2Dir;

                        delta1Dir = CheckCellDirection(delta1);
                        delta2Dir = CheckCellDirection(delta2);

                        if(grid[current] == CellType.Hallway)
                        {
                            PlaceHallway(delta1Dir, delta2Dir, current);
                        }


                    }

                }

                foreach (var pos in path)
                {
                    if (grid[pos] == CellType.Hallway)
                    {
                        //PlaceHallway(pos);
                    }
                }


            }
        }
    }


    private void PlaceCube(Vector2Int location, Vector2Int size, Material material)
    {
        //if(size.x <= 2 || size.y <=2) �̷������� ����� �� ���ϸ� �������� �ʰ� �� �� �ִ�.
        //������ ���� �ĳ���.

        //���ǿ� ���� ���� ���� ���Ѵ�.
        //�ڳ��̸�, Corner -> Rotate y�� �ְ�, BL(BottomLeft), BR(BottomRight), UL(UpLeft), UR(UpRight)
        //���Ա���, Door
        //���� ��躮�̸�, Wall

        //�ڳ��� ���� -> BL : location.x, BR : location.x + size.x, UL : location.y, UR : location.y + size.y
        //���Ա��� ���� -> ��ΰ� Ȯ���Ǿ����
        //���� �� -> �ڳʿ� ���Ա�, ���θ� ������ ��� ��

        if(size.x > 2 && size.y > 2)
        {
            //GameObject go = Instantiate(cubePrefab, new Vector3(location.x, 0, location.y), Quaternion.identity);
            //go.GetComponent<Transform>().localScale = new Vector3(size.x, 3, size.y); // ���� local scale �� ũ�⸦ ���� �� �ִ�.
            //go.GetComponent<MeshRenderer>().material = material;

            
            //��
            for(int i = 0; i < size.y; i++)
            {
                //��
                for (int j = 0; j < size.x; j++)
                {
                    Vector3 createPos = new Vector3(location.x, 0.5f, location.y) + new Vector3(j, 0, i);

                    //�ڳ� i=0,j=0 , i=0,j=size.x-1 , i=size.y-1,j=0 , i=size.y-1 j=size.x
                    if (i == 0 && j == 0)
                    {

                        GameObject cornerBL = Instantiate(cornerBottomLeftPrefab, createPos, Quaternion.identity);
                        cornerBL.gameObject.name = "Prefab_" + count + "cornerBL";
                        continue;
                    }
                    else if(i==0 && j == size.x-1)
                    {
                        GameObject cornerBR = Instantiate(cornerBottomRightPrefab, createPos, Quaternion.identity);
                        cornerBR.gameObject.name = "Prefab_" + count + "cornerBR";
                        continue;
                    }
                    else if (i == size.y -1 && j == 0)
                    {
                        GameObject cornerTL = Instantiate(cornerTopLeftPrefab, createPos, Quaternion.identity);
                        cornerTL.gameObject.name = "Prefab_" + count + "cornerUL";
                        continue;
                    }
                    else if (i == size.y-1 && j == size.x - 1)
                    {
                        GameObject cornerTR = Instantiate(cornerTopRightPrefab, createPos, Quaternion.identity);
                        cornerTR.gameObject.name = "Prefab_" + count + "cornerUR";
                        continue;
                    }
                    //��
                    else if(i == 0 && (j != 0 || j != size.x-1))
                    {
                        GameObject wallBottom = Instantiate(wallBottomPrefab, createPos, Quaternion.identity);
                        wallBottom.gameObject.name = "Prefab_" + count + "wallBottom";
                        continue;
                    }
                    else if(i == size.y -1 && (j != 0 || j != size.x - 1))
                    {
                        GameObject wallTop = Instantiate(wallTopPrefab, createPos, Quaternion.identity);
                        wallTop.gameObject.name = "Prefab_" + count + "wallUp";
                        continue;
                    }
                    else if ((i != 0 || i != size.y - 1) && j == 0)
                    {
                        GameObject wallLeft = Instantiate(wallLeftPrefab, createPos, Quaternion.identity);
                        wallLeft.gameObject.name = "Prefab_" + count + "wallLeft";
                        continue;
                    }
                    else if ((i != 0 || i != size.y - 1) && j == size.x - 1)
                    {
                        GameObject wallRight = Instantiate(wallRightPrefab, createPos, Quaternion.identity);
                        wallRight.gameObject.name = "Prefab_" + count + "wallRight";
                        continue;
                    }


                    GameObject go = Instantiate(cubePrefab, createPos, Quaternion.identity);
                    //go.gameObject.transform.localScale = new Vector3(1, 3, 1);
                    int index = size.x * i + j;
                    go.gameObject.name = "Prefab_" +count+" (" + index +")";
                }
            }

            count++;
        }
        
        
        //���� ������ size.x�� ���϶� size.y�� ���� ��
        //�׿� �ش��ϴ� prefab�� �����
        //���� ����ϰ� �ִ� �޼��带 PlaceRoom���� �ΰ� ��δ� PlaceHallway�� �Ἥ �ٸ��� ������ �ȴ�.
        //���� �������� �ǵ��� ���� ���� ������ ��ġ location���ٰ� �ʺ�� ���̸� ���ؼ� 
        //Ÿ�� �ϳ��ϳ� �����ϰ� �ٿ������ ���� ������?
        //for���� �Ἥ ù��° index�� ��� ���� �����(���� ���)
        //�ϳ��� ť�꿡 

    }

    private void PlacePassageCube(Vector2Int location, Vector2Int size, Material material)
    {
        GameObject go = Instantiate(cubePrefab, new Vector3(location.x, 0.5f, location.y), Quaternion.identity);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y); // ���� local scale �� ũ�⸦ ���� �� �ִ�.
        //go.GetComponent<MeshRenderer>().material = material;
        go.gameObject.name = "Hall : " + countHall + "��° ����";
        countHall++;
    }

    private void PlaceRoom(Vector2Int location, Vector2Int size)
    {
        PlaceCube(location, size, redMaterial);
    }

    //��� ����
    private void PlaceHallway(CellDirection delta1Dir, CellDirection delta2Dir, Vector2Int current)
    {

        if (delta1Dir == CellDirection.right)
        {
            if (delta2Dir == CellDirection.right) //������-������ : �Ϲ������
            {
                //�����Ѵ�.
                GameObject Hallway_Straight = Instantiate(hallwayXPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
            else if (delta2Dir == CellDirection.up) // ������-�� : ���� ���
            {
                GameObject Hallway_Corner = Instantiate(cornerBottomRightPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
            else if (delta2Dir == CellDirection.down) // ������-�Ʒ� : ���� ���
            {
                GameObject Hallway_Corner = Instantiate(cornerTopRightPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
        }
        else if (delta1Dir == CellDirection.left)
        {
            if (delta2Dir == CellDirection.left) // ����-����
            {
                GameObject Hallway_Straight = Instantiate(hallwayXPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
            else if (delta2Dir == CellDirection.up)
            {
                GameObject Hallway_Corner = Instantiate(cornerBottomLeftPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
            else if (delta2Dir == CellDirection.down) 
            {
                GameObject Hallway_Corner = Instantiate(cornerTopLeftPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
        }
        else if (delta1Dir == CellDirection.up)
        {
            if (delta2Dir == CellDirection.right)
            {
                GameObject Hallway_Corner = Instantiate(cornerTopLeftPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
            else if (delta2Dir == CellDirection.left)
            {
                GameObject Hallway_Corner = Instantiate(cornerTopRightPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
            else if (delta2Dir == CellDirection.up)
            {
                GameObject Hallway_Straight = Instantiate(hallwayYPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
        }
        else if (delta1Dir == CellDirection.down)
        {
            if (delta2Dir == CellDirection.right)
            {
                GameObject Hallway_Corner = Instantiate(cornerBottomLeftPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
            else if (delta2Dir == CellDirection.left)
            {
                GameObject Hallway_Corner = Instantiate(cornerBottomRightPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
            else if (delta2Dir == CellDirection.down)
            {
                GameObject Hallway_Straight = Instantiate(hallwayYPrefab, new Vector3(current.x, 0.5f, current.y), Quaternion.identity);
            }
        }

        //�ΰ��� ���� �������� �ִ��� Ȯ���ϰ� �ΰ���� ��� �������� �ϳ��� ����. �ΰ��ΰ��� ���ϰ� �Ʒ�, �Ǵ� �ϳ��� ���̴�.
        CheckOverlapHallway(current);

    }

    private void PlaceHallway(Vector2Int location)
    {
        PlacePassageCube(location, new Vector2Int(1, 1), blueMaterial);
    }

    private CellDirection CheckCellDirection(Vector2Int delta)
    {
        CellDirection result;

        if(delta == Vector2Int.right)
        {
            result = CellDirection.right;
        }
        else if(delta == Vector2Int.left)
        {
            result = CellDirection.left;
        }
        else if(delta == Vector2Int.up)
        {
            result = CellDirection.up;
        }
        else if(delta == Vector2Int.down)
        {
            result = CellDirection.down;
        }
        else
        {
            result = CellDirection.none;
        }

        return result;
    }

    //��(Room)�� ���Ա� ����� �ٽ� �����.
    private void RegenerateEntrance(Vector3 Pos)
    {
        DeleteCube(Pos);

        GameObject RoomEntrance = Instantiate(cubePrefab, Pos, Quaternion.identity);
    }

    //���� ��ġ�°� Ȯ���ϰ� ����
    private void CheckOverlapHallway(Vector2Int current)
    {
        Vector3 checkPos = new Vector3(current.x, 0.5f, current.y);

        int checkCount = 0;

        //������
        Collider[] colliders = Physics.OverlapCapsule(checkPos, checkPos + Vector3.up * 0.5f, 0.1f);
        foreach(Collider collider in colliders)
        {
            checkCount++;
            if(checkCount >= 2)
            {
                Debug.Log("2�� �̻� ��ü�� ��ģ���� �ִ�.");
                DeleteCube(checkPos);
            }
        }

        //checkCount = 0;
        //colliders = Physics.OverlapCapsule(checkPos, checkPos + Vector3.down * 0.5f, 0.1f);
        //foreach (Collider collider in colliders)
        //{
        //    checkCount++;
        //    if (checkCount >= 2)
        //    {
        //        Destroy(collider.gameObject);
        //    }
        //}
    }

    //6���� ����, up,down,right,left,forward,back
    private void DeleteCube(Vector3 Pos)
    {
        RaycastHit hit;

        if (Physics.Raycast(Pos, Vector3.up, out hit, 0.5f))
        {
            Destroy(hit.transform.gameObject);
        }

        if (Physics.Raycast(Pos, Vector3.down, out hit, 0.5f))
        {
            Destroy(hit.transform.gameObject);
        }

        if (Physics.Raycast(Pos, Vector3.forward, out hit, 0.5f))
        {
            Destroy(hit.transform.gameObject);
        }

        if (Physics.Raycast(Pos, Vector3.back, out hit, 0.5f))
        {
            Destroy(hit.transform.gameObject);
        }

        if (Physics.Raycast(Pos, Vector3.right, out hit, 0.5f))
        {
            Destroy(hit.transform.gameObject);
        }

        if (Physics.Raycast(Pos, Vector3.left, out hit, 0.5f))
        {
            Destroy(hit.transform.gameObject);
        }
    }

    //��� DrawLine �����ְ� �ڷ�ƾ
    private IEnumerator ShowLine(List<Prim.Edge> edges)
    {
        for (int i = 1; i < edges.Count; i++)
        {

            Vector3 checkEdgePosition_U = new Vector3(edges[i - 1].U.Position.x, 0, edges[i - 1].U.Position.y);
            Vector3 checkEdgePosition_U2 = new Vector3(edges[i].U.Position.x, 0, edges[i].U.Position.y);
            Vector3 checkEdgePosition_V = new Vector3(edges[i - 1].V.Position.x, 0, edges[i - 1].V.Position.y);
            Vector3 checkEdgePosition_V2 = new Vector3(edges[i].V.Position.x, 0, edges[i].V.Position.y);

            Debug.Log("ù��° U�� ������ : " + checkEdgePosition_U);
            Debug.Log("�ι�° U�� ������ : " + checkEdgePosition_U2);

            Debug.DrawLine(checkEdgePosition_U, checkEdgePosition_U2, Color.green, Mathf.Infinity);
            Debug.DrawLine(checkEdgePosition_V, checkEdgePosition_V2, Color.blue, Mathf.Infinity);

            yield return new WaitForSeconds(0.3f);
        }


    }



}
